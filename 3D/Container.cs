using OpenTK.Mathematics;

namespace GameEngine.Rendering
{
    public class SceneObject : IDisposable
    {
        public string Type { get; set; }
        public string Name { get; set;}
        public bool SmoothShading;
        public bool CastShadow;
        public Mesh Mesh;
        public Light Light;

        public SceneObject(string _name = null, string _type = null, Mesh _mesh = null, Light _light = null)
        {
            this.Name =_name;
            this.Type = _type;
            this.Mesh = _mesh;
        }

        public virtual void Render()
        {

        }

        public virtual void RenderLight(Vector3 cameraPosition, Vector3 direction, float pitch, float yaw)
        {

        }

        public virtual void Dispose()
        {

        }
    }
}