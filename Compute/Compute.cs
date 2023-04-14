using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Modine.Common;
using Modine.Rendering;

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

        public struct Triangle
        {
            public Vector3 v0;
            public Vector3 v1;
            public Vector3 v2;
        }

        private static List<float> triangleData = new List<float>();
        public static void CreateResourceMemory(ref int triangleDataTexture, VertexData[] vertexData)
        {
            // (cx cy cz r) (r g b roughness)
            for (int i = 0; i < 3; i++)
            {
                for (int attribute = 0; attribute < 12; attribute++)
                {
                    triangleData.Add(0.0f);
                }

                Triangle triangle;
                //triangle.v0 = vertexData[i].Position;
                //triangle.v1 = vertexData[i + 1].Position;
                //triangle.v2 = vertexData[i + 2].Position;

                triangle.v0 = new(-1, 0, -1);
                triangle.v1 = new(0, 0, 1);
                triangle.v2 = new(1, 0, -1);

                recordTriangle(i, triangle);
                Console.WriteLine("V0" + triangle.v0 + " - " + "V1" + triangle.v1 + " - " + "V2" + triangle.v1);
            }

            triangleDataTexture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture10);
            GL.BindTexture(TextureTarget.Texture2D, triangleDataTexture);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, 3, 3, 0, PixelFormat.Rgba, PixelType.Float, triangleData.ToArray());

            GL.ActiveTexture(TextureUnit.Texture10);
            GL.BindImageTexture(10, triangleDataTexture, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);
        }

        private static void recordTriangle(int i, Triangle triangle)
        {
            triangleData[12 * i] =     triangle.v0[0];
            triangleData[12 * i + 1] = triangle.v0[1];
            triangleData[12 * i + 2] = triangle.v0[2];
            triangleData[12 * i + 3] = 0;

            triangleData[12 * i + 4] = triangle.v1[0];
            triangleData[12 * i + 5] = triangle.v1[1];
            triangleData[12 * i + 6] = triangle.v1[2];
            triangleData[12 * i + 7] = 0;

            triangleData[12 * i + 8] = triangle.v2[0];
            triangleData[12 * i + 9] = triangle.v2[1];
            triangleData[12 * i + 10] = triangle.v2[2];
            triangleData[12 * i + 11] = 0;
        }
    }
}