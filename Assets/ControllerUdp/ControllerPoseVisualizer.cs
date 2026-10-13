using UnityEngine;

namespace ControllerUdp
{
    /// <summary>Draws a fixed calibration frame and the live local controller frames.</summary>
    public sealed class ControllerPoseVisualizer : MonoBehaviour
    {
        private const float AxisLength = 0.05f;
        private const float AxisThickness = 0.006f;

        private Transform leftAxes;
        private Transform rightAxes;
        private Transform calibrationAxes;
        private Transform leftLiveAxes;
        private Transform rightLiveAxes;
        private TextMesh leftStatus;
        private TextMesh rightStatus;
        private ControllerUdpSender sender;
        private Material red;
        private Material green;
        private Material blue;
        private Material white;
        private Material ghostRed;
        private Material ghostGreen;
        private Material ghostBlue;
        private Material ghostWhite;
        private Material textMaterial;
        private Font statusFont;

        private void Awake()
        {
            red = CreateMaterial(Color.red);
            green = CreateMaterial(Color.green);
            blue = CreateMaterial(Color.blue);
            white = CreateMaterial(Color.white);
            leftAxes = CreateAxes("Left Controller Axes");
            rightAxes = CreateAxes("Right Controller Axes");
            calibrationAxes = CreateAxes("Calibration Origin Axes");
            calibrationAxes.gameObject.SetActive(false);
            ghostRed = CreateMaterial(new Color(1, 0, 0, 0.25f), "ControllerAxesGhost");
            ghostGreen = CreateMaterial(new Color(0, 1, 0, 0.25f), "ControllerAxesGhost");
            ghostBlue = CreateMaterial(new Color(0, 0, 1, 0.25f), "ControllerAxesGhost");
            ghostWhite = CreateMaterial(new Color(1, 1, 1, 0.25f), "ControllerAxesGhost");
            leftLiveAxes = CreateAxes("Left Live Pose", true);
            rightLiveAxes = CreateAxes("Right Live Pose", true);
            statusFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusFont.RequestCharactersInTexture("LEFT RIGHT STICK TRIGGER GRIP ON OFF", 48, FontStyle.Normal);
            textMaterial = CreateMaterial(Color.white, "ControllerStatusText");
            leftStatus = CreateStatus(leftLiveAxes, "Left Input Status");
            rightStatus = CreateStatus(rightLiveAxes, "Right Input Status");
            sender = GetComponent<ControllerUdpSender>();
        }

        private void LateUpdate()
        {
            ControllerCalibration calibration = sender != null ? sender.Calibration : null;
            bool calibrated = calibration != null && calibration.IsCalibrated;
            UpdateHand(leftAxes, leftLiveAxes, leftStatus, sender != null ? sender.LeftPose : null, "L", calibrated);
            UpdateHand(rightAxes, rightLiveAxes, rightStatus, sender != null ? sender.RightPose : null, "R", calibrated);
            textMaterial.mainTexture = statusFont.material.mainTexture;
            calibrationAxes.gameObject.SetActive(calibrated);
            if (calibrated)
                SetTrackingPose(calibrationAxes, calibration.Origin, calibration.Heading);
        }

        private static void UpdateHand(Transform axes, Transform liveAxes, TextMesh status,
            ControllerPoseState pose, string label, bool calibrated)
        {
            bool hasPose = pose != null && pose.HasPose;
            axes.gameObject.SetActive(hasPose && calibrated);
            liveAxes.gameObject.SetActive(pose != null && pose.LiveValid);
            if (pose == null) return;
            if (hasPose) SetTrackingPose(axes, pose.Position, pose.Rotation);
            if (pose.LiveValid) SetTrackingPose(liveAxes, pose.LivePosition, pose.LiveRotation);
            string text = $"{label} STICK {(pose.StickPressed ? "ON" : "OFF")}\nTRIGGER {(pose.TriggerPressed ? "ON" : "OFF")}\nGRIP {(pose.GripPressed ? "ON" : "OFF")}";
            if (status.text != text) status.text = text;
            Camera camera = Camera.main;
            if (camera != null)
            {
                status.transform.position = liveAxes.position + camera.transform.right * 0.075f + camera.transform.up * 0.025f;
                status.transform.rotation = camera.transform.rotation;
            }
        }

        private TextMesh CreateStatus(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMesh>();
            text.font = statusFont;
            text.fontSize = 48;
            text.characterSize = 0.0025f;
            text.anchor = TextAnchor.MiddleLeft;
            text.alignment = TextAlignment.Left;
            text.color = Color.white;
            go.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
            return text;
        }

        private static void SetTrackingPose(Transform axes, Vector3 position, Quaternion rotation)
        {
            Transform trackingOrigin = Camera.main != null ? Camera.main.transform.parent : null;
            if (trackingOrigin != null)
                axes.SetPositionAndRotation(trackingOrigin.TransformPoint(position), trackingOrigin.rotation * rotation);
            else
                axes.SetPositionAndRotation(position, rotation);
        }

        private Transform CreateAxes(string name, bool ghost = false)
        {
            var root = new GameObject(name).transform;
            Object.DontDestroyOnLoad(root.gameObject);
            CreateAxis(root, "X", Vector3.forward, ghost ? ghostRed : red);
            CreateAxis(root, "Y", Vector3.left, ghost ? ghostGreen : green);
            CreateAxis(root, "Z", Vector3.up, ghost ? ghostBlue : blue);

            GameObject origin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            origin.name = "Origin";
            origin.transform.SetParent(root, false);
            origin.transform.localScale = Vector3.one * 0.012f;
            origin.GetComponent<Renderer>().sharedMaterial = ghost ? ghostWhite : white;
            Destroy(origin.GetComponent<Collider>());
            return root;
        }

        private static void CreateAxis(Transform parent, string name, Vector3 direction, Material material)
        {
            GameObject axis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            axis.name = name;
            axis.transform.SetParent(parent, false);
            axis.transform.localPosition = direction * (AxisLength * 0.5f);
            axis.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
            axis.transform.localScale = new Vector3(AxisThickness, AxisLength * 0.5f, AxisThickness);
            axis.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(axis.GetComponent<Collider>());
        }

        private static Material CreateMaterial(Color color, string resource = "ControllerAxes")
        {
            Shader shader = Resources.Load<Shader>(resource);
            if (shader == null) throw new System.InvalidOperationException(resource + " shader is missing from Resources.");
            var material = new Material(shader) { color = color };
            return material;
        }

        private void OnDestroy()
        {
            if (leftAxes != null) Destroy(leftAxes.gameObject);
            if (rightAxes != null) Destroy(rightAxes.gameObject);
            if (calibrationAxes != null) Destroy(calibrationAxes.gameObject);
            if (leftLiveAxes != null) Destroy(leftLiveAxes.gameObject);
            if (rightLiveAxes != null) Destroy(rightLiveAxes.gameObject);
            Destroy(red);
            Destroy(green);
            Destroy(blue);
            Destroy(white);
            Destroy(ghostRed);
            Destroy(ghostGreen);
            Destroy(ghostBlue);
            Destroy(ghostWhite);
            Destroy(textMaterial);
        }
    }
}
