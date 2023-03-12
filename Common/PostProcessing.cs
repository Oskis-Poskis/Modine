
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

            GL.EnableVertexAttribArray(postprocessShader.GetAttribLocation("aPosition"));
            GL.VertexAttribPointer(postprocessShader.GetAttribLocation("aPosition"), 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        }
 
        static Vector3[] ssaoNoise = new Vector3[16];
        static Vector3[] sample = new Vector3[64];
        static int noiseTexture;

        public static void GenNoise()
        {
            for (int i = 0; i < 64; i++)
            {
                Random random = new Random();
                sample[i] = new Vector3(
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble());
                sample[i] = Vector3.Normalize(sample[i]);
                sample[i] *= (float)random.NextDouble();
                float scale = i / 64.0f;

                scale = MathHelper.Lerp(0.1f, 1.0f, scale * scale);
                sample[i] *= scale;
            }

            for (int i = 0; i < 16; i++)
            {
                Random random = new Random();
                Vector3 noise = new Vector3(
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    0.0f);
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

        public static void RenderDefaultRect(ref Shader postprocessShader, int frameBufferTexture, int depthStencilTexture, int gPosition, int gNormal, Matrix4 projectionMatrix)
        {
            postprocessShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            postprocessShader.SetInt("frameBufferTexture", 0);
            
            // Bind depth texture
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.DepthComponent);
            postprocessShader.SetInt("depth", 1);

            // Bind position texture
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            postprocessShader.SetInt("gPosition", 2);

            // Bind normal texture
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            postprocessShader.SetInt("gNormal", 3);
            
            int samplesLocation = GL.GetUniformLocation(postprocessShader.Handle, "samples");
            GL.Uniform3(samplesLocation, 64, ref sample[0].X);

            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            postprocessShader.SetInt("texNoise", 4);
            postprocessShader.SetMatrix4("projection", projectionMatrix);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderSSAOrect(ref Shader SSAOblurShader, int frameBufferTexture)
        {
            SSAOblurShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            SSAOblurShader.SetInt("frameBufferTexture", 0);

            // Render quad with framebuffer and added outline
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderOutlineRect(ref Shader outlineShader, int frameBufferTexture, int depthStencilTexture, int SSAOblur)
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

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, SSAOblur);
            outlineShader.SetInt("SSAOblur", 2);

            // Render quad with framebuffer and added outline
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderFXAARect(ref Shader fxaaShader, int frameBufferTexture)
        {
            fxaaShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            fxaaShader.SetInt("frameBufferTexture", 0);

            // Render quad with framebuffer and added outline
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}