using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Modine.Common;

namespace Modine.Compute
{
    public class RenderTexture
    {
        private static int VAO;
        private static int VBO;

        static float[] FBOvertices =
        {
            -1f,  1f,
            -1f, -1f,
             1f,  1f,
             1f,  1f,
            -1f, -1f,
             1f, -1f,
        };

        public static void SetupCompRect(ref int framebufferTexture, Vector2i viewportSize)
        {
            // Color Texture
            framebufferTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, viewportSize.X, viewportSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, framebufferTexture, 0);
        
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, FBOvertices.Length * sizeof(float), FBOvertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        }

        public static void ResizeTexture(Vector2i viewportSize, ref int texture, PixelInternalFormat pif, PixelFormat pf)
        {            
            //Resize framebuffer
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, pif, viewportSize.X, viewportSize.Y, 0, pf, PixelType.UnsignedByte, IntPtr.Zero);
        }

        public static void RenderCompRect(ref Shader FBOshader, int framebufferTexture)
        {
            FBOshader.Use();
            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            FBOshader.SetInt("framebufferTexture", 0);

            // Render quad with framebuffer and postprocessing
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }
}