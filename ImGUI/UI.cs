using SN = System.Numerics;
using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using GameEngine.Rendering;
using static GameEngine.Rendering.SceneObject;
using GameEngine.Common;

namespace GameEngine.ImGUI
{
    public static class ImGuiWindows
    {
        static float spacing = 5;

        public static void SmallStats(Vector2i viewportSize, Vector2i viewportPos, double fps, double ms, int objectCount, int triangleCount)
        {
            ImGui.GetForegroundDrawList().AddRectFilled(
                new(viewportPos.X + 10, viewportPos.Y + 30),
                new(viewportPos.X + 200, viewportPos.Y + 200),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.2f)));
            ImGui.GetForegroundDrawList().AddRect(
                new(viewportPos.X + 10, viewportPos.Y + 30),
                new(viewportPos.X + 200, viewportPos.Y + 200),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.3f)));

            ImGui.GetForegroundDrawList().AddText(
                new(viewportPos.X + 20, viewportPos.Y + 40),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(150, 150, 150, 255)),
                GL.GetString(StringName.Renderer) + "\n" +
                "Size: " + viewportSize.X + " x " + viewportSize.Y + "\n" +
                "Pos: " + viewportPos.X + " x " + viewportPos.Y + "\n" +
                "Meshes: " + objectCount + "\n" +
                "Triangles: " + triangleCount.ToString("N0") + "\n" +
                "\n" +
                fps.ToString("0") + " FPS" + "\n" +
                ms.ToString("0.00") + " ms");
        }

        public static void ObjectProperties(ref List<SceneObject> sceneObjects, int selectedMesh)
        {
            ImGui.Begin("Properties");
            if (sceneObjects.Count > 0)
            {
                SceneObject _sceneObject = sceneObjects[selectedMesh];

                string newName = _sceneObject.Name;
                if (ImGui.InputText("##Name", ref newName, 30, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)) _sceneObject.Name = newName;
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Separator();

                if (_sceneObject.Type == SceneObjectType.Mesh)
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Checkbox(" Cast shadow", ref _sceneObject.Mesh.castShadow);
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Checkbox(" Smooth Shading", ref _sceneObject.Mesh.smoothShading);
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    if (ImGui.TreeNode("Transform"))
                    {
                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                        SN.Vector3 tempPos = new(_sceneObject.Mesh.position.X, _sceneObject.Mesh.position.Y, _sceneObject.Mesh.position.Z);
                        ImGui.Text("Position");
                        if (ImGui.DragFloat3("##Position", ref tempPos, 0.1f))
                        {
                            _sceneObject.Mesh.position = new(tempPos.X, tempPos.Y, tempPos.Z);
                        }

                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                        SN.Vector3 tempRot = new( _sceneObject.Mesh.rotation.X, _sceneObject.Mesh.rotation.Y, _sceneObject.Mesh.rotation.Z);
                        ImGui.Text("Rotation");
                        if (ImGui.DragFloat3("##Rotation", ref tempRot, 1))
                        {
                            _sceneObject.Mesh.rotation = new(tempRot.X, tempRot.Y, tempRot.Z);
                        }
                        
                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                        SN.Vector3 tempScale = new(_sceneObject.Mesh.scale.X, _sceneObject.Mesh.scale.Y, _sceneObject.Mesh.scale.Z);
                        ImGui.Text("Scale");
                        if (ImGui.DragFloat3("##Scale", ref tempScale, 0.1f))
                        {
                            _sceneObject.Mesh.scale = new(tempScale.X, tempScale.Y, tempScale.Z);
                        }
                    }

                    else if (_sceneObject.Type == SceneObjectType.Light)
                    {
                        SN.Vector3 tempPos = new(_sceneObject.Light.position.X, _sceneObject.Light.position.Y, _sceneObject.Light.position.Z);
                        ImGui.Text("Position");
                        if (ImGui.DragFloat3("##Position", ref tempPos, 0.1f))
                        {
                            _sceneObject.Light.position = new(tempPos.X, tempPos.Y, tempPos.Z);
                        }
                    }
                }
            }

            ImGui.End();
        }

        public static void Viewport(int framebufferTexture, int depthMap, out Vector2i windowSize, out Vector2i viewportPos, out bool viewportHovered, int shadowRes)
        {
            ImGui.Begin("Viewport");
            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            ImGui.Image((IntPtr)framebufferTexture, new(
                MathHelper.Abs(ImGui.GetWindowContentRegionMin().X - ImGui.GetWindowContentRegionMax().X),
                MathHelper.Abs(ImGui.GetWindowContentRegionMin().Y - ImGui.GetWindowContentRegionMax().Y)),
                new(0, 1), new(1, 0), SN.Vector4.One, SN.Vector4.Zero);

            windowSize = new(
                Convert.ToInt32(MathHelper.Abs(ImGui.GetWindowContentRegionMin().X - ImGui.GetWindowContentRegionMax().X)),
                Convert.ToInt32(MathHelper.Abs(ImGui.GetWindowContentRegionMin().Y - ImGui.GetWindowContentRegionMax().Y)));
            viewportPos = new(
                Convert.ToInt32(ImGui.GetWindowPos().X),
                Convert.ToInt32(ImGui.GetWindowPos().Y));

            viewportHovered = ImGui.IsWindowHovered() ? true : false;
            ImGui.End();
        
            ImGui.Begin("Shadow View");
            float width = 400;
            float height = 400;
            if (width != height)
            {
                // adjust the size if not square 
                if (width > height) width = height;
                else height = width;
                ImGui.SetWindowSize(new(width, height));
            }
            ImGui.Image((IntPtr)depthMap, new(width, height), new(0, 1), new(1, 0), SN.Vector4.One, SN.Vector4.Zero); ImGui.End();
        }

        public static void MaterialEditor(ref List<SceneObject> sceneObjects, ref Shader meshShader, int selectedIndex)
        {
            ImGui.Begin("Material Editor");

            if (sceneObjects.Count > 0)
            {
                if (sceneObjects[selectedIndex].Type == SceneObjectType.Mesh)
                {
                    Material _material = sceneObjects[selectedIndex].Mesh.Material;
                    SN.Vector3 color = new(_material.Color.X, _material.Color.Y, _material.Color.Z);
                    if (ImGui.ColorPicker3("Albedo", ref color, ImGuiColorEditFlags.NoInputs))
                    {
                        _material.Color = new(color.X, color.Y, color.Z);
                        _material.SetShaderUniforms(meshShader);
                    }

                    float tempRoughness = _material.Roughness;
                    if (ImGui.SliderFloat("Roughness", ref tempRoughness, 0, 1))
                    {
                        _material.Roughness = tempRoughness;
                        _material.SetShaderUniforms(meshShader);
                    }

                    float tempMetallic = _material.Metallic;
                    if (ImGui.SliderFloat("Metallic", ref tempMetallic, 0, 1))
                    {
                        _material.Metallic = tempMetallic;
                        _material.SetShaderUniforms(meshShader);
                    }
                }
            }
            
            ImGui.End();
        }

        static int selectedIndex = 3;
        static int pcfres = 2;
        static bool debugBool = true;
        static float strength = 1;
        public static void Settings(ref bool vsyncOn, ref int shadowRes, ref int depthMap, ref Vector3 direction, ref Vector3 ambient, ref float ShadowFactor, ref Shader shader, ref Shader ppshader)
        {
            ImGui.Begin("Settings");

            ImGui.Checkbox("VSync", ref vsyncOn);
            if (ImGui.Checkbox("ACES Tonemap", ref debugBool)) ppshader.SetInt("ACES", Convert.ToInt32(debugBool));

            SN.Vector3 dir = new(direction.X, direction.Y, direction.Z);
            ImGui.Text("Sun Direction");
            if (ImGui.SliderFloat3("##Sun Direction", ref dir, -1, 1))
            {
                direction = new(dir.X, dir.Y, dir.Z);
                shader.SetVector3("direction", direction);
            }

            ImGui.Text("Strength");
            if (ImGui.SliderFloat("##Strength", ref strength, 0, 10, "%.1f"))
            {
                shader.SetFloat("dirStrength", strength);
            }

            SN.Vector3 color = new(ambient.X, ambient.Y, ambient.Z);            
            ImGui.Text("Ambient Color");
            if (ImGui.ColorPicker3("##Ambient Color", ref color, ImGuiColorEditFlags.NoInputs))
            {
                ambient = new(color.X, color.Y, color.Z);
                shader.SetVector3("ambient", ambient);
            }

            float shadowFac = ShadowFactor;
            ImGui.Text("Shadow Factor");
            if (ImGui.SliderFloat("##Shadow Factor", ref shadowFac, 0, 1))
            {
                ShadowFactor = shadowFac;
                shader.SetFloat("shadowFactor", ShadowFactor);
            }

            if (ImGui.SliderInt("Shadow PCF res", ref pcfres, 0, 10)) shader.SetInt("shadowPCFres", pcfres);

            int[] options = new int[] { 256, 512, 1024, 2048, 4096, 8192 };
            ImGui.Text("Shadow Resolution");
            if (ImGui.SliderInt("##Resolution", ref selectedIndex, 0, 5, options[selectedIndex].ToString()))
            {
                // Use the selected resolution in your application logic
                shadowRes = options[selectedIndex];
                GL.BindTexture(TextureTarget.Texture2D, depthMap);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, shadowRes, shadowRes, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            }
            
            ImGui.End();
        }

        public static void Header()
        {
            ImGui.BeginMainMenuBar();

             if (ImGui.BeginMenu("File"))
            {
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Window"))
            {
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }


        public static void OldOutliner(List<SceneObject> sceneObjects, ref int selectedMeshIndex)
        {
            ImGui.Begin("Outliner", ImGuiWindowFlags.None);

            for (int i = 0; i < sceneObjects.Count; i++)
            {
                ImGui.BeginGroup();

                if (ImGui.Selectable(sceneObjects[i].Name, selectedMeshIndex == i))
                {
                    selectedMeshIndex = i;
                }
                ImGui.SameLine(ImGui.GetWindowWidth() - 55);
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.5f)));
                ImGui.Text(sceneObjects[i].Type.ToString().ToLower());
                ImGui.PopStyleColor();

                ImGui.EndGroup();
            }

            ImGui.End();
        }

        public static void Outliner(ref List<SceneObject> sceneObjects, ref int selectedMeshIndex, ref int triCount)
        {
            ImGui.Begin("Outliner");

            if (ImGui.Button("Remove Selected") && sceneObjects.Count != 0)
            {
                sceneObjects[selectedMeshIndex].Dispose();
                sceneObjects.RemoveAt(selectedMeshIndex);   
                
                triCount = Game.CalculateTriangles();
                if (selectedMeshIndex != 0) selectedMeshIndex -= 1;
            }

            if (ImGui.BeginTable("table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                ImGui.TableSetupColumn("Type");

                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    ImGui.TableNextRow();

                    if (i % 2 == 0) ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.15f)));
                    if (i % 2 == 1) ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.2f)));

                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Selectable(sceneObjects[i].Name, selectedMeshIndex == i))
                    {
                        selectedMeshIndex = i;
                    }

                    ImGui.TableSetColumnIndex(1);
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.5f)));
                    ImGui.Text(sceneObjects[i].Type.ToString().ToLower() + " ");
                    ImGui.PopStyleColor();
                }

                float tableHeight = ImGui.GetContentRegionAvail().Y;
                float itemHeight = ImGui.GetTextLineHeightWithSpacing();
                int numRows = Convert.ToInt16(tableHeight / (itemHeight + 4)) - 1;

                for (int i = 0; i < numRows; i++)
                {
                    ImGui.TableNextRow();

                    if (i % 2 == 0) ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.15f)));
                    if (i % 2 == 1) ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.2f)));

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("");

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text("");
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }


        public static void LoadTheme()
        {
            ImGui.GetStyle().FrameRounding = 2;
            ImGui.GetStyle().FrameBorderSize = 2;
            ImGui.GetStyle().FramePadding = new System.Numerics.Vector2(4);
            ImGui.GetStyle().CellPadding = new SN.Vector2(3, 3);
            ImGui.GetStyle().ItemSpacing = new System.Numerics.Vector2(4, 2);
            ImGui.GetStyle().ItemInnerSpacing = new System.Numerics.Vector2(0, 4);
            ImGui.GetStyle().TabRounding = 2;
            ImGui.GetStyle().WindowRounding = 5;
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;
            
            ImGui.GetStyle().GrabMinSize = 15;

            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(25, 25, 25, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new System.Numerics.Vector4(15, 15, 15, 200f) / 255);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, new System.Numerics.Vector4(255, 140, 0, 255) / 255);

            // Background color
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new System.Numerics.Vector4(40f, 40f, 40f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new System.Numerics.Vector4(80f, 80f, 80f, 255f) / 255);

            // Popup BG
            ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, new System.Numerics.Vector4(30f, 30f, 30f, 150f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, new System.Numerics.Vector4(150f, 150f, 150f, 255f) / 255);

            // Titles
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);

            // Tabs
            ImGui.PushStyleColor(ImGuiCol.Tab, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabActive, new System.Numerics.Vector4(35f, 35f, 35f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new System.Numerics.Vector4(35f, 35f, 35f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new System.Numerics.Vector4(80f, 80f, 80f, 255f) / 255);
            
            // Header
            ImGui.PushStyleColor(ImGuiCol.Header, new System.Numerics.Vector4(0f, 153f, 76f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new System.Numerics.Vector4(0f, 153f, 76f, 180f) / 255);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new System.Numerics.Vector4(0f, 153f, 76f, 255f) / 255);

            // Rezising bar
            ImGui.PushStyleColor(ImGuiCol.Separator, new System.Numerics.Vector4(40f, 40f, 40f, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, new System.Numerics.Vector4(60f, 60f, 60f, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.SeparatorActive, new System.Numerics.Vector4(80f, 80f, 80f, 255) / 255);

            // Buttons
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(255, 41, 55, 200) / 255);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(255, 41, 55, 150) / 255);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(255, 41, 55, 100) / 255);

            // Docking and rezise
            // ImGui.PushStyleColor(ImGuiCol.DockingPreview, new System.Numerics.Vector4(30, 140, 120, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.DockingPreview, new System.Numerics.Vector4(100, 100, 100, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new System.Numerics.Vector4(217, 35, 35, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new System.Numerics.Vector4(217, 35, 35, 200) / 255);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new System.Numerics.Vector4(217, 35, 35, 150) / 255);
            ImGui.PushStyleColor(ImGuiCol.DockingEmptyBg, new System.Numerics.Vector4(20, 20, 20, 255) / 255);

            // Sliders, buttons, etc
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, new System.Numerics.Vector4(115f, 115f, 115f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new System.Numerics.Vector4(180f, 180f, 180f, 255f) / 255);
        }
    }
}