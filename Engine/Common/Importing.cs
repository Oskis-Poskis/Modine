using Assimp;
using OpenTK.Mathematics;

using Modine.Rendering;
using Mesh = Modine.Rendering.Mesh;

namespace Modine.Common
{
    public static class ModelImporter
    {
        public static List<Mesh> LoadModel(string path, bool castShadow)
        {
            Modine.Common.EngineUtility.DllResolver.InitLoader();

            try
            {
                var importer = new AssimpContext();
                var scene = importer.ImportFile(path,
                    PostProcessPreset.TargetRealTimeMaximumQuality |
                    PostProcessSteps.GenerateSmoothNormals |
                    PostProcessSteps.CalculateTangentSpace |
                    PostProcessSteps.Triangulate);

                List<Mesh> meshes = new();

                for (int m = 0; m < scene.MeshCount; m++)
                {
                    var mesh = scene.Meshes[m];
                    var vertexCount = mesh.VertexCount;
                    var indexCount = mesh.FaceCount * 3;

                    VertexData[] tempData = new VertexData[vertexCount];

                    for (int i = 0; i < vertexCount; i++)
                    {
                        tempData[i].Position = FromVector(mesh.Vertices[i]);
                        tempData[i].Normals = FromVector(mesh.Normals[i]);
                        tempData[i].UVs = FromVector2D(new Vector2D(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y));
                        tempData[i].Tangents = FromVector(mesh.Tangents[i]);
                        tempData[i].BiTangents = FromVector(mesh.BiTangents[i]);
                    }

                    int[] indices = new int[indexCount];
                    for (int i = 0, j = 0; i < mesh.FaceCount; i++)
                    {
                        var face = mesh.Faces[i];
                        for (int k = 0; k < 3; k++)
                        {
                            indices[j++] = face.Indices[k];
                        }
                    }

                    /*Matrix4x4 transform = scene.RootNode.Children[m].Transform;
                    Vector3D position;
                    Assimp.Quaternion rotation;
                    Vector3D scale;
                    transform.Decompose(out scale, out rotation, out position);

                    Vector3 newRot;
                    OpenTK.Mathematics.Quaternion.ToEulerAngles(new(rotation.X, rotation.Y, rotation.Z, rotation.W), out newRot);*/

                    Mesh tempMesh = new(tempData, indices, true, 0);

                    meshes.Add(tempMesh);
                }

                return meshes;
            }

            catch (AssimpException ex)
            {
                Console.WriteLine("Assimp import error: " + ex.Message);
            }

            Mesh temp = new(new VertexData[0], new int[0], true, 0);

            List<Mesh> _meshes = new();
            _meshes.Add(temp);
            
            return _meshes;

            // Console.WriteLine("Indices: " + indices.Count() + " - " + "Vertices: " + vertdata.Count());
            //Console.WriteLine(mesh.HasTangentBasis);
            //Console.WriteLine($"Imported mesh '{mesh.Name}'\nVertices: {vertexCount}\nIndices: {indexCount}\n");
        }

        private static Vector3 FromVector(Assimp.Vector3D vec)
        {
            Vector3 v;
            v.X = vec.X;
            v.Y = vec.Y;
            v.Z = vec.Z;
            return v;
        }

        private static Vector2 FromVector2D(Assimp.Vector2D vec)
        {
            Vector2 v;
            v.X = vec.X;
            v.Y = vec.Y;
            return v;
        }
    }
}
