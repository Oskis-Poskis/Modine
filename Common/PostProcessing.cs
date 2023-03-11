
using OpenTK.Graphics.OpenGL4;

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

        public static void RenderDefaultRect(ref Shader postprocessShader, int frameBufferTexture, int depthStencilTexture, int gPosition, int gNormal, int texNoise)
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

            // Bind depth texture
            postprocessShader.SetInt("texNoise", 4);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, texNoise);
            
            // Render quad with framebuffer and postprocessing
            postprocessShader.Use();
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