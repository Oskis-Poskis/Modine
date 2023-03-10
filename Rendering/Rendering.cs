using OpenTK.Graphics.OpenGL4;
using static GameEngine.Rendering.SceneObject;
using GameEngine.Common;

namespace GameEngine.Rendering
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
            GL.CullFace(CullFaceMode.Front);
            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) sceneObject.Mesh.meshShader = shadowShader;
                if (sceneObject.Type == SceneObjectType.Mesh && sceneObject.Mesh.castShadow == true) sceneObject.Mesh.Render();
            }
            GL.CullFace(CullFaceMode.Back);
        }
    }
}