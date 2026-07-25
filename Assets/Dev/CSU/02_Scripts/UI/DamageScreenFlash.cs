using System.Collections;
using Dev.NKY.Scripts.Health;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.UI
{
    [RequireComponent(typeof(CanvasGroup), typeof(Image))]
    [DisallowMultipleComponent]
    public sealed class DamageScreenFlash : MonoBehaviour
    {
        [Header("Flash")]
        [SerializeField, Range(0f, 1f)]
        private float maximumAlpha = 0.32f;

        [SerializeField, Min(0f)]
        private float fadeInDuration = 0.08f;

        [SerializeField, Min(0f)]
        private float fadeOutDuration = 0.3f;

        private Health _health;
        private CanvasGroup _canvasGroup;
        private Coroutine _flashRoutine;
        private Texture2D _vignetteTexture;
        private Sprite _vignetteSprite;
        private bool _subscribed;

        public float CurrentAlpha =>
            _canvasGroup != null ? _canvasGroup.alpha : 0f;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            Image image = GetComponent<Image>();
            image.color = new Color(0.85f, 0f, 0f, 1f);
            image.sprite = CreateVignetteSprite();
            image.raycastTarget = false;
        }

        private Sprite CreateVignetteSprite()
        {
            const int textureSize = 64;
            _vignetteTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "DamageScreenFlash_Vignette",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                float normalizedY =
                    ((y + 0.5f) / textureSize) * 2f - 1f;
                for (int x = 0; x < textureSize; x++)
                {
                    float normalizedX =
                        ((x + 0.5f) / textureSize) * 2f - 1f;
                    float edgeDistance = Mathf.Max(
                        Mathf.Abs(normalizedX),
                        Mathf.Abs(normalizedY));
                    float edgeAlpha = Mathf.Lerp(
                        0.22f,
                        1f,
                        Mathf.SmoothStep(0.15f, 1f, edgeDistance));
                    pixels[y * textureSize + x] =
                        new Color(1f, 1f, 1f, edgeAlpha);
                }
            }

            _vignetteTexture.SetPixels(pixels);
            _vignetteTexture.Apply(false, true);

            _vignetteSprite = Sprite.Create(
                _vignetteTexture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                100f);
            _vignetteSprite.name =
                "DamageScreenFlash_VignetteSprite";
            _vignetteSprite.hideFlags =
                HideFlags.HideAndDontSave;
            return _vignetteSprite;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        public void Initialize(Health health)
        {
            if (_health == health)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            _health = health;
            Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _health == null)
            {
                return;
            }

            _health.DamageTaken += HandleDamageTaken;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _health == null)
            {
                _subscribed = false;
                return;
            }

            _health.DamageTaken -= HandleDamageTaken;
            _subscribed = false;
        }

        private void HandleDamageTaken(float appliedDamage)
        {
            if (appliedDamage <= 0f)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            yield return FadeAlpha(
                _canvasGroup.alpha,
                maximumAlpha,
                fadeInDuration);
            yield return FadeAlpha(
                maximumAlpha,
                0f,
                fadeOutDuration);

            _canvasGroup.alpha = 0f;
            _flashRoutine = null;
        }

        private IEnumerator FadeAlpha(
            float startAlpha,
            float targetAlpha,
            float duration)
        {
            if (duration <= 0f)
            {
                _canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (_vignetteSprite != null)
            {
                Destroy(_vignetteSprite);
            }

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
            }
        }

        private void OnValidate()
        {
            maximumAlpha = Mathf.Clamp01(maximumAlpha);
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        }
    }

    internal static class DamageScreenFlashRuntimeInstaller
    {
        private const string InGameSceneName = "InGame";
        private const string OverlayName = "DamageScreenFlash";
        private const string CommonUiRootName = "CommonUIRoot";

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
            if (scene.name != InGameSceneName)
            {
                return;
            }

            Health health = Object.FindFirstObjectByType<Health>(
                FindObjectsInactive.Include);
            if (health == null)
            {
                return;
            }

            DamageScreenFlash existing =
                Object.FindFirstObjectByType<DamageScreenFlash>(
                    FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Initialize(health);
                return;
            }

            Canvas gameplayCanvas = FindGameplayCanvas(scene);
            if (gameplayCanvas == null)
            {
                return;
            }

            GameObject overlay = new GameObject(
                OverlayName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(DamageScreenFlash));
            overlay.layer = gameplayCanvas.gameObject.layer;

            RectTransform rect = (RectTransform)overlay.transform;
            rect.SetParent(gameplayCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Transform commonUiRoot =
                gameplayCanvas.transform.Find(CommonUiRootName);
            if (commonUiRoot != null)
            {
                rect.SetSiblingIndex(commonUiRoot.GetSiblingIndex());
            }
            else
            {
                rect.SetAsLastSibling();
            }

            overlay.GetComponent<DamageScreenFlash>()
                .Initialize(health);
        }

        private static Canvas FindGameplayCanvas(Scene scene)
        {
            Canvas fallback = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas[] canvases =
                    root.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas canvas in canvases)
                {
                    if (!canvas.isRootCanvas
                        || canvas.renderMode
                            == RenderMode.WorldSpace)
                    {
                        continue;
                    }

                    if (canvas.transform.Find(CommonUiRootName)
                        != null)
                    {
                        return canvas;
                    }

                    fallback ??= canvas;
                }
            }

            return fallback;
        }
    }
}
