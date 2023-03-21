using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Modine.Common
{
    public class Camera
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 direction = -Vector3.UnitY;
        public float yaw = (MathHelper.Pi / 2) * 3;
        public float pitch = 0.0f;
        public float speed;
        public float sensitivity = 0.006f;

        public Camera(Vector3 startPosition, Vector3 startDirection, float startSpeed = 5)
        {
            position = startPosition;
            direction = startDirection;
            speed = startSpeed;
        }

        public void UpdateCamera(MouseState state)
        {
            float deltaX = state.Delta.X;
            float deltaY = state.Delta.Y;
            yaw += deltaX * sensitivity;
            pitch -= deltaY * sensitivity;
            pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.005f, MathHelper.PiOver2 - 0.005f);

            direction = new Vector3(
                (float)Math.Cos(yaw) * (float)Math.Cos(pitch),
                (float)Math.Sin(pitch),
                (float)Math.Sin(yaw) * (float)Math.Cos(pitch));
        }
    }
}