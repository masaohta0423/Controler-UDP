using UnityEngine;
using UnityEngine.XR;

namespace ControllerUdp
{
    /// <summary>Draws an RGB coordinate frame at each tracked controller pose.</summary>
    public sealed class ControllerPoseVisualizer : MonoBehaviour
    {
        private const float AxisLength = 0.10f;
        private const float AxisThickness = 0.003f;

        private Transform leftAxes;
        private Transform rightAxes;
        private Material red;
        private Material green;
        private Material blue;
        private Material white;

        private void Awake()
        {
            red = CreateMaterial(Color.red);
            green = CreateMaterial(Color.green);
            blue = CreateMaterial(Color.blue);
            white = CreateMaterial(Color.white);
            leftAxes = CreateAxes("Left Controller Axes");
            rightAxes = CreateAxes("Right Controller Axes");
        }

        private void Update()
        {
            UpdateAxes(leftAxes, XRNode.LeftHand);
            UpdateAxes(rightAxes, XRNode.RightHand);
        }

        private static void UpdateAxes(Transform axes, XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            bool tracked = device.isValid &&
                           device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) &&
                           isTracked &&
                           device.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
                           device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);

            axes.gameObject.SetActive(tracked);
            if (!tracked) return;
            axes.SetPositionAndRotation(position, rotation);
        }

        private Transform CreateAxes(string name)
        {
            var root = new GameObject(name).transform;
            Object.DontDestroyOnLoad(root.gameObject);
            CreateAxis(root, "X", Vector3.right, red);
            CreateAxis(root, "Y", Vector3.up, green);
            CreateAxis(root, "Z", Vector3.forward, blue);

            GameObject origin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            origin.name = "Origin";
            origin.transform.SetParent(root, false);
            origin.transform.localScale = Vector3.one * 0.012f;
            origin.GetComponent<Renderer>().sharedMaterial = white;
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

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { color = color };
            return material;
        }

        private void OnDestroy()
        {
            if (leftAxes != null) Destroy(leftAxes.gameObject);
            if (rightAxes != null) Destroy(rightAxes.gameObject);
            Destroy(red);
            Destroy(green);
            Destroy(blue);
            Destroy(white);
        }
    }
}
