using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Modine.Common;

namespace Modine.Rendering
{
    public struct VertexData
    {
        public Vector3 Position;
        public Vector3 Normals;
        public Vector2 UVs;
        public Vector3 Tangents;
        public Vector3 BiTangents;

        public VertexData(Vector3 position, Vector3 normals, Vector2 uvs, Vector3 tangents, Vector3 bitangents)
        {
            this.Position = position;
            this.Normals = normals;
            this.UVs = uvs;
            this.Tangents = tangents;
            this.BiTangents = bitangents;
        }
    }

    public class Mesh
    {
        private int vaoHandle;
        private int vboHandle;
        private int eboHandle;
        public int vertexCount;
        public bool castShadow;

        public int MaterialIndex;
        public int[] indices;
        public VertexData[] vertexData;

        public Mesh(VertexData[] vertData, int[] ind, bool CastShadow, int matIndex)
        {
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertData.Length * 14 * sizeof(float), vertData, BufferUsageHint.StaticDraw);

            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ind.Length * sizeof(uint), ind, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 9 * sizeof(float));
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));
            
            vertexCount = ind.Length;
            castShadow = CastShadow;
            MaterialIndex = matIndex;

            vertexData = vertData;
            indices = ind;

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public void RenderScene(Shader shader, Vector3 pos, Vector3 rot, Vector3 scale, float index)
        {   
            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(scale);
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rot.X)) *
                     Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rot.Y)) *
                     Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rot.Z));
            model *= Matrix4.CreateTranslation(pos);

            shader.SetMatrix4("model", model);
            shader.SetFloat("meshID", index);

            GL.BindVertexArray(vaoHandle);
            if (vertexCount > 0) GL.DrawElements(PrimitiveType.Triangles, vertexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void RenderScene(Shader shader, Vector3 pos, Vector3 rot, Vector3 scale)
        {   
            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(scale);
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rot.X)) *
                     Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rot.Y)) *
                     Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rot.Z));
            model *= Matrix4.CreateTranslation(pos);

            shader.SetMatrix4("model", model);

            GL.BindVertexArray(vaoHandle);
            if (vertexCount > 0) GL.DrawElements(PrimitiveType.Triangles, vertexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteBuffer(vboHandle);
        }
    }
}