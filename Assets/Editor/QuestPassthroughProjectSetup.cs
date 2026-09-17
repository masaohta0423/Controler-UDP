using UnityEditor;

namespace ControllerUdp.Editor
{
    /// <summary>Keeps the Quest manifest capability required by runtime passthrough enabled.</summary>
    [InitializeOnLoad]
    internal static class QuestPassthroughProjectSetup
    {
        static QuestPassthroughProjectSetup()
        {
            EditorApplication.delayCall += EnablePassthroughCapability;
        }

        private static void EnablePassthroughCapability()
        {
            OVRProjectConfig config = OVRProjectConfig.CachedProjectConfig;
            if (config.insightPassthroughSupport == OVRProjectConfig.FeatureSupport.Supported) return;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
            OVRProjectConfig.CommitProjectConfig(config);
        }
    }
}
