using OpenTK.Graphics.OpenGL4;
using static Modine.Rendering.Entity;
using Modine.Common;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

namespace Modine.Rendering
{
    public class Functions
    {
        public static void RenderShadowScene(int shadowRes, ref int depthMapFBO, OpenTK.Mathematics.Matrix4 lightSpaceMatrix, ref List<Entity> sceneObjects, Shader shadowShader, Shader PBRShader)
        {
            // Adjust viewport to shadow resolution
            GL.Viewport(0, 0, shadowRes, shadowRes);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            shadowShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);

            // Draw meshes to shadow map with different shaders
            foreach (Entity sceneObject in sceneObjects)
            {
                if (sceneObject.Type == EntityType.Mesh) sceneObject.Shader = shadowShader;
                if (sceneObject.Type == EntityType.Mesh && sceneObject.Mesh.castShadow == true) sceneObject.Render();
                if (sceneObject.Type == EntityType.Mesh) sceneObject.Shader = PBRShader;
            }
        }

        public struct SSBOlight
        {
            public Vector3 lightPos;
            public float strength;
            public Vector3 lightColor;
            public float p0;
        }

        public static void CreatePointLightResourceMemory(List<Entity> sceneObjs)
        {
            EngineUtility.CountEntities(sceneObjs, out int MeshCount, out int PointLightCount);
            int count_Meshes = MeshCount;
            int count_PointLights = PointLightCount;
            
            if (count_PointLights > 0)
            {
                List<SSBOlight> lightData = new List<SSBOlight>();

                for (int i = 0; i < sceneObjs.Count; i++)
                {
                    if (sceneObjs[i].Type == EntityType.Light)
                    {
                        SSBOlight light;
                        light.lightPos = sceneObjs[i].Position;
                        light.strength = sceneObjs[i].Light.strength;
                        light.lightColor = sceneObjs[i].Light.Color;
                        light.p0 = 0;

                        lightData.Add(light);
                    }
                }

                const int BINDING_INDEX = 0;

                GL.CreateBuffers(1, out int buffer);
                GL.NamedBufferStorage(buffer, sizeof(float) * 8 * lightData.Count(), ref lightData.ToArray()[0], BufferStorageFlags.DynamicStorageBit);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, BINDING_INDEX, buffer);
            }

            else
            {
                SSBOlight lightData = new SSBOlight();
                lightData.lightPos = Vector3.Zero;
                lightData.strength = 0;
                lightData.lightColor = Vector3.Zero;
                lightData.p0 = 0;

                const int BINDING_INDEX = 0;

                GL.CreateBuffers(1, out int buffer);
                GL.NamedBufferStorage(buffer, sizeof(float) * 8, ref lightData, BufferStorageFlags.DynamicStorageBit);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, BINDING_INDEX, buffer);
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