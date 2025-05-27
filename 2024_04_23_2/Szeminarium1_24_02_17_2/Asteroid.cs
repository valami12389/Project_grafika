using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Szeminarium1_24_02_17_2
{
    public class Asteroid
    {
        public GlObject GlObject { get; }
        public Vector3D<float> Position { get; set; }
        public float Scale { get; }
        public Vector3D<float> RotationAxis { get; }
        public float RotationAngle { get; set; }
        public Vector3D<float> Direction { get; set; }
        public bool HasCausedDamage { get; set; } = false;
        public float Speed { get; }

        public Asteroid(GlObject glObject, Vector3D<float> position, float scale, float speed, Vector3D<float> spaceshipPosition)
        {
            GlObject = glObject;
            Position = position;
            Scale = scale;
            RotationAxis = new Vector3D<float>(1f, 0f, 0f);

            RotationAngle = 0;
            Direction = Vector3D.Normalize(spaceshipPosition - position);
            Speed = speed;
        }
    }
}
