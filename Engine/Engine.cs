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

using static Modine.Rendering.Entity;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using Newtonsoft.Json;

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
                APIVersion = new Version(4, 6),
                Flags = ContextFlags.Debug
            })
        {
            CenterWindow();

            viewportSize = this.Size;
            previousViewportSize = viewportSize;

            PBRShader = new Shader("Engine/Shaders/Deferred Rendering/PBR/mesh.vert", "Engine/Shaders/Deferred Rendering/PBR/mesh.frag");
            shadowShader = new Shader("Engine/Shaders/Deferred Rendering/PBR/shadow.vert", "Engine/Shaders/Deferred Rendering/PBR/shadow.frag");
            lightShader = new Shader("Engine/Shaders/Lights/light.vert", "Engine/Shaders/Lights/light.frag");

            deferredCompute = new ComputeShader("Engine/Shaders/Deferred Rendering/deferred.comp");
            outlineCompute = new ComputeShader("Engine/Shaders/Deferred Rendering/outline.comp");
            postprocessCompute = new ComputeShader("Engine/Shaders/Deferred Rendering/postprocess.comp");
        }

        private Vector2i viewportPos, viewportSize, previousViewportSize;

        Vector3 ambient = new(0.03f);
        public static Vector3 SunDirection = new(1);
        public static float farPlane = 1000, nearPlane = 0.1f;
        float shadowFactor = 0.75f;
        
        Material defaultMat;
        public static List<Material> Materials = new List<Material>();
        public static Shader PBRShader, lightShader, shadowShader;
        Texture pointLightTexture;

        Camera camera;
        static List<Entity> sceneObjects = new List<Entity>();
        public static int selectedSceneObject = 0;
        static int count_PointLights, count_Meshes = 0;
        int triangleCount = 0;

        PolygonMode _polygonMode = PolygonMode.Fill;
        private bool vsyncOn = true, fullscreen = false, showStats = false;
        private bool viewportHovered, showOutlines = true, showQuickMenu = false, debugOutlines = false;

        private ImGUI.ImGuiController ImGuiController;
        FPScounter FPScounter = new();

        int selectedTexture = 0;
        int depthStencilTexture, gAlbedo, gNormal, gMetallicRough, mainTexture; 
        int PBR_FBO;

        ComputeShader deferredCompute, outlineCompute, postprocessCompute;
        int renderTexture;

        int depthMapFBO, depthMap;
        int shadowRes = 2048;

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
            GL.Enable(EnableCap.Blend);

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.PointSize(5);

            VSync = VSyncMode.On;
            GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);

            Framebuffers.SetupMainFBO(ref PBR_FBO, ref mainTexture, ref depthStencilTexture, ref gAlbedo, ref gNormal, ref gMetallicRough, viewportSize);
            Framebuffers.SetupShadowFBO(ref depthMapFBO, ref depthMap, shadowRes);
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            renderTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, viewportSize.X, viewportSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            pointLightTexture = Texture.LoadFromFile("Assets/Resources/PointLightIcon.png");

            camera = new Camera(new(0, 0, 2), -Vector3.UnitZ, 75, 5);

            // RaytracingShader.SetVector3("ambient", ambient);
            deferredCompute.SetVector3("ambient", ambient);
            deferredCompute.SetVector3("direction", SunDirection);
            deferredCompute.SetFloat("shadowFactor", shadowFactor);
            PBRShader.SetVector3("direction", SunDirection);

            defaultMat = new("Default", new (0.8f), 0, 0.5f, 0.0f);

            Materials.Add(defaultMat);

            triangleCount = EngineUtility.CalculateTriangles(sceneObjects);
            EngineUtility.CountEntities(sceneObjects, out int MeshCount, out int PointLightCount);
            count_Meshes = MeshCount;
            count_PointLights = PointLightCount;

            ImGuiController = new Modine.ImGUI.ImGuiController(viewportSize.X, viewportSize.Y);
            imnodes.PushColorStyle(ColorStyle.GridBackground, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.15f)));
            imnodes.PushColorStyle(ColorStyle.GridLine, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f)));
            imnodes.PushStyleVar(StyleVar.NodeCornerRounding, 5f);
            imnodes.PushStyleVar(StyleVar.NodeBorderThickness, 2);
            ImGuiWindows.LoadTheme();
            
            Rendering.Functions.CreatePointLightResourceMemory(sceneObjects);
            LoadEditorSettings();

            IsVisible = true;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            SaveEditorSettings();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsMouseButtonDown(MouseButton.Button2))
            {
                CursorState = CursorState.Grabbed;
                camera.Input(MouseState, sceneObjects[selectedSceneObject].Position);
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

                if (IsKeyDown(Keys.LeftAlt)) camera.trackball = true;
                if (IsKeyReleased(Keys.LeftAlt)) camera.trackball = false;
                if (IsKeyPressed(Keys.LeftAlt)) camera.distance = Vector3.Distance(camera.position, sceneObjects[selectedSceneObject].Position);

                if (IsKeyDown(Keys.LeftAlt) && IsKeyPressed(Keys.G)) sceneObjects[selectedSceneObject].Position = Vector3.Zero;
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

        public struct EditorSettings
        {
            public Vector2 WindowSize;
            public Vector2 WindowPos;
            public bool Maximized;
        }

        unsafe public void SaveEditorSettings()
        {
            imnodes.SaveCurrentEditorStateToIniFile("Engine/Editor Settings/nodeeditor.ini");

            EditorSettings editor = new();

            GLFW.GetWindowSize(WindowPtr, out int width, out int height);
            editor.WindowSize = new(width, height);

            GLFW.GetWindowPos(WindowPtr, out int x, out int y);
            editor.WindowPos = new(x, y);

            editor.Maximized = GLFW.GetWindowAttrib(WindowPtr, WindowAttributeGetBool.Maximized);

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };

            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new Vector3Converter());

            string json = JsonConvert.SerializeObject(editor, settings);
            using (StreamWriter writer = new StreamWriter("Engine/Editor Settings/testsave.editorsettings"))
            {
                writer.Write(json);
            }
        }

        unsafe public void LoadEditorSettings()
        {
            imnodes.LoadCurrentEditorStateFromIniFile("Engine/Editor Settings/nodeeditor.ini");

            string json = File.ReadAllText("Engine/Editor Settings/testsave.editorsettings");
            EditorSettings editor = JsonConvert.DeserializeObject<EditorSettings>(json);

            GLFW.SetWindowSize(WindowPtr, (int)editor.WindowSize.X, (int)editor.WindowSize.Y);
            GLFW.SetWindowPos(WindowPtr, (int)editor.WindowPos.X, (int)editor.WindowPos.Y);
            if (editor.Maximized) GLFW.MaximizeWindow(WindowPtr);
        }

        public void RenderScene(double time)
        {
            VSync = vsyncOn ? VSyncMode.On : VSyncMode.Off;
            Functions.RenderShadowScene(shadowRes, ref depthMapFBO, camera.lightSpaceMatrix, ref sceneObjects, shadowShader, PBRShader);

            // Render normal scene
            GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, PBR_FBO);
            Framebuffers.ResizeMainFBO(viewportSize, previousViewportSize, ref mainTexture, ref depthStencilTexture, ref gAlbedo, ref gNormal, ref gMetallicRough);
            GL.DrawBuffers(4, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3 });
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearColor(new Color4(ambient.X, ambient.Y, ambient.Z, 1));
            GL.ClearDepth(1);
            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);

            if (sceneObjects.Count > 0)
            {
                EngineUtility.CountEntities(sceneObjects, out int MeshCount, out int PointLightCount);
                count_Meshes = MeshCount;
                count_PointLights = PointLightCount;

                GL.Enable(EnableCap.StencilTest);
                GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
                GL.StencilMask(0x00);

                GL.ActiveTexture(TextureUnit.Texture5);
                GL.BindTexture(TextureTarget.Texture2D, depthMap);

                camera.Update(viewportSize);
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    if (sceneObjects[i].Type == EntityType.Mesh)
                    {
                        Materials[sceneObjects[i].Mesh.MaterialIndex].SetShaderUniforms(sceneObjects[i].Shader, camera);
                        sceneObjects[i].Render(i);
                    }
                }

                GL.DepthMask(false);
                lightShader.Use();
                lightShader.SetMatrix4("projection", camera.projectionMatrix);
                lightShader.SetMatrix4("view", camera.viewMatrix);
                pointLightTexture.Use(TextureUnit.Texture0);
                for (int i = 0; i < sceneObjects.Count; i++) if (sceneObjects[i].Type == EntityType.Light) sceneObjects[i].Render(camera);
                GL.DepthMask(true);

                // Render selected sceneobject infront of everything and dont write to color buffer
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.ColorMask(false, false, false, false);
                GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                GL.StencilMask(0xFF);

                switch (sceneObjects[selectedSceneObject].Type)
                {
                    case EntityType.Mesh:
                        PBRShader.Use();
                        sceneObjects[selectedSceneObject].Render(selectedSceneObject);
                        break;
                    
                    case EntityType.Light:
                        lightShader.Use();
                        sceneObjects[selectedSceneObject].Render(camera);
                        break;
                }

                GL.ColorMask(true, true, true, true);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.CullFace);
                GL.Disable(EnableCap.StencilTest);
            }

            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            deferredCompute.Use();
            deferredCompute.SetVector3("viewPos", camera.position);
            deferredCompute.SetMatrix4("projMatrixInv", Matrix4.Invert(camera.projectionMatrix));
            deferredCompute.SetMatrix4("viewMatrixInv", Matrix4.Invert(camera.viewMatrix));

            GL.BindTextureUnit(0, mainTexture);
            GL.BindTextureUnit(1, gAlbedo);
            GL.BindTextureUnit(2, gNormal);
            GL.BindTextureUnit(3, gMetallicRough);
           
            // Bind depth
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.DepthComponent);

            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindImageTexture(5, renderTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            // Resize renderTexture
            GL.BindTexture(TextureTarget.Texture2D, renderTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, viewportSize.X, viewportSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.DispatchCompute(Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.X / 8)), Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.Y / 8)), 1);            
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

            /*
            postprocessCompute.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderTexture);
            
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindImageTexture(1, renderTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

            GL.DispatchCompute(Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.X / 8)), Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.Y / 8)), 1);            
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            */

            if (showOutlines)
            {
                outlineCompute.Use();
                outlineCompute.SetInt("debug", Convert.ToInt32(debugOutlines));

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, renderTexture);

                // Bind stencil texture for outline in fragshader
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, depthStencilTexture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthStencilTextureMode, (int)All.StencilIndex);
                
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindImageTexture(2, renderTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

                GL.DispatchCompute(Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.X / 8)), Convert.ToInt32(MathHelper.Ceiling((float)viewportSize.Y / 8)), 1);            
                GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
                GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
            }

            // Show all the ImGUI windows
            ImGuiController.Update(this, (float)time);
            ImGui.DockSpaceOverViewport();

            int[] textures = new int[]{ renderTexture, gAlbedo, gNormal };
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
                    ImGui.Begin("Material Editor##2");
                    if (sceneObjects[selectedSceneObject].Type == EntityType.Mesh ? true : false)
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
                        ImGui.Text("Normal");
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

                        imnodes.BeginOutputAttribute(7, PinShape.CircleFilled);
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

                ImGui.Begin("Test");

                float splitPos = ImGui.GetContentRegionAvail().X * 0.5f;

                if (ImGui.CollapsingHeader("Testing"))
                {
                    float width = ImGui.GetContentRegionAvail().X;
                    ImGui.Indent(10);
                    
                    for (int i = 0; i < 4; i++)
                    {
                        ImGui.SetNextItemWidth(width / 2);
                        ImGui.Text("FOV" + i);
                    
                        ImGui.SameLine(width / 2);
                        ImGui.GetForegroundDrawList().AddLine(
                            ImGui.GetCursorPos() + ImGui.GetWindowPos(),
                            new System.Numerics.Vector2(ImGui.GetCursorPos().X, ImGui.GetCursorPos().Y + ImGui.GetFrameHeightWithSpacing()) + ImGui.GetWindowPos(),
                            ImGui.ColorConvertFloat4ToU32(new(new(0.125f), 1.0f)), 3);

                        ImGui.Dummy(new(10, 0));
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(width / 2 - 20);
                        ImGui.SliderInt("##FOV" + i, ref camera.FOV, 1, 100);
                    }
                    ImGui.Unindent(10);
                }

                if (ImGui.CollapsingHeader("Testing2"))
                {
                    ImGui.Indent(10);

                    float width = ImGui.GetContentRegionAvail().X;

                    ImGui.SetNextItemWidth(width / 2);
                    if (ImGui.Button("Save"))
                    {
                        var settings = new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented,
                        };

                        settings.Converters.Add(new Vector2Converter());
                        settings.Converters.Add(new Vector3Converter());

                        //List<Mesh> meshes = new List<Mesh>();
                        //foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Type == SceneObjectType.Mesh) meshes.Add(sceneObject.Mesh);

                        string json = JsonConvert.SerializeObject(sceneObjects, settings);
                        using (StreamWriter writer = new StreamWriter("Engine/testsave.mod"))
                        {
                            writer.Write(json);
                        }
                    }

                    ImGui.SameLine(width / 2);
                    ImGui.GetForegroundDrawList().AddLine(
                        ImGui.GetCursorPos() + ImGui.GetWindowPos(),
                        new System.Numerics.Vector2(ImGui.GetCursorPos().X, ImGui.GetCursorPos().Y + ImGui.GetFrameHeightWithSpacing()) + ImGui.GetWindowPos(),
                        ImGui.ColorConvertFloat4ToU32(new(new(0.125f), 1.0f)), 3);

                    ImGui.Dummy(new(10, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(width / 2 - 20);

                    if (ImGui.Button("Load"))
                    {
                        if (File.Exists("Engine/testsave.mod"))
                        {
                            string json = File.ReadAllText("Engine/testsave.mod");
                            List<Entity> sceneObjs = JsonConvert.DeserializeObject<List<Entity>>(json);
                            foreach (Entity obj in sceneObjs) if (obj.Type == EntityType.Light) Console.WriteLine(obj.Position);
                        }
                    }

                    ImGui.Unindent(10);
                }                

                ImGui.End();
            
                // ImGuiWindows.AssetBrowser();
                ImGuiWindows.ShadowView(depthMap);
                ImGuiWindows.MaterialEditor(ref sceneObjects, ref PBRShader, selectedSceneObject, ref Materials, camera);
                ImGuiWindows.Outliner(ref sceneObjects, ref selectedSceneObject, ref triangleCount);
                ImGuiWindows.Properties(ref sceneObjects, selectedSceneObject, ref Materials);
                ImGuiWindows.Settings(ref camera.speed, ref farPlane, ref nearPlane, ref vsyncOn, ref showOutlines, ref debugOutlines, ref showStats, ref shadowRes, ref depthMap, ref SunDirection, ref ambient, ref shadowFactor, ref deferredCompute, ref outlineCompute, ref postprocessCompute, ref PBRShader);
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
            Vector4 ray_eye = ray_clip * Matrix4.Invert(camera.projectionMatrix);
            ray_eye = new (ray_eye.X, ray_eye.Y, -1.0f, 0.0f);

            // 4d World Coordinates
            Vector3 ray_wor = (ray_eye * Matrix4.Invert(camera.viewMatrix)).Xyz;
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