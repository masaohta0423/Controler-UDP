using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

namespace ControllerUdp
{
    /// <summary>Sends Quest controller tracking and input as one UTF-8 JSON UDP datagram.</summary>
    public sealed class ControllerUdpSender : MonoBehaviour
    {
        [SerializeField] private string destinationAddress = "255.255.255.255";
        [SerializeField, Min(1)] private int destinationPort = 5005;
        [SerializeField, Range(1f, 120f)] private float sendRateHz = 72f;
        [SerializeField] private bool sendOnlyWhenTracked;

        private UdpClient client;
        private IPEndPoint endpoint;
        private float nextSendTime;
        private uint sequence;
        private readonly ControllerCalibration calibration = new ControllerCalibration();
        private bool leftGripPressed;
        private bool rightGripPressed;
        private readonly StringBuilder json = new StringBuilder(2048);

        public string DestinationAddress { get => destinationAddress; set => destinationAddress = value; }
        public int DestinationPort { get => destinationPort; set => destinationPort = value; }
        public ControllerCalibration Calibration => calibration;
        public ControllerPoseState LeftPose { get; } = new ControllerPoseState();
        public ControllerPoseState RightPose { get; } = new ControllerPoseState();

        private void Awake()
        {
            ApplyCommandLineArguments();
            try
            {
                endpoint = new IPEndPoint(IPAddress.Parse(destinationAddress), destinationPort);
                client = new UdpClient(AddressFamily.InterNetwork);
                client.EnableBroadcast = true;
                client.Client.SendBufferSize = 64 * 1024;
                Debug.Log($"Controller UDP: ready for {endpoint} at {sendRateHz:0.#} Hz; waiting for both grips to calibrate");
            }
            catch (Exception e)
            {
                Debug.LogError($"Controller UDP could not start: {e.Message}");
                enabled = false;
            }
        }

        private void Update()
        {
            UpdateHmdCameraPose();
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool leftTracked = IsTracked(left);
            bool rightTracked = IsTracked(right);
            leftGripPressed = GripPressed(left, leftGripPressed);
            rightGripPressed = GripPressed(right, rightGripPressed);
            bool leftValid = ReadPose(left, leftTracked, out Vector3 leftPosition, out Quaternion leftRotation);
            bool rightValid = ReadPose(right, rightTracked, out Vector3 rightPosition, out Quaternion rightRotation);
            bool recalibrated = calibration.Update(leftGripPressed && rightGripPressed, left.isValid && rightValid,
                leftPosition, rightPosition, rightRotation);
            if (recalibrated)
                Debug.Log($"Controller UDP: calibrated #{calibration.Revision}, origin={calibration.Origin}, right-controller rotation={calibration.Heading}");
            SampleHand(LeftPose, left, leftValid, leftPosition, leftRotation, leftGripPressed, recalibrated);
            SampleHand(RightPose, right, rightValid, rightPosition, rightRotation, rightGripPressed, recalibrated);
            if (!calibration.IsCalibrated) return;
            if (Time.unscaledTime < nextSendTime) return;
            nextSendTime = Time.unscaledTime + 1f / sendRateHz;
            if (sendOnlyWhenTracked && !leftTracked && !rightTracked) return;

            BuildJson(left, leftTracked, right, rightTracked);
            byte[] bytes = Encoding.UTF8.GetBytes(json.ToString());
            try { client.Send(bytes, bytes.Length, endpoint); }
            catch (Exception e) { Debug.LogWarning($"Controller UDP send failed: {e.Message}"); }
        }

        private static void UpdateHmdCameraPose()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            InputDevice hmd = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (hmd.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                camera.transform.localPosition = position;
            if (hmd.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                camera.transform.localRotation = rotation;
        }

        private static bool IsTracked(InputDevice device)
        {
            return device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
        }

        private static bool GripPressed(InputDevice device, bool wasPressed)
        {
            return Get(device, CommonUsages.gripButton, false) ||
                   Get(device, CommonUsages.grip, 0f) >= (wasPressed ? 0.6f : 0.8f);
        }

        private static bool ReadPose(InputDevice device, bool tracked, out Vector3 position, out Quaternion rotation)
        {
            bool hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out position);
            bool hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
            rotation = ControllerCalibration.LocalPoseRotation(rotation);
            return tracked && hasPosition && hasRotation;
        }

        private static void SampleHand(ControllerPoseState pose, InputDevice device, bool valid,
            Vector3 position, Quaternion rotation, bool gripPressed, bool reset)
        {
            pose.StickPressed = Get(device, CommonUsages.primary2DAxisClick, false);
            pose.TriggerPressed = Get(device, CommonUsages.triggerButton, false) ||
                                  Get(device, CommonUsages.trigger, 0f) >= 0.1f;
            pose.GripPressed = gripPressed;
            pose.Sample(valid, position, rotation, Get(device, CommonUsages.deviceVelocity, Vector3.zero),
                Get(device, CommonUsages.deviceAngularVelocity, Vector3.zero), reset);
        }

        private void BuildJson(InputDevice left, bool leftTracked, InputDevice right, bool rightTracked)
        {
            json.Clear();
            json.Append("{\"version\":2,\"sequence\":").Append(sequence++);
            json.Append(",\"timestampMs\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            json.Append(",\"coordinateSystem\":\"right-handed-x-forward-y-left-z-up\"");
            json.Append(",\"calibrated\":").Append(calibration.IsCalibrated ? "true" : "false");
            json.Append(",\"calibrationRevision\":").Append(calibration.Revision);
            json.Append(",\"left\":"); AppendController(left, leftTracked, LeftPose);
            json.Append(",\"right\":"); AppendController(right, rightTracked, RightPose);
            json.Append('}');
        }

        private void AppendController(InputDevice d, bool tracked, ControllerPoseState pose)
        {
            Vector3 position = pose.Position;
            Quaternion rotation = pose.Rotation;
            Vector3 velocity = pose.Velocity;
            Vector3 angularVelocity = pose.AngularVelocity;
            position = calibration.Position(position);
            rotation = calibration.Rotation(rotation);
            velocity = calibration.Velocity(velocity);
            angularVelocity = calibration.AngularVelocity(angularVelocity);
            Vector2 stick = Get(d, CommonUsages.primary2DAxis, Vector2.zero);

            json.Append("{\"connected\":").Append(d.isValid ? "true" : "false");
            json.Append(",\"tracked\":").Append(tracked ? "true" : "false");
            json.Append(",\"poseUpdating\":").Append(pose.Updating ? "true" : "false");
            json.Append(",\"poseValid\":").Append(pose.HasPose ? "true" : "false");
            json.Append(",\"position\":"); if (pose.HasPose) Append(position); else json.Append("null");
            json.Append(",\"rotation\":"); if (pose.HasPose) Append(rotation); else json.Append("null");
            json.Append(",\"velocity\":"); Append(velocity);
            json.Append(",\"angularVelocity\":"); Append(angularVelocity);
            json.Append(",\"trigger\":"); Number(Get(d, CommonUsages.trigger, 0f));
            json.Append(",\"grip\":"); Number(Get(d, CommonUsages.grip, 0f));
            json.Append(",\"stick\":"); Append(stick);
            json.Append(",\"buttons\":{");
            Bool("primary", Get(d, CommonUsages.primaryButton, false), false);
            Bool("secondary", Get(d, CommonUsages.secondaryButton, false));
            Bool("menu", Get(d, CommonUsages.menuButton, false));
            Bool("trigger", Get(d, CommonUsages.triggerButton, false));
            Bool("grip", Get(d, CommonUsages.gripButton, false));
            Bool("stickClick", Get(d, CommonUsages.primary2DAxisClick, false));
            Bool("stickTouch", Get(d, CommonUsages.primary2DAxisTouch, false));
            Bool("primaryTouch", Get(d, CommonUsages.primaryTouch, false));
            Bool("secondaryTouch", Get(d, CommonUsages.secondaryTouch, false));
            json.Append("}}");
        }

        private void Bool(string name, bool value, bool comma = true)
        {
            if (comma) json.Append(',');
            json.Append('\"').Append(name).Append("\":").Append(value ? "true" : "false");
        }

        private void Append(Vector3 v) { json.Append('['); Number(v.x); json.Append(','); Number(v.y); json.Append(','); Number(v.z); json.Append(']'); }
        private void Append(Vector2 v) { json.Append('['); Number(v.x); json.Append(','); Number(v.y); json.Append(']'); }
        private void Append(Quaternion q) { json.Append('['); Number(q.x); json.Append(','); Number(q.y); json.Append(','); Number(q.z); json.Append(','); Number(q.w); json.Append(']'); }
        private void Number(float value) => json.Append(value.ToString("R", CultureInfo.InvariantCulture));

        private static bool Get(InputDevice d, InputFeatureUsage<bool> u, bool f) => d.isValid && d.TryGetFeatureValue(u, out bool v) ? v : f;
        private static float Get(InputDevice d, InputFeatureUsage<float> u, float f) => d.isValid && d.TryGetFeatureValue(u, out float v) ? v : f;
        private static Vector2 Get(InputDevice d, InputFeatureUsage<Vector2> u, Vector2 f) => d.isValid && d.TryGetFeatureValue(u, out Vector2 v) ? v : f;
        private static Vector3 Get(InputDevice d, InputFeatureUsage<Vector3> u, Vector3 f) => d.isValid && d.TryGetFeatureValue(u, out Vector3 v) ? v : f;
        private static Quaternion Get(InputDevice d, InputFeatureUsage<Quaternion> u, Quaternion f) => d.isValid && d.TryGetFeatureValue(u, out Quaternion v) ? v : f;

        private void ApplyCommandLineArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "--udp-address") destinationAddress = args[++i];
                else if (args[i] == "--udp-port" && int.TryParse(args[++i], out int port)) destinationPort = port;
            }
        }

        private void OnDestroy()
        {
            client?.Close();
            client = null;
        }
    }
}
