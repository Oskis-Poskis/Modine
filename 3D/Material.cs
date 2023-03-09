using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GameEngine.Common;

namespace GameEngine.Rendering
{
    public class Material
    {
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public float Metallic { get; set; } = 0.0f;
        public float Roughness { get; set; } = 0.5f;

        public Material(Vector3 color, float metallic, float roughness)
        {
            Color = color;
            Metallic = metallic;
            Roughness = roughness;
        }

        public void SetShaderUniforms(Shader shader)
        {
            shader.Use();
            shader.SetVector3("material.albedo", Color);
            shader.SetFloat("material.metallic", Metallic);
            shader.SetFloat("material.roughness", Roughness);
        }
    }
}