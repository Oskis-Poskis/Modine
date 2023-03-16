using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Modine.Common;

namespace Modine.Rendering
{
    public class Material
    {
        public string Name { get; set; } = "";
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public float Metallic { get; set; } = 0.0f;
        public float Roughness { get; set; } = 0.5f;
        public Texture ColorTexture { get; set; }
        public Texture RoughnessTexture { get; set; }
        public Texture MetallicTexture { get; set; }
        public Texture NormalTexture { get; set; }
        public float EmissionStrength { get; set; } = 0.0f;

        private Texture white1x1 = Texture.LoadFromFile("Resources/White1x1.png");
        private Texture normal1x1 = Texture.LoadFromFile("Resources/Normal1x1.png");

        public Material(string name, Vector3 color, float metallic, float roughness, float emissionStrength, Shader shader, Texture colorTexture = null, Texture roughnessTexture = null, Texture metallitexture = null, Texture normaltexture = null)
        {
            Name = name;
            Color = color;
            Metallic = metallic;
            Roughness = roughness;
            EmissionStrength = emissionStrength;

            ColorTexture = colorTexture ?? white1x1;
            RoughnessTexture = roughnessTexture ?? white1x1;
            MetallicTexture = metallitexture ?? white1x1;
            NormalTexture = normaltexture ?? normal1x1;

            SetShaderUniforms(shader);
        }

        public void SetShaderUniforms(Shader shader)
        {
            shader.Use();
            shader.SetVector3("material.albedo", Color);
            shader.SetFloat("material.metallic", Metallic);
            shader.SetFloat("material.roughness", Roughness);
            //shader.SetFloat("material.emissionStrength", EmissionStrength);

            GL.ActiveTexture(TextureUnit.Texture0);
            ColorTexture.Use(TextureUnit.Texture0);
            shader.SetInt("material.albedoTex", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            RoughnessTexture.Use(TextureUnit.Texture1);
            shader.SetInt("material.roughnessTex", 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            MetallicTexture.Use(TextureUnit.Texture2);
            shader.SetInt("material.metallicTex", 2);
            
            GL.ActiveTexture(TextureUnit.Texture3);
            NormalTexture.Use(TextureUnit.Texture3);
            shader.SetInt("material.normalTex", 3);
        }
    }
}