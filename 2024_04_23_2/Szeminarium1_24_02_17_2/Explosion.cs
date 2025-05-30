using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Szeminarium1_24_02_17_2
{
    class Explosion
    {
        public GlObject GlObject { get; }
        public Vector3D<float> Position { get; }
        public float Scale { get; set; }
        public float Lifetime { get; set; }
        public float MaxLifetime { get; }
        public bool IsActive => Lifetime > 0;
        public float[] Color { get; set; }
        public float Speed { get; set; }

        public Explosion(GlObject glObject, Vector3D<float> position, float maxLifetime = 1.0f)
        {
            GlObject = glObject;
            Position = position;
            Scale = 0.5f;
            MaxLifetime = maxLifetime;
            Lifetime = maxLifetime;
            Speed = (float)(Random.Shared.NextDouble() * 3.0 + 2.0);
            Color = new float[] {
            Math.Clamp((float)(Random.Shared.NextDouble() * 0.5 + 0.5), 0.5f, 1.0f),
            Math.Clamp((float)(Random.Shared.NextDouble() * 0.5), 0.0f, 0.5f),     
            Math.Clamp((float)(Random.Shared.NextDouble() * 0.3), 0.0f, 0.3f),     
            1.0f                                                                  
        };
        }

        public void Update(float deltaTime)
        {
            if (IsActive)
            {
                Lifetime -= deltaTime;
                Scale += deltaTime * Speed;

                Color[3] = Lifetime / MaxLifetime;
            }
        }
    }
}
