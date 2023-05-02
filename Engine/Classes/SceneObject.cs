using OpenTK.Mathematics;
using Modine.Common;

namespace Modine.Rendering
{
    public class Entity : IDisposable
    {
        public EntityType Type;
        public string Name;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Mesh Mesh;
        public Light Light;

        public Shader Shader;

        public enum EntityType
        {
            Mesh,
            Light
        }

        public Entity()
        {

        }

        public Entity(Mesh mesh, Shader shader, Vector3 pos, Vector3 rot, Vector3 scale, string name)
        {
            this.Shader = shader;     
            this.Type = EntityType.Mesh;
            this.Mesh = mesh;
            this.Position = pos;
            this.Rotation = rot + new Vector3(-90, 0, 0);
            this.Scale = scale;
            this.Name = name;
        }

        public Entity(Light light, Vector3 pos, string name)
        {
            this.Type = EntityType.Light;
            this.Light = light;
            this.Position = pos;
            this.Scale = Vector3.One;
            this.Rotation = Vector3.Zero;
            this.Name = name;
        }

        public virtual void Render()
        {
            Mesh.RenderScene(Shader, this.Position, this.Rotation, this.Scale);
        }

        public virtual void Render(Camera cam)
        {
            Light.RenderLight(Game.lightShader, cam, Position);
        }

        public virtual void Dispose()
        {
            switch (this.Type)
            {
                case EntityType.Mesh:
                    Mesh.Dispose();
                    break;
                    
                case EntityType.Light:
                    Light.Dispose();
                    break;
            }
        }
    }
}