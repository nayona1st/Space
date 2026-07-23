using System;
using Dev.CSU.Scripts.Background;
using Dev.CSU.Scripts.Planet;
using UnityEngine;

namespace Dev.CSU.Scripts.Stage
{
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public sealed class StageThemeController : MonoBehaviour
    {
        private const float MinimumScale = 0.0001f;

        [Serializable]
        private sealed class StageBackgroundSetting
        {
            [Tooltip("Stage that uses this background theme.")]
            [Min(1)]
            [SerializeField] private int stageNumber = 1;

            [Tooltip("Sprite applied to both infinite background segments.")]
            [SerializeField] private Sprite backgroundSprite;

            [Tooltip("Local X/Y scale applied to each background segment. The existing Z scale is preserved.")]
            [SerializeField] private Vector2 segmentScale =
                new Vector2(1f, 1.7f);

            [Tooltip("Color tint applied to the background SpriteRenderer.")]
            [SerializeField] private Color backgroundColor = Color.white;

            public int StageNumber => stageNumber;
            public Sprite BackgroundSprite => backgroundSprite;
            public Vector2 SegmentScale => segmentScale;
            public Color BackgroundColor => backgroundColor;

            public void Validate()
            {
                stageNumber = Mathf.Max(1, stageNumber);
                segmentScale.x = Mathf.Max(MinimumScale, segmentScale.x);
                segmentScale.y = Mathf.Max(MinimumScale, segmentScale.y);
            }
        }

        [Tooltip("Planet sequence controller that reports stage completion.")]
        [SerializeField] private PlanetParallaxController planetController;

        [Tooltip("Two-segment infinite background looper.")]
        [SerializeField] private InfiniteBackgroundLooper backgroundLooper;

        [Tooltip("Background Sprite, scale and color for each stage.")]
        [SerializeField] private StageBackgroundSetting[] stageBackgroundSettings;

        private bool[] _convertedSegments;
        private StageBackgroundSetting _pendingSetting;
        private int _pendingStageNumber;
        private int _completionSegmentIndex = -1;
        private bool _transitionPending;
        private bool _completeAfterRecycle;
        private bool _configurationReady;
        private bool _warnedInvalidConfiguration;
        private bool _warnedMissingRenderer;

        public int CurrentStageNumber { get; private set; }

        public event Action<int> StageBackgroundReady;

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            _configurationReady = TryValidateConfiguration();
            if (!_configurationReady)
            {
                return;
            }

            CurrentStageNumber = Mathf.Max(
                1,
                planetController.CurrentStageNumber);

            StageBackgroundSetting initialSetting =
                FindSetting(CurrentStageNumber);

            if (initialSetting != null)
            {
                ApplyInitialTheme(initialSetting);
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (planetController != null)
            {
                planetController.StagePlanetsCompleted +=
                    HandleStagePlanetsCompleted;
            }

            if (backgroundLooper != null)
            {
                backgroundLooper.SegmentWillRecycle +=
                    HandleSegmentWillRecycle;
                backgroundLooper.SegmentRecycled +=
                    HandleSegmentRecycled;
            }
        }

        private void UnsubscribeEvents()
        {
            if (planetController != null)
            {
                planetController.StagePlanetsCompleted -=
                    HandleStagePlanetsCompleted;
            }

            if (backgroundLooper != null)
            {
                backgroundLooper.SegmentWillRecycle -=
                    HandleSegmentWillRecycle;
                backgroundLooper.SegmentRecycled -=
                    HandleSegmentRecycled;
            }
        }

        private bool TryValidateConfiguration()
        {
            if (planetController == null)
            {
                WarnInvalidConfigurationOnce(
                    "Planet Parallax Controller is not assigned.");
                return false;
            }

            if (backgroundLooper == null)
            {
                WarnInvalidConfigurationOnce(
                    "Infinite Background Looper is not assigned.");
                return false;
            }

            if (stageBackgroundSettings == null
                || stageBackgroundSettings.Length == 0)
            {
                WarnInvalidConfigurationOnce(
                    "Stage Background Settings is empty.");
                return false;
            }

            _convertedSegments = new bool[backgroundLooper.SegmentCount];
            _warnedInvalidConfiguration = false;
            return true;
        }

        private void ApplyInitialTheme(StageBackgroundSetting setting)
        {
            for (int segmentIndex = 0;
                 segmentIndex < backgroundLooper.SegmentCount;
                 segmentIndex++)
            {
                if (backgroundLooper.TryGetSegment(
                        segmentIndex,
                        out Transform segment))
                {
                    ApplySettingToSegment(segment, setting);
                }
            }
        }

        private void HandleStagePlanetsCompleted(int completedStageNumber)
        {
            if (!_configurationReady
                || _transitionPending
                || completedStageNumber != CurrentStageNumber)
            {
                return;
            }

            int nextStageNumber = completedStageNumber + 1;
            StageBackgroundSetting nextSetting =
                FindSetting(nextStageNumber);

            if (nextSetting == null)
            {
                return;
            }

            Array.Clear(
                _convertedSegments,
                0,
                _convertedSegments.Length);
            _pendingSetting = nextSetting;
            _pendingStageNumber = nextStageNumber;
            _completionSegmentIndex = -1;
            _completeAfterRecycle = false;
            _transitionPending = true;
        }

        private void HandleSegmentWillRecycle(
            Transform segment,
            int segmentIndex)
        {
            if (!_transitionPending
                || segmentIndex < 0
                || segmentIndex >= _convertedSegments.Length
                || _convertedSegments[segmentIndex])
            {
                return;
            }

            if (!ApplySettingToSegment(segment, _pendingSetting))
            {
                return;
            }

            _convertedSegments[segmentIndex] = true;

            for (int index = 0; index < _convertedSegments.Length; index++)
            {
                if (!_convertedSegments[index])
                {
                    return;
                }
            }

            _completionSegmentIndex = segmentIndex;
            _completeAfterRecycle = true;
        }

        private void HandleSegmentRecycled(
            Transform segment,
            int segmentIndex)
        {
            if (!_transitionPending
                || !_completeAfterRecycle
                || segmentIndex != _completionSegmentIndex)
            {
                return;
            }

            int readyStageNumber = _pendingStageNumber;

            _transitionPending = false;
            _completeAfterRecycle = false;
            _completionSegmentIndex = -1;
            _pendingSetting = null;
            _pendingStageNumber = 0;
            CurrentStageNumber = readyStageNumber;

            StageBackgroundReady?.Invoke(readyStageNumber);
            planetController.StartStage(readyStageNumber);
        }

        private bool ApplySettingToSegment(
            Transform segment,
            StageBackgroundSetting setting)
        {
            if (segment == null
                || setting == null
                || setting.BackgroundSprite == null)
            {
                return false;
            }

            SpriteRenderer renderer =
                segment.GetComponentInChildren<SpriteRenderer>(true);

            if (renderer == null)
            {
                if (!_warnedMissingRenderer)
                {
                    Debug.LogWarning(
                        $"{nameof(StageThemeController)} on '{name}' could not "
                        + $"find a SpriteRenderer under '{segment.name}'.",
                        this);
                    _warnedMissingRenderer = true;
                }

                return false;
            }

            renderer.sprite = setting.BackgroundSprite;
            renderer.color = setting.BackgroundColor;

            Vector3 localScale = segment.localScale;
            localScale.x = setting.SegmentScale.x;
            localScale.y = setting.SegmentScale.y;
            segment.localScale = localScale;
            _warnedMissingRenderer = false;
            return true;
        }

        private StageBackgroundSetting FindSetting(int stageNumber)
        {
            if (stageBackgroundSettings == null)
            {
                return null;
            }

            for (int index = 0;
                 index < stageBackgroundSettings.Length;
                 index++)
            {
                StageBackgroundSetting setting =
                    stageBackgroundSettings[index];

                if (setting != null
                    && setting.StageNumber == stageNumber)
                {
                    return setting;
                }
            }

            return null;
        }

        private void WarnInvalidConfigurationOnce(string reason)
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(StageThemeController)} on '{name}' is inactive: {reason}",
                this);
            _warnedInvalidConfiguration = true;
        }

        private void OnValidate()
        {
            if (stageBackgroundSettings == null)
            {
                return;
            }

            foreach (StageBackgroundSetting setting in stageBackgroundSettings)
            {
                setting?.Validate();
            }
        }
    }
}
