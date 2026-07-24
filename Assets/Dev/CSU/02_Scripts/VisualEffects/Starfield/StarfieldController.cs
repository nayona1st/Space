using System.Collections.Generic;
using Dev.CSU._02_Scripts.Stage;
using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.Starfield
{
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class StarfieldController : MonoBehaviour
    {
        [SerializeField] private StarfieldSettings settings;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private StageThemeController stageThemeController;

        private readonly List<StarfieldParticleLayer> _layers =
            new List<StarfieldParticleLayer>(2);

        private OrthographicCameraViewportProvider _viewportProvider;
        private StarfieldViewport _currentViewport;
        private bool _hasViewport;
        private bool _isInitialized;
        private bool _isStarted;
        private bool _isVisible;
        private bool _warnedMissingStageSource;
        private bool _warnedInvalidCamera;

        public bool IsInitialized => _isInitialized;
        public bool IsVisible => _isVisible;
        public int LayerCount => _layers.Count;

        public int TotalParticleCount
        {
            get
            {
                int total = 0;
                for (int index = 0; index < _layers.Count; index++)
                {
                    total += _layers[index].ParticleCount;
                }

                return total;
            }
        }

        public int MaximumParticleCount
        {
            get
            {
                int total = 0;
                for (int index = 0; index < _layers.Count; index++)
                {
                    total += _layers[index].MaximumParticles;
                }

                return total;
            }
        }

        public void Initialize(
            Camera camera,
            StarfieldSettings starfieldSettings,
            StageThemeController themeController)
        {
            if (_isInitialized
                && targetCamera == camera
                && settings == starfieldSettings
                && stageThemeController == themeController)
            {
                return;
            }

            DisposeLayers();
            UnsubscribeStageEvents();

            targetCamera = camera;
            settings = starfieldSettings;
            stageThemeController = themeController;

            if (targetCamera == null
                || settings == null
                || !settings.SystemEnabled
                || !settings.HasRequiredAssets())
            {
                _isInitialized = false;
                return;
            }

            _viewportProvider =
                new OrthographicCameraViewportProvider(targetCamera);
            CreateConfiguredLayers();
            _isInitialized = _layers.Count > 0;
            SubscribeStageEvents();
            TryRefreshViewport(true);

            if (_isStarted)
            {
                ApplyVisibility();
            }
        }

        private void Start()
        {
            _isStarted = true;
            SubscribeStageEvents();
            ApplyVisibility();
        }

        private void OnEnable()
        {
            if (_isStarted)
            {
                SubscribeStageEvents();
                ApplyVisibility();
            }
        }

        private void LateUpdate()
        {
            if (_isInitialized)
            {
                TryRefreshViewport(false);
                CullParticlesOutsideViewport();
            }
        }

        private void OnDisable()
        {
            UnsubscribeStageEvents();
            StopAllLayers();
        }

        private void OnDestroy()
        {
            UnsubscribeStageEvents();
            DisposeLayers();
        }

        private void CreateConfiguredLayers()
        {
            AddLayerIfEnabled(settings.FarLayer);
            AddLayerIfEnabled(settings.NearLayer);
        }

        private void AddLayerIfEnabled(
            StarfieldLayerSettings layerSettings)
        {
            if (layerSettings == null || !layerSettings.LayerEnabled)
            {
                return;
            }

            _layers.Add(
                new StarfieldParticleLayer(
                    transform,
                    settings,
                    layerSettings));
        }

        private void TryRefreshViewport(bool force)
        {
            if (_viewportProvider == null
                || !_viewportProvider.TryGetViewport(
                    out StarfieldViewport viewport))
            {
                if (!_warnedInvalidCamera)
                {
                    Debug.LogWarning(
                        $"{nameof(StarfieldController)} on '{name}' "
                        + "requires an active orthographic camera.",
                        this);
                    _warnedInvalidCamera = true;
                }

                StopAllLayers();
                return;
            }

            _warnedInvalidCamera = false;
            if (!force
                && _hasViewport
                && _currentViewport.ApproximatelyEquals(viewport))
            {
                return;
            }

            _currentViewport = viewport;
            _hasViewport = true;

            for (int index = 0; index < _layers.Count; index++)
            {
                _layers[index].Rebuild(viewport, _isVisible);
            }
        }

        private void ApplyVisibility()
        {
            if (!_isInitialized || settings == null)
            {
                _isVisible = false;
                StopAllLayers();
                return;
            }

            if (settings.VisibilityMode
                == StarfieldVisibilityMode.AllStages)
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
                        $"{nameof(StarfieldController)} on '{name}' "
                        + "uses Selected Stages visibility but has no "
                        + $"{nameof(StageThemeController)}.",
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
            _isVisible = visible;
            if (!visible)
            {
                StopAllLayers();
                return;
            }

            if (!_hasViewport)
            {
                TryRefreshViewport(true);
                return;
            }

            for (int index = 0; index < _layers.Count; index++)
            {
                _layers[index].Play();
            }
        }

        private void HandleStageChanged(int stageNumber)
        {
            if (settings != null)
            {
                SetVisible(settings.IsVisibleAtStage(stageNumber));
            }
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

        private void StopAllLayers()
        {
            for (int index = 0; index < _layers.Count; index++)
            {
                _layers[index].StopAndClear();
            }
        }

        private void CullParticlesOutsideViewport()
        {
            if (!_hasViewport || settings == null)
            {
                return;
            }

            float localLeftBoundary = -_currentViewport.Width
                - settings.SpawnRightPadding
                - settings.DespawnLeftPadding
                - settings.EmitterWidth;
            for (int index = 0; index < _layers.Count; index++)
            {
                _layers[index].CullLeftOf(localLeftBoundary);
            }
        }

        private void DisposeLayers()
        {
            for (int index = 0; index < _layers.Count; index++)
            {
                _layers[index].Dispose();
            }

            _layers.Clear();
            _viewportProvider = null;
            _hasViewport = false;
            _isVisible = false;
            _isInitialized = false;
        }
    }
}
