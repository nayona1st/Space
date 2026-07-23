using System;
using System.Collections.Generic;
using Dev.CSU.Scripts.Stage;
using UnityEngine;

namespace Dev.CSU.Scripts.Planet
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class PlanetParallaxController : MonoBehaviour
    {
        private enum PresentationMode
        {
            PassBy,
            FollowForDistance
        }

        private enum StagePresentation
        {
            PlanetSequence,
            StarCloud,
            UfoAura
        }

        [Serializable]
        private sealed class StageFormat
        {
            [Tooltip("Stage number that uses this presentation format.")]
            [Min(1)]
            [SerializeField] private int stageNumber = 1;

            [Tooltip("Presentation system enabled while this stage is active.")]
            [SerializeField] private StagePresentation presentation =
                StagePresentation.PlanetSequence;

            public int StageNumber => stageNumber;
            public StagePresentation Presentation => presentation;

            public void Validate()
            {
                stageNumber = Mathf.Max(1, stageNumber);
            }
        }

        [Serializable]
        private sealed class PlanetSetting
        {
            [Tooltip("Planet Prefab displayed in sequence.")]
            [SerializeField] private GameObject prefab;

            [Tooltip("Stage that uses this Planet Setting.")]
            [Min(1)]
            [SerializeField] private int stageNumber = 1;

            [Tooltip("How this image is presented: pass across the screen, or follow the camera for a configured distance.")]
            [SerializeField] private PresentationMode presentationMode =
                PresentationMode.PassBy;

            [Tooltip("Camera travel distance after this planet appears before the next planet can appear. This value is ignored in Follow For Distance mode.")]
            [Min(0f)]
            [SerializeField] private float planetChangeDistance = 200f;

            [Tooltip("How much this planet follows the camera on the X axis. A higher value makes it move more slowly across the screen.")]
            [Range(0f, 1f)]
            [SerializeField] private float parallaxFollow = 0.5f;

            [Tooltip("Vertical viewport position of this planet's center. 0 is bottom and 1 is top.")]
            [Range(0f, 1f)]
            [SerializeField] private float viewportY = 0.5f;

            [Tooltip("Rightward camera travel distance, in Unity Units, for which this image remains fixed at its viewport position.")]
            [Min(0f)]
            [SerializeField] private float followDistance = 200f;

            [Tooltip("Horizontal viewport position used while following the camera. 0 is left and 1 is right.")]
            [Range(0f, 1f)]
            [SerializeField] private float followViewportX = 0.75f;

            [Tooltip("Additional world-space padding outside the camera's right edge when this planet appears.")]
            [Min(0f)]
            [SerializeField] private float spawnRightPadding = 5f;

            [Tooltip("Distance past the camera's left edge before this planet returns to the pool.")]
            [Min(0f)]
            [SerializeField] private float despawnLeftPadding = 5f;

            public GameObject Prefab => prefab;
            public int StageNumber => stageNumber;
            public bool FollowsForDistance =>
                presentationMode == PresentationMode.FollowForDistance;
            public float PlanetChangeDistance => planetChangeDistance;
            public float ParallaxFollow => parallaxFollow;
            public float ViewportY => viewportY;
            public float FollowDistance => followDistance;
            public float FollowViewportX => followViewportX;
            public float SpawnRightPadding => spawnRightPadding;
            public float DespawnLeftPadding => despawnLeftPadding;

            public void Validate()
            {
                stageNumber = Mathf.Max(1, stageNumber);
                planetChangeDistance = Mathf.Max(0f, planetChangeDistance);
                parallaxFollow = Mathf.Clamp01(parallaxFollow);
                viewportY = Mathf.Clamp01(viewportY);
                followDistance = Mathf.Max(0f, followDistance);
                followViewportX = Mathf.Clamp01(followViewportX);
                spawnRightPadding = Mathf.Max(0f, spawnRightPadding);
                despawnLeftPadding = Mathf.Max(0f, despawnLeftPadding);
            }
        }

        [Header("Stage Routing")]
        [Tooltip("Maps each stage number to its dedicated presentation system.")]
        [SerializeField] private StageFormat[] stageFormats;

        [Tooltip("Stage 2 Star Cloud presentation component.")]
        [SerializeField] private StarCloudParallax starCloudParallax;

        [Tooltip("Stage 3 UFO and Aura presentation component.")]
        [SerializeField] private UfoAuraParallax ufoAuraParallax;

        [Header("Stage 1 Planets")]
        [Tooltip("Per-planet Prefab, stage and parallax settings.")]
        [SerializeField] private PlanetSetting[] planetSettings;

        [Header("Common References")]
        [Tooltip("Camera used to calculate parallax movement. Camera.main is used when this is empty.")]
        [SerializeField] private Camera targetCamera;

        [Header("Startup")]
        [Tooltip("Stage started when Play Mode begins.")]
        [Min(1)]
        [SerializeField] private int initialStageNumber = 1;

        [Tooltip("Show the first planet of the initial stage immediately when Play Mode starts.")]
        [SerializeField] private bool spawnFirstImmediately = true;

        private readonly List<ParallaxPlanet> _planetPool =
            new List<ParallaxPlanet>();
        private readonly List<int> _sourcePrefabIndices = new List<int>();
        private readonly List<PlanetSetting> _poolSettings =
            new List<PlanetSetting>();
        private readonly List<int> _currentStagePoolIndices =
            new List<int>();
        private readonly HashSet<int> _warnedMissingStages =
            new HashSet<int>();

        private ParallaxPlanet _activePlanet;
        private PlanetSetting _currentPlanetSetting;
        private StageParallaxDecoration _activeDecoration;
        private StagePresentation _currentPresentation;
        private int _nextStageSequenceIndex;
        private float _previousCameraX;
        private float _distanceSinceLastSpawn;
        private float _followedDistance;
        private bool _poolReady;
        private bool _hasCameraSample;
        private bool _stageRunning;
        private bool _stageCompletionRaised;
        private bool _spawnFirstWhenCameraReady;
        private bool _isFollowingCamera;
        private bool _warnedMissingCamera;
        private bool _warnedMissingPrefabs;

        public int CurrentStageNumber { get; private set; }

        public event Action<int, GameObject> PlanetChanged;
        public event Action<int> StageCompleted;

        private void OnEnable()
        {
            _hasCameraSample = false;
            SubscribeDecorationEvents();
        }

        private void Start()
        {
            _poolReady = BuildPool();

            if (TryResolveCamera())
            {
                BeginCameraTracking();
            }

            StartStageInternal(initialStageNumber, spawnFirstImmediately);
        }

        private void OnDisable()
        {
            UnsubscribeDecorationEvents();
            StopActivePresentation();
        }

        private void LateUpdate()
        {
            if (_currentPresentation != StagePresentation.PlanetSequence
                || !_poolReady
                || !_stageRunning
                || !TryResolveCamera())
            {
                return;
            }

            if (!_hasCameraSample)
            {
                BeginCameraTracking();

                if (_spawnFirstWhenCameraReady)
                {
                    _spawnFirstWhenCameraReady = false;
                    ActivateNextPlanet();
                }

                return;
            }

            float currentCameraX = targetCamera.transform.position.x;
            float cameraDeltaX = currentCameraX - _previousCameraX;
            _previousCameraX = currentCameraX;
            _distanceSinceLastSpawn += Mathf.Max(0f, cameraDeltaX);

            if (_activePlanet != null)
            {
                UpdateActivePlanet(cameraDeltaX);

                if (_activePlanet.IsCompletelyLeftOf(
                        targetCamera,
                        _currentPlanetSetting.DespawnLeftPadding))
                {
                    _activePlanet.ReturnToPool();
                    _activePlanet = null;

                    if (_nextStageSequenceIndex
                        >= _currentStagePoolIndices.Count)
                    {
                        CompleteCurrentStage();
                        return;
                    }
                }
            }

            if (_activePlanet != null
                || _nextStageSequenceIndex
                >= _currentStagePoolIndices.Count)
            {
                return;
            }

            float requiredChangeDistance = _currentPlanetSetting != null
                ? _currentPlanetSetting.PlanetChangeDistance
                : GetNextSetting().PlanetChangeDistance;

            if (_distanceSinceLastSpawn >= requiredChangeDistance)
            {
                ActivateNextPlanet();
            }
        }

        public bool HasStage(int stageNumber)
        {
            return FindStageFormat(Mathf.Max(1, stageNumber)) != null;
        }

        public void StartStage(int stageNumber)
        {
            StartStageInternal(stageNumber, true);
        }

        private void StartStageInternal(
            int stageNumber,
            bool spawnImmediately)
        {
            stageNumber = Mathf.Max(1, stageNumber);
            StageFormat format = FindStageFormat(stageNumber);

            if (format == null)
            {
                WarnMissingStageOnce(
                    stageNumber,
                    "No Stage Format is assigned.");
                return;
            }

            if (!CanStartPresentation(stageNumber, format.Presentation))
            {
                return;
            }

            StopActivePresentation();
            CurrentStageNumber = stageNumber;
            _currentPresentation = format.Presentation;
            _stageRunning = true;
            _stageCompletionRaised = false;

            switch (_currentPresentation)
            {
                case StagePresentation.PlanetSequence:
                    BeginPlanetSequence(stageNumber, spawnImmediately);
                    break;

                case StagePresentation.StarCloud:
                    BeginDecoration(starCloudParallax, stageNumber);
                    break;

                case StagePresentation.UfoAura:
                    BeginDecoration(ufoAuraParallax, stageNumber);
                    break;
            }
        }

        private bool CanStartPresentation(
            int stageNumber,
            StagePresentation presentation)
        {
            switch (presentation)
            {
                case StagePresentation.PlanetSequence:
                    if (!_poolReady)
                    {
                        WarnMissingStageOnce(
                            stageNumber,
                            "The Stage 1 planet pool is not ready.");
                        return false;
                    }

                    if (!HasPlanetSettingForStage(stageNumber))
                    {
                        WarnMissingStageOnce(
                            stageNumber,
                            "No usable Planet Settings are assigned.");
                        return false;
                    }

                    return true;

                case StagePresentation.StarCloud:
                    return ValidateDecoration(
                        stageNumber,
                        starCloudParallax,
                        "Star Cloud");

                case StagePresentation.UfoAura:
                    return ValidateDecoration(
                        stageNumber,
                        ufoAuraParallax,
                        "UFO and Aura");

                default:
                    return false;
            }
        }

        private bool ValidateDecoration(
            int stageNumber,
            StageParallaxDecoration decoration,
            string label)
        {
            if (decoration != null && decoration.IsConfigured)
            {
                return true;
            }

            WarnMissingStageOnce(
                stageNumber,
                $"{label} presentation is not configured.");
            return false;
        }

        private void BeginPlanetSequence(
            int stageNumber,
            bool spawnImmediately)
        {
            _currentStagePoolIndices.Clear();
            for (int poolIndex = 0;
                 poolIndex < _poolSettings.Count;
                 poolIndex++)
            {
                if (_poolSettings[poolIndex].StageNumber == stageNumber)
                {
                    _currentStagePoolIndices.Add(poolIndex);
                }
            }

            _nextStageSequenceIndex = 0;
            _distanceSinceLastSpawn = 0f;
            _followedDistance = 0f;
            _currentPlanetSetting = null;
            _isFollowingCamera = false;
            _spawnFirstWhenCameraReady = spawnImmediately;

            if (!TryResolveCamera())
            {
                _hasCameraSample = false;
                return;
            }

            BeginCameraTracking();

            if (spawnImmediately)
            {
                _spawnFirstWhenCameraReady = false;
                ActivateNextPlanet();
            }
        }

        private void BeginDecoration(
            StageParallaxDecoration decoration,
            int stageNumber)
        {
            if (decoration.BeginStage(targetCamera))
            {
                _activeDecoration = decoration;
                return;
            }

            _stageRunning = false;
            WarnMissingStageOnce(
                stageNumber,
                "The assigned decoration could not start.");
        }

        private void StopActivePresentation()
        {
            if (_activePlanet != null)
            {
                _activePlanet.ReturnToPool();
                _activePlanet = null;
            }

            if (starCloudParallax != null)
            {
                starCloudParallax.StopStage();
            }

            if (ufoAuraParallax != null)
            {
                ufoAuraParallax.StopStage();
            }

            _activeDecoration = null;
            _currentPlanetSetting = null;
            _currentStagePoolIndices.Clear();
            _isFollowingCamera = false;
            _spawnFirstWhenCameraReady = false;
            _stageRunning = false;
        }

        private bool BuildPool()
        {
            if (planetSettings == null || planetSettings.Length == 0)
            {
                WarnMissingPrefabsOnce("The Planet Settings array is empty.");
                return false;
            }

            for (int index = 0; index < planetSettings.Length; index++)
            {
                PlanetSetting setting = planetSettings[index];
                GameObject prefab = setting != null ? setting.Prefab : null;
                if (prefab == null)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"an empty setting at array index {index}.",
                        this);
                    continue;
                }

                if (prefab.GetComponentInChildren<SpriteRenderer>(true) == null)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"'{prefab.name}' because it has no SpriteRenderer.",
                        this);
                    continue;
                }

                GameObject instance = Instantiate(prefab, transform);
                instance.name = $"{prefab.name}_Pooled";

                if (!instance.TryGetComponent(out ParallaxPlanet planet))
                {
                    planet = instance.AddComponent<ParallaxPlanet>();
                }

                if (!planet.Initialize())
                {
                    instance.SetActive(false);
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' could not "
                        + $"initialize the pooled instance for '{prefab.name}'.",
                        this);
                    continue;
                }

                planet.ReturnToPool();
                _planetPool.Add(planet);
                _sourcePrefabIndices.Add(index);
                _poolSettings.Add(setting);
            }

            if (_planetPool.Count == 0)
            {
                WarnMissingPrefabsOnce(
                    "No usable planet Prefabs with SpriteRenderer components were found.");
                return false;
            }

            _warnedMissingPrefabs = false;
            return true;
        }

        private bool HasPlanetSettingForStage(int stageNumber)
        {
            for (int index = 0; index < _poolSettings.Count; index++)
            {
                if (_poolSettings[index].StageNumber == stageNumber)
                {
                    return true;
                }
            }

            return false;
        }

        private StageFormat FindStageFormat(int stageNumber)
        {
            if (stageFormats == null)
            {
                return null;
            }

            for (int index = 0; index < stageFormats.Length; index++)
            {
                StageFormat format = stageFormats[index];
                if (format != null && format.StageNumber == stageNumber)
                {
                    return format;
                }
            }

            return null;
        }

        private void BeginCameraTracking()
        {
            _previousCameraX = targetCamera.transform.position.x;
            _hasCameraSample = true;
        }

        private PlanetSetting GetNextSetting()
        {
            int poolIndex =
                _currentStagePoolIndices[_nextStageSequenceIndex];
            return _poolSettings[poolIndex];
        }

        private void ActivateNextPlanet()
        {
            if (!_stageRunning
                || _activePlanet != null
                || _nextStageSequenceIndex
                >= _currentStagePoolIndices.Count)
            {
                return;
            }

            int poolIndex =
                _currentStagePoolIndices[_nextStageSequenceIndex];
            ParallaxPlanet planet = _planetPool[poolIndex];
            PlanetSetting setting = _poolSettings[poolIndex];
            int sourcePrefabIndex = _sourcePrefabIndices[poolIndex];

            if (setting.FollowsForDistance)
            {
                planet.ActivateAtViewport(
                    targetCamera,
                    setting.FollowViewportX,
                    setting.ViewportY);
            }
            else
            {
                planet.ActivateAtCameraRight(
                    targetCamera,
                    setting.ViewportY,
                    setting.SpawnRightPadding);
            }

            _activePlanet = planet;
            _currentPlanetSetting = setting;
            _distanceSinceLastSpawn = 0f;
            _followedDistance = 0f;
            _isFollowingCamera = setting.FollowsForDistance
                && setting.FollowDistance > 0f;
            _nextStageSequenceIndex++;

            PlanetChanged?.Invoke(sourcePrefabIndex, planet.RootObject);
        }

        private void UpdateActivePlanet(float cameraDeltaX)
        {
            if (_currentPlanetSetting.FollowsForDistance
                && _isFollowingCamera)
            {
                _followedDistance += Mathf.Max(0f, cameraDeltaX);
                _activePlanet.AlignCenterToViewport(
                    targetCamera,
                    _currentPlanetSetting.FollowViewportX,
                    _currentPlanetSetting.ViewportY);

                if (_followedDistance
                    >= _currentPlanetSetting.FollowDistance)
                {
                    _isFollowingCamera = false;
                }

                return;
            }

            _activePlanet.ApplyParallax(
                cameraDeltaX,
                _currentPlanetSetting.ParallaxFollow);
            _activePlanet.MaintainViewportY(
                targetCamera,
                _currentPlanetSetting.ViewportY);
        }

        private void HandleStarCloudCompleted()
        {
            if (_activeDecoration == starCloudParallax)
            {
                CompleteCurrentStage();
            }
        }

        private void HandleUfoAuraCompleted()
        {
            if (_activeDecoration == ufoAuraParallax)
            {
                CompleteCurrentStage();
            }
        }

        private void CompleteCurrentStage()
        {
            if (_stageCompletionRaised)
            {
                return;
            }

            _stageRunning = false;
            _stageCompletionRaised = true;
            _activeDecoration = null;
            _currentPlanetSetting = null;
            _isFollowingCamera = false;
            _spawnFirstWhenCameraReady = false;

            StageCompleted?.Invoke(CurrentStageNumber);
        }

        private void SubscribeDecorationEvents()
        {
            UnsubscribeDecorationEvents();

            if (starCloudParallax != null)
            {
                starCloudParallax.Completed +=
                    HandleStarCloudCompleted;
            }

            if (ufoAuraParallax != null)
            {
                ufoAuraParallax.Completed +=
                    HandleUfoAuraCompleted;
            }
        }

        private void UnsubscribeDecorationEvents()
        {
            if (starCloudParallax != null)
            {
                starCloudParallax.Completed -=
                    HandleStarCloudCompleted;
            }

            if (ufoAuraParallax != null)
            {
                ufoAuraParallax.Completed -=
                    HandleUfoAuraCompleted;
            }
        }

        private bool TryResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                _warnedMissingCamera = false;
                return true;
            }

            if (!_warnedMissingCamera)
            {
                Debug.LogWarning(
                    $"{nameof(PlanetParallaxController)} on '{name}' could not "
                    + "find a camera.",
                    this);
                _warnedMissingCamera = true;
            }

            return false;
        }

        private void WarnMissingPrefabsOnce(string reason)
        {
            if (_warnedMissingPrefabs)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(PlanetParallaxController)} on '{name}' is inactive: "
                + reason,
                this);
            _warnedMissingPrefabs = true;
        }

        private void WarnMissingStageOnce(
            int stageNumber,
            string reason)
        {
            if (!_warnedMissingStages.Add(stageNumber))
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(PlanetParallaxController)} on '{name}' could not "
                + $"start stage {stageNumber}: {reason}",
                this);
        }

        private void OnValidate()
        {
            initialStageNumber = Mathf.Max(1, initialStageNumber);

            if (stageFormats != null)
            {
                foreach (StageFormat format in stageFormats)
                {
                    format?.Validate();
                }
            }

            if (planetSettings != null)
            {
                foreach (PlanetSetting setting in planetSettings)
                {
                    setting?.Validate();
                }
            }
        }
    }
}
