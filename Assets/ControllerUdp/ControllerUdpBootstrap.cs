using UnityEngine;

namespace ControllerUdp
{
    /// <summary>Creates the sender automatically, so no scene editing is required.</summary>
    internal static class ControllerUdpBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateSender()
        {
            if (Object.FindAnyObjectByType<ControllerUdpSender>() != null) return;
            var go = new GameObject("Controller UDP Sender");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ControllerUdpSender>();
        }
    }
}
