using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class RocketWindStreakController : MonoBehaviour
    {
        private const int MinimumPoolSize = 1;
        private const int MaximumPoolSize = 128;
        private const int MaximumPlacementAttempts = 32;
        private const float MinimumScale = 0.01f;

        [Header("References")]
        [Tooltip("Launch sequence that supplies the phase, scroll speed, and acceleration progress.")]
        [SerializeField] private RocketShootingDirector launchDirector;

        [Tooltip("Background scroller that reports when Ground has completely exited.")]
        [SerializeField] private VerticalBackgroundScroller backgroundScroller;

        [Tooltip("Camera whose viewport defines spawn and recycle bounds.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Rain_0 through Rain_17, used as white wind-streak visuals.")]
        [SerializeField] private Sprite[] streakSprites;

        [Tooltip("Single-renderer prefab cloned once while the fixed pool is built.")]
        [SerializeField] private SpriteRenderer streakPrefab;

        [Tooltip("Parent that owns every runtime wind-streak instance.")]
        [SerializeField] private Transform instancesRoot;

        [Header("Pool and Density")]
        [Tooltip("Fixed number of instances created once during Awake.")]
        [Range(MinimumPoolSize, MaximumPoolSize)]
        [SerializeField] private int poolSize = 28;

        [Tooltip("Visible streak target when acceleration begins.")]
        [Min(0)]
        [SerializeField] private int minimumActiveCount = 4;

        [Tooltip("Visible streak target at full acceleration.")]
        [Min(0)]
        [SerializeField] private int maximumActiveCount = 20;

        [Tooltip("How quickly inactive pooled streaks are revealed, from low to high intensity.")]
        [SerializeField] private Vector2 activationRateRange =
            new Vector2(4f, 14f);

        [Header("Viewport Placement")]
        [Tooltip("World-space clearance kept from each horizontal viewport edge.")]
        [Min(0f)]
        [SerializeField] private float horizontalMargin = 1.25f;

        [Tooltip("World-space distance above the viewport where streaks begin.")]
        [Min(0f)]
        [SerializeField] private float spawnMargin = 3.5f;

        [Tooltip("World-space distance below the viewport before a streak is recycled.")]
        [Min(0f)]
        [SerializeField] private float recycleMargin = 3.5f;

        [Tooltip("Minimum world-space center distance requested between active streaks.")]
        [Min(0f)]
        [SerializeField] private float minimumHorizontalSpacing = 1.5f;

        [Range(1, MaximumPlacementAttempts)]
        [SerializeField] private int placementAttempts = 10;

        [Header("Motion and Presentation")]
        [SerializeField] private Vector2 scaleRange =
            new Vector2(0.65f, 1.15f);

        [SerializeField] private Vector2 speedMultiplierRange =
            new Vector2(1.3f, 2.2f);

        [Tooltip("Optional floor for the speed source. Leave at zero to use only CurrentScrollSpeed.")]
        [Min(0f)]
        [SerializeField] private float minimumAssistSpeed;

        [Tooltip("Very small world-units-per-second horizontal drift range.")]
        [SerializeField] private Vector2 horizontalDriftRange =
            new Vector2(-0.06f, 0.06f);

        [Tooltip("Small clockwise/counter-clockwise rotation range in degrees.")]
        [SerializeField] private Vector2 rotationRange =
            new Vector2(-2f, 2f);

        [SerializeField] private Vector2 alphaRange =
            new Vector2(0.12f, 0.38f);

        [Tooltip("Maps the Director's clamped acceleration progress to visual intensity.")]
        [SerializeField] private AnimationCurve intensityCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Randomization")]
        [Tooltip("Uses an independent seed on each play session when enabled.")]
        [SerializeField] private bool randomizeSeed = true;

        [SerializeField] private int randomSeed = 20260725;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("Background is 0 and the rocket body is 2 in Rocket Shooting.")]
        [SerializeField] private int sortingOrder = 1;

        [Header("Safety")]
        [Tooltip("Maximum recycle operations allowed in one frame after a large time step.")]
        [Range(1, MaximumPoolSize)]
        [SerializeField] private int maxRecyclesPerFrame = 28;

        private StreakRuntime[] _streaks;
        private int[] _spriteOrder;
        private System.Random _random;
        private int _spriteCursor;
        private int _activeCount;
        private float _activationAccumulator;
        private bool _isInitialized;
        private bool _warnedInvalidConfiguration;
        private bool _warnedRecycleLimit;

        public int PoolCount => _streaks?.Length ?? 0;
        public int ConfiguredPoolSize => poolSize;
        public int ActiveStreakCount => _activeCount;
        public int RecycledStreakCount { get; private set; }
        public float CurrentIntensity { get; private set; }
        public bool IsInitialized => _isInitialized;
        public bool HasGroundExited =>
            backgroundScroller != null
            && backgroundScroller.HasGroundExited;

        public bool IsConfigured
        {
            get
            {
                if (launchDirector == null
                    || backgroundScroller == null
                    || targetCamera == null
                    || streakPrefab == null
                    || streakPrefab.sprite == null
                    || instancesRoot == null
                    || streakSprites == null
                    || streakSprites.Length == 0)
                {
                    return false;
                }

                for (int index = 0;
                     index < streakSprites.Length;
                     index++)
                {
                    if (streakSprites[index] == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Awake()
        {
            _isInitialized = TryInitialize();
            if (!_isInitialized)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f
                || float.IsNaN(deltaTime)
                || float.IsInfinity(deltaTime))
            {
                return;
            }

            if (!ShouldShowStreaks())
            {
                DeactivateAll();
                CurrentIntensity = 0f;
                _activationAccumulator = 0f;
                return;
            }

            CurrentIntensity = EvaluateIntensity();
            int targetActiveCount = Mathf.Clamp(
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        minimumActiveCount,
                        maximumActiveCount,
                        CurrentIntensity)),
                0,
                _streaks.Length);

            ApproachActiveCount(
                targetActiveCount,
                deltaTime,
                CurrentIntensity);
            MoveAndRecycleStreaks(deltaTime, CurrentIntensity);
        }

        private void OnDisable()
        {
            DeactivateAll();
            CurrentIntensity = 0f;
            _activationAccumulator = 0f;
        }

        private bool TryInitialize()
        {
            if (!IsConfigured)
            {
                WarnInvalidConfigurationOnce(
                    "assign the Director, Background Scroller, Camera, "
                    + "Rain Sprite list, renderer prefab, and instances root. "
                    + "Every Sprite reference and the prefab Sprite must be valid.");
                return false;
            }

            _random = new System.Random(GetEffectiveSeed());
            _spriteOrder = new int[streakSprites.Length];
            for (int index = 0;
                 index < _spriteOrder.Length;
                 index++)
            {
                _spriteOrder[index] = index;
            }

            ShuffleSpriteOrder();
            _spriteCursor = 0;
            _streaks = new StreakRuntime[poolSize];

            for (int index = 0; index < _streaks.Length; index++)
            {
                SpriteRenderer renderer = Instantiate(
                    streakPrefab,
                    instancesRoot,
                    false);
                renderer.gameObject.name =
                    $"RocketWindStreak_Pooled_{index:00}";
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;

                _streaks[index] = new StreakRuntime(
                    renderer,
                    renderer.transform.localScale,
                    renderer.transform.localRotation,
                    renderer.color);
                renderer.gameObject.SetActive(false);
            }

            _activeCount = 0;
            _activationAccumulator = 0f;
            CurrentIntensity = 0f;
            RecycledStreakCount = 0;
            _warnedInvalidConfiguration = false;
            return true;
        }

        private bool ShouldShowStreaks()
        {
            if (!backgroundScroller.HasGroundExited)
            {
                return false;
            }

            LaunchPhase phase = launchDirector.Phase;
            return phase == LaunchPhase.LiftOff
                || phase == LaunchPhase.Cruise;
        }

        private float EvaluateIntensity()
        {
            float progress =
                Mathf.Clamp01(launchDirector.AccelerationProgress);
            float curved = intensityCurve != null
                ? intensityCurve.Evaluate(progress)
                : progress;
            return Mathf.Clamp01(curved);
        }

        private void ApproachActiveCount(
            int targetActiveCount,
            float deltaTime,
            float intensity)
        {
            if (_activeCount > targetActiveCount)
            {
                for (int index = _streaks.Length - 1;
                     index >= 0 && _activeCount > targetActiveCount;
                     index--)
                {
                    StreakRuntime streak = _streaks[index];
                    if (!streak.Renderer.gameObject.activeSelf)
                    {
                        continue;
                    }

                    streak.Renderer.gameObject.SetActive(false);
                    _activeCount--;
                }

                return;
            }

            if (_activeCount >= targetActiveCount)
            {
                _activationAccumulator = 0f;
                return;
            }

            float activationsPerSecond = Mathf.Lerp(
                activationRateRange.x,
                activationRateRange.y,
                intensity);
            _activationAccumulator +=
                Mathf.Max(0f, activationsPerSecond) * deltaTime;
            int activationBudget = Mathf.Min(
                Mathf.FloorToInt(_activationAccumulator),
                4);
            if (activationBudget <= 0)
            {
                return;
            }

            _activationAccumulator -= activationBudget;
            for (int index = 0;
                 index < _streaks.Length
                 && activationBudget > 0
                 && _activeCount < targetActiveCount;
                 index++)
            {
                StreakRuntime streak = _streaks[index];
                if (streak.Renderer.gameObject.activeSelf)
                {
                    continue;
                }

                streak.Renderer.gameObject.SetActive(true);
                PrepareAndPlaceAtTop(streak, intensity);
                _activeCount++;
                activationBudget--;
            }
        }

        private void MoveAndRecycleStreaks(
            float deltaTime,
            float intensity)
        {
            int recycleCount = 0;
            float sourceSpeed = Mathf.Max(
                minimumAssistSpeed,
                launchDirector.CurrentScrollSpeed);
            float intensitySpeedMultiplier =
                Mathf.Lerp(0.9f, 1.15f, intensity);

            for (int index = 0;
                 index < _streaks.Length;
                 index++)
            {
                StreakRuntime streak = _streaks[index];
                SpriteRenderer renderer = streak.Renderer;
                if (renderer == null
                    || !renderer.gameObject.activeSelf)
                {
                    continue;
                }

                float verticalSpeed =
                    sourceSpeed
                    * streak.SpeedMultiplier
                    * intensitySpeedMultiplier;
                Vector3 position = streak.Transform.position;
                position.x += streak.HorizontalSpeed * deltaTime;
                position.y -= verticalSpeed * deltaTime;
                streak.Transform.position = position;

                UpdateAlpha(streak, intensity);
                ClampToHorizontalViewport(streak);

                ViewportBounds viewport =
                    GetViewportBounds(streak.Transform.position);
                if (renderer.bounds.max.y
                    >= viewport.Bottom - recycleMargin)
                {
                    continue;
                }

                if (recycleCount >= maxRecyclesPerFrame)
                {
                    WarnRecycleLimitOnce();
                    continue;
                }

                PrepareAndPlaceAtTop(streak, intensity);
                RecycledStreakCount++;
                recycleCount++;
            }

            if (recycleCount < maxRecyclesPerFrame)
            {
                _warnedRecycleLimit = false;
            }
        }

        private void PrepareAndPlaceAtTop(
            StreakRuntime streak,
            float intensity)
        {
            SpriteRenderer renderer = streak.Renderer;
            renderer.sprite = NextBalancedSprite();

            float scale =
                NextFloat(scaleRange.x, scaleRange.y);
            streak.Transform.localScale =
                streak.InitialLocalScale * scale;
            streak.Transform.localRotation =
                streak.InitialLocalRotation
                * Quaternion.Euler(
                    0f,
                    0f,
                    NextFloat(rotationRange.x, rotationRange.y));

            streak.SpeedMultiplier = NextFloat(
                speedMultiplierRange.x,
                speedMultiplierRange.y);
            streak.HorizontalSpeed = NextFloat(
                horizontalDriftRange.x,
                horizontalDriftRange.y);
            streak.PeakAlpha = NextFloat(
                alphaRange.x,
                alphaRange.y);
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            UpdateAlpha(streak, intensity);

            ViewportBounds viewport =
                GetViewportBounds(streak.Transform.position);
            float halfWidth = renderer.bounds.extents.x;
            float minimumCenterX =
                viewport.Left + horizontalMargin + halfWidth;
            float maximumCenterX =
                viewport.Right - horizontalMargin - halfWidth;
            float targetCenterX =
                SelectSpacedHorizontalPosition(
                    streak,
                    minimumCenterX,
                    maximumCenterX);

            float viewportHeight =
                Mathf.Max(0f, viewport.Top - viewport.Bottom);
            float desiredBottom =
                viewport.Top
                + spawnMargin
                + NextFloat(0f, viewportHeight * 0.12f);
            Vector3 position = streak.Transform.position;
            position.x +=
                targetCenterX - renderer.bounds.center.x;
            position.y +=
                desiredBottom - renderer.bounds.min.y;
            streak.Transform.position = position;
        }

        private float SelectSpacedHorizontalPosition(
            StreakRuntime streak,
            float minimumCenterX,
            float maximumCenterX)
        {
            if (minimumCenterX > maximumCenterX)
            {
                return (minimumCenterX + maximumCenterX) * 0.5f;
            }

            float candidate =
                NextFloat(minimumCenterX, maximumCenterX);
            int attemptCount = Mathf.Clamp(
                placementAttempts,
                1,
                MaximumPlacementAttempts);

            for (int attempt = 0;
                 attempt < attemptCount;
                 attempt++)
            {
                candidate =
                    NextFloat(minimumCenterX, maximumCenterX);
                if (HasEnoughHorizontalSpacing(streak, candidate))
                {
                    break;
                }
            }

            return candidate;
        }

        private bool HasEnoughHorizontalSpacing(
            StreakRuntime excludedStreak,
            float candidateCenterX)
        {
            if (minimumHorizontalSpacing <= 0f)
            {
                return true;
            }

            for (int index = 0;
                 index < _streaks.Length;
                 index++)
            {
                StreakRuntime other = _streaks[index];
                if (other == excludedStreak
                    || other.Renderer == null
                    || !other.Renderer.gameObject.activeSelf)
                {
                    continue;
                }

                if (Mathf.Abs(
                        other.Renderer.bounds.center.x
                        - candidateCenterX)
                    < minimumHorizontalSpacing)
                {
                    return false;
                }
            }

            return true;
        }

        private void ClampToHorizontalViewport(
            StreakRuntime streak)
        {
            SpriteRenderer renderer = streak.Renderer;
            ViewportBounds viewport =
                GetViewportBounds(streak.Transform.position);
            float halfWidth = renderer.bounds.extents.x;
            float minimumCenterX =
                viewport.Left + horizontalMargin + halfWidth;
            float maximumCenterX =
                viewport.Right - horizontalMargin - halfWidth;
            float targetCenterX = minimumCenterX <= maximumCenterX
                ? Mathf.Clamp(
                    renderer.bounds.center.x,
                    minimumCenterX,
                    maximumCenterX)
                : (viewport.Left + viewport.Right) * 0.5f;
            float correction =
                targetCenterX - renderer.bounds.center.x;

            if (Mathf.Approximately(correction, 0f))
            {
                return;
            }

            Vector3 position = streak.Transform.position;
            position.x += correction;
            streak.Transform.position = position;
        }

        private ViewportBounds GetViewportBounds(
            Vector3 worldPosition)
        {
            float depth = Mathf.Abs(Vector3.Dot(
                worldPosition - targetCamera.transform.position,
                targetCamera.transform.forward));
            depth = Mathf.Max(depth, targetCamera.nearClipPlane);
            Vector3 bottomLeft =
                targetCamera.ViewportToWorldPoint(
                    new Vector3(0f, 0f, depth));
            Vector3 topRight =
                targetCamera.ViewportToWorldPoint(
                    new Vector3(1f, 1f, depth));

            return new ViewportBounds(
                Mathf.Min(bottomLeft.x, topRight.x),
                Mathf.Max(bottomLeft.x, topRight.x),
                Mathf.Min(bottomLeft.y, topRight.y),
                Mathf.Max(bottomLeft.y, topRight.y));
        }

        private void UpdateAlpha(
            StreakRuntime streak,
            float intensity)
        {
            Color color = streak.InitialColor;
            color.a = Mathf.Lerp(
                alphaRange.x,
                streak.PeakAlpha,
                intensity);
            streak.Renderer.color = color;
        }

        private Sprite NextBalancedSprite()
        {
            if (_spriteCursor >= _spriteOrder.Length)
            {
                ShuffleSpriteOrder();
                _spriteCursor = 0;
            }

            int spriteIndex = _spriteOrder[_spriteCursor];
            _spriteCursor++;
            return streakSprites[spriteIndex];
        }

        private void ShuffleSpriteOrder()
        {
            for (int index = _spriteOrder.Length - 1;
                 index > 0;
                 index--)
            {
                int swapIndex = _random.Next(0, index + 1);
                int value = _spriteOrder[index];
                _spriteOrder[index] = _spriteOrder[swapIndex];
                _spriteOrder[swapIndex] = value;
            }
        }

        private void DeactivateAll()
        {
            if (_streaks == null || _activeCount == 0)
            {
                return;
            }

            for (int index = 0;
                 index < _streaks.Length;
                 index++)
            {
                SpriteRenderer renderer =
                    _streaks[index]?.Renderer;
                if (renderer != null
                    && renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            _activeCount = 0;
        }

        private int GetEffectiveSeed()
        {
            if (!randomizeSeed)
            {
                return randomSeed;
            }

            return unchecked(
                Environment.TickCount
                ^ GetInstanceID()
                ^ DateTime.UtcNow.Millisecond);
        }

        private float NextFloat(float minimum, float maximum)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            return minimum
                + (float)_random.NextDouble()
                * (maximum - minimum);
        }

        private void WarnInvalidConfigurationOnce(string reason)
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(RocketWindStreakController)} on '{name}' is "
                + $"inactive: {reason}",
                this);
            _warnedInvalidConfiguration = true;
        }

        private void WarnRecycleLimitOnce()
        {
            if (_warnedRecycleLimit)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(RocketWindStreakController)} on '{name}' reached "
                + "its per-frame recycle safety limit after an unusually "
                + "large time step.",
                this);
            _warnedRecycleLimit = true;
        }

        private void OnValidate()
        {
            poolSize = Mathf.Clamp(
                poolSize,
                MinimumPoolSize,
                MaximumPoolSize);
            minimumActiveCount = Mathf.Clamp(
                minimumActiveCount,
                0,
                poolSize);
            maximumActiveCount = Mathf.Clamp(
                maximumActiveCount,
                minimumActiveCount,
                poolSize);
            ClampRange(
                ref activationRateRange,
                0f,
                float.MaxValue);
            horizontalMargin = Mathf.Max(0f, horizontalMargin);
            spawnMargin = Mathf.Max(0f, spawnMargin);
            recycleMargin = Mathf.Max(0f, recycleMargin);
            minimumHorizontalSpacing =
                Mathf.Max(0f, minimumHorizontalSpacing);
            placementAttempts = Mathf.Clamp(
                placementAttempts,
                1,
                MaximumPlacementAttempts);
            ClampRange(
                ref scaleRange,
                MinimumScale,
                float.MaxValue);
            ClampRange(
                ref speedMultiplierRange,
                0f,
                float.MaxValue);
            minimumAssistSpeed = Mathf.Max(
                0f,
                minimumAssistSpeed);
            SortRange(ref horizontalDriftRange);
            SortRange(ref rotationRange);
            ClampRange(ref alphaRange, 0f, 1f);
            maxRecyclesPerFrame = Mathf.Clamp(
                maxRecyclesPerFrame,
                1,
                poolSize);

            if (intensityCurve == null
                || intensityCurve.length == 0)
            {
                intensityCurve =
                    AnimationCurve.EaseInOut(
                        0f,
                        0f,
                        1f,
                        1f);
            }
        }

        private static void ClampRange(
            ref Vector2 range,
            float absoluteMinimum,
            float absoluteMaximum)
        {
            range.x = Mathf.Clamp(
                range.x,
                absoluteMinimum,
                absoluteMaximum);
            range.y = Mathf.Clamp(
                range.y,
                range.x,
                absoluteMaximum);
        }

        private static void SortRange(ref Vector2 range)
        {
            if (range.x <= range.y)
            {
                return;
            }

            float value = range.x;
            range.x = range.y;
            range.y = value;
        }

        private sealed class StreakRuntime
        {
            public StreakRuntime(
                SpriteRenderer renderer,
                Vector3 initialLocalScale,
                Quaternion initialLocalRotation,
                Color initialColor)
            {
                Renderer = renderer;
                Transform = renderer.transform;
                InitialLocalScale = initialLocalScale;
                InitialLocalRotation = initialLocalRotation;
                InitialColor = initialColor;
                SpeedMultiplier = 1f;
                PeakAlpha = initialColor.a;
            }

            public SpriteRenderer Renderer { get; }
            public Transform Transform { get; }
            public Vector3 InitialLocalScale { get; }
            public Quaternion InitialLocalRotation { get; }
            public Color InitialColor { get; }
            public float SpeedMultiplier { get; set; }
            public float HorizontalSpeed { get; set; }
            public float PeakAlpha { get; set; }
        }

        private readonly struct ViewportBounds
        {
            public ViewportBounds(
                float left,
                float right,
                float bottom,
                float top)
            {
                Left = left;
                Right = right;
                Bottom = bottom;
                Top = top;
            }

            public float Left { get; }
            public float Right { get; }
            public float Bottom { get; }
            public float Top { get; }
        }
    }
}
