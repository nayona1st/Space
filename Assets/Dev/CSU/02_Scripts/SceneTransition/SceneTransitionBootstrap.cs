using UnityEngine;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    public static class SceneTransitionBootstrap
    {
        private const string PrefabResourcePath = "SceneTransitionRoot";

        private static bool _warnedMissingPrefab;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _warnedMissingPrefab = false;
            SceneTransitionService.ResetStaticState();
            SceneTransitions.ResetRegistration();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (SceneTransitionService.HasActiveInstance)
            {
                return;
            }

            GameObject prefab =
                Resources.Load<GameObject>(PrefabResourcePath);

            if (prefab == null)
            {
                WarnMissingPrefabOnce(
                    $"No Resources/{PrefabResourcePath} prefab was found.");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = prefab.name;

            if (!instance.activeSelf)
            {
                instance.SetActive(true);
            }

            if (instance.GetComponentInChildren<SceneTransitionService>(true)
                == null)
            {
                Object.Destroy(instance);
                WarnMissingPrefabOnce(
                    $"Resources/{PrefabResourcePath} does not contain "
                    + $"{nameof(SceneTransitionService)}.");
            }
        }

        private static void WarnMissingPrefabOnce(string reason)
        {
            if (_warnedMissingPrefab)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(SceneTransitionBootstrap)} could not install the "
                + $"global scene transition system. {reason}");
            _warnedMissingPrefab = true;
        }
    }
}
