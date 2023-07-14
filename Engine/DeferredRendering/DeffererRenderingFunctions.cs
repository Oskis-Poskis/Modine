using OpenTK.Graphics.OpenGL4;

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


        public static void RenderDefferedRect(ref Shader defferedShader, int depthStencilTexture, int gAlbedo, int gNormal, int gPosition, int gMetallicRough)
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

            // Bind position texture
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            defferedShader.SetInt("gPosition", 3);
        
            // Bind Metallic and Roughness texture
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, gMetallicRough);
            defferedShader.SetInt("gMetallicRough", 4);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
        }

        public static void RenderPPRect(ref Shader postprocessShader, int frameBufferTexture)
        {
            postprocessShader.Use();

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, frameBufferTexture);
            postprocessShader.SetInt("frameBufferTexture", 0);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
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