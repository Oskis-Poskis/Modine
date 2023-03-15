using Assimp;
using OpenTK.Mathematics;

using Modine.Rendering;

namespace Modine.Common
{
    public static class ModelImporter
    {
        public static void LoadModel(string path, out VertexData[] vertdata, out int[] indices, out string name)
        {
            var importer = new AssimpContext();
            var scene = importer.ImportFile(path,
                PostProcessPreset.TargetRealTimeMaximumQuality |
                PostProcessSteps.GenerateSmoothNormals |
                PostProcessSteps.CalculateTangentSpace);

            var mesh = scene.Meshes[0];
            var vertexCount = mesh.VertexCount;
            var indexCount = mesh.FaceCount * 3;

            vertdata = new VertexData[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertdata[i].Position = FromVector(mesh.Vertices[i]);
                vertdata[i].Normals = FromVector(mesh.Normals[i]);
                if (mesh.HasTextureCoords(0)) vertdata[i].UVs = FromVector2(new Vector2D(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y));
                else vertdata[i].UVs = new(1, 1);
                vertdata[i].Tangents = FromVector(mesh.Tangents[i]);
                vertdata[i].BiTangents = FromVector(mesh.BiTangents[i]);
            }

            Console.WriteLine(mesh.HasTangentBasis);

            indices = new int[indexCount];
            for (int i = 0, j = 0; i < mesh.FaceCount; i++)
            {
                var face = mesh.Faces[i];
                for (int k = 0; k < 3; k++)
                {
                    indices[j++] = face.Indices[k];
                }
            }
            
            name = mesh.Name;
            //Console.WriteLine(mesh.HasTangentBasis);
            //Console.WriteLine($"Imported mesh '{mesh.Name}'\nVertices: {vertexCount}\nIndices: {indexCount}\n");
        }

        public static void LoadModel(string path, out VertexData[] vertdata, out int[] indices)
        {
            string name;
            LoadModel(path, out vertdata, out indices, out name);
        }

        private static Vector3 FromVector(Assimp.Vector3D vec)
        {
            Vector3 v;
            v.X = vec.X;
            v.Y = vec.Y;
            v.Z = vec.Z;
            return v;
        }

        private static Vector2 FromVector2(Assimp.Vector2D vec)
        {
            Vector2 v;
            v.X = vec.X;
            v.Y = vec.Y;
            return v;
        }
    }
}
