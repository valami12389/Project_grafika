using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    public class Spaceship
    {
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
        public float Speed { get; set; } = 10.0f;

        public float RollAngle { get; private set; } = 0.0f;   
        public float PitchAngle { get; private set; } = 0.0f;  
        private const float MaxTiltAngle = MathF.PI / 6f; 
        private const float TiltSpeed = 5f;
        private const float ReturnToCenterSpeed = 3f;

        private bool isMovingLeft = false;
        private bool isMovingRight = false;
        private bool isMovingUp = false;
        private bool isMovingDown = false;

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
            Position += new Vector3D<float>(Speed * deltaTime, 0, 0);
            isMovingRight = true;
            isMovingLeft = false;
        }

        public void StopMovingUpDown()
        {
            isMovingUp = false;
            isMovingDown = false;
        }

        public void StopMovingLeftRight()
        {
            isMovingLeft = false;
            isMovingRight = false;
        }

        public void MoveRight(float deltaTime)
        {
            Position += new Vector3D<float>(-Speed * deltaTime, 0, 0);
            isMovingLeft = true;
            isMovingRight = false;
        }

        public void MoveUp(float deltaTime)
        {
            Position += new Vector3D<float>(0, Speed * deltaTime, 0);
            isMovingUp = true;
            isMovingDown = false;
        }

        public void MoveDown(float deltaTime)
        {
            Position += new Vector3D<float>(0, -Speed * deltaTime, 0);
            isMovingDown = true;
            isMovingUp = false;
        }

        public void Update(float deltaTime)
        {
            if (isMovingLeft)
            {
                PitchAngle = Math.Min(PitchAngle + TiltSpeed * deltaTime, MaxTiltAngle);
            }
            else if (isMovingRight)
            {
                PitchAngle = Math.Max(PitchAngle - TiltSpeed * deltaTime, -MaxTiltAngle);
            }
            else
            {
                RollAngle = SmoothDamp(RollAngle, 0, ReturnToCenterSpeed * deltaTime);
            }
            if (isMovingUp)
            {
                RollAngle = Math.Max(RollAngle - TiltSpeed * deltaTime, -MaxTiltAngle);
               
            }
            else if (isMovingDown)
            {
                RollAngle = Math.Min(RollAngle + TiltSpeed * deltaTime, MaxTiltAngle);
                
            }
            else
            {
                PitchAngle = SmoothDamp(PitchAngle, 0, ReturnToCenterSpeed * deltaTime);
            }
        }

        private float SmoothDamp(float current, float target, float speed)
        {
            return current + (target - current) * speed;
        }
    }
}