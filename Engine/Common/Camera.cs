using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Modine.Common
{
    public class Camera
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 direction = -Vector3.UnitZ;
        public Vector3 forward = Vector3.UnitZ;
        public Vector3 right = Vector3.UnitX;
        public Vector3 up = Vector3.UnitY;

        public float theta = -90;
        public float phi = 0;

        public float speed;
        public float sensitivity = 0.5f;

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

            theta += deltaX * sensitivity;
            phi -= deltaY * sensitivity;

            if (theta < 0) theta += 360;
            else if (theta > 360) theta -= 360;
            phi = Math.Clamp(phi, -89, 89);

            direction = new Vector3((float)Math.Cos(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)),
                                    (float)Math.Sin(MathHelper.DegreesToRadians(phi)),
                                    (float)Math.Sin(MathHelper.DegreesToRadians(theta)) * (float)Math.Cos(MathHelper.DegreesToRadians(phi)));

            forward = new Vector3(MathF.Cos((float)MathHelper.DegreesToRadians(theta)) * MathF.Cos((float)MathHelper.DegreesToRadians(phi)), 
                                  MathF.Sin((float)MathHelper.DegreesToRadians(theta)) * MathF.Cos((float)MathHelper.DegreesToRadians(phi)), 
                                  MathF.Sin((float)MathHelper.DegreesToRadians(phi)));

            right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, forward));
        }

        public void ForwardBackward(float dir, float delta)
        {
            Vector3 targetPos = position;
            targetPos += speed * forward * dir;
            position = Vector3.Lerp(position, targetPos, delta * 2);
        }

        public void RightLeft(float dir, float delta)
        {
            Vector3 targetPos = position;
            targetPos += speed * right * dir;
            position = Vector3.Lerp(position, targetPos, delta * 2);
        }

        public void UpDown(float dir, float delta)
        {
            Vector3 targetPos = position;
            targetPos += speed * Vector3.UnitY * dir;
            position = Vector3.Lerp(position, targetPos, delta * 2);
        }
    }
}