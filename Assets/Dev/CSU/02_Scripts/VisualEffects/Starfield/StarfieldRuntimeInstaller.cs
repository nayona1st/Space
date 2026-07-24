using Dev.CSU._02_Scripts.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.VisualEffects.Starfield
{
    internal static class StarfieldRuntimeInstaller
    {
        private static bool _warnedMissingSettings;
        private static bool _warnedMissingCamera;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneInstaller()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            InstallIntoActiveScene();
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode)
        {
            InstallIntoActiveScene();
        }

        private static void InstallIntoActiveScene()
        {
            StarfieldSettings settings =
                Resources.Load<StarfieldSettings>(
                    StarfieldSettings.ResourcePath);
            if (settings == null)
            {
                if (!_warnedMissingSettings)
                {
                    Debug.LogWarning(
                        "Starfield could not load Resources/"
                        + $"{StarfieldSettings.ResourcePath}.");
                    _warnedMissingSettings = true;
                }

                return;
            }

            _warnedMissingSettings = false;
            if (!settings.SystemEnabled)
            {
                return;
            }

            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                if (!_warnedMissingCamera)
                {
                    Debug.LogWarning(
                        "Starfield could not find Camera.main.");
                    _warnedMissingCamera = true;
                }

                return;
            }

            _warnedMissingCamera = false;
            StarfieldController controller =
                targetCamera.GetComponent<StarfieldController>();
            if (controller == null)
            {
                controller =
                    targetCamera.gameObject.AddComponent<
                        StarfieldController>();
            }

            StageThemeController themeController =
                Object.FindFirstObjectByType<StageThemeController>(
                    FindObjectsInactive.Include);
            controller.Initialize(
                targetCamera,
                settings,
                themeController);
        }
    }
}
