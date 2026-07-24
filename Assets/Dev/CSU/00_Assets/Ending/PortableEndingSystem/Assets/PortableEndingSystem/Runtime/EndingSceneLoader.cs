using Dev.CSU._02_Scripts.SceneTransition;
using UnityEngine;

namespace PortableEndingSystem
{
    [DisallowMultipleComponent]
    public sealed class EndingSceneLoader : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField] private string endingSceneName = "Ending";

        private bool isLoading;

        public bool IsLoading => isLoading;

        public void LoadEnding()
        {
            if (isLoading)
            {
                return;
            }

            string sceneName = string.IsNullOrWhiteSpace(endingSceneName)
                ? string.Empty
                : endingSceneName.Trim();

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning($"{nameof(EndingSceneLoader)} needs an Ending Scene Name.", this);
                return;
            }

            isLoading = true;
            if (SceneTransitions.TryLoadScene(sceneName))
            {
                return;
            }

            isLoading = false;
            Debug.LogError(
                $"{nameof(EndingSceneLoader)} on '{name}' could not request "
                + $"the shared fade transition to '{sceneName}'.",
                this);
        }

        private void OnValidate()
        {
            endingSceneName = string.IsNullOrWhiteSpace(endingSceneName)
                ? "Ending"
                : endingSceneName.Trim();
        }
    }
}
