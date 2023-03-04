using SN = System.Numerics;
using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using GameEngine.Rendering;
using OpenTK.Windowing.Common;
using GameEngine.Common;

namespace GameEngine.ImGUI
{
    public static class ImGUICommands
    {
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
                "\n" +
                "Meshes: " + objectCount + "\n" +
                "Triangles: " + triangleCount.ToString("N0") + "\n" +
                "\n" +
                fps.ToString("0") + " FPS" + "\n" +
                ms.ToString("0.00") + " ms");
        }

        public static void Viewport(int framebufferTexture, out Vector2i windowSize, out Vector2i viewportPos, out bool viewportHovered)
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
        }

        public static void MaterialEditor(ref Material _material, ref Shader meshShader, ref Mesh mesh)
        {
            ImGui.Begin("Material Editor");

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

            bool tempSmoothShading = mesh.smoothShading;
            if (ImGui.Checkbox("Smooth Shading", ref tempSmoothShading))
            {
                mesh.smoothShading = tempSmoothShading;
                mesh.UpdateShading(tempSmoothShading);
            }

            ImGui.End();
        }

        public static void Settings(ref bool vsyncOn)
        {
            ImGui.Begin("Settings");

            ImGui.Checkbox("VSync", ref vsyncOn);
            
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


        public static void Outliner(List<Mesh> meshes, ref int selectedMeshIndex)
        {
            ImGui.Begin("Outliner", ImGuiWindowFlags.None);

            for (int i = 0; i < meshes.Count; i++)
            {
                bool isSelected = false;

                if (i % 2 == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, new SN.Vector4(0.6f, 0.6f, 0.6f, 1.0f));

                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, new SN.Vector4(0.2f, 0.2f, 0.2f, 1.0f));
                }

                ImGui.BeginGroup();

                if (ImGui.Selectable(meshes[i].name, isSelected))
                {
                    // Handle mesh selection
                }

                ImGui.EndGroup();
                ImGui.PopStyleColor();
            }

            ImGui.End();
        }

        public static void LoadTheme()
        {
            ImGui.GetStyle().FrameRounding = 6;
            ImGui.GetStyle().FrameBorderSize = 1;
            ImGui.GetStyle().TabRounding = 2;
            ImGui.GetStyle().WindowRounding = 7;
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;
            ImGui.GetStyle().GrabMinSize = 15;

            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(25, 25, 25, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new System.Numerics.Vector4(15, 15, 15, 200f) / 255);

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