using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dev.CSU._02_Scripts.Planet
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
            [Tooltip("이 연출 형식을 사용하는 스테이지 번호입니다.")]
            [Min(1)]
            [SerializeField] private int stageNumber = 1;

            [Tooltip("이 스테이지가 활성화된 동안 사용할 연출 시스템입니다.")]
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
            [Tooltip("순서대로 표시할 행성 프리팹입니다.")]
            [SerializeField] private GameObject prefab;

            [Tooltip("이 행성 설정을 사용하는 스테이지입니다.")]
            [Min(1)]
            [SerializeField] private int stageNumber = 1;

            [Tooltip("이미지 연출 방식을 설정합니다. 화면을 가로질러 지나가거나, 설정된 거리만큼 카메라를 따라갑니다.")]
            [SerializeField] private PresentationMode presentationMode =
                PresentationMode.PassBy;

            [Tooltip("이 행성이 나타난 후 다음 행성이 나타날 수 있을 때까지 카메라가 이동해야 하는 거리입니다. 'Follow For Distance' 모드에서는 무시됩니다.")]
            [Min(0f)]
            [SerializeField] private float planetChangeDistance = 200f;

            [Tooltip("이 행성이 X축에서 카메라를 따라가는 정도입니다. 값이 클수록 화면을 더 천천히 가로지릅니다.")]
            [Range(0f, 1f)]
            [SerializeField] private float parallaxFollow = 0.5f;

            [Tooltip("행성 중심의 뷰포트 세로 위치입니다. 0은 아래쪽, 1은 위쪽입니다.")]
            [Range(0f, 1f)]
            [SerializeField] private float viewportY = 0.5f;

            [Tooltip("이미지가 뷰포트 위치에 고정된 채 유지되는 카메라의 오른쪽 이동 거리입니다. 단위는 Unity Unit입니다.")]
            [Min(0f)]
            [SerializeField] private float followDistance = 200f;

            [Tooltip("카메라를 따라가는 동안 사용할 뷰포트 가로 위치입니다. 0은 왼쪽, 1은 오른쪽입니다.")]
            [Range(0f, 1f)]
            [SerializeField] private float followViewportX = 0.75f;

            [Tooltip("행성이 나타날 때 카메라 오른쪽 가장자리 바깥에 추가할 월드 공간 여백입니다.")]
            [Min(0f)]
            [SerializeField] private float spawnRightPadding = 5f;

            [Tooltip("행성이 풀로 반환되기 전 카메라 왼쪽 가장자리를 지나 이동할 거리입니다.")]
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

        [Serializable]
        private sealed class StageDecorationSetting
        {
            [Tooltip("이 장식 설정을 사용하는 연출 형식입니다.")]
            [SerializeField] private StagePresentation presentation =
                StagePresentation.StarCloud;

            [Tooltip("한 번 생성한 뒤 이 연출이 활성화될 때마다 재사용할 프리팹입니다.")]
            [SerializeField] private GameObject decorationPrefab;

            [Tooltip("X축 이동으로 패럴랙스를 구동할 트랜스폼입니다. 비어 있으면 Target Camera의 트랜스폼을 사용합니다.")]
            [SerializeField] private Transform horizontalMovementSource;

            [Header("Vertical Placement")]
            [Tooltip("스테이지 내내 유지할 뷰포트 세로 위치입니다. 0은 아래쪽, 1은 위쪽입니다.")]
            [Range(0f, 1f)]
            [SerializeField] private float viewportY = 0.5f;

            [Tooltip("카메라 오른쪽 바깥의 최초 생성 위치에 추가할 월드 공간 가로 오프셋입니다.")]
            [SerializeField] private float horizontalOffset;

            [Tooltip("설정된 뷰포트 위치에 추가할 월드 공간 세로 오프셋입니다.")]
            [SerializeField] private float verticalOffset;

            [Tooltip("생성된 장식에 적용할 균일 스케일입니다.")]
            [Min(MinimumDecorationScale)]
            [SerializeField] private float uniformScale = 1f;

            [Tooltip("장식 아래의 모든 SpriteRenderer에 적용할 정렬 순서입니다.")]
            [SerializeField] private int sortingOrder = -1;

            [Header("Horizontal Parallax")]
            [Tooltip("켜면 Stage Travel Distance의 진행률에 맞춰 장식이 오른쪽 바깥에서 왼쪽 바깥까지 정확히 한 번 이동합니다.")]
            [SerializeField] private bool matchPassToStageDistance = true;

            [Tooltip("자동 거리 맞춤을 끈 경우에만 화면 공간의 가로 이동 속도를 정합니다. 0이면 화면에 고정되고 1이면 월드 X축 위치에 고정됩니다.")]
            [Min(0f)]
            [SerializeField] private float horizontalParallaxStrength = 0.5f;

            [Tooltip("한 프레임의 X축 이동 거리가 이 값 이상이면 순간이동으로 간주하고 이동 샘플만 초기화합니다.")]
            [Min(0f)]
            [SerializeField] private float horizontalTeleportThreshold = 50f;

            [Tooltip("스테이지가 유지되는 거리입니다. 자동 거리 맞춤이 켜져 있으면 장식의 1회 통과 속도도 함께 결정합니다. 거리가 길수록 느리고 짧을수록 빠르게 이동합니다.")]
            [Min(0f)]
            [SerializeField] private float stageTravelDistance = 200f;

            [Tooltip("장식의 최초 생성 위치와 카메라 오른쪽 가장자리 사이의 월드 공간 여백입니다.")]
            [Min(0f)]
            [SerializeField] private float spawnRightPadding = 5f;

            [Tooltip("장식을 한 번만 표시한 뒤 비활성화하기 전에 카메라 왼쪽 가장자리를 지나 이동할 거리입니다.")]
            [Min(0f)]
            [FormerlySerializedAs("recycleLeftPadding")]
            [SerializeField] private float despawnLeftPadding = 5f;

            [Header("Transitions")]
            [Tooltip("장식이 나타나고 사라질 때 적용할 페이드 시간(초)입니다. 0이면 즉시 전환됩니다.")]
            [Min(0f)]
            [SerializeField] private float transitionFadeDuration = 0.5f;

            public StagePresentation Presentation => presentation;
            public GameObject DecorationPrefab => decorationPrefab;
            public Transform HorizontalMovementSource =>
                horizontalMovementSource;
            public float ViewportY => viewportY;
            public float HorizontalOffset => horizontalOffset;
            public float VerticalOffset => verticalOffset;
            public float UniformScale => uniformScale;
            public int SortingOrder => sortingOrder;
            public bool MatchPassToStageDistance =>
                matchPassToStageDistance;
            public float HorizontalParallaxStrength =>
                horizontalParallaxStrength;
            public float HorizontalTeleportThreshold =>
                horizontalTeleportThreshold;
            public float StageTravelDistance => stageTravelDistance;
            public float SpawnRightPadding => spawnRightPadding;
            public float DespawnLeftPadding => despawnLeftPadding;
            public float TransitionFadeDuration =>
                transitionFadeDuration;

            public void Validate()
            {
                viewportY = Mathf.Clamp01(viewportY);
                uniformScale = Mathf.Max(
                    MinimumDecorationScale,
                    uniformScale);
                horizontalParallaxStrength =
                    Mathf.Max(0f, horizontalParallaxStrength);
                horizontalTeleportThreshold =
                    Mathf.Max(0f, horizontalTeleportThreshold);
                stageTravelDistance =
                    Mathf.Max(0f, stageTravelDistance);
                spawnRightPadding = Mathf.Max(0f, spawnRightPadding);
                despawnLeftPadding =
                    Mathf.Max(0f, despawnLeftPadding);
                transitionFadeDuration =
                    Mathf.Max(0f, transitionFadeDuration);
            }
        }

        private sealed class StageDecorationRuntime
        {
            public StageDecorationRuntime(
                StageDecorationSetting setting,
                GameObject instance,
                SpriteRenderer primaryRenderer)
            {
                Setting = setting;
                Instance = instance;
                PrimaryRenderer = primaryRenderer;
                Renderers =
                    instance.GetComponentsInChildren<SpriteRenderer>(true);
                BaseColors = new Color[Renderers.Length];

                for (int index = 0; index < Renderers.Length; index++)
                {
                    BaseColors[index] = Renderers[index].color;
                }
            }

            public StageDecorationSetting Setting { get; }
            public GameObject Instance { get; }
            public SpriteRenderer PrimaryRenderer { get; }
            public SpriteRenderer[] Renderers { get; }
            public Color[] BaseColors { get; }
            public Coroutine FadeRoutine { get; set; }
            public float Opacity { get; set; }
            public float PreviousCameraX { get; set; }
            public float PreviousHorizontalSourceX { get; set; }
            public float TravelledDistance { get; set; }
            public bool HasCompletedPass { get; set; }
            public bool IsCompleting { get; set; }
            public bool IsRunning { get; set; }
        }

        private const float MinimumDecorationScale = 0.0001f;
        private const float MinimumSupplementalBackgroundSpeed = 0.05f;

        [Header("Stage Routing")]
        [Tooltip("각 스테이지 번호를 해당 연출 시스템에 연결합니다.")]
        [SerializeField] private StageFormat[] stageFormats;

        [Header("Stage Decorations")]
        [Tooltip("연출별 Star Cloud, UFO 및 Aura 패럴랙스 설정입니다.")]
        [SerializeField] private StageDecorationSetting[] decorationSettings;

        [Header("Stage 1 Planets")]
        [Tooltip("행성별 프리팹, 스테이지 및 패럴랙스 설정입니다.")]
        [SerializeField] private PlanetSetting[] planetSettings;

        [Header("Common References")]
        [Tooltip("패럴랙스 이동 계산에 사용할 카메라입니다. 비어 있으면 Camera.main을 사용합니다.")]
        [SerializeField] private Camera targetCamera;

        [Header("Startup")]
        [Tooltip("플레이 모드가 시작될 때 시작할 스테이지입니다.")]
        [Min(1)]
        [SerializeField] private int initialStageNumber = 1;

        [Tooltip("플레이 모드가 시작되면 초기 스테이지의 첫 번째 행성을 즉시 표시합니다.")]
        [SerializeField] private bool spawnFirstImmediately = true;

        private readonly List<ParallaxPlanet> _planetPool =
            new List<ParallaxPlanet>();
        private readonly List<int> _sourcePrefabIndices = new List<int>();
        private readonly List<PlanetSetting> _poolSettings =
            new List<PlanetSetting>();
        private readonly List<int> _currentStagePoolIndices =
            new List<int>();
        private readonly List<StageDecorationRuntime> _decorationPool =
            new List<StageDecorationRuntime>();
        private readonly HashSet<int> _warnedMissingStages =
            new HashSet<int>();

        private ParallaxPlanet _activePlanet;
        private PlanetSetting _currentPlanetSetting;
        private StageDecorationRuntime _activeDecoration;
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
        private float _ufoAuraBackgroundMoveSpeed = 1f;

        public int CurrentStageNumber { get; private set; }

        public bool IsUfoAuraThemeActive =>
            _stageRunning
            && _currentPresentation == StagePresentation.UfoAura;

        public bool TryGetUfoAuraThemeProgress(out float progress)
        {
            if (!IsUfoAuraThemeActive
                || _activeDecoration == null
                || !_activeDecoration.IsRunning)
            {
                progress = 0f;
                return false;
            }

            progress = GetUfoAuraBackgroundProgress(_activeDecoration);
            return true;
        }

        public void SetUfoAuraBackgroundMoveSpeed(float moveSpeed)
        {
            _ufoAuraBackgroundMoveSpeed = Mathf.Max(
                MinimumSupplementalBackgroundSpeed,
                moveSpeed);
        }

        public event Action<int, GameObject> PlanetChanged;
        public event Action<int> StageCompleted;

        private void OnEnable()
        {
            _hasCameraSample = false;
        }

        private void Start()
        {
            _poolReady = BuildPool();
            BuildDecorationPool();

            if (TryResolveCamera())
            {
                BeginCameraTracking();
            }

            StartStageInternal(initialStageNumber, spawnFirstImmediately);
        }

        private void OnDisable()
        {
            StopActivePresentation();
        }

        private void LateUpdate()
        {
            if (!_stageRunning)
            {
                return;
            }

            if (_currentPresentation != StagePresentation.PlanetSequence)
            {
                UpdateActiveDecoration();
                return;
            }

            UpdatePlanetSequence();
        }

        private void UpdatePlanetSequence()
        {
            if (!_poolReady || !TryResolveCamera())
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
                case StagePresentation.UfoAura:
                    BeginDecoration(
                        _currentPresentation,
                        stageNumber);
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
                        presentation,
                        "Star Cloud");

                case StagePresentation.UfoAura:
                    return ValidateDecoration(
                        stageNumber,
                        presentation,
                        "UFO and Aura");

                default:
                    return false;
            }
        }

        private bool ValidateDecoration(
            int stageNumber,
            StagePresentation presentation,
            string label)
        {
            if (FindDecorationRuntime(presentation) != null)
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
            StagePresentation presentation,
            int stageNumber)
        {
            StageDecorationRuntime decoration =
                FindDecorationRuntime(presentation);

            if (decoration == null || !TryResolveCamera())
            {
                _stageRunning = false;
                WarnMissingStageOnce(
                    stageNumber,
                    "The assigned decoration could not start.");
                return;
            }

            decoration.Instance.SetActive(true);
            ApplyDecorationVisualSettings(decoration);
            AlignDecorationOutsideRightEdge(decoration);

            ResetDecorationHorizontalSamples(decoration);
            decoration.TravelledDistance = 0f;
            decoration.HasCompletedPass = false;
            decoration.IsCompleting = false;
            decoration.IsRunning = true;
            _activeDecoration = decoration;
            SetDecorationOpacity(decoration, 0f);
            StartDecorationFade(decoration, 1f, false);
        }

        private void StopActivePresentation()
        {
            if (_activePlanet != null)
            {
                _activePlanet.ReturnToPool();
                _activePlanet = null;
            }

            for (int index = 0;
                 index < _decorationPool.Count;
                 index++)
            {
                StopDecoration(_decorationPool[index]);
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

        private void BuildDecorationPool()
        {
            _decorationPool.Clear();

            if (decorationSettings == null)
            {
                return;
            }

            for (int index = 0;
                 index < decorationSettings.Length;
                 index++)
            {
                StageDecorationSetting setting =
                    decorationSettings[index];

                if (setting == null)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"an empty Decoration Setting at array index {index}.",
                        this);
                    continue;
                }

                if (setting.Presentation
                    == StagePresentation.PlanetSequence)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"Decoration Setting {index} because Planet Sequence "
                        + "does not use a stage decoration.",
                        this);
                    continue;
                }

                if (FindDecorationRuntime(setting.Presentation) != null)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"duplicate {setting.Presentation} Decoration Setting "
                        + $"at array index {index}.",
                        this);
                    continue;
                }

                GameObject prefab = setting.DecorationPrefab;
                if (prefab == null)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"{setting.Presentation} because it has no Prefab.",
                        this);
                    continue;
                }

                if (prefab.GetComponentInChildren<SpriteRenderer>(true)
                    == null)
                {
                    Debug.LogWarning(
                        $"{nameof(PlanetParallaxController)} on '{name}' skipped "
                        + $"'{prefab.name}' because it has no SpriteRenderer.",
                        this);
                    continue;
                }

                GameObject instance = Instantiate(prefab, transform);
                instance.name =
                    $"{prefab.name}_{setting.Presentation}_Pooled";
                SpriteRenderer primaryRenderer =
                    instance.GetComponentInChildren<SpriteRenderer>(true);

                DisablePhysics(instance);
                instance.SetActive(false);
                _decorationPool.Add(
                    new StageDecorationRuntime(
                        setting,
                        instance,
                        primaryRenderer));
            }
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

        private StageDecorationRuntime FindDecorationRuntime(
            StagePresentation presentation)
        {
            for (int index = 0;
                 index < _decorationPool.Count;
                 index++)
            {
                StageDecorationRuntime decoration =
                    _decorationPool[index];

                if (decoration.Setting.Presentation == presentation)
                {
                    return decoration;
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

        private void UpdateActiveDecoration()
        {
            if (_activeDecoration == null
                || !_activeDecoration.IsRunning)
            {
                return;
            }

            if (!TryResolveCamera())
            {
                StopDecoration(_activeDecoration);
                _stageRunning = false;
                return;
            }

            StageDecorationRuntime decoration = _activeDecoration;
            StageDecorationSetting setting = decoration.Setting;
            float currentCameraX = targetCamera.transform.position.x;
            float cameraDeltaX =
                currentCameraX - decoration.PreviousCameraX;
            decoration.PreviousCameraX = currentCameraX;

            float currentHorizontalSourceX =
                GetDecorationHorizontalSourceX(decoration);
            float horizontalSourceDeltaX =
                currentHorizontalSourceX
                - decoration.PreviousHorizontalSourceX;
            decoration.PreviousHorizontalSourceX =
                currentHorizontalSourceX;

            if (setting.HorizontalTeleportThreshold > 0f
                && (Mathf.Abs(cameraDeltaX)
                        >= setting.HorizontalTeleportThreshold
                    || Mathf.Abs(horizontalSourceDeltaX)
                        >= setting.HorizontalTeleportThreshold))
            {
                return;
            }

            decoration.TravelledDistance +=
                Mathf.Max(0f, horizontalSourceDeltaX);

            if (setting.MatchPassToStageDistance)
            {
                float stageProgress =
                    GetDecorationStageProgress(decoration);
                PositionDecorationForStageProgress(
                    decoration,
                    stageProgress);

                if (stageProgress < 1f)
                {
                    return;
                }

                bool waitForSupplementalBackground =
                    _currentPresentation == StagePresentation.UfoAura
                    && GetUfoAuraBackgroundProgress(decoration) < 1f;

                if (waitForSupplementalBackground)
                {
                    if (!decoration.HasCompletedPass)
                    {
                        decoration.HasCompletedPass = true;
                        StartDecorationFade(decoration, 0f, false);
                    }

                    return;
                }

                if (!decoration.IsCompleting)
                {
                    decoration.HasCompletedPass = true;
                    decoration.IsCompleting = true;
                    StartDecorationFade(decoration, 0f, true);
                }

                return;
            }

            if (!decoration.HasCompletedPass)
            {
                ApplyDecorationHorizontalParallax(
                    decoration,
                    cameraDeltaX,
                    horizontalSourceDeltaX);
                MaintainDecorationViewportY(decoration);
                HideDecorationIfCompletelyLeft(decoration);
            }

            if (decoration.TravelledDistance
                >= setting.StageTravelDistance
                && !decoration.IsCompleting)
            {
                decoration.IsCompleting = true;

                if (decoration.HasCompletedPass)
                {
                    CompleteCurrentStage();
                }
                else
                {
                    StartDecorationFade(decoration, 0f, true);
                }
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
            if (_activeDecoration != null)
            {
                StopDecoration(_activeDecoration);
            }

            _activeDecoration = null;
            _currentPlanetSetting = null;
            _isFollowingCamera = false;
            _spawnFirstWhenCameraReady = false;

            StageCompleted?.Invoke(CurrentStageNumber);
        }

        private void ApplyDecorationVisualSettings(
            StageDecorationRuntime decoration)
        {
            StageDecorationSetting setting = decoration.Setting;
            decoration.Instance.transform.localScale =
                Vector3.one * setting.UniformScale;

            for (int index = 0;
                 index < decoration.Renderers.Length;
                 index++)
            {
                decoration.Renderers[index].sortingOrder =
                    setting.SortingOrder;
            }
        }

        private void StartDecorationFade(
            StageDecorationRuntime decoration,
            float targetOpacity,
            bool completeStageAfterFade)
        {
            if (decoration.FadeRoutine != null)
            {
                StopCoroutine(decoration.FadeRoutine);
                decoration.FadeRoutine = null;
            }

            float duration = decoration.Setting.TransitionFadeDuration;
            if (duration <= 0f)
            {
                SetDecorationOpacity(decoration, targetOpacity);

                if (completeStageAfterFade)
                {
                    StopDecoration(decoration);
                    CompleteCurrentStage();
                }

                return;
            }

            decoration.FadeRoutine = StartCoroutine(
                FadeDecorationOpacity(
                    decoration,
                    targetOpacity,
                    duration,
                    completeStageAfterFade));
        }

        private IEnumerator FadeDecorationOpacity(
            StageDecorationRuntime decoration,
            float targetOpacity,
            float duration,
            bool completeStageAfterFade)
        {
            float startOpacity = decoration.Opacity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress =
                    Mathf.SmoothStep(0f, 1f, progress);
                SetDecorationOpacity(
                    decoration,
                    Mathf.Lerp(
                        startOpacity,
                        targetOpacity,
                        easedProgress));
                yield return null;
            }

            SetDecorationOpacity(decoration, targetOpacity);
            decoration.FadeRoutine = null;

            if (!completeStageAfterFade)
            {
                yield break;
            }

            StopDecoration(decoration);
            CompleteCurrentStage();
        }

        private static void SetDecorationOpacity(
            StageDecorationRuntime decoration,
            float opacity)
        {
            decoration.Opacity = Mathf.Clamp01(opacity);

            for (int index = 0;
                 index < decoration.Renderers.Length;
                 index++)
            {
                SpriteRenderer renderer = decoration.Renderers[index];
                Color color = decoration.BaseColors[index];
                color.a *= decoration.Opacity;
                renderer.color = color;
            }
        }

        private void AlignDecorationOutsideRightEdge(
            StageDecorationRuntime decoration)
        {
            PositionDecorationForStageProgress(decoration, 0f);
        }

        private float GetDecorationStageProgress(
            StageDecorationRuntime decoration)
        {
            float stageTravelDistance =
                decoration.Setting.StageTravelDistance;

            if (stageTravelDistance <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(
                decoration.TravelledDistance / stageTravelDistance);
        }

        private float GetUfoAuraBackgroundProgress(
            StageDecorationRuntime decoration)
        {
            float stageTravelDistance =
                decoration.Setting.StageTravelDistance;

            if (stageTravelDistance <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(
                decoration.TravelledDistance
                / stageTravelDistance
                * _ufoAuraBackgroundMoveSpeed);
        }

        private void PositionDecorationForStageProgress(
            StageDecorationRuntime decoration,
            float stageProgress)
        {
            StageDecorationSetting setting = decoration.Setting;
            float depth = GetDecorationCameraDepth(decoration);
            Vector3 cameraLeftCenter = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, setting.ViewportY, depth));
            Vector3 cameraRightCenter = targetCamera.ViewportToWorldPoint(
                new Vector3(1f, setting.ViewportY, depth));
            Bounds rendererBounds = decoration.PrimaryRenderer.bounds;
            float halfWidth = rendererBounds.extents.x;
            float startCenterX =
                cameraRightCenter.x
                + setting.SpawnRightPadding
                + setting.HorizontalOffset
                + halfWidth;
            float endCenterX =
                cameraLeftCenter.x
                - setting.DespawnLeftPadding
                - halfWidth;
            float targetCenterX = Mathf.Lerp(
                startCenterX,
                endCenterX,
                Mathf.Clamp01(stageProgress));

            Vector3 position = decoration.Instance.transform.position;
            position.x +=
                targetCenterX - rendererBounds.center.x;
            position.y +=
                cameraRightCenter.y
                + setting.VerticalOffset
                - rendererBounds.center.y;
            decoration.Instance.transform.position = position;
        }

        private void MaintainDecorationViewportY(
            StageDecorationRuntime decoration)
        {
            StageDecorationSetting setting = decoration.Setting;
            float depth = GetDecorationCameraDepth(decoration);
            float targetCenterY = targetCamera.ViewportToWorldPoint(
                new Vector3(0.5f, setting.ViewportY, depth)).y
                + setting.VerticalOffset;

            Vector3 position = decoration.Instance.transform.position;
            position.y +=
                targetCenterY - decoration.PrimaryRenderer.bounds.center.y;
            decoration.Instance.transform.position = position;
        }

        private void ApplyDecorationHorizontalParallax(
            StageDecorationRuntime decoration,
            float cameraDeltaX,
            float horizontalSourceDeltaX)
        {
            Vector3 position = decoration.Instance.transform.position;
            position.x += cameraDeltaX
                - horizontalSourceDeltaX
                * decoration.Setting.HorizontalParallaxStrength;
            decoration.Instance.transform.position = position;
        }

        private void HideDecorationIfCompletelyLeft(
            StageDecorationRuntime decoration)
        {
            StageDecorationSetting setting = decoration.Setting;
            float depth = GetDecorationCameraDepth(decoration);
            float cameraLeftEdge = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, setting.ViewportY, depth)).x;

            if (decoration.PrimaryRenderer.bounds.max.x
                >= cameraLeftEdge - setting.DespawnLeftPadding)
            {
                return;
            }

            if (decoration.FadeRoutine != null)
            {
                StopCoroutine(decoration.FadeRoutine);
                decoration.FadeRoutine = null;
            }

            decoration.HasCompletedPass = true;
            SetDecorationOpacity(decoration, 0f);
            decoration.Instance.SetActive(false);
        }

        private void ResetDecorationHorizontalSamples(
            StageDecorationRuntime decoration)
        {
            decoration.PreviousCameraX =
                targetCamera.transform.position.x;
            decoration.PreviousHorizontalSourceX =
                GetDecorationHorizontalSourceX(decoration);
        }

        private float GetDecorationHorizontalSourceX(
            StageDecorationRuntime decoration)
        {
            Transform movementSource =
                decoration.Setting.HorizontalMovementSource;
            return movementSource != null
                ? movementSource.position.x
                : targetCamera.transform.position.x;
        }

        private float GetDecorationCameraDepth(
            StageDecorationRuntime decoration)
        {
            float depth = Vector3.Dot(
                decoration.Instance.transform.position
                - targetCamera.transform.position,
                targetCamera.transform.forward);

            if (depth > 0f)
            {
                return depth;
            }

            return Mathf.Abs(
                decoration.Instance.transform.position.z
                - targetCamera.transform.position.z);
        }

        private void StopDecoration(
            StageDecorationRuntime decoration)
        {
            if (decoration.FadeRoutine != null)
            {
                StopCoroutine(decoration.FadeRoutine);
                decoration.FadeRoutine = null;
            }

            decoration.IsRunning = false;
            decoration.IsCompleting = false;
            decoration.HasCompletedPass = false;
            decoration.TravelledDistance = 0f;
            SetDecorationOpacity(decoration, 0f);

            if (decoration.Instance != null)
            {
                decoration.Instance.SetActive(false);
            }
        }

        private static void DisablePhysics(GameObject root)
        {
            Collider2D[] colliders2D =
                root.GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders2D.Length; index++)
            {
                colliders2D[index].enabled = false;
            }

            Rigidbody2D[] rigidbodies2D =
                root.GetComponentsInChildren<Rigidbody2D>(true);
            for (int index = 0; index < rigidbodies2D.Length; index++)
            {
                rigidbodies2D[index].simulated = false;
            }

            Collider[] colliders3D =
                root.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders3D.Length; index++)
            {
                colliders3D[index].enabled = false;
            }

            Rigidbody[] rigidbodies3D =
                root.GetComponentsInChildren<Rigidbody>(true);
            for (int index = 0; index < rigidbodies3D.Length; index++)
            {
                rigidbodies3D[index].isKinematic = true;
                rigidbodies3D[index].detectCollisions = false;
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

            if (decorationSettings != null)
            {
                foreach (StageDecorationSetting setting
                         in decorationSettings)
                {
                    setting?.Validate();
                }
            }
        }
    }
}
