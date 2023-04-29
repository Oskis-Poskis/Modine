using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Modine.Common
{
    public class Camera
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 direction = -Vector3.UnitZ;

        public float theta = -90;
        public float phi = 0;

        public float speed;
        public float sensitivity = 0.5f;

        public bool trackball = false;

        public Camera(Vector3 startPosition, Vector3 startDirection, float startSpeed = 5)
        {
            position = startPosition;
            direction = startDirection;
            speed = startSpeed;
        }

        public void UpdateCamera(MouseState state)
        {
            if (!trackball)
            {
                float deltaX = state.Delta.X;
                float deltaY = state.Delta.Y;
                theta += deltaX * sensitivity;
                phi -= deltaY * sensitivity;

                if (theta < 0) theta += 360;
                else if (theta > 360) theta -= 360;
                phi = Math.Clamp(phi, -89, 89);

                direction = new Vector3((float)Math.Cos(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)),
                                        (float)Math.Sin(MathHelper.DegreesToRadians(phi)),
                                        (float)Math.Sin(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)));
            }
        }
    }
}