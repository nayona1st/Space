using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    [DisallowMultipleComponent]
    public sealed class UnitySceneLoader : MonoBehaviour, ISceneLoader
    {
        public bool CanLoadScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && Application.CanStreamedLevelBeLoaded(sceneName.Trim());
        }

        public AsyncOperation LoadSceneAsync(string sceneName)
        {
            if (!CanLoadScene(sceneName))
            {
                return null;
            }

            return SceneManager.LoadSceneAsync(
                sceneName.Trim(),
                LoadSceneMode.Single);
        }
    }
}
