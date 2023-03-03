using SN = System.Numerics;
using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GameEngine.ImGUI
{
    public static class ImGUICommands
    {
        public static void SmallStats(Vector2i windowSize, Double fps, double ms)
        {
            ImGui.GetForegroundDrawList().AddText(
                new SN.Vector2(20, 40),
                ImGui.ColorConvertFloat4ToU32(new SN.Vector4(150, 150, 150, 255)),
                GL.GetString(StringName.Renderer) + "\n" +
                windowSize.X + " x " + windowSize.Y + "\n" +
                "\n" +
                fps.ToString("0") + " FPS" + "\n" +
                ms.ToString("0.00") + " ms");
        }

        public static void Viewport(int framebufferTexture)
        {
            ImGui.Begin("Viewport");
            GL.BindTexture(TextureTarget.Texture2D, framebufferTexture);
            ImGui.Image((IntPtr)framebufferTexture, new(
                MathHelper.Abs(ImGui.GetWindowContentRegionMin().X - ImGui.GetWindowContentRegionMax().X),
                MathHelper.Abs(ImGui.GetWindowContentRegionMin().Y - ImGui.GetWindowContentRegionMax().Y)),
                new(0, 1), new(1, 0), SN.Vector4.One, SN.Vector4.Zero);
            
        }

        public static void LoadTheme()
        {
            ImGui.GetStyle().FrameRounding = 6;
            ImGui.GetStyle().FrameBorderSize = 1;
            ImGui.GetStyle().TabRounding = 2;
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;

            // Background color
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(22f, 22f, 22f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(20f, 20f, 20f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new System.Numerics.Vector4(60f, 60f, 60f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new System.Numerics.Vector4(80f, 80f, 80f, 255f) / 255);

            // Popup BG
            ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, new System.Numerics.Vector4(30f, 30f, 30f, 150f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, new System.Numerics.Vector4(150f, 150f, 150f, 255f) / 255);

            // Titles
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(20f, 20f, 20f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new System.Numerics.Vector4(20f, 20f, 20f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new System.Numerics.Vector4(15f, 15f, 15f, 255f) / 255);

            // Tabs
            ImGui.PushStyleColor(ImGuiCol.Tab, new System.Numerics.Vector4(20f, 20f, 20f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabActive, new System.Numerics.Vector4(35f, 35f, 35f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new System.Numerics.Vector4(16f, 16f, 16f, 255f) / 255);
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
            ImGui.PushStyleColor(ImGuiCol.DockingPreview, new System.Numerics.Vector4(30, 140, 120, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new System.Numerics.Vector4(217, 35, 35, 255) / 255);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new System.Numerics.Vector4(217, 35, 35, 200) / 255);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new System.Numerics.Vector4(217, 35, 35, 150) / 255);
            ImGui.PushStyleColor(ImGuiCol.DockingEmptyBg, new System.Numerics.Vector4(20, 20, 20, 255) / 255);

            // Sliders, buttons, etc
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, new System.Numerics.Vector4(120f, 120f, 120f, 255f) / 255);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new System.Numerics.Vector4(180f, 180f, 180f, 255f) / 255);
        }
    }
}