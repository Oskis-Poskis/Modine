using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Modine.Common;

namespace Modine.Rendering
{
    public class Light : SceneObject
    {
        private int vaoHandle;
        private int vboHandle;
        public Shader lightShader;

        public Vector3 lightColor = new(1, 1, 0);
        public float strength = 1.0f;
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

        public Light(Shader shader, Vector3 _color, float _strength) : base(lightShader: shader)
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
            this.strength = _strength;
        }

        public override void Render(Camera cam, Vector3 pos)
        {   
            Matrix4 viewMatrix = Matrix4.LookAt(cam.position, cam.position + cam.direction, Vector3.UnitY);

            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(0.15f);
            model *= Matrix4.CreateRotationX(Math.Clamp(cam.phi, -89, 89)) *
                     Matrix4.CreateRotationY(-cam.theta - MathHelper.PiOver2) * 
                     Matrix4.CreateRotationZ(0);
            model *= Matrix4.CreateTranslation(pos);

            lightShader.SetMatrix4("model", model);
            lightShader.SetVector3("lightColor", lightColor);

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