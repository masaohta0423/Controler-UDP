using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace ControllerUdp.Editor
{
    /// <summary>One-click build setup. OpenXR feature selection remains visible in Project Settings.</summary>
    public static class QuestUdpProjectSetup
    {
        private const string ConfiguredKey = "ControllerUdp.QuestConfigured.v1";

        [InitializeOnLoadMethod]
        private static void ConfigureAfterPackageImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(ConfiguredKey, false)) Configure();
            };
        }

        [MenuItem("Tools/Controller UDP/Configure Quest 3")]
        public static void Configure()
        {
            SessionState.SetBool(ConfiguredKey, true);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.leus.controllerudp");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.colorSpace = ColorSpace.Linear;

            XRGeneralSettingsPerBuildTarget allSettings = LoadOrCreateXrSettings();
            if (!allSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                allSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            XRGeneralSettings general = allSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            bool assigned = XRPackageMetadataStore.AssignLoader(
                general.Manager,
                "UnityEngine.XR.OpenXR.OpenXRLoader",
                BuildTargetGroup.Android);
            if (!assigned && general.Manager.activeLoaders.Count == 0)
                Debug.LogWarning("Controller UDP: OpenXR loader could not be assigned automatically. Enable OpenXR under Project Settings > XR Plug-in Management > Android.");

            OpenXRSettings openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            Enable<OculusTouchControllerProfile>(openXr);
            Enable<MetaQuestTouchPlusControllerProfile>(openXr);
            Enable<MetaQuestFeature>(openXr);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            AssetDatabase.SaveAssets();
            Debug.Log("Controller UDP: Quest 3 Android/OpenXR setup complete.");
        }

        /// <summary>Command-line entry point used for a reproducible Quest APK build.</summary>
        public static void BuildQuestApk()
        {
            ControllerCalibrationValidation.Validate();
            Configure();
            string[] scenes = System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0) throw new BuildFailedException("No enabled scenes in Build Settings.");

            System.IO.Directory.CreateDirectory("Builds/Quest");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Quest/ControllerUDP.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Quest APK build failed: {report.summary.result}");
        }

        private static void Enable<T>(OpenXRSettings settings) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            T feature = settings != null ? settings.GetFeature<T>() : null;
            if (feature != null) feature.enabled = true;
        }

        private static XRGeneralSettingsPerBuildTarget LoadOrCreateXrSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
            var settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, settings, true);
            return settings;
        }
    }
}
