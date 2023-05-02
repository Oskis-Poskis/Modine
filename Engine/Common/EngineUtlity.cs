using Modine.Rendering;
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
    }
}