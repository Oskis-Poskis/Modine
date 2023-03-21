using SN = System.Numerics;
using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Modine.Rendering;
using Modine.Common;

using static Modine.Rendering.SceneObject;

namespace Modine.ImGUI
{
    public static class ImGuiWindows
    {
        static float spacing = 5;

        public static void SmallStats(Vector2i viewportSize, Vector2i viewportPos, double fps, double ms, int meshCount, int plCount, int triangleCount)
        {
            ImGui.GetForegroundDrawList().AddRectFilled(
                new(viewportPos.X + 10, viewportPos.Y + 30),
                new(viewportPos.X + 300, viewportPos.Y + 220),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.2f)));
            ImGui.GetForegroundDrawList().AddRect(
                new(viewportPos.X + 10, viewportPos.Y + 30),
                new(viewportPos.X + 300, viewportPos.Y + 220),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(0.3f)));

            ImGui.GetForegroundDrawList().AddText(
                new(viewportPos.X + 20, viewportPos.Y + 40),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(150, 150, 150, 255)),
                GL.GetString(StringName.Renderer) + "\n" +
                GL.GetString(StringName.Version) + "\n" +
                "Size: " + viewportSize.X + " x " + viewportSize.Y + "\n" +
                "Pos: " + viewportPos.X + " x " + viewportPos.Y + "\n" +
                //"X:" + direction.X + " Y: " + direction.Y  + " Z: " + direction.Z  +"\n" + 
                "Meshes: " + meshCount + "\n" +
                "Lights: " + plCount + "\n" +
                "Triangles: " + triangleCount.ToString("N0") + "\n" +
                "\n" +
                fps.ToString("0") + " FPS" + "\n" +
                ms.ToString("0.00") + " ms");
        }

        public static void ObjectProperties(ref List<SceneObject> sceneObjects, int selectedMesh)
        {
            ImGui.Begin("Properties");

            Properties(ref sceneObjects, selectedMesh);

            ImGui.End();
        }

        public static void Properties(ref List<SceneObject> sceneObjects, int selectedObject)
        {
            if (sceneObjects.Count > 0)
            {
                SceneObject _sceneObject = sceneObjects[selectedObject];

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                string newName = _sceneObject.Name;
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputText("##Name", ref newName, 30, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)) _sceneObject.Name = Modine.Game.NewName(newName);
                ImGui.PopItemWidth();

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();

                if (_sceneObject.Type == SceneObjectType.Mesh)
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Checkbox(" Cast shadow", ref _sceneObject.Mesh.castShadow);
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    if (ImGui.CollapsingHeader("Transform"))
                    {
                        ImGui.Indent();

                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                        SN.Vector3 tempPos = new(_sceneObject.Position.X, _sceneObject.Position.Y, _sceneObject.Position.Z);
                        ImGui.Text("Position");
                        if (ImGui.DragFloat3("##Position", ref tempPos, 0.1f))
                        {
                            sceneObjects[selectedObject].Position = new(tempPos.X, tempPos.Y, tempPos.Z);
                        }

                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                        ImGui.Separator();
                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                        SN.Vector3 tempRot = new( _sceneObject.Rotation.X, _sceneObject.Rotation.Y, _sceneObject.Rotation.Z);
                        ImGui.Text("Rotation");
                        if (ImGui.DragFloat3("##Rotation", ref tempRot, 1))
                        {
                            sceneObjects[selectedObject].Rotation = new(tempRot.X, tempRot.Y, tempRot.Z);
                        }
                        
                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                        ImGui.Separator(); 
                        ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                        SN.Vector3 tempScale = new(_sceneObject.Scale.X, _sceneObject.Scale.Y, _sceneObject.Scale.Z);
                        ImGui.Text("Scale");
                        if (ImGui.DragFloat3("##Scale", ref tempScale, 0.1f))
                        {
                            sceneObjects[selectedObject].Scale = new(tempScale.X, tempScale.Y, tempScale.Z);
                        }

                        ImGui.Unindent();
                    }

                    else if (_sceneObject.Type == SceneObjectType.Light)
                    {
                        SN.Vector3 tempPos = new(_sceneObject.Position.X, _sceneObject.Position.Y, _sceneObject.Position.Z);
                        ImGui.Text("Position");
                        if (ImGui.DragFloat3("##Position", ref tempPos, 0.1f))
                        {
                            sceneObjects[selectedObject].Position = new(tempPos.X, tempPos.Y, tempPos.Z);
                        }
                    }
                }

                else if (_sceneObject.Type == SceneObjectType.Light)
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    SN.Vector3 tempPos = new(_sceneObject.Position.X, _sceneObject.Position.Y, _sceneObject.Position.Z);
                    ImGui.Text("Position");
                    if (ImGui.DragFloat3("##Position", ref tempPos, 0.1f))
                    {
                        _sceneObject.Position = new(tempPos.X, tempPos.Y, tempPos.Z);
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    float tempStrength = _sceneObject.Light.strength;
                    ImGui.Text("Strength");
                    if (ImGui.DragFloat("##Strength", ref tempStrength, 0.1f))
                    {
                        _sceneObject.Light.strength = tempStrength;
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    SN.Vector3 color = new(_sceneObject.Light.lightColor.X, _sceneObject.Light.lightColor.Y, _sceneObject.Light.lightColor.Z);
                    ImGui.Text("Albedo");
                    if (ImGui.ColorPicker3("##Albedo", ref color))
                    {
                        _sceneObject.Light.lightColor = new(color.X, color.Y, color.Z);
                    }
                }
            }
        }

        public static void Viewport(int framebufferTexture, int depthMap, out Vector2i windowSize, out Vector2i viewportPos, out bool viewportHovered, int shadowRes)
        {
            ImGui.Begin("Viewport");
            windowSize = new(
                Convert.ToInt32(MathHelper.Abs(ImGui.GetWindowContentRegionMin().X - ImGui.GetWindowContentRegionMax().X)),
                Convert.ToInt32(MathHelper.Abs(ImGui.GetWindowContentRegionMin().Y - ImGui.GetWindowContentRegionMax().Y)));
            viewportPos = new(
                Convert.ToInt32(ImGui.GetWindowPos().X),
                Convert.ToInt32(ImGui.GetWindowPos().Y));

            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            
            
            ImGui.Image((IntPtr)framebufferTexture, new(windowSize.X, windowSize.Y), new(0, 1), new(1, 0), new(1, 1, 1, 1), new(0));

            viewportHovered = ImGui.IsWindowHovered() ? true : false;
            ImGui.End();
        }

        public static void ShadowView(int depthMap)
        {
            ImGui.Begin("Shadow View");
            if (ImGui.TreeNode("Directional light"))
            {
                float width = 400;
                float height = 400;
                if (width != height)
                {
                    // adjust the size if not square 
                    if (width > height) width = height;
                    else height = width;
                    ImGui.SetWindowSize(new(width, height));
                }

                GL.BindTexture(TextureTarget.Texture2D, depthMap);
                ImGui.Image((IntPtr)depthMap, new(width, height), new(0, 1), new(1, 0), SN.Vector4.One, SN.Vector4.Zero); ImGui.End();
            }
            ImGui.End();
        }

        public static void MaterialEditor(ref List<SceneObject> sceneObjects, ref Shader meshShader, int selectedIndex, ref List<Material> materials)
        {
            ImGui.Begin("Material Editor");

            if (sceneObjects.Count > 0)
            {
                if (sceneObjects[selectedIndex].Type == SceneObjectType.Mesh)
                {
                    string[] materialNames = new string[materials.Count];
                    for (int i = 0; i < materials.Count; i++)
                    {
                        materialNames[i] = materials[i].Name;
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(20, 20, 20, 255f) / 255));
                    ImGui.ListBox("##Materials", ref sceneObjects[selectedIndex].Mesh.MaterialIndex, materialNames, materialNames.Length);
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(45f, 45f, 45f, 255f) / 255));
                    ImGui.PopItemWidth();

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    Material _material = materials[sceneObjects[selectedIndex].Mesh.MaterialIndex];
                    string newName = _material.Name;
                    if (ImGui.InputText("##Name", ref newName, 30, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)) _material.Name = newName;

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    if (ImGui.Button("Add Material"))
                    {
                        string baseName = "New Material";
                        int index = 0;
                        string nName = baseName;

                        // Loop through the existing material names to find a unique name
                        while (materials.Any(m => m.Name == nName))
                        {
                            index++;
                            nName = $"{baseName}.{index.ToString("D3")}";
                        }

                        Material newMat = new(nName, Vector3.One, 0, 0.5f, 0, meshShader);
                        materials.Add(newMat);

                        sceneObjects[selectedIndex].Mesh.MaterialIndex = materials.Count - 1;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Delete Material") && sceneObjects[selectedIndex].Mesh.MaterialIndex != 0)
                    {
                        foreach (SceneObject sceneObject in sceneObjects) if (sceneObject.Mesh.MaterialIndex == sceneObjects[selectedIndex].Mesh.MaterialIndex) sceneObject.Mesh.MaterialIndex -= 1;
                        materials.RemoveAt(sceneObjects[selectedIndex].Mesh.MaterialIndex + 1);
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    SN.Vector4 color = new(_material.Color.X, _material.Color.Y, _material.Color.Z, 1);
                    ImGui.Text("Albedo");
                    if (ImGui.ColorEdit4("##colbutton", ref color)) _material.Color = new(color.X, color.Y, color.Z);

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    
                    if (ImGui.Button("Load Albedo Texture"))
                    {
                        OpenFileDialog selectFile = new OpenFileDialog()
                        {
                            Title = "Select File",
                            Filter = "Formats:|*.PNG;"
                        };
                        selectFile.ShowDialog();

                        string path = selectFile.FileName;

                        if (File.Exists(path))
                        {
                            materials[sceneObjects[selectedIndex].Mesh.MaterialIndex].ColorTexture = Texture.LoadFromFile(path);
                        }
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    float tempRoughness = _material.Roughness;
                    ImGui.Text("Roughness");
                    if (ImGui.SliderFloat("##Roughness", ref tempRoughness, 0, 1))
                    {
                        _material.Roughness = tempRoughness;
                        _material.SetShaderUniforms(meshShader);
                    }
                    if (ImGui.Button("Load Roughness Texture"))
                    {
                        OpenFileDialog selectFile = new OpenFileDialog()
                        {
                            Title = "Select File",
                            Filter = "Formats:|*.PNG;"
                        };
                        selectFile.ShowDialog();

                        string path = selectFile.FileName;

                        if (File.Exists(path))
                        {
                            materials[sceneObjects[selectedIndex].Mesh.MaterialIndex].RoughnessTexture = Texture.LoadFromFile(path);
                        }
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    float tempMetallic = _material.Metallic;
                    ImGui.Text("Metallic");
                    if (ImGui.SliderFloat("##Metallic", ref tempMetallic, 0, 1))
                    {
                        _material.Metallic = tempMetallic;
                        _material.SetShaderUniforms(meshShader);
                    }
                    if (ImGui.Button("Load Metallic Texture"))
                    {
                        OpenFileDialog selectFile = new OpenFileDialog()
                        {
                            Title = "Select File",
                            Filter = "Formats:|*.PNG;"
                        };
                        selectFile.ShowDialog();

                        string path = selectFile.FileName;

                        if (File.Exists(path))
                        {
                            materials[sceneObjects[selectedIndex].Mesh.MaterialIndex].MetallicTexture = Texture.LoadFromFile(path);
                        }
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    float tempEmission = _material.EmissionStrength;
                    ImGui.Text("Emission Strength");
                    if (ImGui.SliderFloat("##Emission Strength", ref tempEmission, 0, 100))
                    {
                        _material.EmissionStrength = tempEmission;
                        _material.SetShaderUniforms(meshShader);
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Separator();
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    if (ImGui.Button("Load Normal Texture"))
                    {
                        OpenFileDialog selectFile = new OpenFileDialog()
                        {
                            Title = "Select File",
                            Filter = "Formats:|*.PNG;"
                        };
                        selectFile.ShowDialog();

                        string path = selectFile.FileName;

                        if (File.Exists(path))
                        {
                            materials[sceneObjects[selectedIndex].Mesh.MaterialIndex].NormalTexture = Texture.LoadFromFile(path);
                        }
                    }
                }
            }
            
            ImGui.End();
        }

        private static string selectedFolderPath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
        private static Texture folderIcon = Texture.LoadFromFile("Resources/FolderIcon.png");
        private static Texture idk = Texture.LoadFromFile("Resources/failedtoload.png");

        public static void AssetBrowser()
        {
            ImGui.Begin("Folder View");
            
            string enginePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            DirectoryInfo directoryInfo = new DirectoryInfo(enginePath);

            if (ImGui.CollapsingHeader("Folders"))
            {
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    string folderName = directory.Name;         
                    bool hasSubdirectories = directory.GetDirectories().Length > 0;

                    if (hasSubdirectories)
                    {
                        if (ImGui.TreeNodeEx(folderName, ImGuiTreeNodeFlags.SpanAvailWidth))
                        {
                            selectedFolderPath = directory.FullName;

                            foreach (var subdirectory in directory.GetDirectories()) ShowSubdirectory(subdirectory);
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        ImGui.Indent(20);
                        if (ImGui.Selectable(folderName, false, ImGuiSelectableFlags.SpanAllColumns)) selectedFolderPath = directory.FullName;
                        ImGui.Unindent(20);
                    }
                }
            }

            ImGui.End();
        
            ImGui.Begin("Asset Browser");
            
            ImGui.Text(selectedFolderPath);
            ImGui.Separator();

            string[] files = System.IO.Directory.GetFileSystemEntries(selectedFolderPath);

            float thumbnailSize = 100f;
            float spacing = 10f;
            float contentWidth = ImGui.GetContentRegionAvail().X;
            int numColumns = Convert.ToInt32(Math.Floor((contentWidth + spacing) / (thumbnailSize + spacing)));
            numColumns = Math.Max(numColumns, 1);

            ImGui.Columns(numColumns, "AssetGrid", false);
            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);

                int crntHandle = idk.Handle;
                if (Directory.Exists(file)) crntHandle = folderIcon.Handle;

                ImGui.Image((IntPtr)crntHandle, new(thumbnailSize), new(0, 1), new(1, 0));
                ImGui.TextWrapped(fileName);

                ImGui.NextColumn();
            }

            // End the grid layout
            ImGui.Columns(1);

            ImGui.End();
        }

        private static void ShowSubdirectory(DirectoryInfo directory)
        {
            string folderName = directory.Name;
            bool hasSubdirectories = directory.GetDirectories().Length > 0;

            if (hasSubdirectories)
            {
                if (ImGui.TreeNodeEx(folderName, ImGuiTreeNodeFlags.SpanFullWidth))
                {
                    selectedFolderPath = directory.FullName;
                    foreach (var subdirectory in directory.GetDirectories()) ShowSubdirectory(subdirectory);

                    ImGui.TreePop();
                }
            }

            else
            {
                ImGui.Indent(20);
                if (ImGui.Selectable(folderName, false, ImGuiSelectableFlags.SpanAllColumns)) selectedFolderPath = directory.FullName;
                ImGui.Unindent(20);
            }
        }

        static int selectedIndex = 3;
        static float shadowBias = 0.0018f;
        static bool fxaaOnOff = true;
        static bool ACESonoff = true;

        static bool ssaoOnOff = true;
        static float ssaoRadius = 0.8f;
        static float SSAOpower = 0.5f;
        static int gaussianRadius = 3;

        static bool showImGUIdemo = false;
        static float strength = 1.75f;
        static float fontSize = 0.9f;

        static float outlineWidth = 3;
        static int outlineSteps = 12;

        public static void Settings(ref float camSpeed, ref bool vsyncOn, ref bool showDepth, ref bool showStats, ref int shadowRes, ref int depthMap, ref Vector3 direction, ref Vector3 ambient, ref float ShadowFactor, ref int numAOsamples, ref Shader defshader, ref Shader ppshader, ref Shader outlineShader, ref Shader fxaaShader, ref Shader SSAOshader, ref Shader PBRshader)
        {
            ImGui.Begin("Settings");

            if (ImGui.CollapsingHeader("Rendering"))
            {
                ImGui.Indent(20);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Checkbox(" VSync", ref vsyncOn);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Post Processing"))
            {
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                
                ImGui.Indent(20);
                if (ImGui.CollapsingHeader("SSAO"))
                {
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                    if (ImGui.Checkbox(" Use SSAO", ref ssaoOnOff))
                    {
                        ppshader.SetInt("ssaoOnOff", Convert.ToInt32(ssaoOnOff));
                        SSAOshader.SetInt("ssaoOnOff", Convert.ToInt32(ssaoOnOff));
                    }

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    
                    ImGui.Text("SSAO Radius");
                    if (ImGui.SliderFloat("##SSAO Radius", ref ssaoRadius, 0.0f, 5.0f, "%.1f")) ppshader.SetFloat("radius", ssaoRadius);
                    
                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Text("SSAO Strength");
                    if (ImGui.SliderFloat("##SSAO Power", ref SSAOpower, 0.0f, 5.0f, "%.1f")) ppshader.SetFloat("SSAOpower", SSAOpower);

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Text("SSAO Samples");
                    if (ImGui.SliderInt("##SSAO Samples", ref numAOsamples, 1, 128)) ppshader.SetInt("kernelSize", numAOsamples);

                    ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                    ImGui.Text("Gaussian Radius");
                    if (ImGui.SliderInt("##Gaussian Radius", ref gaussianRadius, 1, 16)) SSAOshader.SetInt("gaussianRadius", gaussianRadius);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                
                if (ImGui.Checkbox(" FXAA", ref fxaaOnOff)) fxaaShader.SetInt("fxaaOnOff", Convert.ToInt32(fxaaOnOff));
                
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                if (ImGui.Checkbox(" Tonemapping", ref ACESonoff)) defshader.SetInt("ACES", Convert.ToInt32(ACESonoff));
                
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Environment"))
            {
                ImGui.Indent(20);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                SN.Vector3 dir = new(direction.X, direction.Y, direction.Z);
                ImGui.Text("Sun Direction");
                if (ImGui.SliderFloat3("##Sun Direction", ref dir, -1, 1))
                {
                    direction = new(dir.X, dir.Y, dir.Z);
                    defshader.SetVector3("direction", direction);
                    PBRshader.SetVector3("direction", direction);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Text("Sun Strength");
                if (ImGui.SliderFloat("##Strength", ref strength, 0, 10, "%.1f"))
                {
                    defshader.SetFloat("dirStrength", strength);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                SN.Vector3 color = new(ambient.X, ambient.Y, ambient.Z);            
                ImGui.Text("Ambient Color");
                if (ImGui.ColorEdit3("##Ambient Color", ref color))
                {
                    ambient = new(color.X, color.Y, color.Z);
                    defshader.SetVector3("ambient", ambient);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Shadows"))
            {
                ImGui.Indent(20);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                float shadowFac = ShadowFactor;
                ImGui.Text("Shadow Factor");
                if (ImGui.SliderFloat("##Shadow Factor", ref shadowFac, 0, 1))
                {
                    ShadowFactor = shadowFac;
                    PBRshader.SetFloat("shadowFactor", ShadowFactor);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Text("Shadow Bias");
                if (ImGui.SliderFloat("##Shadow Bias", ref shadowBias, 0.0001f, 0.01f))
                {
                    PBRshader.SetFloat("shadowBias", shadowBias);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                int[] options = new int[] { 256, 512, 1024, 2048, 4096, 8192 };
                ImGui.Text("Shadow Resolution");
                if (ImGui.SliderInt("##Resolution", ref selectedIndex, 0, 5, options[selectedIndex].ToString()))
                {
                    shadowRes = options[selectedIndex];
                    GL.BindTexture(TextureTarget.Texture2D, depthMap);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, shadowRes, shadowRes, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Editor"))
            {
                ImGui.Indent(20);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Text("Camera Speed");
                ImGui.SliderFloat("##Camera Speed", ref camSpeed, 1, 20, "%.1f");

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Text("Font Size");
                if (ImGui.SliderFloat("##Font Size", ref fontSize, 0.1f, 2)) ImGui.GetIO().FontGlobalScale = fontSize;

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                
                ImGui.Text("Outline Width");
                if (ImGui.SliderFloat("##Outline Width", ref outlineWidth, 0.5f, 20, "%.1f")) outlineShader.SetFloat("radius", outlineWidth);
                
                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));
                
                ImGui.Text("Outline Steps");
                if (ImGui.SliderInt("##Outline Steps", ref outlineSteps, 1, 32)) outlineShader.SetInt("numSteps", outlineSteps);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Checkbox(" Show ImGUI Demo", ref showImGUIdemo);

                ImGui.Dummy(new System.Numerics.Vector2(0f, spacing));

                ImGui.Checkbox(" Show Stats Overlay", ref showStats);

                ImGui.Unindent();
            }

            if (showImGUIdemo) ImGui.ShowDemoWindow();
            
            ImGui.End();
        }

        public static void Header(double FPS, double MS, int meshCount)
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

            float textWidth = ImGui.CalcTextSize("FPS: " + FPS.ToString("0") + "      " + GL.GetString(StringName.Renderer) + "      " + "ms: " + MS.ToString("0.00")).X;
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - textWidth - 10);
            ImGui.TextColored(new(0.5f), "FPS: " + FPS.ToString("0") + "      " + "ms: " + MS.ToString("0.00") + "      " + GL.GetString(StringName.Renderer));

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
                    if (sceneObjects[i].Type == SceneObjectType.Light) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(new SN.Vector4(
                        sceneObjects[i].Light.lightColor.X,
                        sceneObjects[i].Light.lightColor.Y,
                        sceneObjects[i].Light.lightColor.Z, 1)));
                    ImGui.Text(sceneObjects[i].Type.ToString().ToLower() + " ");
                    ImGui.PopStyleColor();
                    if (sceneObjects[i].Type == SceneObjectType.Light) ImGui.PopStyleColor();
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

        unsafe public static void LoadTheme()
        {
            ImGui.GetStyle().FrameRounding = 2;
            ImGui.GetStyle().FrameBorderSize = 2;
            ImGui.GetStyle().FramePadding = new System.Numerics.Vector2(4);
            ImGui.GetStyle().ChildBorderSize = 0;
            ImGui.GetStyle().CellPadding = new SN.Vector2(3, 3);
            ImGui.GetStyle().ItemSpacing = new System.Numerics.Vector2(4, 2);
            ImGui.GetStyle().ItemInnerSpacing = new System.Numerics.Vector2(0, 4);
            ImGui.GetStyle().WindowPadding = new System.Numerics.Vector2(2, 2);
            ImGui.GetStyle().TabRounding = 4;
            ImGui.GetStyle().ColorButtonPosition = ImGuiDir.Left;
            ImGui.GetStyle().WindowRounding = 3;
            ImGui.GetStyle().WindowBorderSize = 0;
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;
            ImGui.GetStyle().SelectableTextAlign = new(0.02f, 0);
            ImGui.GetStyle().PopupBorderSize = 0;
            ImGui.GetStyle().GrabMinSize = 15;
            ImGui.GetStyle().GrabRounding = 2;
            
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(230, 230, 230, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(65, 65, 65, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new System.Numerics.Vector4(30, 30, 30, 200f) / 255);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, new System.Numerics.Vector4(255, 140, 0, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new System.Numerics.Vector4(12, 12, 12, 255) / 255);

            // Background color
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(20f, 20f, 20f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(45f, 45f, 45f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new System.Numerics.Vector4(40f, 40f, 40f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new System.Numerics.Vector4(80f, 80f, 80f, 255f) / 255);

            // Popup BG
            ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, new System.Numerics.Vector4(30f, 30f, 30f, 150f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, new System.Numerics.Vector4(150f, 150f, 150f, 255f) / 255);

            // Titles
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(14f, 14f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new System.Numerics.Vector4(14f, 14f, 14f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new System.Numerics.Vector4(14f, 14f, 14f, 255f) / 255);

            // Tabs
            ImGui.PushStyleColor(ImGuiCol.Tab, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabActive, new System.Numerics.Vector4(35f, 35f, 35f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new System.Numerics.Vector4(35f, 35f, 35f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new System.Numerics.Vector4(80f, 80f, 80f, 255f) / 255);
            
            // Header
            ImGui.PushStyleColor(ImGuiCol.Header, new System.Numerics.Vector4(40, 40, 40, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new System.Numerics.Vector4(100, 100, 100, 180f) / 255);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new System.Numerics.Vector4(70, 70, 70, 255f) / 255);

            // Rezising bar
            ImGui.PushStyleColor(ImGuiCol.Separator, new System.Numerics.Vector4(30f, 30f, 30f, 255) / 255);
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