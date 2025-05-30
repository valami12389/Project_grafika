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

        public Explosion(GlObject glObject, Vector3D<float> position, float maxLifetime = 1.0f)
        {
            GlObject = glObject;
            Position = position;
            Scale = 1.0f;
            MaxLifetime = maxLifetime;
            Lifetime = maxLifetime;
        }

        public void Update(float deltaTime)
        {
            if (IsActive)
            {
                Lifetime -= deltaTime;
                Scale += deltaTime * 5.0f;
            }
        }
    }
}
