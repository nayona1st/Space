using Dev.CSU._02_Scripts.Planet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.Stage
{
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class UfoAuraBackgroundPresenter : MonoBehaviour
    {
        private const string SettingsResourcePath =
            "StageBackgrounds/UfoAuraBackgroundSettings";

        [SerializeField] private UfoAuraBackgroundSettings settings;
        [SerializeField] private PlanetParallaxController planetController;

        private Camera _targetCamera;
        private GameObject _backgroundInstance;
        private GameObject _sourcePrefab;
        private SpriteRenderer[] _renderers;
        private Color[] _baseColors;
        private bool _warnedMissingSettings;
        private bool _warnedMissingPrefab;

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
            InstallPresenters();
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode)
        {
            InstallPresenters();
        }

        private static void InstallPresenters()
        {
            PlanetParallaxController[] controllers =
                FindObjectsByType<PlanetParallaxController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0; index < controllers.Length; index++)
            {
                PlanetParallaxController controller = controllers[index];
                if (controller.GetComponent<UfoAuraBackgroundPresenter>()
                    == null)
                {
                    controller.gameObject.AddComponent<
                        UfoAuraBackgroundPresenter>();
                }
            }
        }

        private void Awake()
        {
            if (settings == null)
            {
                settings = Resources.Load<UfoAuraBackgroundSettings>(
                    SettingsResourcePath);
            }

            if (planetController == null)
            {
                planetController =
                    GetComponent<PlanetParallaxController>();
            }

            ApplyMoveSpeed();
        }

        private void LateUpdate()
        {
            if (!TryResolveConfiguration())
            {
                ResetMoveSpeed();
                ReleaseBackground();
                return;
            }

            ApplyMoveSpeed();

            if (!planetController.isActiveAndEnabled
                || !planetController.TryGetUfoAuraThemeProgress(
                    out float themeProgress))
            {
                ReleaseBackground();
                return;
            }

            EnsureBackground();
            ApplyVisualSettings();
            PositionBackground(themeProgress);
        }

        private void OnDisable()
        {
            ResetMoveSpeed();
            ReleaseBackground();
        }

        private void OnDestroy()
        {
            ResetMoveSpeed();
            ReleaseBackground();
        }

        private void ApplyMoveSpeed()
        {
            if (planetController != null && settings != null)
            {
                planetController.SetUfoAuraBackgroundMoveSpeed(
                    settings.MoveSpeed);
            }
        }

        private void ResetMoveSpeed()
        {
            if (planetController != null)
            {
                planetController.SetUfoAuraBackgroundMoveSpeed(1f);
            }
        }

        private bool TryResolveConfiguration()
        {
            if (settings == null)
            {
                if (!_warnedMissingSettings)
                {
                    Debug.LogWarning(
                        $"{nameof(UfoAuraBackgroundPresenter)} on '{name}' "
                        + $"could not load Resources/{SettingsResourcePath}.",
                        this);
                    _warnedMissingSettings = true;
                }

                return false;
            }

            _warnedMissingSettings = false;

            if (planetController == null)
            {
                planetController =
                    GetComponent<PlanetParallaxController>();
            }

            GameObject prefab = settings.BackgroundPrefab;
            if (prefab != null)
            {
                _warnedMissingPrefab = false;
                return planetController != null;
            }

            if (!_warnedMissingPrefab)
            {
                Debug.LogWarning(
                    $"{nameof(UfoAuraBackgroundPresenter)} on '{name}' "
                    + "has no supplemental background prefab assigned.",
                    this);
                _warnedMissingPrefab = true;
            }

            return false;
        }

        private void EnsureBackground()
        {
            GameObject prefab = settings.BackgroundPrefab;
            if (_backgroundInstance != null && _sourcePrefab == prefab)
            {
                return;
            }

            ReleaseBackground();

            if (!TryResolveCamera())
            {
                return;
            }

            _backgroundInstance = Instantiate(prefab);
            _backgroundInstance.name = prefab.name;
            _sourcePrefab = prefab;
            _renderers =
                _backgroundInstance.GetComponentsInChildren<
                    SpriteRenderer>(true);
            _baseColors = new Color[_renderers.Length];

            for (int index = 0; index < _renderers.Length; index++)
            {
                _baseColors[index] = _renderers[index].color;
            }
        }

        private void ApplyVisualSettings()
        {
            if (_backgroundInstance == null)
            {
                return;
            }

            Vector2 configuredScale = settings.LocalScale;
            _backgroundInstance.transform.localScale =
                new Vector3(
                    configuredScale.x,
                    configuredScale.y,
                    1f);

            string layerName = settings.SortingLayerName;
            int sortingOrder = settings.SortingOrder;
            float opacity = settings.Opacity;

            for (int index = 0; index < _renderers.Length; index++)
            {
                SpriteRenderer renderer = _renderers[index];
                renderer.sortingLayerName = layerName;
                renderer.sortingOrder = sortingOrder;

                Color color = _baseColors[index];
                color.a *= opacity;
                renderer.color = color;
            }
        }

        private void PositionBackground(float themeProgress)
        {
            if (_backgroundInstance == null || !TryResolveCamera())
            {
                return;
            }

            if (!TryGetRendererBounds(out Bounds bounds))
            {
                return;
            }

            float depth = settings.CameraDepth;
            Vector3 cameraLeftCenter =
                _targetCamera.ViewportToWorldPoint(
                    new Vector3(0f, 0.5f, depth));
            Vector3 cameraRightCenter =
                _targetCamera.ViewportToWorldPoint(
                    new Vector3(1f, 0.5f, depth));
            float halfWidth = bounds.extents.x;
            Vector2 offset = settings.PositionOffset;
            float startCenterX =
                cameraRightCenter.x
                + settings.SpawnRightPadding
                + halfWidth
                + offset.x;
            float endCenterX =
                cameraLeftCenter.x
                - settings.DespawnLeftPadding
                - halfWidth
                + offset.x;
            float targetCenterX = Mathf.Lerp(
                startCenterX,
                endCenterX,
                Mathf.Clamp01(themeProgress));
            Vector3 targetCenter = new Vector3(
                targetCenterX,
                cameraRightCenter.y + offset.y,
                cameraRightCenter.z);

            _backgroundInstance.transform.position +=
                targetCenter - bounds.center;
        }

        private bool TryResolveCamera()
        {
            if (_targetCamera != null && _targetCamera.isActiveAndEnabled)
            {
                return true;
            }

            _targetCamera = Camera.main;
            if (_targetCamera == null)
            {
                _targetCamera = FindFirstObjectByType<Camera>(
                    FindObjectsInactive.Exclude);
            }

            return _targetCamera != null;
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            bounds = default;
            if (_renderers == null || _renderers.Length == 0)
            {
                return false;
            }

            bool hasBounds = false;
            for (int index = 0; index < _renderers.Length; index++)
            {
                SpriteRenderer renderer = _renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void ReleaseBackground()
        {
            if (_backgroundInstance != null)
            {
                Destroy(_backgroundInstance);
            }

            _backgroundInstance = null;
            _sourcePrefab = null;
            _renderers = null;
            _baseColors = null;
            _targetCamera = null;
        }
    }
}
