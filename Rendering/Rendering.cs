using OpenTK.Graphics.OpenGL4;
using static Modine.Rendering.SceneObject;
using Modine.Common;

namespace Modine.Rendering
{
    public class Rendering
    {
        public static void RenderShadowScene(int shadowRes, ref int depthMapFBO, OpenTK.Mathematics.Matrix4 lightSpaceMatrix, ref List<SceneObject> sceneObjects, Shader shadowShader)
        {
            // Adjust viewport to shadow resolution
            GL.Viewport(0, 0, shadowRes, shadowRes);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            shadowShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);

            // Draw meshes to shadow map with different shaders
            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) sceneObject.Mesh.meshShader = shadowShader;
                if (sceneObject.Type == SceneObjectType.Mesh && sceneObject.Mesh.castShadow == true) sceneObject.Render();
            }
        }
    }
}