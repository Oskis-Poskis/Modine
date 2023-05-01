using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Modine.Common;

namespace Modine.Rendering
{
    public class Light
    {
        private int vaoHandle;
        private int vboHandle;
        public Vector3 Color { get; set; }
        public float strength;

        float[] vertices = new float[]
        {
            // First triangle
            -1,  1, 0,
            -1, -1, 0,
             1,  1, 0,

             // Second triangle
             1,  1, 0,
            -1, -1, 0,
             1, -1, 0
        };

        public Light(Vector3 _color, float _strength)
        {
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            this.Color = _color;
            this.strength = _strength;
        }

        public void RenderLight(Shader shader, Camera cam, Vector3 pos)
        {   
            Matrix4 viewMatrix = Matrix4.LookAt(cam.position, cam.position + cam.direction, new(Vector3.UnitY));

            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(0.3f);
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(cam.phi)) *
                     Matrix4.CreateRotationY(-MathHelper.DegreesToRadians(cam.theta + 90)) * 
                     Matrix4.CreateRotationZ(0);
            model *= Matrix4.CreateTranslation(pos);

            shader.SetMatrix4("model", model);
            shader.SetVector3("lightColor", Color);

            GL.BindVertexArray(vaoHandle);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteBuffer(vboHandle);
        }
    }
}