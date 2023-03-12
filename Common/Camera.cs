using OpenTK.Mathematics;

namespace Modine.Common
{
    public class Camera
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 direction = -Vector3.UnitY;
        public float speed;

        public Camera(Vector3 startPosition, Vector3 startDirection, float startSpeed = 5)
        {
            position = startPosition;
            direction = startDirection;
            speed = startSpeed;
        }
    }
}