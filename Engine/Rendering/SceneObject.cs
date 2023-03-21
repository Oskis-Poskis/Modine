using OpenTK.Mathematics;
using Modine.Common;

namespace Modine.Rendering
{
    public class SceneObject : IDisposable
    {
        public SceneObjectType Type;
        public string Name;
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = new(-90, 0, 0);
        public Vector3 Scale = Vector3.One;
        public Shader Shader;
        public Mesh Mesh;
        public Light Light;

        public enum SceneObjectType
        {
            Mesh,
            Light
        }

        public SceneObject(Shader meshShader, string _name = "Mesh", Mesh _mesh = null)
        {
            this.Name = _name;
            this.Type = SceneObjectType.Mesh;
            this.Mesh = _mesh;
            this.Shader = meshShader;
        }

        public SceneObject(Shader lightShader, string _name = "Light", Light _light = null)
        {
            this.Name = _name;
            this.Type = SceneObjectType.Light;
            this.Light = _light;
            this.Shader = lightShader;
        }

        public virtual void Render(Vector3 position = default(Vector3), Vector3 rotation = default(Vector3), Vector3 scale = default(Vector3), Shader shader = default(Shader))
        {
            Mesh.Render(Position, Rotation, Scale, Shader);
        }

        public virtual void Render(Camera cam, Vector3 pos = default(Vector3))
        {
            Light.Render(cam, Position);
        }

        public virtual void Dispose()
        {
            switch (this.Type)
            {
                case SceneObjectType.Mesh:
                    Mesh.Dispose();
                    break;
                    
                case SceneObjectType.Light:
                    Light.Dispose();
                    break;
            }
        }
    }
}