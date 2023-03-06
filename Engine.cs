using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ImGuiNET;

using SN = System.Numerics;

using GameEngine.Common;
using GameEngine.Importer;
using GameEngine.Rendering;
using static GameEngine.Rendering.SceneObject;
using GameEngine.ImGUI;

namespace GameEngine
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
                APIVersion = new Version(3, 3)
            })
        {
            CenterWindow();
            viewportSize = this.Size;
            previousViewportSize = viewportSize;
        }

        private bool viewportHovered;

        private Vector2i viewportSize;
        private Vector2i previousViewportSize;
        private Vector2i viewportPos;
        private float pitch = 0.5f, yaw = 0.0f;
        float sensitivity = 0.01f;

        int frameCount = 0;
        double elapsedTime = 0.0, fps = 0.0, ms;

        Vector3 ambient = new(0.05f);
        Vector3 direction = new(1);
        float shadowFactor = 0.75f;
        Material _material;
        Material _material2;
        public Shader defaultShader;
        public Shader lightShader;
        public Shader shadowShader;
        public Shader postprocessShader;
        Matrix4 projectionMatrix;
        Matrix4 viewMatrix;

        Mesh suzanne;
        Mesh floor;
        Mesh cube;
        VertexData[] vertexData;
        int[] indices;
        VertexData[] planeVertexData;
        int[] planeIndices;
        VertexData[] cubeVertexData;
        int[] cubeIndices;
        VertexData[] sphereVertexData;
        int[] sphereIndices;
        int triangleCount = 0;

        Camera camera;
        List<Mesh> Meshes = new List<Mesh>();
        static List<SceneObject> sceneObjects = new List<SceneObject>();
        int selectedMesh = 0;
        Light light;
        Light light2;

        PolygonMode _polygonMode = PolygonMode.Fill;
        private bool vsyncOn = true;

        private ImGuiController ImGuiController;
        int FBO;
        int framebufferTexture;
        int depthTexture;

        int depthMapFBO;
        int depthMap;
        int shadowRes = 2048;

        bool renderShadowMap = false;

        private int VAO;
        private int VBO;

        float[] PPvertices =
        {
            -1f,  1f,
            -1f, -1f,
             1f,  1f,
             1f,  1f,
            -1f, -1f,
             1f, -1f,
        };

        unsafe protected override void OnLoad()
        {
            base.OnLoad();

            MakeCurrent();
            IsVisible = true;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.LineWidth(2);
            GL.PointSize(5);

            FBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            VSync = VSyncMode.Adaptive;

            Framebuffers.SetupFBO(ref framebufferTexture, ref depthTexture, viewportSize);
            Framebuffers.SetupShadowFBO(ref depthMapFBO, ref depthMap, shadowRes);

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1280 / 768, 0.1f, 100);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);

            defaultShader = new Shader("Shaders/mesh.vert", "Shaders/mesh.frag");
            lightShader = new Shader("Shaders/light.vert", "Shaders/light.frag");
            shadowShader = new Shader("Shaders/shadow.vert", "Shaders/shadow.frag");
            postprocessShader = new Shader("Shaders/postprocess.vert", "Shaders/postprocess.frag");

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, PPvertices.Length * sizeof(float), PPvertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(postprocessShader.GetAttribLocation("aPosition"));
            GL.VertexAttribPointer(postprocessShader.GetAttribLocation("aPosition"), 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

            camera = new Camera(new(0, 1, 2), -Vector3.UnitZ, 10);
            _material = new(new(1, 1, 1), 0, 0.2f);
            _material2 = new(new(1, 0.5f, 0), 0, 0.4f);
            _material.SetShaderUniforms(defaultShader);
            _material2.SetShaderUniforms(defaultShader);
            defaultShader.SetVector3("ambient", ambient);
            defaultShader.SetVector3("direction", direction);
            defaultShader.SetFloat("shadowFactor", shadowFactor);

            ModelImporter.LoadModel("Importing/Suzanne.fbx", out vertexData, out indices);
            ModelImporter.LoadModel("Importing/Floor.fbx", out planeVertexData, out planeIndices);  
            ModelImporter.LoadModel("Importing/Cube.fbx", out cubeVertexData, out cubeIndices);
            ModelImporter.LoadModel("Importing/Sphere.fbx", out sphereVertexData, out sphereIndices);
            suzanne = new Mesh(vertexData, indices, defaultShader, true, true, _material);
            suzanne.position = new(0, 2, 0);
            suzanne.rotation = new(-125, 0, 0);

            floor = new Mesh(planeVertexData, planeIndices, defaultShader, true, true, _material);
            floor.position = new(0, 0, 0);
            floor.scale = new(1);
            floor.rotation = new(-90, 0, 0);

            cube = new Mesh(cubeVertexData, cubeIndices, defaultShader, true, true, _material2);
            cube.position = new(3, 1, 0);
            cube.rotation = new(-90, 30, 0);

            light = new Light(lightShader);
            light2 = new Light(lightShader);
            light.position = new(3, 4, -3);
            light2.position = new(-2, 7, -6);

            SceneObject _monkey = new("Monkey", SceneObjectType.Mesh, suzanne);
            SceneObject _plane = new("Floor", SceneObjectType.Mesh, floor);
            SceneObject _cube = new("Cube", SceneObjectType.Mesh, cube);
            SceneObject _light = new("Light1", SceneObjectType.Light, null, light);
            SceneObject _light2 = new("Light2", SceneObjectType.Light, null, light2);
            sceneObjects.Add(_monkey);
            sceneObjects.Add(_plane);
            sceneObjects.Add(_light);
            sceneObjects.Add(_light2);
            sceneObjects.Add(_cube);

            triangleCount = CalculateTriangles();

            ImGuiController = new ImGuiController(viewportSize.X, viewportSize.Y);
            ImGuiWindows.LoadTheme();

            GLFW.MaximizeWindow(WindowPtr);
        }

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
                if (IsKeyDown(Keys.W)) camera.position += moveAmount * camera.direction;
                if (IsKeyDown(Keys.S)) camera.position -= moveAmount * camera.direction;
                if (IsKeyDown(Keys.A)) camera.position -= moveAmount * Vector3.Normalize(Vector3.Cross(camera.direction, Vector3.UnitY));
                if (IsKeyDown(Keys.D)) camera.position += moveAmount * Vector3.Normalize(Vector3.Cross(camera.direction, Vector3.UnitY));
                if (IsKeyDown(Keys.E)) camera.position += moveAmount * Vector3.UnitY;
                if (IsKeyDown(Keys.Q)) camera.position -= moveAmount * Vector3.UnitY;                
            }

            frameCount++;
            elapsedTime += args.Time;
            if (elapsedTime >= 0.1f)
            {
                fps = frameCount / elapsedTime;
                ms = 1000.0 / fps;
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
            // Render shadow scene
            GL.Viewport(0, 0, shadowRes, shadowRes);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Type == SceneObjectType.Mesh) sceneObject.Mesh.meshShader = shadowShader;
            renderShadowMap = true;
            UpdateMatrices();
            foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Type == SceneObjectType.Mesh && sceneObject.Mesh.castShadow == true) sceneObject.Mesh.Render();

            // Render normal scene
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.ClearColor(new Color4(ambient.X, ambient.Y, ambient.Z, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);

            foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Type == SceneObjectType.Mesh) sceneObject.Mesh.meshShader = defaultShader;
            defaultShader.SetVector3("viewPos", camera.position);
            renderShadowMap = false;
            UpdateMatrices();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, depthMap);

            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh)
                {
                    defaultShader.Use();
                    sceneObject.Mesh.meshShader.SetInt("smoothShading", Convert.ToInt32(sceneObject.Mesh.smoothShading));
                    sceneObject.Mesh.Material.SetShaderUniforms(defaultShader);
                    sceneObject.Mesh.Render();
                }
                else if (sceneObject.Type == SceneObjectType.Light)
                {
                    lightShader.Use();
                    sceneObject.Light.Render(camera.position, camera.direction, pitch, yaw);
                }
            }
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            // Post processing here
            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            postprocessShader.Use();
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // Resize framebuffer
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            if (viewportSize != previousViewportSize)
            {
                GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, viewportSize.X, viewportSize.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                GL.BindTexture(TextureTarget.Texture2D, depthTexture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, viewportSize.X, viewportSize.Y, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, IntPtr.Zero);

                UpdateMatrices();
                previousViewportSize = viewportSize;
            }

            ImGuiController.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();

            ImGuiWindows.Header();
            ImGuiWindows.SmallStats(viewportSize, viewportPos, fps, ms, sceneObjects.Count, triangleCount);
            ImGuiWindows.Viewport(framebufferTexture, depthMap, out viewportSize, out viewportPos, out viewportHovered, shadowRes);
            ImGuiWindows.MaterialEditor(ref sceneObjects, ref defaultShader, selectedMesh);
            ImGuiWindows.Outliner(ref sceneObjects, ref selectedMesh, ref triangleCount);
            ImGuiWindows.ObjectProperties(ref sceneObjects, selectedMesh);
            //ImGui.ShowDemoWindow();

            if (IsKeyPressed(Keys.Space))
            {
                SN.Vector2 mousePos = new SN.Vector2(MouseState.Position.X, MouseState.Position.Y) - new SN.Vector2(20, 20);
                ImGui.SetNextWindowPos(mousePos);
                ImGui.OpenPopup("test popup");
            }

            if (ImGui.BeginPopup("test popup", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Add");
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                if (ImGui.BeginMenu("Mesh"))
                {
                    Random rnd = new Random();
                    int randomNum = rnd.Next(1, 101);
                    if (ImGui.MenuItem("Cube"))
                    {
                        Mesh cube = new Mesh(cubeVertexData, cubeIndices, defaultShader, true, true, _material);
                        cube.rotation.X = -90;
                        SceneObject _cube = new("Cube" + randomNum, SceneObjectType.Mesh, cube);
                        sceneObjects.Add(_cube);

                        selectedMesh = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                    if (ImGui.MenuItem("Sphere"))
                    {
                        Mesh sphere = new Mesh(sphereVertexData, sphereIndices, defaultShader, true, true, _material2);
                        sphere.rotation.X = -90;
                        SceneObject _sphere = new("Sphere" + randomNum, SceneObjectType.Mesh, sphere);
                        sceneObjects.Add(_sphere);

                        selectedMesh = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                    if (ImGui.MenuItem("Plane"))
                    {
                        Mesh plane = new Mesh(planeVertexData, planeIndices, defaultShader, true, true, _material);
                        plane.rotation.X = -90;
                        SceneObject _plane = new("Plane" + randomNum, SceneObjectType.Mesh, plane);
                        sceneObjects.Add(_plane);

                        selectedMesh = sceneObjects.Count - 1;

                        triangleCount = CalculateTriangles();
                    }
                    
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Light"))
                {
                    ImGui.EndMenu();
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, 5));

                if (ImGui.Button("Remove Selected") && sceneObjects.Count != 0)
                {
                    sceneObjects.RemoveAt(selectedMesh);
                    triangleCount = Game.CalculateTriangles();
                    if (selectedMesh != 0) selectedMesh -= 1;
                }

                ImGui.EndPopup();
            }

            ImGuiWindows.Settings(ref vsyncOn, ref shadowRes, ref depthMap, ref direction, ref ambient, ref shadowFactor, ref defaultShader);
            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;

            ImGuiController.Render();

            SwapBuffers();
        }

        public static int CalculateTriangles()
        {
            int count = 0;
            foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Type == SceneObjectType.Mesh) count += sceneObject.Mesh.vertexCount / 3;
            
            return count;
        }

        public void FocusObject()
        {
            Vector3 targetPosition = suzanne.position;
            Vector3 direction = Vector3.Normalize(targetPosition - camera.position);

            camera.direction = direction;
        }

        public void UpdateMatrices()
        {
            float aspectRatio = (float)viewportSize.X / viewportSize.Y;
            Matrix4 lightSpaceMatrix = Matrix4.LookAt(direction * 10, new(0, 0, 0), Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 0.1f, 100);
            
            if (!renderShadowMap)
            {
                projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), aspectRatio, 0.1f, 100);
                viewMatrix = Matrix4.LookAt(camera.position, camera.position + camera.direction, Vector3.UnitY);
                defaultShader.SetMatrix4("projection", projectionMatrix);
                defaultShader.SetMatrix4("view", viewMatrix);
                defaultShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);
                lightShader.SetMatrix4("projection", projectionMatrix);
                lightShader.SetMatrix4("view", viewMatrix);
            }
            else
            {
                shadowShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (viewportHovered)
            {
                if (e.Key == Keys.F) FocusObject();

                if (e.Key == Keys.Escape) Close();
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