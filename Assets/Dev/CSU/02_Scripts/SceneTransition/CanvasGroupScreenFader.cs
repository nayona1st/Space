using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup), typeof(Image))]
    public sealed class CanvasGroupScreenFader : MonoBehaviour, IScreenFader
    {
        private const int MaxSortingOrder = 32767;

        [Header("References")]
        [Tooltip("Screen Space Overlay canvas that owns the full-screen fade image.")]
        [SerializeField] private Canvas rootCanvas;

        [Tooltip("Canvas Group used as the single source of fade opacity and input blocking.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Full-screen image rendered over every other game UI element.")]
        [SerializeField] private Image fadeImage;

        [Header("Presentation")]
        [Tooltip("Color drawn over the screen while the fader is visible.")]
        [SerializeField] private Color fadeColor = Color.black;

        [Tooltip("Normalized opacity progression used by every fade operation.")]
        [SerializeField] private AnimationCurve fadeCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Opacity applied before the first scene frame is rendered.")]
        [Range(0f, 1f)]
        [SerializeField] private float initialAlpha = 1f;

        [Tooltip("Canvas order used to keep the fade above all regular scene UI.")]
        [Min(0)]
        [SerializeField] private int sortingOrder = MaxSortingOrder;

        public float Alpha =>
            canvasGroup != null
                ? canvasGroup.alpha
                : 0f;

        private void Awake()
        {
            if (!EnsureReferences())
            {
                return;
            }

            ConfigurePresentation();
            SetAlpha(initialAlpha);
        }

        public void SetAlpha(float alpha)
        {
            if (!EnsureReferences())
            {
                return;
            }

            float safeAlpha = SanitizeNormalizedValue(alpha);
            ApplyAlpha(safeAlpha);
            SetInputBlocked(safeAlpha > 0f);
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (!EnsureReferences())
            {
                yield break;
            }

            float safeTargetAlpha = SanitizeNormalizedValue(targetAlpha);
            float safeDuration = Mathf.Max(0f, duration);

            SetInputBlocked(true);

            if (safeDuration <= 0f)
            {
                ApplyAlpha(safeTargetAlpha);
                SetInputBlocked(safeTargetAlpha > 0f);
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed = Mathf.Min(
                    safeDuration,
                    elapsed + Mathf.Max(0f, Time.unscaledDeltaTime));

                float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
                float curvedTime = EvaluateFadeCurve(normalizedTime);
                ApplyAlpha(Mathf.Lerp(startAlpha, safeTargetAlpha, curvedTime));

                if (elapsed < safeDuration)
                {
                    yield return null;
                }
            }

            ApplyAlpha(safeTargetAlpha);
            SetInputBlocked(safeTargetAlpha > 0f);
        }

        private bool EnsureReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (fadeImage == null)
            {
                fadeImage = GetComponent<Image>();
            }

            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>(true);
            }

            return rootCanvas != null
                && canvasGroup != null
                && fadeImage != null;
        }

        private void ConfigurePresentation()
        {
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = sortingOrder;

            Color opaqueFadeColor = fadeColor;
            opaqueFadeColor.a = 1f;
            fadeImage.color = opaqueFadeColor;
            fadeImage.raycastTarget = true;

            RectTransform rectTransform = fadeImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            canvasGroup.ignoreParentGroups = true;
        }

        private void ApplyAlpha(float alpha)
        {
            canvasGroup.alpha = SanitizeNormalizedValue(alpha);
        }

        private void SetInputBlocked(bool isBlocked)
        {
            canvasGroup.blocksRaycasts = isBlocked;
            canvasGroup.interactable = isBlocked;
        }

        private float EvaluateFadeCurve(float normalizedTime)
        {
            float value = fadeCurve != null
                ? fadeCurve.Evaluate(Mathf.Clamp01(normalizedTime))
                : normalizedTime;
            return SanitizeNormalizedValue(value);
        }

        private static float SanitizeNormalizedValue(float value)
        {
            return float.IsNaN(value)
                ? 0f
                : Mathf.Clamp01(value);
        }

        private void OnValidate()
        {
            initialAlpha = SanitizeNormalizedValue(initialAlpha);
            sortingOrder = Mathf.Clamp(sortingOrder, 0, MaxSortingOrder);

            if (fadeCurve == null || fadeCurve.length == 0)
            {
                fadeCurve =
                    AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
}
