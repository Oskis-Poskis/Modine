using OpenTK.Mathematics;

namespace GameEngine.Rendering
{
    public class SceneObject : IDisposable
    {
        public SceneObjectType Type { get; set; }
        public string Name { get; set;}
        public bool SmoothShading;
        public bool CastShadow;
        public Mesh Mesh;
        public Light Light;

        public enum SceneObjectType
        {
            Mesh,
            Light
        }

        public SceneObject(string _name = null, SceneObjectType _type = SceneObjectType.Mesh, Mesh _mesh = null, Light _light = null)
        {
            this.Name =_name;
            this.Type = _type;
            this.Mesh = _mesh;
            this.Light = _light;
        }

        public virtual void Dispose()
        {

        }
    }
}