using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeanBlaster.Editor
{
    /// <summary>Applies the Android-first project contract and creates development APKs.</summary>
    public static class AndroidBuildTools
    {
        private const string ApplicationIdentifier = "com.beanblaster.game";
        private const string DevelopmentApkPath = "Builds/Android/BeanBlaster-dev.apk";

        /// <summary>Applies deterministic Android player and build settings.</summary>
        [MenuItem("Tools/Game Setup/Android/Configure Project")]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = "Bean Blaster";
            PlayerSettings.productName = "Bean Blaster";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, ApplicationIdentifier);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;

            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.Android.renderOutsideSafeArea = false;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;

            AssetDatabase.SaveAssets();
            Debug.Log("Configured Android project settings for Bean Blaster.");
        }

        /// <summary>Configures Android and builds the enabled scenes into a development APK.</summary>
        [MenuItem("Tools/Game Setup/Android/Build Development APK")]
        public static void BuildDevelopmentApk()
        {
            ConfigureProject();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException("Unity could not switch the active build target to Android.");
            }

            string[] scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled)
                .Map(scene => scene.path);
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes exist in Build Settings.");
            }

            string directory = Path.GetDirectoryName(DevelopmentApkPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = DevelopmentApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Android build failed with {report.summary.totalErrors} errors. Result: {report.summary.result}.");
            }

            Debug.Log($"Built Android development APK at '{DevelopmentApkPath}' ({report.summary.totalSize} bytes). ");
        }

        private static TResult[] Map<TSource, TResult>(this TSource[] source, Func<TSource, TResult> selector)
        {
            TResult[] result = new TResult[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                result[index] = selector(source[index]);
            }

            return result;
        }
    }
}
