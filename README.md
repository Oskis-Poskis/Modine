# Modine
## This is my final high school project, a 3D engine made with OpenGL and C#
**GUI** using [ImGUI.NET](https://www.nuget.org/packages/ImGui.NET)

**3D Model Loading** using [AssimpNet](https://www.nuget.org/packages/AssimpNet)

## Editor Usage
First person camera rotation
> Right click and move mouse

Fly navigation
> WASD

Up and down
> Q and E

Toggle Fullscreen
> Ctrl + Space

Open Quick Menu
> Shift + Space

## Features
Raycasting

PBR Shading

Point Lights

Directional light PCF shadow-mapping

Screen space ambient occlusion (SSAO)

Fast Approximate Anti-Aliasing (FXAA)

Object outlines using stencilbuffer and fragmentshader

Modify object transform in viewport

Viewport, outliner, settings, properties and material editor using ImGUI Docking

Framebuffer resizing to fit ImGUI window

## Problems
Normals/TBN matrix is glitched on Intel iGPU

Shadow mapping not entirely accurate

Famebuffer glitching on some drivers/devices

Deffered renderer color attachments BAAAD performance

## Useful Resources
LearnOpenTK:
>https://opentk.net/learn/index.html

LearnOpenGL:
>https://learnopengl.com/Introduction

Two-Bit Coding OpenTK tutorials:
>https://www.youtube.com/watch?v=ZOxgD16C3GM&list=PLSlpr6o9vURyxd-keTeGLXy980pF7boki

Victor Gordan OpenGL tutorials:
>https://www.youtube.com/watch?v=XpBGwZNyUh0&list=PLPaoO-vpZnumdcb4tZc4x5Q-v7CkrQ6M-

Assimp Example:
>https://github.com/assimp/assimp-net/blob/master/AssimpNet.Sample/SimpleOpenGLSample.cs

ImGUI Demo:
>https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp

ImGUI Controller Class By NogginBops:
>https://github.com/NogginBops/ImGui.NET_OpenTK_Sample/blob/opentk4.0/Dear%20ImGui%20Sample/ImGuiController.cs
