using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ImGuiNET;

using GameEngine.Common;
using GameEngine.Importer;
using GameEngine.Rendering;
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

        Material _material;
        public Shader defaultShader;
        public Shader lightShader;
        public Shader shadowShader;
        Matrix4 projectionMatrix;
        Matrix4 viewMatrix;

        VertexData[] vertexData;
        int[] indices;
        VertexData[] vertexData2;
        int[] indices2;
        int triangleCount = 0;

        Camera camera;
        Mesh suzanne;
        Mesh floor;
        List<Mesh> Meshes = new List<Mesh>();
        int selectedMesh = 0;
        Light light;
        Light light2;

        PolygonMode _polygonMode = PolygonMode.Fill;
        private bool vsyncOn = true;

        private ImGuiController _controller;
        int FBO;
        int framebufferTexture;
        int depthTexture;

        int depthMapFBO;
        int depthMap;
        int shadowRes = 2048;

        bool renderShadowMap = false;

        protected override void OnLoad()
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

            // Color Texture
            framebufferTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, (int)viewportSize.X, (int)viewportSize.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            // Attach color to FBO
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, framebufferTexture, 0);

            // Depth Texture
            depthTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, (int)viewportSize.X, (int)viewportSize.X, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            // Attach Depth to FBO
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0);



            depthMapFBO = GL.GenFramebuffer();

            depthMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthMap);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, shadowRes, shadowRes, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthMap, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);



            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1280 / 768, 0.1f, 100);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
            //projectionMatrix = Matrix4.CreateOrthographic(1280, 778, 0.1f, 100);

            defaultShader = new Shader("Shaders/mesh.vert", "Shaders/mesh.frag");
            lightShader = new Shader("Shaders/light.vert", "Shaders/light.frag");
            shadowShader = new Shader("Shaders/shadow.vert", "Shaders/shadow.frag");

            camera = new Camera(new(0, 1, 2), -Vector3.UnitZ, 10);
            _material = new(new(1, 1, 1), 0, 0.2f);
            _material.SetShaderUniforms(defaultShader);

            ModelImporter.LoadModel("Importing/Suzanne.fbx", out vertexData, out indices);
            suzanne = new Mesh("Suzanne", vertexData, indices, defaultShader, true, _material);
            suzanne.position = new(0, 2, 0);
            suzanne.rotation = new(-90, 0, 0);

            ModelImporter.LoadModel("Importing/Floor.fbx", out vertexData2, out indices2);
            floor = new Mesh("Floor", vertexData2, indices2, defaultShader, true, _material);
            floor.position = new(0, 0, 0);
            floor.rotation = new(-90, 0, 0);

            Meshes.Add(suzanne);
            Meshes.Add(floor);

            /*
            int amount = 5;
            for (int z = 0; z < amount; z++)
            {
                for (int y = 0; y < amount; y++)
                {
                    for (int x = 0; x < amount; x++)
                    {
                        int index = z * amount * amount + y * amount + x;
                        Meshes.Add(new Mesh("Monkey_" + index, vertexData, indices, defaultShader, true, _material));
                        Meshes[index].position = new Vector3(x * 3, y * 3, z * -3);
                        Meshes[index].rotation = new Vector3(-90, 0, 0);
                    }
                }
            }
            */
            
            foreach (Mesh mesh in Meshes) triangleCount += mesh.vertexCount / 3;

            light = new Light(lightShader);
            light.position = new(3, 4, -3);
            light2 = new Light(lightShader);
            light2.position = new(-2, 7, -6);

            _controller = new ImGuiController(viewportSize.X, viewportSize.Y);
            ImGUICommands.LoadTheme();
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
                if (IsKeyDown(Keys.Space) | IsKeyDown(Keys.E)) camera.position += moveAmount * Vector3.UnitY;
                if (IsKeyDown(Keys.LeftShift) | IsKeyDown(Keys.Q)) camera.position -= moveAmount * Vector3.UnitY;
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

            _controller.WindowResized(e.Width, e.Height);
        }

        public void RenderScene(double time)
        {
            // Render shadow scene
            GL.Viewport(0, 0, shadowRes, shadowRes);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            GL.CullFace(CullFaceMode.Front);
            foreach (Mesh mesh in Meshes) mesh.meshShader = shadowShader;
            renderShadowMap = true;
            UpdateMatrices();

            foreach (Mesh mesh in Meshes) mesh.Render();

            // Render normal scene
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.ClearColor(new Color4(0.05f, 0.05f, 0.05f, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
            GL.CullFace(CullFaceMode.Back);
            foreach (Mesh mesh in Meshes) mesh.meshShader = defaultShader;
            defaultShader.SetVector3("viewPos", camera.position);
            renderShadowMap = false;
            UpdateMatrices();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, depthMap);
            foreach (Mesh mesh in Meshes) mesh.Render();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            light.Render(camera.position, camera.direction, pitch, yaw);
            light2.Render(camera.position, camera.direction, pitch, yaw);

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

            _controller.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();

            ImGUICommands.Header();
            ImGUICommands.SmallStats(viewportSize, viewportPos, yaw, pitch, fps, ms, Meshes.Count, triangleCount);
            ImGUICommands.Viewport(framebufferTexture, depthMap, out viewportSize, out viewportPos, out viewportHovered, shadowRes);
            ImGUICommands.MaterialEditor(ref _material, ref defaultShader, ref suzanne);
            ImGUICommands.Outliner(Meshes, ref selectedMesh);
            //ImGui.ShowDemoWindow();

            ImGUICommands.Settings(ref vsyncOn);
            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;

            _controller.Render();

            SwapBuffers();
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
            Matrix4 lightSpaceMatrix = Matrix4.LookAt(new(10, 10, 10), new(0, 0, 0), Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 0.1f, 100);
            
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

            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _controller.MouseScroll(e.Offset);
        }
    }
}