using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    public class Spaceship
    {

        public Vector3D<float> KnockbackVelocity { get; set; } = Vector3D<float>.Zero;
        public float KnockbackDecay { get; set; } = 5.0f; 
        public bool IsBeingKnockedBack { get; set; } = false;
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
        public float Speed { get; set; } = 50.0f;

        public float RollAngle { get; private set; } = 0.0f;
        public float PitchAngle { get; private set; } = 0.0f;
        public Vector3D<float> BoundingBoxSize { get; } = new Vector3D<float>(5f, 5f, 10f);
        private const float MaxTiltAngle = MathF.PI / 6f;
        private const float TiltSpeed = 5f;
        private const float ReturnToCenterSpeed = 3f;

        public bool isMovingForward { get; set; } = false;
        public bool isMovingBackward { get; set; } = false;
        public bool isMovingLeft { get; set; } = false;
        public bool isMovingRight { get; set; } = false;
        public bool isMovingUp { get; set; } = false;
        public bool isMovingDown { get; set; } = false;

        public float MaxHealth { get; } = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public float invulnerabilityTimer { get; set; } = 0f;
        public  float InvulnerabilityDuration { get; set; } = 1f;

        public void ApplyKnockback(Vector3D<float> knockbackDirection, float knockbackForce)
        {
            KnockbackVelocity = knockbackDirection * knockbackForce;
            IsBeingKnockedBack = true;
        }

        public Spaceship()
        {
            CurrentHealth = MaxHealth;
        }

        public void MoveForward(float deltaTime)
        {
            Position += new Vector3D<float>(0, 0, Speed * deltaTime);
        }

        public void MoveBackward(float deltaTime)
        {
            Position += new Vector3D<float>(0, 0, -Speed * deltaTime);
        }

        public void MoveLeft(float deltaTime)
        {
            Position += new Vector3D<float>(Speed * deltaTime, 0, 0);
        }

        public void TakeDamage(float damage)
        {
            if (invulnerabilityTimer > 0) return;

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            invulnerabilityTimer = InvulnerabilityDuration;

            Console.WriteLine($"Damage taken! Health: {CurrentHealth}");
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
        }

        public void MoveUp(float deltaTime)
        {
            Position += new Vector3D<float>(0, Speed * deltaTime, 0);
        }

        public void MoveDown(float deltaTime)
        {
            Position += new Vector3D<float>(0, -Speed * deltaTime, 0);
        }

        public Vector3D<float> ForwardVector
        {
            get
            {
                return Vector3D.Normalize(new Vector3D<float>(0, 0, 1));
            }
        }

        public Vector3D<float> RightVector
        {
            get
            {
                return Vector3D.Normalize(new Vector3D<float>(1, 0, 0));
            }
        }

        public void Reset() {
            Position = new Vector3D<float>(0, 0, 0);
            CurrentHealth = MaxHealth;
            invulnerabilityTimer = 0;
            RollAngle = 0;
            PitchAngle = 0;
            isMovingLeft = false;
            isMovingRight = false;
            isMovingForward = false;
            isMovingBackward = false;
            isMovingUp = false;
            isMovingDown = false;
        }

        public void Update(float deltaTime)
        {

            if (isMovingForward) MoveForward(deltaTime);
            if (isMovingBackward) MoveBackward(deltaTime);
            if (isMovingLeft) MoveLeft(deltaTime);
            if (isMovingRight) MoveRight(deltaTime);
            if (isMovingUp) MoveUp(deltaTime);
            if (isMovingDown) MoveDown(deltaTime);

            if (invulnerabilityTimer > 0)
            {
                invulnerabilityTimer -= deltaTime;
                if (invulnerabilityTimer < 0)
                    invulnerabilityTimer = 0;
            }

            if (IsBeingKnockedBack)
            {
                Position += KnockbackVelocity * deltaTime;

                KnockbackVelocity *= (1.0f - KnockbackDecay * deltaTime);

                if (KnockbackVelocity.Length < 0.1f)
                {
                    KnockbackVelocity = Vector3D<float>.Zero;
                    IsBeingKnockedBack = false;
                }
            }

            if (!IsBeingKnockedBack)
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
        }

        private float SmoothDamp(float current, float target, float speed)
        {
            return current + (target - current) * speed;
        }
    }
}