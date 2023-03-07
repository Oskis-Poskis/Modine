using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GameEngine.Common;

namespace GameEngine.Rendering
{
    public class Light : SceneObject
    {
        private int vaoHandle;
        private int vboHandle;
        public Shader lightShader;

        public Vector3 position = Vector3.Zero;
        public Vector3 scale = Vector3.One * 0.25f;
        public Vector3 lightColor = new(1, 1, 0);
        public float intensity = 1.0f;
        public string lightName;

        public static Light _light;

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

        public Light(Shader shader, Vector3 _color) : base()
        {
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(shader.GetAttribLocation("aPosition"));
            GL.VertexAttribPointer(shader.GetAttribLocation("aPosition"), 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            _light = this;

            this.lightShader = shader;
            this.lightName = Name;
            this.lightColor = _color;
        }

        Matrix4 viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);

        public void Render(Vector3 cameraPosition, Vector3 direction, float pitch, float yaw)
        {   
            viewMatrix = Matrix4.LookAt(cameraPosition, cameraPosition + direction, Vector3.UnitY);

            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(scale);
            model *= Matrix4.CreateRotationX(Math.Clamp(pitch, -89, 89)) *
                     Matrix4.CreateRotationY(-yaw - MathHelper.PiOver2) * 
                     Matrix4.CreateRotationZ(0);
            model *= Matrix4.CreateTranslation(position);

            lightShader.SetMatrix4("model", model);
            lightShader.SetMatrix4("view", viewMatrix);

            GL.BindVertexArray(vaoHandle);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            GL.BindVertexArray(0);
        }

        public override void Dispose()
        {
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteBuffer(vboHandle);
        }
    }
}