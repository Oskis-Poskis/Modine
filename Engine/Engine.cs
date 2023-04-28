using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ImGuiNET;
using imnodesNET;

using System.Runtime.InteropServices;

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
                APIVersion = new Version(4, 3),
                Flags = ContextFlags.Debug
            })
        {
            CenterWindow();
            viewportSize = this.Size;
            previousViewportSize = viewportSize;

            PBRShader = new Shader("Engine/Shaders/PBR/mesh.vert", "Engine/Shaders/PBR/mesh.frag");
            shadowShader = new Shader("Engine/Shaders/PBR/shadow.vert", "Engine/Shaders/PBR/shadow.frag");
            lightShader = new Shader("Engine/Shaders/Lights/light.vert", "Engine/Shaders/Lights/light.frag");

            deferredCompute = new ComputeShader("Engine/Shaders/Compute/deferred.comp");
            outlineCompute = new ComputeShader("Engine/Shaders/Compute/outline.comp");

            fxaaShader = new Shader("Engine/Shaders/Postprocessing/1_rect.vert", "Engine/Shaders/Postprocessing/fxaa.frag");
            // RaytracingShader = new ComputeShader("Compute/raytracer.comp");
        }

        private bool viewportHovered;
        public bool showOutlines = true;
        public bool debugOutlines = false;

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
        static int count_PointLights, count_Meshes = 0;

        float farPlane = 1000;
        float nearPlane = 0.1f;

        PolygonMode _polygonMode = PolygonMode.Fill;
        private bool vsyncOn = true;
        private bool fullscreen = false;

        private Modine.ImGUI.ImGuiController ImGuiController;
        int selectedTexture = 0;
        int depthStencilTexture, gAlbedo, gNormal, gMetallicRough, gPosition;
        int FBO;

        ComputeShader deferredCompute;
        ComputeShader outlineCompute;
        int renderTexture;

        int depthMapFBO;
        int depthMap;
        int shadowRes = 2048;

        FPScounter FPScounter = new ();

        private static void OnDebugMessage(
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
            Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);

            // Potentially, you may want to throw from the function for certain severity
            // messages.
            if (type == DebugType.DebugTypeError)
            {
                throw new Exception(message);
            }
        }

        private static DebugProc DebugMessageDelegate = OnDebugMessage;

        unsafe protected override void OnLoad()
        {
            base.OnLoad();

            MakeCurrent();
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.StencilTest);
            GL.Enable(EnableCap.DebugOutput);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.PointSize(5);
            IsVisible = true;

            VSync = VSyncMode.On;
            GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);

            FBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

            Framebuffers.SetupFBO(ref depthStencilTexture, ref gAlbedo, ref gNormal, ref gMetallicRough, ref gPosition, viewportSize);
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            Framebuffers.SetupShadowFBO(ref depthMapFBO, ref depthMap, shadowRes);
            Postprocessing.SetupPPRect(ref postprocessShader);

            renderTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, viewportSize.X, viewportSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), 1, nearPlane, farPlane);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitY, new(0, 1, 0));
            camera = new Camera(new(0, 0, 2), -Vector3.UnitZ, 5);
            defaultMat = new ("Default", new (0.8f), 0, 0.3f, 0.0f, PBRShader);

            // RaytracingShader.SetVector3("ambient", ambient);
            deferredCompute.SetVector3("ambient", ambient);
            deferredCompute.SetVector3("direction", SunDirection);
            deferredCompute.SetFloat("shadowFactor", shadowFactor);
            PBRShader.SetVector3("direction", SunDirection);

            krissVectorMat = new ("VectorMat", new(1), 1, 1, 0, PBRShader,
                Texture.LoadFromFile("Assets/Resources/1_Albedo.png"),
                Texture.LoadFromFile("Assets/Resources/1_Roughness.png"),
                Texture.LoadFromFile("Assets/Resources/1_Metallic.png"),
                Texture.LoadFromFile("Assets/Resources/1_Normal.png"));
            ModelImporter.LoadModel("Assets/Resources/KrissVector.fbx", out vectorData, out vectorIndicies);

            Materials.Add(defaultMat);
            Materials.Insert(1, krissVectorMat);
            
            int numRows = 3;
            int numCols = 3;
            int spacing = 5;
            int startX = -((numCols - 1) * spacing) / 2;
            int startY = -((numRows - 1) * spacing) / 2;

            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    int x = startX + col * spacing;
                    int z = startY + row * spacing;

                    krissVector = new(vectorData, vectorIndicies, PBRShader, true, 1);
                    SceneObject vector = new(PBRShader, EngineUtility.NewName(sceneObjects, "Vector"), krissVector);
                    vector.Scale = new(0.5f);
                    vector.Position.X = x;
                    vector.Position.Z = z;
                    vector.Position.Y = 0;
                    sceneObjects.Add(vector);
                }
            }

            /*
            int numRows2 = 15;
            int numCols2 = 15;
            int spacing2 = (int)(25/3);
            int startX2 = -((numCols2 - 1) * spacing2) / 2;
            int startY2 = -((numRows2 - 1) * spacing2) / 2;

            for (int row = 0; row < numRows2; row++)
            {
                for (int col = 0; col < numCols2; col++)
                {
                    int x = startX2 + col * spacing2;
                    int z = startY2 + row * spacing2;

                    Light light = new(lightShader, GetRandomBrightColor(), 10);
                    SceneObject _light = new(lightShader, EngineUtility.NewName(sceneObjects, "Light"), light);
                    _light.Position.X = x;
                    _light.Position.Z = z;
                    _light.Position.Y = 6;

                    sceneObjects.Add(_light);
                }
            }
            */

            count_Meshes = 0;
            count_PointLights = 0;
            foreach (SceneObject sceneObject in sceneObjects)
            {
                if (sceneObject.Type == SceneObjectType.Mesh) count_Meshes += 1;
                else if (sceneObject.Type == SceneObjectType.Light) count_PointLights += 1;
            }

            triangleCount = EngineUtility.CalculateTriangles(sceneObjects);

            ImGuiController = new Modine.ImGUI.ImGuiController(viewportSize.X, viewportSize.Y);
            imnodes.PushColorStyle(ColorStyle.GridBackground, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.15f)));
            imnodes.PushColorStyle(ColorStyle.GridLine, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f)));
            imnodes.PushStyleVar(StyleVar.NodeCornerRounding, 5f);
            imnodes.PushStyleVar(StyleVar.NodeBorderThickness, 2);
            ImGuiWindows.LoadTheme();

            CreatePointLightResourceMemory(sceneObjects);

            // SetupCompRect(ref compTexture, compSize);
            // CreateResourceMemory(sceneObjects[0].Mesh.vertexData, sceneObjects[0].Mesh.indices);
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
        bool selectedIsMesh = false;

        public struct SSBOlight
        {
            public Vector3 lightPos;
            public float strength;
            public Vector3 lightColor;
            public float p0;
        }
    
        public static void CreatePointLightResourceMemory(List<SceneObject> sceneObjs)
        {
            List<SSBOlight> lightData = new List<SSBOlight>();

            for (int i = 0; i < sceneObjs.Count; i++)
            {
                if (sceneObjs[i].Type == SceneObjectType.Light)
                {
                    SSBOlight light;
                    light.lightPos = sceneObjs[i].Position;
                    light.strength = sceneObjs[i].Light.strength;
                    light.lightColor = sceneObjs[i].Light.lightColor;
                    light.p0 = 0;

                    lightData.Add(light);
                }
            }

            if (count_PointLights > 0)
            {
                const int BINDING_INDEX = 0;

                GL.CreateBuffers(1, out int buffer);
                GL.NamedBufferStorage(buffer, sizeof(float) * 8 * lightData.Count(), ref lightData.ToArray()[0], BufferStorageFlags.DynamicStorageBit);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, BINDING_INDEX, buffer);
            }
        }

        public void RenderScene(double time)
        {
            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;
            RenderFuncs.RenderShadowScene(shadowRes, ref depthMapFBO, lightSpaceMatrix, ref sceneObjects, shadowShader);

            // Render normal scene
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.DrawBuffers(5, new  DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3, DrawBuffersEnum.ColorAttachment4 });
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearColor(new  Color4(ambient.X, ambient.Y, ambient.Z, 1));
            GL.ClearDepth(1);
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
                lightSpaceMatrix = Matrix4.LookAt(SunDirection * 10, Vector3.Zero, Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-15, 15, -15, 15, 0.1f, 100);
                projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75), aspectRatio, nearPlane, farPlane);
                viewMatrix = Matrix4.LookAt(camera.position, camera.position + camera.direction, Vector3.UnitY);

                GL.ActiveTexture(TextureUnit.Texture5);
                GL.BindTexture(TextureTarget.Texture2D, depthMap);
                PBRShader.Use();
                PBRShader.SetMatrix4("projection", projectionMatrix);
                PBRShader.SetMatrix4("view", viewMatrix);
                PBRShader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);

                for (int i = 0; i < sceneObjects.Count; i++)
                {
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
            
            deferredCompute.Use();
            deferredCompute.SetVector3("viewPos", camera.position);

            // Bind framebuffer texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gAlbedo);
            
            // Bind normal texture
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);

            // Bind position texture
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
        
            // Bind Metallic and Roughness texture
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, gMetallicRough);

            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindImageTexture(4, renderTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            // Resize renderTexture
            GL.BindTexture(TextureTarget.Texture2D, renderTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, viewportSize.X, viewportSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.DispatchCompute(Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.X / 8)), Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.Y / 8)), 1);            
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);


            
            if (showOutlines)
            {
                outlineCompute.Use();
                outlineCompute.SetInt("debug", Convert.ToInt32(debugOutlines));

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, renderTexture);
                outlineCompute.SetInt("renderTexture", 0);

                // Bind stencil texture for outline in fragshader
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.StencilIndex);
                outlineCompute.SetInt("stencilTexture", 1);
                
                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindImageTexture(4, renderTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

                GL.DispatchCompute(Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.X / 8)), Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.Y / 8)), 1);            
                GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
                GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            }



            lightShader.Use();
            lightShader.SetMatrix4("projection", projectionMatrix);
            lightShader.SetMatrix4("view", viewMatrix);
            for (int i = 0; i < sceneObjects.Count; i++) if (sceneObjects[i].Type == SceneObjectType.Light) sceneObjects[i].Render(camera);
            
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            Framebuffers.ResizeFBO(viewportSize, previousViewportSize, ref depthStencilTexture, ref gAlbedo, ref gNormal, ref gMetallicRough, ref gPosition);

            // Show all the ImGUI windows
            ImGuiController.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();

            int[] textures = new int[]{ renderTexture, gAlbedo, gPosition, gNormal };
            ImGuiWindows.Header(FPScounter.fps, FPScounter.ms, count_Meshes, ref selectedTexture);
            ImGuiWindows.Viewport(textures[selectedTexture], out viewportSize, out viewportPos, out viewportHovered);
            if (showStats) ImGuiWindows.SmallStats(viewportSize, viewportPos, FPScounter.fps, FPScounter.ms, count_Meshes, count_PointLights, triangleCount);

            // Quick menu
            if (IsKeyDown(Keys.LeftShift) && IsKeyPressed(Keys.Space))
            {
                showQuickMenu = EngineUtility.ToggleBool(showQuickMenu);
                if (showQuickMenu) ImGui.SetNextWindowPos(new(MouseState.Position.X, MouseState.Position.Y));
            }
            
            if (showQuickMenu) ImGuiWindows.QuickMenu(ref sceneObjects, ref selectedSceneObject, ref showQuickMenu, ref triangleCount);
            
            if (!fullscreen)
            {
                if (sceneObjects.Count > 0)
                {
                    selectedIsMesh = sceneObjects[selectedSceneObject].Type == SceneObjectType.Mesh ? true : false;
                
                    ImGui.Begin("Material Editor##2");
                    if (selectedIsMesh)
                    {
                        if (sceneObjects[selectedSceneObject].Mesh.MaterialIndex < Materials.Count()) ImGui.Text("   " + Materials[sceneObjects[selectedSceneObject].Mesh.MaterialIndex].Name);
                        else ImGui.Text("   " + "Temp");
                        imnodes.BeginNodeEditor();

                        // Output node
                        imnodes.PushColorStyle(ColorStyle.TitleBar, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.875f, 0.233f, 0.203f, 1.000f)));
                        imnodes.PushColorStyle(ColorStyle.TitleBarHovered, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.875f, 0.233f, 0.203f, 1.000f) * 1.25f));
                        imnodes.PushColorStyle(ColorStyle.TitleBarSelected, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.875f, 0.233f, 0.203f, 1.000f) * 0.9f));
                        imnodes.BeginNode(1);
                        imnodes.BeginNodeTitleBar();
                        if (sceneObjects[selectedSceneObject].Mesh.MaterialIndex < Materials.Count()) ImGui.Text(Materials[sceneObjects[selectedSceneObject].Mesh.MaterialIndex].Name);
                        else ImGui.Text("Temp");

                        imnodes.EndNodeTitleBar();

                        imnodes.BeginInputAttribute(2, PinShape.CircleFilled);
                        ImGui.Text("Albedo");
                        imnodes.EndInputAttribute();

                        imnodes.BeginInputAttribute(3, PinShape.CircleFilled);
                        ImGui.Text("Roughness");
                        imnodes.EndInputAttribute();

                        imnodes.BeginInputAttribute(4, PinShape.CircleFilled);
                        ImGui.Text("Metallic");
                        imnodes.EndInputAttribute();

                        imnodes.BeginInputAttribute(5, PinShape.CircleFilled);
                        ImGui.Text("Normals");
                        imnodes.EndInputAttribute();
                        
                        ImGui.Dummy(new(80, 30));
                        imnodes.EndNode();

                        // Input node
                        imnodes.PushColorStyle(ColorStyle.TitleBar, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.231f, 0.779f, 0.148f, 1.000f)));
                        imnodes.PushColorStyle(ColorStyle.TitleBarHovered, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.231f, 0.779f, 0.148f, 1.000f) * 1.25f));
                        imnodes.PushColorStyle(ColorStyle.TitleBarSelected, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.231f, 0.779f, 0.148f, 1.000f) * 0.9f));
                        
                        imnodes.BeginNode(6);
                        imnodes.BeginNodeTitleBar();
                        ImGui.Text("Input");
                        imnodes.EndNodeTitleBar();

                        imnodes.BeginOutputAttribute(7, PinShape.QuadFilled);
                        ImGui.Indent(60);
                        ImGui.Text("UVs");
                        ImGui.Unindent();
                        imnodes.EndOutputAttribute();
                        ImGui.Dummy(new(80, 30));
                        imnodes.EndNode();

                        imnodes.EndNodeEditor();
                    }
                    
                    ImGui.End();
                }

                else
                {
                    ImGui.Begin("Material Editor##2");
                    ImGui.End();
                }
            
                // ImGuiWindows.AssetBrowser();
                ImGuiWindows.MaterialEditor(ref sceneObjects, ref PBRShader, selectedSceneObject, ref Materials);
                ImGuiWindows.Outliner(ref sceneObjects, ref selectedSceneObject, ref triangleCount);
                ImGuiWindows.Properties(ref sceneObjects, selectedSceneObject, ref Materials);
                ImGuiWindows.Settings(ref camera.speed, ref farPlane, ref nearPlane, ref vsyncOn, ref showOutlines, ref debugOutlines, ref showStats, ref shadowRes, ref depthMap, ref SunDirection, ref ambient, ref shadowFactor, ref deferredCompute, ref postprocessShader, ref outlineCompute, ref fxaaShader, ref PBRShader);
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
                if (e.Key == Keys.KeyPad1)
                {
                    GL.Enable(EnableCap.CullFace);
                    _polygonMode = PolygonMode.Fill;
                }
                if (e.Key == Keys.KeyPad2)
                {
                    GL.Disable(EnableCap.CullFace);
                    _polygonMode = PolygonMode.Line;
                }
                if (e.Key == Keys.KeyPad3)
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