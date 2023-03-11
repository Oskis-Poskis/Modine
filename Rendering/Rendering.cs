using OpenTK.Graphics.OpenGL4;
using static GameEngine.Rendering.SceneObject;
using GameEngine.Common;
using System.Runtime.InteropServices;

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

        public static void OnDebugMessage(
            DebugSource source,     // Source of the debugging message.
            DebugType type,         // Type of the debugging message.
            int id,                 // ID associated with the message.
            DebugSeverity severity, // Severity of the message.
            int length,             // Length of the string in pMessage.
            IntPtr pMessage,        // Pointer to message string.
            IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
        {
            // In order to access the string pointed to by pMessage, you can use Marshal
            // class to copy its contents to a C# string without unsafe code. You can
            // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
            string message = Marshal.PtrToStringAnsi(pMessage, length);

            // The rest of the function is up to you to implement, however a debug output
            // is always useful.
            Console.WriteLine("[{0} source={1} type={2} id={3}] \n{4}", severity, source, type, id, message);

            // Potentially, you may want to throw from the function for certain severity
            // messages.
            if (type == DebugType.DebugTypeError)
            {
                throw new Exception(message);
            }
        }
    }
}