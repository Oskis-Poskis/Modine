using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Modine.Common
{
    public class Camera
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 direction = -Vector3.UnitZ;

        public Matrix4 lightSpaceMatrix, projectionMatrix, viewMatrix;

        public float theta = -90;
        public float phi = 0;

        public float speed;
        public float sensitivity = 0.5f;
        public int FOV;

        public bool trackball = false;
        public float distance = 10;

        public Camera(Vector3 startPosition, Vector3 startDirection, int fov, float startSpeed = 5)
        {
            position = startPosition;
            direction = startDirection;
            speed = startSpeed;
            FOV = fov;

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), 1, Game.nearPlane, Game.farPlane);
            viewMatrix = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitY, new(0, 1, 0));
        }

        public void Input(MouseState state, Vector3 selectedPos)
        {
            float deltaX = state.Delta.X;
            float deltaY = state.Delta.Y;
            theta += deltaX * sensitivity;
            phi -= deltaY * sensitivity;

            distance += state.ScrollDelta.Y;

            if (theta < 0) theta += 360;
            else if (theta > 360) theta -= 360;
            phi = Math.Clamp(phi, -89, 89);

            if (!trackball)
            {
                direction = new Vector3((float)Math.Cos(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)),
                                        (float)Math.Sin(MathHelper.DegreesToRadians(phi)),
                                        (float)Math.Sin(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)));
            }

            else
            {
                float tempPhi = phi * -1;
                position = selectedPos + distance * new Vector3(
                    (float)Math.Cos(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)),
                    (float)Math.Sin(MathHelper.DegreesToRadians(tempPhi)),
                    (float)Math.Sin(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)));

                direction = Vector3.Normalize(selectedPos - position);
            }
        }

        public void Update(Vector2 viewportSize)
        {
            float aspectRatio = (float)viewportSize.X / viewportSize.Y;
            lightSpaceMatrix = Matrix4.LookAt(Game.SunDirection * 10, Vector3.Zero, Vector3.UnitY) * Matrix4.CreateOrthographicOffCenter(-15, 15, -15, 15, 0.1f, 100);
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), aspectRatio, Game.nearPlane, Game.farPlane);
            viewMatrix = Matrix4.LookAt(position, position + direction, Vector3.UnitY);
        }
    }
}