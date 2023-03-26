
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Modine.Common
{
    public class Postprocessing
    {
        private static int VAO;
        private static int VBO;

        static float[] PPvertices =
        {
            -1f,  1f,
            -1f, -1f,
             1f,  1f,
             1f,  1f,
            -1f, -1f,
             1f, -1f,
        };

        public static void SetupPPRect(ref Shader postprocessShader)
        {
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, PPvertices.Length * sizeof(float), PPvertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        }
 
        static Vector3[] ssaoNoise = new Vector3[16];
        static Vector3[] sample = new Vector3[128];
        static int noiseTexture;

        public static void GenNoise(int numSamples)
        {
            for (int i = 0; i < numSamples; i++)
            {
                Random random = new Random();
                sample[i] = new Vector3(
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble());
                sample[i] = Vector3.Normalize(sample[i]);
                sample[i] *= (float)random.NextDouble();
                float scale = i / numSamples;

                scale = MathHelper.Lerp(0.1f, 1.0f, scale * scale);
                sample[i] *= scale;
            }

            for (int i = 0; i < 16; i++)
            {
                Random random = new Random();
                Vector3 noise = new Vector3(
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble());
                ssaoNoise[i] = noise;
            }

            // Generate noise texture
            noiseTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, 4, 4, 0, PixelFormat.Rgb, PixelType.Float, Marshal.UnsafeAddrOfPinnedArrayElement(ssaoNoise, 0));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        public static void RenderDefferedRect(ref Shader defferedShader, int depthStencilTexture, int gAlbedo, int gNormal, int gMetallicRough)
        {
            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gAlbedo);
            defferedShader.SetInt("gAlbedo", 0);
            
            // Bind depth texture
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.DepthComponent);
            defferedShader.SetInt("depth", 1);

            // Bind normal texture
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            defferedShader.SetInt("gNormal", 2);
        
            // Bind Metallic and Roughness texture
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, gMetallicRough);
            defferedShader.SetInt("gMetallicRough", 3);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderPPRect(ref Shader postprocessShader, int frameBufferTexture, int depthStencilTexture, int gNormal, int gPosition, int numSamples, Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            postprocessShader.Use();
            postprocessShader.SetMatrix4("projMatrixInv", Matrix4.Invert(projectionMatrix));
            postprocessShader.SetMatrix4("viewMatrix", viewMatrix);

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            postprocessShader.SetInt("frameBufferTexture", 0);

            // Bind normal texture
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            postprocessShader.SetInt("gNormal", 1);
            
            int samplesLocation = GL.GetUniformLocation(postprocessShader.Handle, "samples");
            GL.Uniform3(samplesLocation, numSamples, ref sample[0].X);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            postprocessShader.SetInt("texNoise", 2);
            postprocessShader.SetMatrix4("projection", projectionMatrix);

            // Bind depth texture
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.DepthComponent);
            postprocessShader.SetInt("depth", 3);

            // Bind normal texture
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            postprocessShader.SetInt("gPosition", 4);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderSSAOrect(ref Shader SSAOblurShader, int blurAO)
        {
            SSAOblurShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, blurAO);
            SSAOblurShader.SetInt("inAO", 0);

            // Render quad with framebuffer and added outline
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderOutlineRect(ref Shader outlineShader, int frameBufferTexture, int depthStencilTexture)
        {
            outlineShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            outlineShader.SetInt("frameBufferTexture", 0);

            // Bind stencil texture for outline in fragshader
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.StencilIndex);
            outlineShader.SetInt("stencilTexture", 1);

            // Render quad with framebuffer and added outline
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderFXAARect(ref Shader fxaaShader, int frameBufferTexture, int blurAO, int depthStencilTexture)
        {
            fxaaShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            fxaaShader.SetInt("frameBufferTexture", 0);

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, blurAO);
            fxaaShader.SetInt("inAO", 1);

            // Bind depth texture
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.DepthComponent);
            fxaaShader.SetInt("depth", 2);

            // Render quad with framebuffer and added outline
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}