using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ImGuiNET;

using SN = System.Numerics;

using Modine.Common;
using Modine.Importer;
using Modine.Rendering;
using Modine.ImGUI;

using static Modine.Rendering.SceneObject;
using System.Runtime.InteropServices;

namespace Modine
{
    class Game : GameWindow
    {
        public Game(int width, int height, string title)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Title = title,
                Size = new Vector2i(width, height),
                WindowBorder = WindowBorder.Resizable,
                StartVisible = false,
                StartFocused = true,
                WindowState = WindowState.Normal,
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(3, 3),
                Flags = ContextFlags.Debug
            })
        {
            CenterWindow();
            viewportSize = this.Size;
            previousViewportSize = viewportSize;
        }

        private bool viewportHovered;
        public bool ShowDepth_Stencil = false;

        private Vector2i viewportSize;
        private Vector2i previousViewportSize;
        private Vector2i viewportPos;
        private float pitch = 0, yaw = (MathHelper.Pi / 2) * 3;
        float sensitivity = 0.006f;

        int frameCount = 0;
        double elapsedTime = 0.0, fps = 0.0, ms;

        Vector3 ambient = new(0.1f);
        Vector3 direction = new(1);
        float shadowFactor = 0.75f;
        
        Material defaultMat;
        Material krissVectorMat;
        Material emissiveMaterial;
        public Shader PBRShader;
        public Shader lightShader;
        public Shader shadowShader;
        public Shader postprocessShader;
        public Shader outlineShader;
        public Shader fxaaShader;
        public Shader SSAOblurShader;
        Matrix4 projectionMatrix;
        Matrix4 viewMatrix;
        Matrix4 lightSpaceMatrix;

        Mesh krissVector;
        int[] vectorIndicies;
        VertexData[] vectorData;

        Mesh suzanne;
        int[] indices;
        int[] cubeIndices;
        int[] planeIndices;
        int[] sphereIndices;
        VertexData[] vertexData;
        VertexData[] cubeVertexData;
        VertexData[] planeVertexData;
        VertexData[] sphereVertexData;
        int triangleCount = 0;

        Camera camera;
        static List<SceneObject> sceneObjects = new List<SceneObject>();
        int selectedSceneObject = 0;
        int count_PointLights, count_Meshes = 0;

        PolygonMode _polygonMode = PolygonMode.Fill;
        private bool vsyncOn = true;
        private bool fullscreen = false;

        private ImGuiController ImGuiController;
        int FBO;
        int framebufferTexture;
        int depthStencilTexture;
        int gPosition;
        int gNormal;
        int SSAOblur;
        public static int numAOSamples = 16;
        public static int previousAOSamples = numAOSamples;

        int depthMapFBO;
        int depthMap;
        int shadowRes = 2048;

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

        private static DebugProc DebugMessageDelegate;

        unsafe protected override void OnLoad()
        {
            base.OnLoad();

            GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);

            MakeCurrent();
            IsVisible = true;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.StencilTest);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.PointSize(5);

            VSync = VSyncMode.On;

            FBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            Framebuffers.SetupFBO(ref framebufferTexture, ref depthStencilTexture, ref gPosition, ref gNormal, ref SSAOblur, viewportSize);
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError) Console.WriteLine("OpenGL Error: " + error.ToString());
            if (status != FramebufferErrorCode.FramebufferComplete) Console.WriteLine($"Framebuffer is incomplete: {status}");

            Framebuffers.SetupShadowFBO(ref depthMapFBO, ref depthMap, shadowRes);
            DebugMessageDelegate = OnDebugMessage;

            PBRShader = new Shader("Shaders/PBR/mesh.vert", "Shaders/PBR/mesh.frag");
            shadowShader = new Shader("Shaders/PBR/shadow.vert", "Shaders/PBR/shadow.frag");
            lightShader = new Shader("Shaders/Lights/light.vert", "Shaders/Lights/light.frag");
            postprocessShader = new Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/postprocess.frag");
            outlineShader = new Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/outline.frag");
            fxaaShader = new Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/fxaa.frag");
            SSAOblurShader = new Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/SSAOblur.frag");

            Postprocessing.SetupPPRect(ref postprocessShader);

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1, 0.1f, 100);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, new(0, 1, 0));
            camera = new Camera(new(0, 0, 2), -Vector3.UnitZ, 6);
            defaultMat = new(new(0.8f), 0, 0.3f, 0.0f, PBRShader);
            emissiveMaterial = new(new(0.25f, 0.15f, 1.0f), 0, 0.5f, 10, PBRShader);

            PBRShader.SetVector3("ambient", ambient);
            PBRShader.SetVector3("direction", direction);
            PBRShader.SetFloat("shadowFactor", shadowFactor);

            krissVectorMat = new(new(1), 1, 1, 0, PBRShader,
                Texture.LoadFromFile("Resources/1_Albedo.png"),
                Texture.LoadFromFile("Resources/1_Roughness.png"),
                Texture.LoadFromFile("Resources/1_Metallic.png"),
                Texture.LoadFromFile("Resources/1_Normal.png"));
            ModelImporter.LoadModel("Resources/KrissVector.fbx", out vectorData, out vectorIndicies);
            krissVector = new(vectorData, vectorIndicies, PBRShader, true, true, krissVectorMat);

            ModelImporter.LoadModel("Importing/TestRoom.fbx", out vertexData, out indices);
            ModelImporter.LoadModel("Importing/Floor.fbx", out planeVertexData, out planeIndices);  
            ModelImporter.LoadModel("Importing/Cube.fbx", out cubeVertexData, out cubeIndices);
            ModelImporter.LoadModel("Importing/Sphere.fbx", out sphereVertexData, out sphereIndices);
            suzanne = new Mesh(vertexData, indices, PBRShader, true, true, defaultMat);

            Postprocessing.GenNoise(numAOSamples);

            SceneObject _room = new("Room", SceneObjectType.Mesh, suzanne);
            SceneObject _vector = new("Vector", SceneObjectType.Mesh, krissVector);
            sceneObjects.Add(_room);
            sceneObjects.Add(_vector);

            count_Meshes = 0;
            count_PointLights = 0;
            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) count_Meshes += 1;
                else if (sceneObject.Type == SceneObjectType.Light) count_PointLights += 1;
            }

            triangleCount = CalculateTriangles();

            ImGuiController = new ImGuiController(viewportSize.X, viewportSize.Y);
            ImGuiWindows.LoadTheme();

            //GLFW.MaximizeWindow(WindowPtr);
        }

        bool isObjectPickedUp = false;
        bool grabX = false;
        bool grabY = false;
        bool grabZ = false;
        float originalDistance = 0;
        Vector3 originalPosition = Vector3.Zero;
        Vector3 newPosition = Vector3.Zero;
        Vector3 newPosition2 = Vector3.Zero;

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsMouseButtonDown(MouseButton.Button2))
            {
                CursorState = CursorState.Grabbed;
                
                float deltaX = MouseState.Delta.X;
                float deltaY = MouseState.Delta.Y;
                yaw += deltaX * sensitivity;
                pitch -= deltaY * sensitivity;
                pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.005f, MathHelper.PiOver2 - 0.005f);

                camera.direction = new Vector3(
                    (float)Math.Cos(yaw) * (float)Math.Cos(pitch),
                    (float)Math.Sin(pitch),
                    (float)Math.Sin(yaw) * (float)Math.Cos(pitch));
            }
            else CursorState = CursorState.Normal;

            float moveAmount = (float)(camera.speed * args.Time);
            if (viewportHovered)
            {
                //Console.WriteLine(sceneObjects[selectedSceneObject].Position);

                if (IsKeyDown(Keys.W)) camera.position += moveAmount * camera.direction;
                if (IsKeyDown(Keys.S)) camera.position -= moveAmount * camera.direction;
                if (IsKeyDown(Keys.A)) camera.position -= moveAmount * Vector3.Normalize(Vector3.Cross(camera.direction, Vector3.UnitY));
                if (IsKeyDown(Keys.D)) camera.position += moveAmount * Vector3.Normalize(Vector3.Cross(camera.direction, Vector3.UnitY));
                if (IsKeyDown(Keys.E)) camera.position += moveAmount * Vector3.UnitY;
                if (IsKeyDown(Keys.Q)) camera.position -= moveAmount * Vector3.UnitY;
                if (IsKeyDown(Keys.LeftAlt) && IsKeyPressed(Keys.G))
                {
                    sceneObjects[selectedSceneObject].Position = Vector3.Zero;
                }
                
                if (IsKeyPressed(Keys.G) && !IsKeyDown(Keys.LeftAlt))
                {
                    if (isObjectPickedUp) return;
                    originalPosition = sceneObjects[selectedSceneObject].Position;
                    isObjectPickedUp = true;
                    originalDistance = Vector3.Distance(camera.position, originalPosition);
                    grabX = false;
                    grabY = false;
                    grabZ = false;
                }

                if (isObjectPickedUp)
                {
                    float x = MapRange(MousePosition.X, 0, viewportSize.X, -1, 1);
                    float y = MapRange(MousePosition.Y, 0, viewportSize.Y, 1, -1);
                    float z = 1.0f;
                    Vector3 ray_nds = new(x, y, z);
                    Vector4 ray_clip = new(ray_nds.X, ray_nds.Y, -1.0f, 1.0f);
                    Vector4 ray_eye = ray_clip * Matrix4.Invert(projectionMatrix);
                    ray_eye = new(ray_eye.X, ray_eye.Y, -1.0f, 1.0f);
                    Vector3 ray_wor = (ray_eye * Matrix4.Invert(viewMatrix)).Xyz;

                    if (sceneObjects[selectedSceneObject].Type == SceneObjectType.Mesh) newPosition = Raycast(ray_wor, originalDistance);
                    if (sceneObjects[selectedSceneObject].Type == SceneObjectType.Light) newPosition = Raycast(camera.position, Vector3.Distance(camera.position, sceneObjects[selectedSceneObject].Position));
                    newPosition2 = Raycast(camera.position, Vector3.Distance(camera.position, sceneObjects[selectedSceneObject].Position));
                    if (IsMouseButtonPressed(MouseButton.Button1)) isObjectPickedUp = false;
                    
                    if (IsKeyPressed(Keys.X))
                    {
                        grabX = ToggleBool(grabX);
                        grabY = false;
                        grabZ = false;
                    }

                    if (IsKeyPressed(Keys.Y))
                    {
                        grabX = false;
                        grabY = ToggleBool(grabY);;
                        grabZ = false;
                    }

                    if (IsKeyPressed(Keys.Z))
                    {
                        grabX = false;
                        grabY = false;
                        grabZ = ToggleBool(grabZ);;
                    }

                    if (IsKeyPressed(Keys.Escape))
                    {
                        sceneObjects[selectedSceneObject].Position = originalPosition;
                        isObjectPickedUp = false;
                        return;
                    }

                    if (grabX) sceneObjects[selectedSceneObject].Position = new(newPosition2.X, originalPosition.Y, originalPosition.Z);
                    if (grabY) sceneObjects[selectedSceneObject].Position = new(originalPosition.X, newPosition2.Y, originalPosition.Z);
                    if (grabZ) sceneObjects[selectedSceneObject].Position = new(originalPosition.X, originalPosition.Y, newPosition2.Z);
                    if (!grabX && !grabY && !grabZ) sceneObjects[selectedSceneObject].Position = newPosition;
                }
            }

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

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            RenderScene(args.Time);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            RenderScene(0.017f);

            ImGuiController.WindowResized(e.Width, e.Height);
        }

        public void RenderScene(double time)
        {
            count_Meshes = 0;
            count_PointLights = 0;
            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) count_Meshes += 1;
                else if (sceneObject.Type == SceneObjectType.Light) count_PointLights += 1;
            }

            Modine.Rendering.Rendering.RenderShadowScene(shadowRes, ref depthMapFBO, lightSpaceMatrix, ref sceneObjects, shadowShader);

            // Render normal scene
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            DrawBuffersEnum[] buffers = new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3};
            GL.DrawBuffers(4, buffers);
            GL.ClearColor(new Color4(ambient.X, ambient.Y, ambient.Z, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);

            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) sceneObject.Mesh.meshShader = PBRShader;
                if (sceneObject.Type == SceneObjectType.Light) sceneObject.Light.lightShader = lightShader;
            }

            PBRShader.Use();
            PBRShader.SetVector3("viewPos", camera.position);
            PBRShader.SetInt("countPL", count_PointLights);

            if (sceneObjects.Count > 0)
            {
                // Render sceneobject list
                GL.Enable(EnableCap.StencilTest);

                GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
                GL.StencilMask(0x00);

                int index = 0;
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    if (sceneObjects[i].Type == SceneObjectType.Light)
                    {
                        PBRShader.SetVector3("pointLights[" + index + "].lightColor", sceneObjects[i].Light.lightColor);
                        PBRShader.SetVector3("pointLights[" + index + "].lightPos", sceneObjects[i].Position);
                        PBRShader.SetFloat("pointLights[" + index + "].strength", sceneObjects[i].Light.strength);
                        index += 1;
                    }
                }
                
                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.Texture2D, depthMap);
                PBRShader.SetInt("shadowMap", 4);

                UpdateMatrices();
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    if (sceneObjects[i].Type == SceneObjectType.Mesh)
                    {
                        sceneObjects[i].Mesh.meshShader.SetInt("smoothShading", Convert.ToInt32(sceneObjects[i].Mesh.smoothShading));
                        sceneObjects[i].Mesh.Material.SetShaderUniforms(PBRShader);
                        sceneObjects[i].Render();
                    }
                }

                lightShader.Use();
                for (int i = 0; i < sceneObjects.Count; i++) if (sceneObjects[i].Type == SceneObjectType.Light) sceneObjects[i].Render(pitch, yaw, camera);

                // Render selected sceneobject infront of everything and dont write to color buffer
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.ColorMask(false, false, false, false);
                GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                GL.StencilMask(0xFF);

                if (sceneObjects[selectedSceneObject].Type == SceneObjectType.Mesh)
                {
                    PBRShader.Use();
                    sceneObjects[selectedSceneObject].Mesh.meshShader.SetInt("smoothShading", Convert.ToInt32(sceneObjects[selectedSceneObject].Mesh.smoothShading));
                    sceneObjects[selectedSceneObject].Mesh.Material.SetShaderUniforms(PBRShader);
                    sceneObjects[selectedSceneObject].Render();
                }
                else if (sceneObjects[selectedSceneObject].Type == SceneObjectType.Light)
                {
                    lightShader.Use();
                    sceneObjects[selectedSceneObject].Render(pitch, yaw, camera);
                }

                GL.ColorMask(true, true, true, true);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.CullFace);
                GL.Disable(EnableCap.StencilTest);
            }

            if (numAOSamples != previousAOSamples)
            {
                Postprocessing.GenNoise(numAOSamples);
                previousAOSamples = numAOSamples;
            }

            // Use different shaders for engine and viewport effects
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            Postprocessing.RenderDefaultRect(ref postprocessShader, framebufferTexture, depthStencilTexture, gPosition, gNormal, projectionMatrix, numAOSamples);
            Postprocessing.RenderSSAOrect(ref SSAOblurShader, framebufferTexture);
            Postprocessing.RenderOutlineRect(ref outlineShader, framebufferTexture, depthStencilTexture, SSAOblur);
            Postprocessing.RenderFXAARect(ref fxaaShader, framebufferTexture);

            // Resize depth and framebuffer texture if size has changed
            Framebuffers.ResizeFBO(viewportSize, previousViewportSize, ClientSize, ref framebufferTexture, ref depthStencilTexture, ref gPosition, ref gNormal, ref SSAOblur);
            GL.Finish();

            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError) Console.WriteLine("OpenGL Error: " + error.ToString());

            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Show all the ImGUI windows
            ImGuiController.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();

            ImGuiWindows.Viewport(framebufferTexture, depthMap, out viewportSize, out viewportPos, out viewportHovered, shadowRes);
            if (!fullscreen)
            {
                ImGuiWindows.Header();
                ImGuiWindows.SmallStats(viewportSize, viewportPos, fps, ms, count_Meshes, count_PointLights, triangleCount, camera.direction, yaw, pitch);
                ImGuiWindows.ShadowView(depthMap);
                ImGuiWindows.MaterialEditor(ref sceneObjects, ref PBRShader, selectedSceneObject);
                ImGuiWindows.Outliner(ref sceneObjects, ref selectedSceneObject, ref triangleCount);
                ImGuiWindows.ObjectProperties(ref sceneObjects, selectedSceneObject);
                ImGuiWindows.Settings(ref camera.speed, ref vsyncOn, ref ShowDepth_Stencil, ref shadowRes, ref depthMap, ref direction, ref ambient, ref shadowFactor, ref numAOSamples, ref PBRShader, ref postprocessShader, ref outlineShader, ref fxaaShader, ref SSAOblurShader);
            }
            
            // Quick menu
            if (IsKeyDown(Keys.LeftShift) && IsKeyPressed(Keys.Space))
            {
                SN.Vector2 mousePos = new SN.Vector2(MouseState.Position.X, MouseState.Position.Y);
                ImGui.SetNextWindowPos(mousePos);
                ImGui.OpenPopup("QuickMenu");
            }

            // Toggle fullscreen
            if (IsKeyDown(Keys.LeftControl) && IsKeyPressed(Keys.Space)) fullscreen = ToggleBool(fullscreen);

            if (ImGui.BeginPopup("QuickMenu", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Quick Menu");
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                Random rnd = new Random();
                int randomNum = rnd.Next(1, 101);

                if (ImGui.BeginMenu("Mesh"))
                {
                    if (ImGui.MenuItem("Cube"))
                    {
                        Mesh cube = new Mesh(cubeVertexData, cubeIndices, PBRShader, true, true, emissiveMaterial);
                        //cube.position = Raycast(camera.position, 5);
                        SceneObject _cube = new("Cube" + randomNum, SceneObjectType.Mesh, cube);
                        sceneObjects.Add(_cube);

                        selectedSceneObject = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                    if (ImGui.MenuItem("Sphere"))
                    {
                        Mesh sphere = new Mesh(sphereVertexData, sphereIndices, PBRShader, true, true, defaultMat);
                        //sphere.position = Raycast(camera.position, 5);
                        SceneObject _sphere = new("Sphere" + randomNum, SceneObjectType.Mesh, sphere);
                        sceneObjects.Add(_sphere);

                        selectedSceneObject = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                    if (ImGui.MenuItem("Plane"))
                    {
                        Mesh plane = new Mesh(planeVertexData, planeIndices, PBRShader, true, true, defaultMat);
                        //plane.position = Raycast(camera.position, 5);
                        SceneObject _plane = new("Plane" + randomNum, SceneObjectType.Mesh, plane);
                        sceneObjects.Add(_plane);

                        selectedSceneObject = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }
                    
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Light"))
                {
                    if (ImGui.MenuItem("Point Light"))
                    {
                        Light light = new Light(lightShader, new(1, 1, 1), 5);
                        SceneObject _light = new("Light" + randomNum, SceneObjectType.Light, null, light);
                        sceneObjects.Add(_light);

                        selectedSceneObject = sceneObjects.Count - 1;
                    }

                    ImGui.EndMenu();
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                if (ImGui.Button("Delete selection") && sceneObjects.Count != 0)
                {
                    sceneObjects[selectedSceneObject].Dispose();
                    sceneObjects.RemoveAt(selectedSceneObject);
                    triangleCount = Game.CalculateTriangles();
                    if (selectedSceneObject != 0) selectedSceneObject -= 1;
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                if (ImGui.TreeNode("Properties")) ImGuiWindows.Properties(ref sceneObjects, selectedSceneObject);

                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                ImGui.EndPopup();
            }

            ImGuiController.Render();

            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;

            SwapBuffers();
        }

        public bool ToggleBool(bool toggleBool)
        {
            bool _bool = false;

            if (toggleBool == true) _bool = false;
            if (toggleBool == false) _bool = true;

            return _bool;
        }

        public static int CalculateTriangles()
        {
            int count = 0;
            foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Type == SceneObjectType.Mesh) count += sceneObject.Mesh.vertexCount / 3;
            
            return count;
        }

        public void UpdateMatrices()
        {
            float aspectRatio = (float)viewportSize.X / viewportSize.Y;
            lightSpaceMatrix = Matrix4.LookAt(direction * 10, new(0, 0, 0), Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 0.1f, 100);
            
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), aspectRatio, 0.1f, 100);
            viewMatrix = Matrix4.LookAt(camera.position, camera.position + camera.direction, Vector3.UnitY);
            PBRShader.SetMatrix4("projection", projectionMatrix);
            PBRShader.SetMatrix4("view", viewMatrix);
            PBRShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);
            lightShader.SetMatrix4("projection", projectionMatrix);
            lightShader.SetMatrix4("view", viewMatrix);
        }

        float MapRange(float value, float inputMin, float inputMax, float outputMin, float outputMax) {
            return ((value - inputMin) / (inputMax - inputMin)) * (outputMax - outputMin) + outputMin;
        }

        Vector3 Raycast(Vector3 origin, float distance)
        {
            // NDS
            float x = MapRange(MousePosition.X, 0, viewportSize.X, -1, 1);
            float y = MapRange(MousePosition.Y, 0, viewportSize.Y, 1, -1);
            float z = 1.0f;
            Vector3 ray_nds = new(x, y, z);

            // 4d Homogeneous Clip Coordinates
            Vector4 ray_clip = new(ray_nds.X, ray_nds.Y, -1.0f, 1.0f);

            // 4d Eye coordinates
            Vector4 ray_eye = ray_clip * Matrix4.Invert(projectionMatrix);
            ray_eye = new(ray_eye.X, ray_eye.Y, -1.0f, 0.0f);

            // 4d World Coordinates
            Vector3 ray_wor = (ray_eye * Matrix4.Invert(viewMatrix)).Xyz;
            ray_wor = Vector3.Normalize(ray_wor);

            Vector3 position = origin + distance * ray_wor;

            return position;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (viewportHovered)
            {
                //if (e.Key == Keys.Escape) Close();
                if (e.Key == Keys.D1)
                {
                    GL.Enable(EnableCap.CullFace);
                    _polygonMode = PolygonMode.Fill;
                }
                if (e.Key == Keys.D2)
                {
                    GL.Disable(EnableCap.CullFace);
                    _polygonMode = PolygonMode.Line;
                }
                if (e.Key == Keys.D3)
                {
                    GL.Disable(EnableCap.CullFace);
                    _polygonMode = PolygonMode.Point;
                }
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            ImGuiController.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            ImGuiController.MouseScroll(e.Offset);
        }
    }
}