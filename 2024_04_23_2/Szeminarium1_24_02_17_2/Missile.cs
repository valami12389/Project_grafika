using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Szeminarium1_24_02_17_2
{
    class Missile
    {
        public GlObject GlObject { get; }
        public Vector3D<float> Position { get; set; }
        public Vector3D<float> Direction { get; }
        public float Speed { get; } = 100f;
        public bool IsActive { get; set; } = true;

        public Missile(GlObject glObject, Vector3D<float> position, Vector3D<float> direction)
        {
            GlObject = glObject;
            Position = position;
            Direction = direction;
        }

        public void Update(float deltaTime)
        {
            Position += Direction * Speed * deltaTime;
        }
    }
}
