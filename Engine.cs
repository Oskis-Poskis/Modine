using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using SN = System.Numerics;
using ImGuiNET;

using Modine.Common;
using Modine.Rendering;
using Modine.ImGUI;

using static Modine.Rendering.SceneObject;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace Modine
{
    class Game : GameWindow
    {
        public Game(int width, int height, string title)
            : base(GameWindowSettings.Default, new  NativeWindowSettings()
            {
                Title = title,
                Size = new  Vector2i(width, height),
                WindowBorder = WindowBorder.Resizable,
                StartVisible = false,
                StartFocused = true,
                WindowState = WindowState.Normal,
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new  Version(3, 3),
                Flags = ContextFlags.Debug
            })
        {
            CenterWindow();
            viewportSize = this.Size;
            previousViewportSize = viewportSize;

            PBRShader = new  Shader("Shaders/PBR/mesh.vert", "Shaders/PBR/mesh.frag");
            shadowShader = new  Shader("Shaders/PBR/shadow.vert", "Shaders/PBR/shadow.frag");
            lightShader = new  Shader("Shaders/Lights/light.vert", "Shaders/Lights/light.frag");
            postprocessShader = new  Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/postprocess.frag");
            defferedShader = new Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/deffered.frag");
            outlineShader = new  Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/outline.frag");
            fxaaShader = new  Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/fxaa.frag");
            SSAOblurShader = new  Shader("Shaders/Postprocessing/1_rect.vert", "Shaders/Postprocessing/SSAOblur.frag");
        }

        private bool viewportHovered;
        public bool ShowDepth_Stencil = false;

        private Vector2i viewportPos, viewportSize, previousViewportSize;

        Vector3 ambient = new (0.1f);
        Vector3 SunDirection = new (1);
        float shadowFactor = 0.75f;
        
        Material defaultMat, krissVectorMat;
        public static List<Material> Materials = new  List<Material>();
        public Shader PBRShader, lightShader, shadowShader;
        public Shader postprocessShader, defferedShader, outlineShader, fxaaShader, SSAOblurShader;
        Matrix4 projectionMatrix, viewMatrix, lightSpaceMatrix;

        Mesh krissVector, Room;
        VertexData[] vectorData, vertexData;
        int[] vectorIndicies, indices;
        int triangleCount = 0;

        Camera camera;
        static List<SceneObject> sceneObjects = new  List<SceneObject>();
        public static int selectedSceneObject = 0;
        int count_PointLights, count_Meshes = 0;

        PolygonMode _polygonMode = PolygonMode.Fill;
        private bool vsyncOn = true;
        private bool fullscreen = false;

        private ImGuiController ImGuiController;
        int framebufferTexture, depthStencilTexture, gAlbedo, gPosition, gNormal, gMetallicRough, SSAOblur;
        int FBO;

        public static int numAOSamples = 16;
        public static int previousAOSamples = numAOSamples;

        int depthMapFBO;
        int depthMap;
        int shadowRes = 2048;

        FPScounter FPScounter = new ();

        unsafe protected override void OnLoad()
        {
            base.OnLoad();

            MakeCurrent();
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.StencilTest);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.PointSize(5);
            IsVisible = true;

            VSync = VSyncMode.On;

            FBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            Framebuffers.SetupFBO(ref framebufferTexture, ref depthStencilTexture, ref gAlbedo, ref gPosition, ref gNormal, ref gMetallicRough, ref SSAOblur, viewportSize);
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError) Console.WriteLine("OpenGL Error: " + error.ToString());
            if (status != FramebufferErrorCode.FramebufferComplete) Console.WriteLine($"Framebuffer is incomplete: {status}");

            Framebuffers.SetupShadowFBO(ref depthMapFBO, ref depthMap, shadowRes);
            Postprocessing.SetupPPRect(ref postprocessShader);

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1, 0.1f, 100);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, new (0, 1, 0));
            camera = new  Camera(new (0, 0, 2), -Vector3.UnitZ, 6);
            defaultMat = new ("Default", new (0.8f), 0, 0.3f, 0.0f, PBRShader);

            defferedShader.SetVector3("ambient", ambient);
            defferedShader.SetVector3("direction", SunDirection);
            defferedShader.SetFloat("shadowFactor", shadowFactor);

            krissVectorMat = new ("VectorMat", new (1), 1, 1, 0, PBRShader,
                Texture.LoadFromFile("Resources/1_Albedo.png"),
                Texture.LoadFromFile("Resources/1_Roughness.png"),
                Texture.LoadFromFile("Resources/1_Metallic.png"),
                Texture.LoadFromFile("Resources/1_Normal.png"));
            ModelImporter.LoadModel("Resources/KrissVector.fbx", out vectorData, out vectorIndicies);

            ModelImporter.LoadModel("Importing/TestRoom.fbx", out vertexData, out indices);
            Room = new  Mesh(vertexData, indices, PBRShader, true, 0);

            Postprocessing.GenNoise(numAOSamples);

            SceneObject _room = new (PBRShader, "Room", Room);

            krissVector = new (vectorData, vectorIndicies, PBRShader, true, 1);
            SceneObject _vector = new (PBRShader, NewName("Vector"), krissVector);
            _vector.Scale = new (0.3f);

            sceneObjects.Add(_vector);

            Materials.Add(defaultMat);
            Materials.Insert(1, krissVectorMat);

            int numRows = 5;
            int numCols = 5;
            int spacing = 5;
            int startX = -((numCols - 1) * spacing) / 2;
            int startY = -((numRows - 1) * spacing) / 2;

            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    int x = startX + col * spacing;
                    int z = startY + row * spacing;
                    Light light = new(lightShader, GetRandomBrightColor(), 1);
                    SceneObject _light = new(lightShader, NewName("Light"), light);
                    _light.Position.X = x;
                    _light.Position.Z = z;
                    _light.Position.Y = 2;

                    sceneObjects.Add(_light);
                }
            }

            count_Meshes = 0;
            count_PointLights = 0;
            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) count_Meshes += 1;
                else if (sceneObject.Type == SceneObjectType.Light) count_PointLights += 1;
            }

            triangleCount = CalculateTriangles();

            ImGuiController = new  ImGuiController(viewportSize.X, viewportSize.Y);
            ImGuiWindows.LoadTheme();

            GLFW.MaximizeWindow(WindowPtr);
        }

        public static Vector3 GetRandomBrightColor()
        {
            Random rand = new Random();
            float r = (float)rand.NextDouble(); // random value between 0 and 1
            float g = (float)rand.NextDouble();
            float b = (float)rand.NextDouble();
            // Make sure at least two of the three color components are greater than 0.5
            int numComponentsOverHalf = (r > 0.5f ? 1 : 0) + (g > 0.5f ? 1 : 0) + (b > 0.5f ? 1 : 0);
            while (numComponentsOverHalf < 2)
            {
                r = (float)rand.NextDouble();
                g = (float)rand.NextDouble();
                b = (float)rand.NextDouble();
                numComponentsOverHalf = (r > 0.5f ? 1 : 0) + (g > 0.5f ? 1 : 0) + (b > 0.5f ? 1 : 0);
            }
            return new Vector3(r, g, b);
        }

        bool isObjectPickedUp = false;
        bool grabX = false, grabY = false, grabZ = false;
        float originalDistance = 0;
        Vector3 originalPosition = Vector3.Zero;
        Vector3 newPosition = Vector3.Zero, newPosition2 = Vector3.Zero;
        public bool showStats;

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsMouseButtonDown(MouseButton.Button2))
            {
                CursorState = CursorState.Grabbed;
                camera.UpdateCamera(MouseState);
            }
            else CursorState = CursorState.Normal;

            float moveAmount = (float)(camera.speed * args.Time);
            if (viewportHovered)
            {
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
                    grabX = false; grabY = false; grabZ = false;
                }

                if (isObjectPickedUp)
                {
                    float x = MapRange(MousePosition.X, 0, viewportSize.X, -1, 1);
                    float y = MapRange(MousePosition.Y, 0, viewportSize.Y, 1, -1);
                    float z = 1.0f;
                    Vector3 ray_nds = new (x, y, z);
                    Vector4 ray_clip = new (ray_nds.X, ray_nds.Y, -1.0f, 1.0f);
                    Vector4 ray_eye = ray_clip * Matrix4.Invert(projectionMatrix);
                    ray_eye = new (ray_eye.X, ray_eye.Y, -1.0f, 1.0f);
                    Vector3 ray_wor = (ray_eye * Matrix4.Invert(viewMatrix)).Xyz;

                    newPosition = Raycast(ray_wor, originalDistance);
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

                    if (grabX) sceneObjects[selectedSceneObject].Position = new (newPosition2.X, originalPosition.Y, originalPosition.Z);
                    if (grabY) sceneObjects[selectedSceneObject].Position = new (originalPosition.X, newPosition2.Y, originalPosition.Z);
                    if (grabZ) sceneObjects[selectedSceneObject].Position = new (originalPosition.X, originalPosition.Y, newPosition2.Z);
                    if (!grabX && !grabY && !grabZ) sceneObjects[selectedSceneObject].Position = newPosition;
                }
            }
            
            FPScounter.Count(args);
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
            RenderClass.RenderShadowScene(shadowRes, ref depthMapFBO, lightSpaceMatrix, ref sceneObjects, shadowShader);

            // Render normal scene
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.ClearColor(new  Color4(ambient.X, ambient.Y, ambient.Z, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);

            count_Meshes = 0; count_PointLights = 0;
            if (sceneObjects.Count > 0)
            {
                foreach (SceneObject sceneObject in sceneObjects)
                {
                    switch (sceneObject.Type)
                    {
                        case SceneObjectType.Mesh:
                            sceneObject.Shader = PBRShader;
                            count_Meshes += 1;
                            break;

                        case SceneObjectType.Light:
                            sceneObject.Light.lightShader = lightShader;
                            count_PointLights += 1;
                            break;
                    }
                }

                // Before drawing all objects
                GL.Enable(EnableCap.StencilTest);
                GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
                GL.StencilMask(0x00);
                
                float aspectRatio = (float)viewportSize.X / viewportSize.Y;
                lightSpaceMatrix = Matrix4.LookAt(SunDirection * 10, new (0, 0, 0), Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 0.1f, 100);
                projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), aspectRatio, 0.1f, 100);
                viewMatrix = Matrix4.LookAt(camera.position, camera.position + camera.direction, Vector3.UnitY);

                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.Texture2D, depthMap);
                GL.DrawBuffers(6, new  DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3, DrawBuffersEnum.ColorAttachment4, DrawBuffersEnum.ColorAttachment5 });
                PBRShader.Use();
                PBRShader.SetMatrix4("projection", projectionMatrix);
                PBRShader.SetMatrix4("view", viewMatrix);
                PBRShader.SetInt("shadowMap", 4);
                PBRShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);

                int index = 0;
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    if (sceneObjects[i].Type == SceneObjectType.Light)
                    {
                        defferedShader.SetVector3("pointLights[" + index + "].lightColor", sceneObjects[i].Light.lightColor);
                        defferedShader.SetVector3("pointLights[" + index + "].lightPos", sceneObjects[i].Position);
                        defferedShader.SetFloat("pointLights[" + index + "].strength", sceneObjects[i].Light.strength);
                        index += 1;
                    }

                    if (sceneObjects[i].Type == SceneObjectType.Mesh)
                    {
                        Materials[sceneObjects[i].Mesh.MaterialIndex].SetShaderUniforms(PBRShader);
                        sceneObjects[i].Render();
                    }
                }

                // Render selected sceneobject infront of everything and dont write to color buffer
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.ColorMask(false, false, false, false);
                GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                GL.StencilMask(0xFF);

                switch (sceneObjects[selectedSceneObject].Type)
                {
                    case SceneObjectType.Mesh:
                        PBRShader.Use();
                        Materials[sceneObjects[selectedSceneObject].Mesh.MaterialIndex].SetShaderUniforms(PBRShader);
                        sceneObjects[selectedSceneObject].Render();
                        break;
                    
                    case SceneObjectType.Light:
                        lightShader.Use();
                        sceneObjects[selectedSceneObject].Render(camera);
                        break;
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
            
            defferedShader.Use();
            defferedShader.SetInt("countPL", count_PointLights);
            defferedShader.SetVector3("viewPos", camera.position); 

            GL.Finish();
            Postprocessing.RenderDefferedRect(ref defferedShader, depthStencilTexture, gAlbedo, gPosition, gNormal, gMetallicRough);
            Postprocessing.RenderOutlineRect(ref outlineShader, framebufferTexture, depthStencilTexture);
            Postprocessing.RenderDefaultRect(ref postprocessShader, framebufferTexture, depthStencilTexture, gPosition, gNormal, projectionMatrix, numAOSamples);
            //Postprocessing.RenderSSAOrect(ref SSAOblurShader, framebufferTexture);
            Postprocessing.RenderFXAARect(ref fxaaShader, framebufferTexture);

            // Draw lights after postprocessing to avoid overlaps
            lightShader.Use();
            lightShader.SetMatrix4("projection", projectionMatrix);
            lightShader.SetMatrix4("view", viewMatrix);
            for (int i = 0; i < sceneObjects.Count; i++) if (sceneObjects[i].Type == SceneObjectType.Light) sceneObjects[i].Render(camera);

            // Resize depth and framebuffer texture if size has changed
            Framebuffers.ResizeFBO(viewportSize, previousViewportSize, ClientSize, ref framebufferTexture, ref depthStencilTexture, ref gAlbedo, ref gPosition, ref gNormal, ref gMetallicRough, ref SSAOblur);

            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError) Console.WriteLine("OpenGL Error: " + error.ToString());

            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Show all the ImGUI windows
            ImGuiController.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();

            ImGuiWindows.Viewport(framebufferTexture, depthMap, out viewportSize, out viewportPos, out viewportHovered, shadowRes);
            if (showStats) ImGuiWindows.SmallStats(viewportSize, viewportPos, FPScounter.fps, FPScounter.ms, count_Meshes, count_PointLights, triangleCount);
            if (!fullscreen)
            {
                ImGuiWindows.Header(FPScounter.fps, FPScounter.ms, count_Meshes);
                ImGuiWindows.AssetBrowser();
                ImGuiWindows.MaterialEditor(ref sceneObjects, ref PBRShader, selectedSceneObject, ref Materials);
                ImGuiWindows.Outliner(ref sceneObjects, ref selectedSceneObject, ref triangleCount);
                ImGuiWindows.ObjectProperties(ref sceneObjects, selectedSceneObject);
                ImGuiWindows.Settings(ref camera.speed, ref vsyncOn, ref ShowDepth_Stencil, ref showStats, ref shadowRes, ref depthMap, ref SunDirection, ref ambient, ref shadowFactor, ref numAOSamples, ref defferedShader, ref postprocessShader, ref outlineShader, ref fxaaShader, ref SSAOblurShader, ref PBRShader);
            }
            
            // Quick menu
            if (IsKeyDown(Keys.LeftShift) && IsKeyPressed(Keys.Space))
            {
                SN.Vector2 mousePos = new  SN.Vector2(MouseState.Position.X, MouseState.Position.Y);
                ImGui.SetNextWindowPos(mousePos);
                ImGui.OpenPopup("QuickMenu");
            }

            // Toggle fullscreen
            if (IsKeyDown(Keys.LeftControl) && IsKeyPressed(Keys.Space)) fullscreen = ToggleBool(fullscreen);

            if (ImGui.BeginPopup("QuickMenu", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));

                // Center text
                float availableWidth = ImGui.GetContentRegionAvail().X;
                ImGui.SetCursorPosX((availableWidth - ImGui.CalcTextSize("Quick Menu").X) / 2);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new  SN.Vector2(10, 2));
                ImGui.Text("Quick Menu");
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new  SN.Vector2(4, 2));

                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));

                if (ImGui.BeginMenu("Mesh"))
                {
                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));

                    if (ImGui.MenuItem("Cube"))
                    {
                        int[] cubeIndices;
                        VertexData[] cubeVertexData;
                        ModelImporter.LoadModel("Importing/Cube.fbx", out cubeVertexData, out cubeIndices);

                        Mesh cube = new  Mesh(cubeVertexData, cubeIndices, PBRShader, true, 0);
                        SceneObject _cube = new (PBRShader, NewName("Cube"), cube);
                        sceneObjects.Add(_cube);

                        selectedSceneObject = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));
                    ImGui.Separator();
                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));

                    if (ImGui.MenuItem("Sphere"))
                    {
                        VertexData[] sphereVertexData;
                        int[] sphereIndices;
                        ModelImporter.LoadModel("Importing/Sphere.fbx", out sphereVertexData, out sphereIndices);

                        Mesh sphere = new  Mesh(sphereVertexData, sphereIndices, PBRShader, true, 0);
                        SceneObject _sphere = new (PBRShader, NewName("Sphere"), sphere);
                        sceneObjects.Add(_sphere);

                        selectedSceneObject = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));
                    ImGui.Separator();
                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));

                    if (ImGui.MenuItem("Plane"))
                    {
                        VertexData[] planeVertexData;
                        int[] planeIndices;
                        ModelImporter.LoadModel("Importing/Floor.fbx", out planeVertexData, out planeIndices);

                        Mesh plane = new  Mesh(planeVertexData, planeIndices, PBRShader, true, 0);
                        SceneObject _plane = new (PBRShader, NewName("Plane"), plane);
                        sceneObjects.Add(_plane);

                        selectedSceneObject = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));
                    ImGui.Separator();
                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));

                    if (ImGui.MenuItem("Import Mesh"))
                    {
                        OpenFileDialog selectFile = new  OpenFileDialog()
                        {
                            Title = "Select File",
                            Filter = "Formats:|*.FBX; *.OBJ;"
                        };
                        selectFile.ShowDialog();
                        string path = selectFile.FileName;

                        if (File.Exists(path))
                        {
                            VertexData[] cubeVertexData;
                            int[] cubeIndices;
                            string name;
                            ModelImporter.LoadModel(path, out cubeVertexData, out cubeIndices, out name);

                            Mesh import = new  Mesh(cubeVertexData, cubeIndices, PBRShader, true, 0);
                            SceneObject _import = new (PBRShader, NewName(name), import);
                            sceneObjects.Add(_import);

                            selectedSceneObject = sceneObjects.Count - 1;
                        }
                    }

                    ImGui.Dummy(new  System.Numerics.Vector2(0f, 3));
                        
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Light"))
                {
                    if (ImGui.MenuItem("Point Light"))
                    {
                        Light light = new  Light(lightShader, new (1, 1, 1), 5);
                        SceneObject _light = new (PBRShader, NewName("Light"), light);
                        sceneObjects.Add(_light);

                        selectedSceneObject = sceneObjects.Count - 1;
                    }

                    ImGui.EndMenu();
                }

                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));

                if (ImGui.Button("Delete selection") && sceneObjects.Count != 0)
                {
                    sceneObjects[selectedSceneObject].Dispose();
                    sceneObjects.RemoveAt(selectedSceneObject);
                    triangleCount = Game.CalculateTriangles();
                    if (selectedSceneObject != 0) selectedSceneObject -= 1;
                }

                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));

                if (ImGui.TreeNode("Properties")) ImGuiWindows.Properties(ref sceneObjects, selectedSceneObject);

                ImGui.Dummy(new  System.Numerics.Vector2(0f, 5));

                ImGui.EndPopup();
            }

            ImGuiController.Render();

            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;

            SwapBuffers();
        }

        string NewName(string baseName)
        {
            int index = 0;
            string nName = baseName;

            // Loop through the existing material names to find a unique name
            while (sceneObjects.Any(m => m.Name == nName))
            {
                index++;
                nName = $"{baseName}.{index.ToString("D3")}";
            }

            return nName;
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

        float MapRange(float value, float inputMin, float inputMax, float outputMin, float outputMax) {
            return ((value - inputMin) / (inputMax - inputMin)) * (outputMax - outputMin) + outputMin;
        }

        Vector3 Raycast(Vector3 origin, float distance)
        {
            // NDS
            float x = MapRange(MousePosition.X, 0, viewportSize.X, -1, 1);
            float y = MapRange(MousePosition.Y, 0, viewportSize.Y, 1, -1);
            float z = 1.0f;
            Vector3 ray_nds = new (x, y, z);

            // 4d Homogeneous Clip Coordinates
            Vector4 ray_clip = new (ray_nds.X, ray_nds.Y, -1.0f, 1.0f);

            // 4d Eye coordinates
            Vector4 ray_eye = ray_clip * Matrix4.Invert(projectionMatrix);
            ray_eye = new (ray_eye.X, ray_eye.Y, -1.0f, 0.0f);

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