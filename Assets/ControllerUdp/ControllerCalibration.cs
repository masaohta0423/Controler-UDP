using UnityEngine;

namespace ControllerUdp
{
    /// <summary>Session calibration and Unity-to-right-handed X-forward/Y-left/Z-up conversion.</summary>
    public sealed class ControllerCalibration
    {
        public bool IsCalibrated { get; private set; }
        public uint Revision { get; private set; }
        public Vector3 Origin { get; private set; }
        public Quaternion Heading { get; private set; } = Quaternion.identity;
        private bool bothGripsWerePressed;

        // Right-handed output Y is Unity local -X; reflection reverses the rotation sign.
        // Post-multiply so the correction is about the controller's local axis.
        public static Quaternion LocalPoseRotation(Quaternion rawRotation) =>
            (rawRotation * Quaternion.AngleAxis(45f, Vector3.right)).normalized;

        public bool Update(bool bothGripsPressed, bool posesValid, Vector3 leftPosition,
            Vector3 rightPosition, Quaternion rightRotation)
        {
            bool pressed = bothGripsPressed && !bothGripsWerePressed;
            bothGripsWerePressed = bothGripsPressed;
            if (!pressed || !posesValid) return false;
            Origin = rightPosition;
            Heading = rightRotation.normalized;
            IsCalibrated = true;
            Revision++;
            return true;
        }

        public Vector3 Position(Vector3 value) => Polar(Quaternion.Inverse(Heading) * (value - Origin));
        public Vector3 Velocity(Vector3 value) => Polar(Quaternion.Inverse(Heading) * value);
        // Angular velocity is an axial vector: a handedness change requires the extra minus sign.
        public Vector3 AngularVelocity(Vector3 value) => -Polar(Quaternion.Inverse(Heading) * value);
        public Quaternion Rotation(Quaternion value)
        {
            Quaternion relative = Quaternion.Inverse(Heading) * value;
            return new Quaternion(-relative.z, relative.x, -relative.y, relative.w);
        }
        public static Vector3 Polar(Vector3 value) => new Vector3(value.z, -value.x, value.y);
    }
}
