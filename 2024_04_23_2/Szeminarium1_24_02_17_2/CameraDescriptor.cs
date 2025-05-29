using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    public enum CameraMode
    { 
        External,
        Cockpit
    }
    internal class CameraDescriptor
    {
        private double DistanceToOrigin = 4;

        private double AngleToZYPlane = 0;

        private double AngleToZXPlane = 0;

        private const double DistanceScaleFactor = 1.1;

        private const double AngleChangeStepSize = Math.PI / 180 * 5;

        private Vector3D<float> externalOffset = new Vector3D<float>(0, -5, 40);

        public CameraMode CurrentMode { get; set; } = CameraMode.External;
        private Spaceship targetSpaceship;

        private Vector3D<float> cockpitOffset = new Vector3D<float>(0f, 0.5f, -1f);

        public CameraDescriptor(Spaceship spaceship = null)
        {
            targetSpaceship = spaceship;
        }

        public void SetTargetSpaceship(Spaceship spaceship)
        {
            targetSpaceship = spaceship;
        }

        public void ToggleCameraMode()
        {
            CurrentMode = CurrentMode == CameraMode.External ? CameraMode.Cockpit : CameraMode.External;
        }

        public Vector3D<float> Position
        {
            get
            {
                if (CurrentMode == CameraMode.Cockpit && targetSpaceship != null)
                {
                    return GetCockpitCameraPosition();
                }
                else if (targetSpaceship != null)
                {
                    var rotationMatrix = Matrix4X4.CreateRotationX(targetSpaceship.RollAngle) *
                                       Matrix4X4.CreateRotationZ(targetSpaceship.PitchAngle);

                    var transformedOffset = Vector3D.Transform(externalOffset, rotationMatrix);
                    return targetSpaceship.Position + transformedOffset;
                }
                else
                {
                    return GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane);
                }
            }
        }

        public Vector3D<float> UpVector
        {
            get
            {
                if (CurrentMode == CameraMode.Cockpit && targetSpaceship != null)
                {    
                    return Vector3D<float>.UnitY;
                }
                else
                {
                    return Vector3D.Normalize(GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane + Math.PI / 2));
                }
            }
        }

        public Vector3D<float> Target
        {
            get
            {
                if (CurrentMode == CameraMode.Cockpit && targetSpaceship != null)
                {
                    return GetCockpitLookTarget();
                }
                else if (targetSpaceship != null)
                {
                    var forwardDirection = new Vector3D<float>(0f, 0f, 10f);
                    var rotationMatrix = Matrix4X4.CreateRotationX(targetSpaceship.RollAngle) *
                                       Matrix4X4.CreateRotationZ(targetSpaceship.PitchAngle);
                    var transformedDirection = Vector3D.Transform(forwardDirection, rotationMatrix);
                    return targetSpaceship.Position + transformedDirection;
                }
                else
                {
                    return Vector3D<float>.Zero;
                }
            }
        }

        private Vector3D<float> GetCockpitCameraPosition()
        {
            if (targetSpaceship == null) return Vector3D<float>.Zero;
            var rotationMatrix = Matrix4X4.CreateRotationX(targetSpaceship.RollAngle) *
                               Matrix4X4.CreateRotationZ(targetSpaceship.PitchAngle);

            var transformedOffset = Vector3D.Transform(cockpitOffset, rotationMatrix);

            return targetSpaceship.Position + transformedOffset;
        }

        private Vector3D<float> GetCockpitLookTarget()
        {
            if (targetSpaceship == null) return Vector3D<float>.Zero;
            var forwardDirection = new Vector3D<float>(0f, 0f, 10f);

            var rotationMatrix = Matrix4X4.CreateRotationX(targetSpaceship.RollAngle) *
                               Matrix4X4.CreateRotationZ(targetSpaceship.PitchAngle);

            var transformedDirection = Vector3D.Transform(forwardDirection, rotationMatrix);

            return targetSpaceship.Position + transformedDirection;
        }


        public void IncreaseZXAngle()
        {
            if (CurrentMode == CameraMode.External)
                AngleToZXPlane += AngleChangeStepSize;
        }

        public void DecreaseZXAngle()
        {
            if (CurrentMode == CameraMode.External)
                AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            if (CurrentMode == CameraMode.External)
                AngleToZYPlane += AngleChangeStepSize;

        }

        public void DecreaseZYAngle()
        {
            if (CurrentMode == CameraMode.External)
                AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            if (CurrentMode == CameraMode.External)
                DistanceToOrigin = DistanceToOrigin * DistanceScaleFactor;
        }

        public void DecreaseDistance()
        {
            if (CurrentMode == CameraMode.External)
                DistanceToOrigin = DistanceToOrigin / DistanceScaleFactor;
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}
