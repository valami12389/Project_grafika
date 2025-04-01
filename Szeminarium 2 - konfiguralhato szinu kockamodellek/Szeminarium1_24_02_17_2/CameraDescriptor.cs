using Silk.NET.Maths;
using System.ComponentModel;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        private Vector3D<float> position = new(0, 0, 3);
        private Vector3D<float> target = Vector3D<float>.Zero;
        private Vector3D<float> up = new(0, 1, 0);
        private float movementSpeed = 0.2f;
        private const float rotationSpeed = (float)(Math.PI / 180 * 5);

        public Vector3D<float> Position => position;
        public Vector3D<float> Target => target;
        public Vector3D<float> UpVector => up;

        private Vector3D<float> GetDirection()
        {
            return Vector3D.Normalize(target - position);
        }

        private Vector3D<float> GetCrossDirection(Vector3D<float> direction)
        {
            return Vector3D.Normalize(Vector3D.Cross(up, direction));
        }

        private void ApplyRotation(Vector3D<float> axis, float angle)
        {
            var direction = GetDirection();
            var rotation = Matrix4X4.CreateFromAxisAngle(axis, angle);
            var newDirection = Vector3D.Transform(direction, rotation);
            target = position + newDirection;
        }

        public void MoveForward()
        {
            var direction = GetDirection();
            position += direction * movementSpeed;
            target += direction * movementSpeed;
        }

        public void MoveBackward()
        {
            var direction = GetDirection();
            position -= direction * movementSpeed;
            target -= direction * movementSpeed;
        }

        public void MoveLeft()
        {
            var direction = GetDirection();
            var left = GetCrossDirection(direction);
            position -= left * movementSpeed;
            target -= left * movementSpeed;
        }

        public void MoveRight()
        {
            var direction = GetDirection();
            var right = GetCrossDirection(direction);
            position += right * movementSpeed;
            target += right * movementSpeed;
        }

        public void MoveUp()
        {
            position += up * movementSpeed;
            target += up * movementSpeed;
        }

        public void MoveDown()
        {
            position -= up * movementSpeed;
            target -= up * movementSpeed;
        }

        public void TurnLeft() => ApplyRotation(up, -rotationSpeed);
        public void TurnRight() => ApplyRotation(up, rotationSpeed);
        public void LookUp() => ApplyRotation(GetCrossDirection(GetDirection()), -rotationSpeed);
        public void LookDown() => ApplyRotation(GetCrossDirection(GetDirection()), rotationSpeed);
    }
}
