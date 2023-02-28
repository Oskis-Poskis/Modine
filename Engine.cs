using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GameEngine.Common;
using GameEngine.Importer;
using GameEngine.Rendering;

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
            windowSize = this.Size;
        }

        private Vector2i windowSize;
        private float pitch = 0.5f, yaw = 0.0f;
        float sensitivity = 0.01f;

        int frameCount = 0;
        double elapsedTime = 0.0, fps = 0.0;

        public Shader defaultShader;
        public Shader lightShader;
        Matrix4 projectionMatrix;

        VertexData[] vertexData;
        int[] indices;
        VertexData[] vertexData2;
        int[] indices2;

        Camera camera;
        Mesh suzanne, floor;
        List<Mesh> Meshes = new List<Mesh>();
        Light light;
        Light light2;

        

        protected override void OnLoad()
        {
            MakeCurrent();
            IsVisible = true;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.PointSize(5);

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1280 / 768, 0.1f, 100);

            defaultShader = new Shader("Shaders/mesh.vert", "Shaders/mesh.frag");
            lightShader = new Shader("Shaders/light.vert", "Shaders/light.frag");

            ModelImporter.LoadModel("ImportClass/Suzanne.fbx", out vertexData, out indices);
            ModelImporter.LoadModel("ImportClass/floor.fbx", out vertexData2, out indices2);

            camera = new Camera(new(0, 1, 2), -Vector3.UnitZ, 10);

            suzanne = new Mesh(vertexData, indices, defaultShader, true);
            suzanne.position = new(0, 2, 0);
            suzanne.rotation = new(-90, 0, 0);
            suzanne.scale = new(1);

            floor = new Mesh(vertexData2, indices2, defaultShader, true);
            floor.position = new(0, 0, 0);
            floor.rotation = new(-90, 0, 0);

            Meshes.Add(suzanne);
            Meshes.Add(floor);

            light = new Light(lightShader);
            light.position = new(3, 4, -3);
            light2 = new Light(lightShader);
            light2.position = new(-2, 7, -6);

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
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

            base.OnUpdateFrame(args);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(new Color4(0.1f, 0.1f, 0.1f, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            frameCount++;
            elapsedTime += args.Time;
            if (elapsedTime >= 1.0)
            {
                fps = frameCount / elapsedTime;
                this.Title = "FPS: " + (int)fps;
                frameCount = 0;
                elapsedTime = 0.0;
            } 

            foreach (Mesh mesh in Meshes) mesh.Render(camera.position, camera.direction);

            light.Render(camera.position, camera.direction, pitch, yaw);
            light2.Render(camera.position, camera.direction, pitch, yaw);

            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            UpdateProjectionMatrix(e.Width, e.Height);

            base.OnResize(e);
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
            if (e.Key == Keys.Escape) Close();
            if (e.Key == Keys.D1)
            {
                GL.Enable(EnableCap.CullFace);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
            if (e.Key == Keys.D2)
            {
                GL.Disable(EnableCap.CullFace);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }
            if (e.Key == Keys.D3)
            {
                GL.Disable(EnableCap.CullFace);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            }

            if (e.Key == Keys.N) foreach (Mesh mesh in Meshes) mesh.smoothShading = true;
            if (e.Key == Keys.M) foreach (Mesh mesh in Meshes) mesh.smoothShading = false;

            base.OnKeyDown(e);
        }
    }
}