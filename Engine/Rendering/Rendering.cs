using OpenTK.Graphics.OpenGL4;
using static Modine.Rendering.SceneObject;
using Modine.Common;
using OpenTK.Windowing.Common;

namespace Modine.Rendering
{
    public class RenderClass
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
                if (sceneObject.Type == SceneObjectType.Mesh) sceneObject.Shader = shadowShader;
                if (sceneObject.Type == SceneObjectType.Mesh && sceneObject.Mesh.castShadow == true) sceneObject.Render();
            }
        }
    }

    class FPScounter
    {
        public int frameCount = 0;
        public double elapsedTime = 0.0, fps = 0.0, ms;

        public void Count(FrameEventArgs args)
        {
            frameCount++;
            elapsedTime += args.Time;
            if (elapsedTime >= 1f)
            {
                fps = frameCount / elapsedTime;
                ms = 1000 * elapsedTime / frameCount;
                frameCount = 0;
                elapsedTime = 0.0;
            }
        }
    }
}