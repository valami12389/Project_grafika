using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Szeminarium1_24_02_17_2
{
    public class Spaceship
    {
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
        public float Speed { get; set; } = 10.0f;
        public float RotationAngle { get; set; } = 0.0f;

        public void MoveForward(float deltaTime)
        {
            Position += new Vector3D<float>(0, 0, -Speed * deltaTime);
        }

        public void MoveBackward(float deltaTime)
        {
            Position += new Vector3D<float>(0, 0, Speed * deltaTime);
        }

        public void MoveLeft(float deltaTime)
        {
            Position += new Vector3D<float>(-Speed * deltaTime, 0, 0);
        }

        public void MoveRight(float deltaTime)
        {
            Position += new Vector3D<float>(Speed * deltaTime, 0, 0);
        }

        public void MoveUp(float deltaTime)
        {
            Position += new Vector3D<float>(0, Speed * deltaTime, 0);
        }

        public void MoveDown(float deltaTime)
        {
            Position += new Vector3D<float>(0, -Speed * deltaTime, 0);
        }
    }
}
