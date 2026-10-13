using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ControllerUdp
{
    /// <summary>Enables a Meta Quest passthrough underlay behind Unity content.</summary>
    public sealed class QuestPassthrough : MonoBehaviour
    {
        private void Awake()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.nearClipPlane = 0.01f;
                camera.allowHDR = false;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            OVRManager manager = FindAnyObjectByType<OVRManager>();
            if (manager == null) manager = gameObject.AddComponent<OVRManager>();
            manager.isInsightPassthroughEnabled = true;
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

            OVRPassthroughLayer layer = FindAnyObjectByType<OVRPassthroughLayer>();
            if (layer == null) layer = gameObject.AddComponent<OVRPassthroughLayer>();
#pragma warning disable CS0618
            layer.overlayType = OVROverlay.OverlayType.Underlay;
#pragma warning restore CS0618
#endif
        }
    }
}
