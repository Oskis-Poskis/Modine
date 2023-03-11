
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GameEngine.Common
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

        static Random random = new Random();
        static List<Vector3> ssaoNoise = new List<Vector3>();
        static Vector3[] sample = new Vector3[64];
        public static void GenNoise()
        {
            for (int i = 0; i < 64; i++)
            {
                sample[i] = new Vector3(
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    0.0f);
                sample[i] = Vector3.Normalize(sample[i]);
                sample[i] *= (float)random.NextDouble();

                float scale = i / 64.0f;
                sample[i] *= MathHelper.Lerp(0.1f, 1.0f, scale * scale);
            }

            for (int i = 0; i < 16; i++)
            {
                Vector3 noise = new Vector3(
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    (float)random.NextDouble() * 2.0f - 1.0f, 
                    0.0f);
                ssaoNoise.Add(noise);
            }
        }

        public static void RenderDefaultRect(ref Shader postprocessShader, int frameBufferTexture, int depthStencilTexture, int gPosition, int gNormal, Matrix4 projectionMatrix)
        {
            // Bind framebuffer texture
            postprocessShader.SetInt("frameBufferTexture", 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);

            // Bind depth texture
            postprocessShader.SetInt("depth", 1);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.DepthComponent);

            // Bind depth texture
            postprocessShader.SetInt("gPosition", 2);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);

            // Bind depth texture
            postprocessShader.SetInt("gNormal", 3);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            
            postprocessShader.Use();
            int samplesLocation = GL.GetUniformLocation(postprocessShader.Handle, "samples");
            // Generate sample kernel
            
            for (int i = 0; i < 64; i++)
            {
                GL.Uniform3(samplesLocation + i, sample[i]);
            }

            // Generate noise texture
            int noiseTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, 4, 4, 0, PixelFormat.Rgb, PixelType.Float, Marshal.UnsafeAddrOfPinnedArrayElement(ssaoNoise.ToArray(), 0));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            postprocessShader.SetInt("texNoise", 4);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);

            postprocessShader.SetMatrix4("projection", projectionMatrix);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderOutlineRect(ref Shader outlineShader, int frameBufferTexture, int depthStencilTexture)
        {
            // Bind framebuffer texture
            outlineShader.SetInt("frameBufferTexture", 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);

            // Bind stencil texture for outline in fragshader
            outlineShader.SetInt("stencilTexture", 2);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.StencilIndex);

            // Render quad with framebuffer and added outline
            outlineShader.Use();
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderFXAARect(ref Shader fxaaShader, int frameBufferTexture)
        {
            // Bind framebuffer texture
            fxaaShader.SetInt("frameBufferTexture", 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);

            // Render quad with framebuffer and added outline
            fxaaShader.Use();
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}