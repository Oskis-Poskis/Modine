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
            public float pad0;
            public Vector3 v1;
            public float pad1;
            public Vector3 v2;
            public float pad2;
            public Vector3 normal0;
            public float pad3;
            public Vector3 normal1;
            public float pad4;
            public Vector3 normal2;
            public float pad5;
        }

        public static void CreateResourceMemory(VertexData[] vertexData, int[] indices)
        {
            Triangle[] triangleData = new Triangle[indices.Length];

            for (int i = 0; i < indices.Length; i += 3)
            {
                Triangle triangle;
                triangle.v0 = vertexData[indices[i]].Position;
                triangle.pad0 = 0;
                triangle.v1 = vertexData[indices[i + 1]].Position;
                triangle.pad1 = 0;
                triangle.v2 = vertexData[indices[i + 2]].Position;
                triangle.pad2 = 0;
                triangle.normal0 = vertexData[indices[i]].Normals;
                triangle.pad3 = 0;
                triangle.normal1 = vertexData[indices[i + 1]].Normals;
                triangle.pad4 = 0;
                triangle.normal2 = vertexData[indices[i + 2]].Normals;
                triangle.pad5 = 0;

                triangleData[i] = triangle;
            }

            const int BINDING_INDEX = 0;

            GL.CreateBuffers(1, out int buffer);
            GL.NamedBufferStorage(buffer, sizeof(float) * 24 * triangleData.Count(), ref triangleData.ToArray()[0], BufferStorageFlags.DynamicStorageBit);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, BINDING_INDEX, buffer);
        }
    }
}