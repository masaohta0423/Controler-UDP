using UnityEngine;

namespace ControllerUdp
{
    /// <summary>Independent pose latch; buttons remain live while the pose is held.</summary>
    public sealed class ControllerPoseState
    {
        public bool HasPose { get; private set; }
        public bool Updating { get; private set; }
        public bool LiveValid { get; private set; }
        public Vector3 LivePosition { get; private set; }
        public Quaternion LiveRotation { get; private set; } = Quaternion.identity;
        public bool StickPressed { get; set; }
        public bool TriggerPressed { get; set; }
        public bool GripPressed { get; set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public Vector3 Velocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }

        public void Sample(bool valid, Vector3 position, Quaternion rotation, Vector3 velocity,
            Vector3 angularVelocity, bool reset = false)
        {
            if (reset) HasPose = false;
            LiveValid = valid;
            if (valid)
            {
                LivePosition = position;
                LiveRotation = rotation;
            }
            Updating = valid && StickPressed;
            if (Updating)
            {
                Position = position;
                Rotation = rotation;
                HasPose = true;
            }
            Velocity = Updating ? velocity : Vector3.zero;
            AngularVelocity = Updating ? angularVelocity : Vector3.zero;
        }
    }
}
