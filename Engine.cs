using SN = System.Numerics;
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

        private Vector2i viewportSize;
        private Vector2i previousViewportSize;
        private Vector2i viewportPos;
        private float pitch = 0.5f, yaw = 0.0f;
        float sensitivity = 0.01f;

        int frameCount = 0;
        double elapsedTime = 0.0, fps = 0.0, ms;

        public Shader defaultShader;
        public Shader lightShader;
        Matrix4 projectionMatrix;

        VertexData[] vertexData;
        int[] indices;
        VertexData[] vertexData2;
        int[] indices2;

        Camera camera;
        Mesh suzanne;
        List<Mesh> Meshes = new List<Mesh>();
        Light light;
        Light light2;

        PolygonMode _polygonMode = PolygonMode.Fill;

        private ImGuiController _controller;
        int FBO;
        int framebufferTexture;
        int depthTexture;

        protected override void OnLoad()
        {
            base.OnLoad();

            MakeCurrent();
            IsVisible = true;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
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

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1280 / 768, 0.1f, 100);

            defaultShader = new Shader("Shaders/mesh.vert", "Shaders/mesh.frag");
            lightShader = new Shader("Shaders/light.vert", "Shaders/light.frag");

            ModelImporter.LoadModel("Importing/Suzanne.fbx", out vertexData, out indices);
            ModelImporter.LoadModel("Importing/floor.fbx", out vertexData2, out indices2);

            camera = new Camera(new(0, 1, 2), -Vector3.UnitZ, 10);
            Material material;

            suzanne = new Mesh(vertexData, indices, defaultShader, true, material);
            suzanne.position = new(0, 2, 0);
            suzanne.rotation = new(-90, 0, 0);
            suzanne.scale = new(1);

            Meshes.Add(suzanne);

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

            if (IsKeyDown(Keys.W)) camera.position += moveAmount * camera.direction;
            if (IsKeyDown(Keys.S)) camera.position -= moveAmount * camera.direction;
            if (IsKeyDown(Keys.A)) camera.position -= moveAmount * Vector3.Normalize(Vector3.Cross(camera.direction, Vector3.UnitY));
            if (IsKeyDown(Keys.D)) camera.position += moveAmount * Vector3.Normalize(Vector3.Cross(camera.direction, Vector3.UnitY));
            if (IsKeyDown(Keys.Space) | IsKeyDown(Keys.E)) camera.position += moveAmount * Vector3.UnitY;
            if (IsKeyDown(Keys.LeftShift) | IsKeyDown(Keys.Q)) camera.position -= moveAmount * Vector3.UnitY;

            frameCount++;
            elapsedTime += args.Time;
            if (elapsedTime >= 1.0)
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
            RenderScene(args);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            //RenderScene();

            _controller.WindowResized(e.Width, e.Height);
        }

        public void RenderScene(FrameEventArgs args)
        {
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            GL.ClearColor(new Color4(0.05f, 0.05f, 0.05f, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
            foreach (Mesh mesh in Meshes) mesh.Render(camera.position, camera.direction);
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

                UpdateProjectionMatrix(viewportSize.X, viewportSize.Y);
                previousViewportSize = viewportSize;
            }

            _controller.Update(this, (float)args.Time);
            ImGui.DockSpaceOverViewport();

            ImGUICommands.Header();
            ImGUICommands.SmallStats(viewportSize, viewportPos, fps, ms);
            ImGUICommands.Viewport(framebufferTexture, out viewportSize, out viewportPos);

            _controller.Render();

            SwapBuffers();
        }

        public void Focus()
        {
            Vector3 targetPosition = suzanne.position;
            Vector3 direction = Vector3.Normalize(targetPosition - camera.position);

            camera.direction = direction;
        }

        public void UpdateProjectionMatrix(int width, int height)
        {
            float aspectRatio = (float)width / height;
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), aspectRatio, 0.1f, 100);
            defaultShader.SetMatrix4("projection", projectionMatrix);
            lightShader.SetMatrix4("projection", projectionMatrix);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Keys.F) Focus();

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

            if (e.Key == Keys.N) foreach (Mesh mesh in Meshes) mesh.smoothShading = true;
            if (e.Key == Keys.M) foreach (Mesh mesh in Meshes) mesh.smoothShading = false;
        }
    }
}