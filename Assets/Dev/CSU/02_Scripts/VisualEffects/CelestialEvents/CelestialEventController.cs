using System.Collections.Generic;
using Dev.CSU._02_Scripts.Stage;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    [DefaultExecutionOrder(1210)]
    [DisallowMultipleComponent]
    public sealed class CelestialEventController : MonoBehaviour
    {
        [SerializeField] private CelestialEventSettings settings;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private StageThemeController stageThemeController;

        private readonly List<CelestialEffectPresenter> _activeEffects =
            new List<CelestialEffectPresenter>(4);

        private CelestialCameraViewportProvider _viewportProvider;
        private CelestialEventScheduler _shootingStarScheduler;
        private CelestialEventScheduler _cometScheduler;
        private CelestialEffectPool _shootingStarPool;
        private CelestialEffectPool _cometPool;
        private Transform _runtimeRoot;
        private UniversalAdditionalCameraData _cameraData;
        private bool _originalPostProcessing;
        private bool _changedPostProcessing;
        private bool _isInitialized;
        private bool _isVisible;
        private bool _warnedInvalidCamera;
        private bool _warnedMissingStageSource;

        public bool IsInitialized => _isInitialized;
        public bool IsVisible => _isVisible;
        public int ActiveShootingStarCount =>
            _shootingStarPool?.ActiveCount ?? 0;
        public int ActiveCometCount =>
            _cometPool?.ActiveCount ?? 0;
        public int ShootingStarPoolCapacity =>
            _shootingStarPool?.Capacity ?? 0;
        public int CometPoolCapacity =>
            _cometPool?.Capacity ?? 0;

        public void Initialize(
            Camera camera,
            CelestialEventSettings eventSettings,
            StageThemeController themeController)
        {
            if (_isInitialized
                && targetCamera == camera
                && settings == eventSettings
                && stageThemeController == themeController)
            {
                return;
            }

            DisposeRuntime();
            UnsubscribeStageEvents();

            targetCamera = camera;
            settings = eventSettings;
            stageThemeController = themeController;

            if (targetCamera == null
                || settings == null
                || !settings.SystemEnabled
                || !settings.HasRequiredAssets())
            {
                return;
            }

            _viewportProvider =
                new CelestialCameraViewportProvider(targetCamera);
            CreateRuntime();
            EnablePostProcessingIfRequested();
            SubscribeStageEvents();
            _isInitialized = true;
            ApplyVisibility();
        }

        [ContextMenu("Test/Force Shooting Star")]
        public void ForceSpawnShootingStar()
        {
            TrySpawn(settings?.ShootingStar);
        }

        [ContextMenu("Test/Force Comet")]
        public void ForceSpawnComet()
        {
            TrySpawn(settings?.Comet);
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                EnablePostProcessingIfRequested();
                SubscribeStageEvents();
                ApplyVisibility();
            }
        }

        private void Update()
        {
            if (!_isInitialized || !_isVisible)
            {
                return;
            }

            if (!_viewportProvider.TryGetViewport(
                out CelestialViewport viewport))
            {
                if (!_warnedInvalidCamera)
                {
                    Debug.LogWarning(
                        $"{nameof(CelestialEventController)} on "
                        + $"'{name}' requires an active "
                        + "orthographic camera.",
                        this);
                    _warnedInvalidCamera = true;
                }

                return;
            }

            _warnedInvalidCamera = false;
            float deltaTime = settings.UseUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            TickSchedulers(deltaTime);
            TickActiveEffects(deltaTime, viewport);
        }

        private void OnDisable()
        {
            UnsubscribeStageEvents();
            ReleaseAllEffects();
            RestorePostProcessing();
        }

        private void OnDestroy()
        {
            UnsubscribeStageEvents();
            DisposeRuntime();
        }

        private void CreateRuntime()
        {
            var root = new GameObject("CelestialEvents");
            _runtimeRoot = root.transform;
            _runtimeRoot.SetParent(transform, false);

            _shootingStarScheduler =
                new CelestialEventScheduler(settings.ShootingStar);
            _cometScheduler =
                new CelestialEventScheduler(settings.Comet);

            if (settings.ShootingStar.Enabled)
            {
                _shootingStarPool = new CelestialEffectPool(
                    settings.ShootingStar,
                    _runtimeRoot);
            }

            if (settings.Comet.Enabled)
            {
                _cometPool = new CelestialEffectPool(
                    settings.Comet,
                    _runtimeRoot);
            }
        }

        private void TickSchedulers(float deltaTime)
        {
            float multiplier =
                settings.TestProbabilityMultiplier;
            if (_shootingStarScheduler.Tick(
                deltaTime,
                multiplier))
            {
                TrySpawn(settings.ShootingStar);
            }

            if (_cometScheduler.Tick(deltaTime, multiplier))
            {
                TrySpawn(settings.Comet);
            }
        }

        private void TickActiveEffects(
            float deltaTime,
            in CelestialViewport viewport)
        {
            for (int index = _activeEffects.Count - 1;
                 index >= 0;
                 index--)
            {
                CelestialEffectPresenter effect =
                    _activeEffects[index];
                if (!effect.Tick(
                    deltaTime,
                    viewport,
                    settings.DespawnOutsideMargin))
                {
                    continue;
                }

                _activeEffects.RemoveAt(index);
                GetPool(effect.EffectType)?.Release(effect);
            }
        }

        private bool TrySpawn(CelestialEffectProfile profile)
        {
            if (!_isInitialized
                || !_isVisible
                || profile == null
                || !profile.Enabled
                || !_viewportProvider.TryGetViewport(
                    out CelestialViewport viewport))
            {
                return false;
            }

            CelestialEffectPool pool =
                GetPool(profile.EffectType);
            if (pool == null
                || !pool.TryAcquire(
                    out CelestialEffectPresenter presenter))
            {
                return false;
            }

            CelestialSpawnData spawnData =
                CreateSpawnData(profile, viewport);
            presenter.Play(profile, spawnData);
            _activeEffects.Add(presenter);
            return true;
        }

        private CelestialSpawnData CreateSpawnData(
            CelestialEffectProfile profile,
            in CelestialViewport viewport)
        {
            Vector2 position =
                CreateSpawnPosition(profile, viewport);
            float slope = Random.Range(
                profile.DownwardSlopeRange.x,
                profile.DownwardSlopeRange.y);
            Vector2 direction =
                new Vector2(-1f, -slope).normalized;

            float leftBoundary =
                -viewport.HalfWidth
                - settings.DespawnOutsideMargin;
            float bottomBoundary =
                -viewport.HalfHeight
                - settings.DespawnOutsideMargin;
            float distanceToLeft =
                (position.x - leftBoundary)
                / -direction.x;
            float distanceToBottom =
                (position.y - bottomBoundary)
                / -direction.y;
            float travelDistance = Mathf.Min(
                distanceToLeft,
                distanceToBottom);
            float crossingDuration = Random.Range(
                profile.CrossingDurationRange.x,
                profile.CrossingDurationRange.y);
            Vector2 velocity =
                direction * (travelDistance / crossingDuration);

            Sprite[] sprites = profile.HeadSprites;
            Sprite sprite =
                sprites[Random.Range(0, sprites.Length)];
            float scale = Random.Range(
                profile.ScaleRange.x,
                profile.ScaleRange.y);
            var localPosition =
                new Vector3(
                    position.x,
                    position.y,
                    profile.CameraDepth);
            return new CelestialSpawnData(
                localPosition,
                velocity,
                scale,
                sprite);
        }

        private Vector2 CreateSpawnPosition(
            CelestialEffectProfile profile,
            in CelestialViewport viewport)
        {
            bool spawnAtTop = profile.SpawnEdgeMode
                == CelestialSpawnEdgeMode.Top
                || (profile.SpawnEdgeMode
                    == CelestialSpawnEdgeMode.RandomRightOrTop
                    && Random.value < 0.5f);

            if (spawnAtTop)
            {
                float normalizedX = Random.Range(
                    profile.NormalizedTopStartRange.x,
                    profile.NormalizedTopStartRange.y);
                return new Vector2(
                    Mathf.Lerp(
                        -viewport.HalfWidth,
                        viewport.HalfWidth,
                        normalizedX),
                    viewport.HalfHeight
                    + settings.SpawnOutsideMargin);
            }

            float normalizedY = Random.Range(
                profile.NormalizedVerticalStartRange.x,
                profile.NormalizedVerticalStartRange.y);
            return new Vector2(
                viewport.HalfWidth
                + settings.SpawnOutsideMargin,
                Mathf.Lerp(
                    -viewport.HalfHeight,
                    viewport.HalfHeight,
                    normalizedY));
        }

        private CelestialEffectPool GetPool(
            CelestialEffectType effectType)
        {
            return effectType == CelestialEffectType.Comet
                ? _cometPool
                : _shootingStarPool;
        }

        private void ApplyVisibility()
        {
            if (!_isInitialized || settings == null)
            {
                SetVisible(false);
                return;
            }

            if (settings.VisibilityMode
                == CelestialVisibilityMode.AllStages)
            {
                _warnedMissingStageSource = false;
                SetVisible(true);
                return;
            }

            if (stageThemeController == null)
            {
                if (!_warnedMissingStageSource)
                {
                    Debug.LogWarning(
                        $"{nameof(CelestialEventController)} on "
                        + $"'{name}' uses Selected Stages but has "
                        + $"no {nameof(StageThemeController)}.",
                        this);
                    _warnedMissingStageSource = true;
                }

                SetVisible(false);
                return;
            }

            _warnedMissingStageSource = false;
            SetVisible(
                settings.IsVisibleAtStage(
                    stageThemeController.CurrentStageNumber));
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;
            if (!visible)
            {
                ReleaseAllEffects();
            }

            _shootingStarScheduler?.Reset();
            _cometScheduler?.Reset();
        }

        private void HandleStageChanged(int stageNumber)
        {
            SetVisible(settings.IsVisibleAtStage(stageNumber));
        }

        private void SubscribeStageEvents()
        {
            UnsubscribeStageEvents();
            if (stageThemeController != null)
            {
                stageThemeController.StageBackgroundReady +=
                    HandleStageChanged;
            }
        }

        private void UnsubscribeStageEvents()
        {
            if (stageThemeController != null)
            {
                stageThemeController.StageBackgroundReady -=
                    HandleStageChanged;
            }
        }

        private void ReleaseAllEffects()
        {
            _shootingStarPool?.ReleaseAll();
            _cometPool?.ReleaseAll();
            _activeEffects.Clear();
        }

        private void EnablePostProcessingIfRequested()
        {
            if (!settings.EnableCameraPostProcessing
                || !targetCamera.TryGetComponent(
                    out _cameraData))
            {
                return;
            }

            _originalPostProcessing =
                _cameraData.renderPostProcessing;
            if (!_originalPostProcessing)
            {
                _cameraData.renderPostProcessing = true;
                _changedPostProcessing = true;
            }
        }

        private void RestorePostProcessing()
        {
            if (_changedPostProcessing && _cameraData != null)
            {
                _cameraData.renderPostProcessing =
                    _originalPostProcessing;
            }

            _cameraData = null;
            _changedPostProcessing = false;
        }

        private void DisposeRuntime()
        {
            ReleaseAllEffects();
            _shootingStarPool?.Dispose();
            _cometPool?.Dispose();
            _shootingStarPool = null;
            _cometPool = null;
            _shootingStarScheduler = null;
            _cometScheduler = null;
            _viewportProvider = null;

            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot.gameObject);
                _runtimeRoot = null;
            }

            RestorePostProcessing();
            _isInitialized = false;
            _isVisible = false;
        }
    }
}
