using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Modine.Common;

namespace Modine.Rendering
{
    public class Material
    {
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public float Metallic { get; set; } = 0.0f;
        public float Roughness { get; set; } = 0.5f;
        public float EmissionStrength { get; set; } = 0.0f;

        public Material(Vector3 color, float metallic, float roughness, float emissionStrength)
        {
            Color = color;
            Metallic = metallic;
            Roughness = roughness;
            EmissionStrength = emissionStrength;
        }

        public void SetShaderUniforms(Shader shader)
        {
            shader.Use();
            shader.SetVector3("material.albedo", Color);
            shader.SetFloat("material.metallic", Metallic);
            shader.SetFloat("material.roughness", Roughness);
            shader.SetFloat("material.emissionStrength", EmissionStrength);
        }
    }
}