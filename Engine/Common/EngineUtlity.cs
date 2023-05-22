using System.Reflection;
using System.Runtime.InteropServices;
using Modine.Rendering;
using OpenTK.Mathematics;
using static Modine.Rendering.Entity;

namespace Modine.Common
{
    public static class EngineUtility
    {
        public static string NewName(List<Entity> sceneObjects, string baseName)
        {
            if (sceneObjects.Count > 0)
            {
                int index = 0;
                string nName = baseName;

                // Loop through the existing material names to find a unique name
                while (sceneObjects.Any(m => m.Name == nName))
                {
                    index++;
                    nName = $"{baseName}.{index.ToString("D3")}";
                }

                return nName;
            }

            else return baseName;
        }

        public static bool ToggleBool(bool toggleBool)
        {
            bool _bool = false;

            if (toggleBool == true) _bool = false;
            if (toggleBool == false) _bool = true;

            return _bool;
        }

        public static int CalculateTriangles(List<Entity> sceneObjects)
        {
            int count = 0;
            foreach (Entity sceneObject in sceneObjects) if (sceneObject.Type == EntityType.Mesh) count += sceneObject.Mesh.vertexCount / 3;
            
            return count;
        }

        public static float MapRange(float value, float inputMin, float inputMax, float outputMin, float outputMax)
        {
            return ((value - inputMin) / (inputMax - inputMin)) * (outputMax - outputMin) + outputMin;
        }

        public static void CountEntities(List<Entity> entities, out int MeshCount, out int PointLightCount)
        {
            MeshCount = 0;
            PointLightCount = 0;
            foreach (Entity entity in entities)
            {
                if (entity.Type == EntityType.Mesh) MeshCount += 1;
                else if (entity.Type == EntityType.Light) PointLightCount += 1;
            }
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

        public static class DllResolver
        {
            static DllResolver()
            {
                NativeLibrary.SetDllImportResolver(typeof(Assimp.AssimpContext).Assembly, DllImportResolver);
            }

            public static void InitLoader() { }

            public static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
            {
                if (OperatingSystem.IsLinux())
                {
                    if (NativeLibrary.TryLoad("/lib/x86_64-linux-gnu/libdl.so.2", assembly, searchPath, out IntPtr lib))
                    {
                        Console.WriteLine("Exists");
                        return lib;
                    }
                }

                return IntPtr.Zero;
            }
        }
    }
}