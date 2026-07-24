using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortableEndingSystem
{
    [DisallowMultipleComponent]
    public sealed class EndingSceneLoader : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField] private string endingSceneName = "Ending";

        [Header("Optional Transition")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [Min(0f)]
        [SerializeField] private float delayBeforeFade;
        [Min(0f)]
        [SerializeField] private float fadeOutDuration = 1f;

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

            if (Application.CanStreamedLevelBeLoaded(sceneName) == false)
            {
                Debug.LogWarning(
                    $"Cannot load '{sceneName}'. Add it to Build Settings or change Ending Scene Name.",
                    this);
                return;
            }

            isLoading = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            float elapsed = 0f;
            while (elapsed < delayBeforeFade)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.transform.SetAsLastSibling();
                fadeOverlay.interactable = true;
                fadeOverlay.blocksRaycasts = true;

                float startAlpha = fadeOverlay.alpha;
                elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = fadeOutDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(elapsed / fadeOutDuration);
                    fadeOverlay.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                    yield return null;
                }

                fadeOverlay.alpha = 1f;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogWarning($"Could not start loading scene '{sceneName}'.", this);
                isLoading = false;
            }
        }

        private void OnValidate()
        {
            delayBeforeFade = Mathf.Max(0f, delayBeforeFade);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        }
    }
}
