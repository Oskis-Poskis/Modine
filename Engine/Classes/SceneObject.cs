using OpenTK.Mathematics;
using Modine.Common;

namespace Modine.Rendering
{
    public class SceneObject : IDisposable
    {
        public SceneObjectType Type;
        public string Name;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Mesh Mesh;
        public Light Light;

        public Shader Shader;

        public enum SceneObjectType
        {
            Mesh,
            Light
        }

        public SceneObject()
        {

        }

        public SceneObject(Mesh mesh, Shader shader, Vector3 pos, Vector3 rot, Vector3 scale, string name)
        {
            this.Shader = shader;     
            this.Type = SceneObjectType.Mesh;
            this.Mesh = mesh;
            this.Position = pos;
            this.Rotation = rot + new Vector3(-90, 0, 0);
            this.Scale = scale;
            this.Name = name;
        }

        public SceneObject(Light light, Vector3 pos, string name)
        {
            this.Type = SceneObjectType.Light;
            this.Light = light;
            this.Position = pos;
            this.Scale = Vector3.One;
            this.Rotation = Vector3.Zero;
            this.Name = name;
        }

        public virtual void Render()
        {
            Mesh.RenderScene(Game.PBRShader, this.Position, this.Rotation, this.Scale);
        }

        public virtual void Render(Camera cam)
        {
            Light.RenderLight(Game.lightShader, cam, Position);
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