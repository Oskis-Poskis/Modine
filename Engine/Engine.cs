using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ImGuiNET;

using Modine.Common;
using Modine.Rendering;
using Modine.Compute;
using Modine.ImGUI;

using static Modine.Rendering.SceneObject;
using static Modine.Compute.RenderTexture;
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
                APIVersion = new  Version(4, 3),
                Flags = ContextFlags.Debug
            })
        {
            CenterWindow();
            viewportSize = this.Size;
            previousViewportSize = viewportSize;

            PBRShader = new Shader("Engine/Shaders/PBR/mesh.vert", "Engine/Shaders/PBR/mesh.frag");
            shadowShader = new Shader("Engine/Shaders/PBR/shadow.vert", "Engine/Shaders/PBR/shadow.frag");
            lightShader = new Shader("Engine/Shaders/Lights/light.vert", "Engine/Shaders/Lights/light.frag");
            postprocessShader = new Shader("Engine/Shaders/Postprocessing/1_rect.vert", "Engine/Shaders/Postprocessing/postprocess.frag");
            defferedShader = new Shader("Engine/Shaders/Postprocessing/1_rect.vert", "Engine/Shaders/Postprocessing/deffered.frag");
            outlineShader = new Shader("Engine/Shaders/Postprocessing/1_rect.vert", "Engine/Shaders/Postprocessing/outline.frag");
            fxaaShader = new Shader("Engine/Shaders/Postprocessing/1_rect.vert", "Engine/Shaders/Postprocessing/fxaa.frag");

            CompDisplayShader = new("Compute/fbo.vert", "Compute/fbo.frag");
            RaytracingShader = new ComputeShader("Compute/raytracer.comp");
        }

        private bool viewportHovered;
        public bool showOutlines = true;

        private Vector2i viewportPos, viewportSize, previousViewportSize;

        Vector3 ambient = new (0.08f);
        Vector3 SunDirection = new(1);
        float shadowFactor = 0.75f;
        
        Material defaultMat, krissVectorMat;
        public static List<Material> Materials = new  List<Material>();
        public static Shader PBRShader, lightShader, shadowShader;
        public Shader postprocessShader, defferedShader, outlineShader, fxaaShader;
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
        int selectedTexture = 0;
        int framebufferTexture, depthStencilTexture, gAlbedo, gNormal, gMetallicRough, gPosition;
        int FBO;

        public static int numAOSamples = 16;
        public static int previousAOSamples = numAOSamples;

        int depthMapFBO;
        int depthMap;
        int shadowRes = 2048;

        FPScounter FPScounter = new ();

        Shader CompDisplayShader;
        ComputeShader RaytracingShader;
        int compTexture;
        Vector2i compSize = new(1024);

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

            Framebuffers.SetupFBO(ref framebufferTexture, ref depthStencilTexture, ref gAlbedo, ref gNormal, ref gMetallicRough, ref gPosition, viewportSize);
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError) Console.WriteLine("OpenGL Error: " + error.ToString());
            if (status != FramebufferErrorCode.FramebufferComplete) Console.WriteLine($"Framebuffer is incomplete: {status}");

            Framebuffers.SetupShadowFBO(ref depthMapFBO, ref depthMap, shadowRes);
            Postprocessing.SetupPPRect(ref postprocessShader);

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1, 0.5f, 100);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitY, new(0, 1, 0));
            camera = new Camera(new(0, 0, 2), -Vector3.UnitZ, 6);
            defaultMat = new ("Default", new (0.8f), 0, 0.3f, 0.0f, PBRShader);

            defferedShader.SetVector3("ambient", ambient);
            defferedShader.SetVector3("direction", SunDirection);
            PBRShader.SetVector3("direction", SunDirection);
            defferedShader.SetFloat("shadowFactor", shadowFactor);

            krissVectorMat = new ("VectorMat", new(1), 1, 1, 0, PBRShader,
                Texture.LoadFromFile("Assets/Resources/1_Albedo.png"),
                Texture.LoadFromFile("Assets/Resources/1_Roughness.png"),
                Texture.LoadFromFile("Assets/Resources/1_Metallic.png"),
                Texture.LoadFromFile("Assets/Resources/1_Normal.png"));
            ModelImporter.LoadModel("Assets/Models/Suzanne.fbx", out vectorData, out vectorIndicies);

            ModelImporter.LoadModel("Assets/Models/TestRoom.fbx", out vertexData, out indices);
            Room = new  Mesh(vertexData, indices, PBRShader, true, 0);

            SceneObject _room = new (PBRShader, "Room", Room);

            krissVector = new (vectorData, vectorIndicies, PBRShader, true, 1);
            SceneObject _vector = new (PBRShader, EngineUtility.NewName(sceneObjects, "Vector"), krissVector);
            _vector.Scale = new (0.3f);

            sceneObjects.Add(_vector);

            Materials.Add(defaultMat);
            Materials.Insert(1, krissVectorMat);

            int numRows = 0;
            int numCols = 0;
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
                    SceneObject _light = new(lightShader, EngineUtility.NewName(sceneObjects, "Light"), light);
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

            triangleCount = EngineUtility.CalculateTriangles(sceneObjects);

            ImGuiController = new  ImGuiController(viewportSize.X, viewportSize.Y);
            ImGuiWindows.LoadTheme();

            SetupCompRect(ref compTexture, compSize);
            CreateResourceMemory(sceneObjects[0].Mesh.vertexData, sceneObjects[0].Mesh.indices);
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
        Vector3 newPosWS = Vector3.Zero;
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
                if (IsKeyDown(Keys.LeftControl) && IsKeyPressed(Keys.Space)) fullscreen = EngineUtility.ToggleBool(fullscreen);
                
                if (IsKeyDown(Keys.LeftAlt) && IsKeyPressed(Keys.G)) sceneObjects[selectedSceneObject].Position = Vector3.Zero;
                if (IsKeyPressed(Keys.G) && !IsKeyDown(Keys.LeftAlt))
                {
                    if (isObjectPickedUp) return;
                    originalPosition = sceneObjects[selectedSceneObject].Position;
                    originalDistance = Vector3.Distance(camera.position, originalPosition);
                    isObjectPickedUp = true;
                    grabX = false; grabY = false; grabZ = false;
                }

                if (isObjectPickedUp)
                {
                    float x = EngineUtility.MapRange(MousePosition.X, 0, viewportSize.X, -1, 1);
                    float y = EngineUtility.MapRange(MousePosition.Y, 0, viewportSize.Y, 1, -1);
                    Vector3 ray_nds = new(x, y, 1.0f);
                    Vector4 ray_clip = new(ray_nds.X, ray_nds.Y, -1.0f, 1.0f);
                    Vector4 ray_eye = ray_clip * Matrix4.Invert(projectionMatrix);
                    ray_eye = new(ray_eye.X, ray_eye.Y, -1.0f, 1.0f);
                    Vector3 ray_wor = (ray_eye * Matrix4.Invert(viewMatrix)).Xyz;

                    newPosWS = Raycast(ray_wor, originalDistance);

                    if (IsMouseButtonPressed(MouseButton.Button1)) isObjectPickedUp = false;

                    if (IsKeyPressed(Keys.X))
                    {
                        grabX = EngineUtility.ToggleBool(grabX);
                        grabY = false;
                        grabZ = false;
                    }

                    if (IsKeyPressed(Keys.Y))
                    {
                        grabX = false;
                        grabY = EngineUtility.ToggleBool(grabY);
                        grabZ = false;
                    }

                    if (IsKeyPressed(Keys.Z))
                    {
                        grabX = false;
                        grabY = false;
                        grabZ = EngineUtility.ToggleBool(grabZ);
                    }

                    if (IsKeyPressed(Keys.Escape))
                    {
                        sceneObjects[selectedSceneObject].Position = originalPosition;
                        isObjectPickedUp = false;
                        return;
                    }

                    if (grabX) sceneObjects[selectedSceneObject].Position = new(newPosWS.X, originalPosition.Y, originalPosition.Z);
                    if (grabY) sceneObjects[selectedSceneObject].Position = new(originalPosition.X, newPosWS.Y, originalPosition.Z);
                    if (grabZ) sceneObjects[selectedSceneObject].Position = new(originalPosition.X, originalPosition.Y, newPosWS.Z);
                    if (!grabX && !grabY && !grabZ) sceneObjects[selectedSceneObject].Position = newPosWS;
                }
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            RenderScene(args.Time);
            FPScounter.Count(args);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            RenderScene(0.017f);

            ImGuiController.WindowResized(e.Width, e.Height);
        }

        bool showQuickMenu = false;

        public void RenderScene(double time)
        {
            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;
            RenderFuncs.RenderShadowScene(shadowRes, ref depthMapFBO, lightSpaceMatrix, ref sceneObjects, shadowShader);

            // Render normal scene
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.DrawBuffers(6, new  DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3, DrawBuffersEnum.ColorAttachment4, DrawBuffersEnum.ColorAttachment5 });
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
                lightSpaceMatrix = Matrix4.LookAt(SunDirection * 10, Vector3.Zero, Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 0.1f, 100);
                projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), aspectRatio, 0.1f, 100);
                viewMatrix = Matrix4.LookAt(camera.position, camera.position + camera.direction, Vector3.UnitY);

                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.Texture2D, depthMap);
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

            // Use different shaders for engine and viewport effects
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            
            defferedShader.Use();
            defferedShader.SetInt("countPL", count_PointLights);
            defferedShader.SetVector3("viewPos", camera.position); 
            Postprocessing.RenderDefferedRect(ref defferedShader, depthStencilTexture, gAlbedo, gNormal, gPosition, gMetallicRough);
            Postprocessing.RenderPPRect(ref postprocessShader, framebufferTexture);
            if (showOutlines) Postprocessing.RenderOutlineRect(ref outlineShader, framebufferTexture, depthStencilTexture);
            Postprocessing.RenderFXAARect(ref fxaaShader, framebufferTexture);
            Framebuffers.ResizeFBO(viewportSize, previousViewportSize, ref framebufferTexture, ref depthStencilTexture, ref gAlbedo, ref gNormal, ref gMetallicRough, ref gPosition);

            lightShader.Use();
            lightShader.SetMatrix4("projection", projectionMatrix);
            lightShader.SetMatrix4("view", viewMatrix);
            for (int i = 0; i < sceneObjects.Count; i++) if (sceneObjects[i].Type == SceneObjectType.Light) sceneObjects[i].Render(camera);

            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Show all the ImGUI windows
            ImGuiController.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();
            int[] textures = new int[]{framebufferTexture, gAlbedo, gPosition, gNormal, compTexture};
            ImGuiWindows.Header(FPScounter.fps, FPScounter.ms, count_Meshes, ref selectedTexture);
            ImGuiWindows.Viewport(textures[selectedTexture], depthMap, out viewportSize, out viewportPos, out viewportHovered, shadowRes);
            if (showStats) ImGuiWindows.SmallStats(viewportSize, viewportPos, FPScounter.fps, FPScounter.ms, count_Meshes, count_PointLights, triangleCount);

            RaytracingShader.Use();
            RaytracingShader.SetVector3("camera.direction", camera.direction);
            RaytracingShader.SetVector3("camera.position", camera.position);
            RaytracingShader.SetInt("triangleCount", sceneObjects[0].Mesh.indices.Length);
            ResizeTexture(viewportSize, ref compTexture, PixelInternalFormat.Rgba32f, PixelFormat.Rgba);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindImageTexture(0, compTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

            GL.DispatchCompute(Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.X / 8)), Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.Y / 8)), 1);            
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

            // Quick menu
            if (IsKeyDown(Keys.LeftShift) && IsKeyPressed(Keys.Space))
            {
                showQuickMenu = EngineUtility.ToggleBool(showQuickMenu);
                if (showQuickMenu) ImGui.SetNextWindowPos(new(MouseState.Position.X, MouseState.Position.Y));
            }
            if (showQuickMenu) ImGuiWindows.QuickMenu(ref sceneObjects, ref selectedSceneObject, ref showQuickMenu, ref triangleCount);
            
            if (!fullscreen)
            {
                // ImGuiWindows.AssetBrowser();
                ImGuiWindows.MaterialEditor(ref sceneObjects, ref PBRShader, selectedSceneObject, ref Materials);
                ImGuiWindows.Outliner(ref sceneObjects, ref selectedSceneObject, ref triangleCount);
                ImGuiWindows.ObjectProperties(ref sceneObjects, selectedSceneObject);
                ImGuiWindows.Settings(ref camera.speed, ref vsyncOn, ref showOutlines, ref showStats, ref shadowRes, ref depthMap, ref SunDirection, ref ambient, ref shadowFactor, ref numAOSamples, ref defferedShader, ref postprocessShader, ref outlineShader, ref fxaaShader, ref PBRShader);
            }

            ImGuiController.Render();
            SwapBuffers();

            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError) Console.WriteLine("OpenGL Error: " + error.ToString());
        }

        Vector3 Raycast(Vector3 origin, float distance)
        {
            // NDS
            float x = EngineUtility.MapRange(MousePosition.X, 0, viewportSize.X, -1, 1);
            float y = EngineUtility.MapRange(MousePosition.Y, 0, viewportSize.Y, 1, -1);
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