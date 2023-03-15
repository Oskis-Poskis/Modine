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

    public class Mesh : SceneObject
    {
        private int vaoHandle;
        private int vboHandle;
        private int eboHandle;
        public int vertexCount;
        public bool castShadow;
        public string meshName;
        public int MaterialIndex;

        public Mesh(VertexData[] vertData, int[] indices, Shader shader, bool CastShadow, int matIndex) : base(meshShader: shader)
        {
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertData.Length * 14 * sizeof(float), vertData, BufferUsageHint.StaticDraw);

            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(shader.GetAttribLocation("aPosition"));
            GL.VertexAttribPointer(shader.GetAttribLocation("aPosition"), 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);
            GL.EnableVertexAttribArray(shader.GetAttribLocation("aNormals"));
            GL.VertexAttribPointer(shader.GetAttribLocation("aNormals"), 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(shader.GetAttribLocation("aUVs"));
            GL.VertexAttribPointer(shader.GetAttribLocation("aUVs"), 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(shader.GetAttribLocation("aTangents"));
            GL.VertexAttribPointer(shader.GetAttribLocation("aTangents"), 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 9 * sizeof(float));
            GL.EnableVertexAttribArray(shader.GetAttribLocation("aBiTangents"));
            GL.VertexAttribPointer(shader.GetAttribLocation("aBiTangents"), 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));
            
            meshName = Name;
            vertexCount = indices.Length;
            castShadow = CastShadow;
            MaterialIndex = matIndex;

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public override void Render(Vector3 pos, Vector3 rot, Vector3 scale, Shader meshShader)
        {   
            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(scale);
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rot.X)) *
                     Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rot.Y)) *
                     Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rot.Z));
            model *= Matrix4.CreateTranslation(pos);

            meshShader.SetMatrix4("model", model);

            GL.BindVertexArray(vaoHandle);
            if (vertexCount > 0) GL.DrawElements(PrimitiveType.Triangles, vertexCount, DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);
        }

        public override void Dispose()
        {
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteBuffer(vboHandle);
        }
    }
}