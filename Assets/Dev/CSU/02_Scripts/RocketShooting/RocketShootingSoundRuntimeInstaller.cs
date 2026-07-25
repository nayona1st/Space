using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    internal static class RocketShootingSoundRuntimeInstaller
    {
        private const string RocketSceneName = "Rocket Shooting";

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
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != RocketSceneName)
            {
                return;
            }

            RocketShootingDirector director =
                Object.FindFirstObjectByType<RocketShootingDirector>(
                    FindObjectsInactive.Include);
            if (director == null
                || director.TryGetComponent(
                    out RocketShootingButtonSoundInstaller _))
            {
                return;
            }

            director.gameObject.AddComponent<
                RocketShootingButtonSoundInstaller>();
        }
    }

    [DisallowMultipleComponent]
    internal sealed class RocketShootingButtonSoundInstaller :
        MonoBehaviour
    {
        private IEnumerator Start()
        {
            InstallButtonSounds();
            yield return null;
            InstallButtonSounds();
        }

        private void InstallButtonSounds()
        {
            Scene scene = gameObject.scene;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Button[] buttons =
                    root.GetComponentsInChildren<Button>(true);
                foreach (Button button in buttons)
                {
                    if (button.GetComponent<
                            RocketShootingUIButtonSound>()
                        == null)
                    {
                        button.gameObject.AddComponent<
                            RocketShootingUIButtonSound>();
                    }
                }
            }
        }
    }
}
