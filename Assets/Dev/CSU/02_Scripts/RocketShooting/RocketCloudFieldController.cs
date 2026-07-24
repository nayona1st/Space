using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    [DisallowMultipleComponent]
    public sealed class RocketCloudFieldController : MonoBehaviour
    {
        private const int MinimumCloudCount = 2;
        private const int MaximumCloudCount = 128;
        private const float MinimumScale = 0.05f;

        [Header("References")]
        [Tooltip("Launch sequence that supplies the phase and vertical scroll speed.")]
        [SerializeField] private RocketShootingDirector launchDirector;

        [Tooltip("Background scroller that reports when the Ground has completely exited.")]
        [SerializeField] private VerticalBackgroundScroller backgroundScroller;

        [Tooltip("Camera whose viewport defines cloud spawn and recycle bounds.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("First cloud visual used by the fixed-size pool.")]
        [SerializeField] private GameObject cloud0Prefab;

        [Tooltip("Second cloud visual used by the fixed-size pool.")]
        [SerializeField] private GameObject cloud1Prefab;

        [Tooltip("Parent that owns every runtime cloud instance.")]
        [SerializeField] private Transform cloudInstancesRoot;

        [Header("Pool")]
        [Tooltip("Fixed number of cloud instances created during initialization.")]
        [Range(MinimumCloudCount, MaximumCloudCount)]
        [SerializeField] private int cloudCount = 14;

        [Tooltip("Minimum clear vertical distance between consecutive clouds.")]
        [Min(0f)]
        [SerializeField] private float minimumVerticalSpacing = 2f;

        [Tooltip("Maximum clear vertical distance between consecutive clouds.")]
        [Min(0f)]
        [SerializeField] private float maximumVerticalSpacing = 5.5f;

        [Header("Viewport Placement")]
        [Tooltip("World-space distance kept between clouds and each horizontal screen edge.")]
        [Min(0f)]
        [SerializeField] private float horizontalMargin = 1.5f;

        [Tooltip("World-space distance above the camera where recycled clouds begin.")]
        [Min(0f)]
        [SerializeField] private float spawnMargin = 3f;

        [Tooltip("World-space distance below the camera before a cloud is recycled.")]
        [Min(0f)]
        [SerializeField] private float recycleMargin = 4f;

        [Header("Variation")]
        [SerializeField] private Vector2 scaleRange =
            new Vector2(0.8f, 1.35f);

        [SerializeField] private Vector2 speedMultiplierRange =
            new Vector2(0.72f, 1.08f);

        [SerializeField] private Vector2 alphaRange =
            new Vector2(0.72f, 1f);

        [SerializeField] private bool allowHorizontalFlip = true;

        [Tooltip("When enabled, a different independent random sequence is used each run.")]
        [SerializeField] private bool randomizeSeed = true;

        [SerializeField] private int randomSeed = 20260725;

        [Header("Rendering")]
        [Tooltip("Cloud order between the background (0) and rocket (2).")]
        [SerializeField] private int sortingOrder = 1;

        [Header("Safety")]
        [Tooltip("Maximum number of cloud recycle operations allowed in one frame.")]
        [Range(1, MaximumCloudCount)]
        [SerializeField] private int maxRecyclesPerFrame = 32;

        private readonly List<CloudRuntime> _clouds =
            new List<CloudRuntime>();
        private readonly List<CloudRuntime> _verticalOrder =
            new List<CloudRuntime>();

        private System.Random _random;
        private bool _isInitialized;
        private bool _cloudsActivated;
        private bool _warnedInvalidConfiguration;
        private bool _warnedRecycleLimit;

        public int PoolCount => _clouds.Count;
        public int ConfiguredCloudCount => cloudCount;
        public int RecycledCloudCount { get; private set; }

        public int ActiveCloudCount
        {
            get
            {
                int activeCount = 0;
                for (int index = 0; index < _clouds.Count; index++)
                {
                    if (_clouds[index].Root.gameObject.activeSelf)
                    {
                        activeCount++;
                    }
                }

                return activeCount;
            }
        }

        public bool IsInitialized => _isInitialized;
        public bool AreCloudsActivated => _cloudsActivated;
        public bool HasGroundExited =>
            backgroundScroller != null
            && backgroundScroller.HasGroundExited;

        public bool IsConfigured =>
            launchDirector != null
            && backgroundScroller != null
            && targetCamera != null
            && cloud0Prefab != null
            && cloud1Prefab != null
            && cloudInstancesRoot != null
            && TryGetPrefabRenderer(cloud0Prefab, out _)
            && TryGetPrefabRenderer(cloud1Prefab, out _);

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

            if (!_cloudsActivated)
            {
                if (!backgroundScroller.HasGroundExited)
                {
                    return;
                }

                ActivateCloudsAboveViewport();
                return;
            }

            ClampCloudsToHorizontalViewport();

            if (!ShouldMoveClouds())
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, Time.deltaTime);
            float baseDistance =
                launchDirector.CurrentScrollSpeed * deltaTime;
            if (baseDistance <= 0f)
            {
                return;
            }

            MoveClouds(baseDistance);
            EnforceMinimumVerticalSpacing();
            RecycleClouds();
        }

        public void ConfigureReferences(
            RocketShootingDirector director,
            VerticalBackgroundScroller scroller,
            Camera camera,
            GameObject firstCloudPrefab,
            GameObject secondCloudPrefab,
            Transform instancesRoot)
        {
            launchDirector = director;
            backgroundScroller = scroller;
            targetCamera = camera;
            cloud0Prefab = firstCloudPrefab;
            cloud1Prefab = secondCloudPrefab;
            cloudInstancesRoot = instancesRoot;
        }

        public void RepositionAllClouds()
        {
            if (!_isInitialized)
            {
                return;
            }

            SetCloudPoolActive(false);
            _cloudsActivated = false;

            if (backgroundScroller.HasGroundExited)
            {
                ActivateCloudsAboveViewport();
            }
        }

        private bool TryInitialize()
        {
            if (!IsConfigured)
            {
                WarnInvalidConfigurationOnce(
                    "assign Launch Director, Background Scroller, Target "
                    + "Camera, both cloud prefabs, and Cloud Instances Root. "
                    + "Each prefab must contain an enabled SpriteRenderer "
                    + "with a Sprite.");
                return false;
            }

            _random = new System.Random(GetEffectiveSeed());
            RecycledCloudCount = 0;
            if (!BuildPool())
            {
                return false;
            }

            SetCloudPoolActive(false);
            _cloudsActivated = false;
            _warnedInvalidConfiguration = false;
            return true;
        }

        private bool BuildPool()
        {
            _clouds.Clear();

            for (int index = 0; index < cloudCount; index++)
            {
                GameObject prefab = index % 2 == 0
                    ? cloud0Prefab
                    : cloud1Prefab;
                GameObject instance = Instantiate(
                    prefab,
                    cloudInstancesRoot,
                    false);
                instance.name = $"{prefab.name}_Pooled_{index:00}";

                if (!TryGetUsableRenderer(
                        instance,
                        out SpriteRenderer renderer))
                {
                    WarnInvalidConfigurationOnce(
                        $"the pooled instance for '{prefab.name}' does not "
                        + "contain an enabled SpriteRenderer with a Sprite.");
                    return false;
                }

                renderer.sortingOrder = sortingOrder;
                _clouds.Add(new CloudRuntime(
                    instance.transform,
                    renderer,
                    instance.transform.localScale));
                instance.SetActive(false);
            }

            return _clouds.Count == cloudCount;
        }

        private void ActivateCloudsAboveViewport()
        {
            if (_cloudsActivated || _clouds.Count == 0)
            {
                return;
            }

            float nextBottom = 0f;

            for (int index = 0; index < _clouds.Count; index++)
            {
                CloudRuntime cloud = _clouds[index];
                cloud.Root.gameObject.SetActive(true);
                RandomizePresentation(cloud);

                if (index == 0)
                {
                    nextBottom =
                        GetCameraTop(cloud.Renderer) + spawnMargin;
                }

                PlaceAtBottom(cloud, nextBottom);

                nextBottom = cloud.Renderer.bounds.max.y
                    + NextFloat(
                        minimumVerticalSpacing,
                        maximumVerticalSpacing);
            }

            _cloudsActivated = true;
        }

        private void SetCloudPoolActive(bool isActive)
        {
            for (int index = 0; index < _clouds.Count; index++)
            {
                GameObject cloudObject =
                    _clouds[index].Root.gameObject;
                if (cloudObject.activeSelf != isActive)
                {
                    cloudObject.SetActive(isActive);
                }
            }
        }

        private bool ShouldMoveClouds()
        {
            return launchDirector.Phase == LaunchPhase.LiftOff
                || launchDirector.Phase == LaunchPhase.Cruise;
        }

        private void MoveClouds(float baseDistance)
        {
            for (int index = 0; index < _clouds.Count; index++)
            {
                CloudRuntime cloud = _clouds[index];
                Vector3 position = cloud.Root.position;
                position.y -= baseDistance * cloud.SpeedMultiplier;
                cloud.Root.position = position;
            }
        }

        private void EnforceMinimumVerticalSpacing()
        {
            if (minimumVerticalSpacing <= 0f || _clouds.Count < 2)
            {
                return;
            }

            _verticalOrder.Clear();
            _verticalOrder.AddRange(_clouds);
            _verticalOrder.Sort(CompareCloudBottoms);

            for (int index = 1;
                 index < _verticalOrder.Count;
                 index++)
            {
                CloudRuntime lowerCloud = _verticalOrder[index - 1];
                CloudRuntime upperCloud = _verticalOrder[index];
                float requiredBottom =
                    lowerCloud.Renderer.bounds.max.y
                    + minimumVerticalSpacing;
                float correction =
                    requiredBottom - upperCloud.Renderer.bounds.min.y;
                if (correction <= 0f)
                {
                    continue;
                }

                Vector3 position = upperCloud.Root.position;
                position.y += correction;
                upperCloud.Root.position = position;
            }
        }

        private void RecycleClouds()
        {
            int recycledCount = 0;
            float recycleThreshold = GetCameraBottom()
                - recycleMargin;

            for (int index = 0; index < _clouds.Count; index++)
            {
                CloudRuntime cloud = _clouds[index];
                if (cloud.Renderer.bounds.max.y >= recycleThreshold)
                {
                    continue;
                }

                if (recycledCount >= maxRecyclesPerFrame)
                {
                    WarnRecycleLimitOnce();
                    return;
                }

                RecycleCloud(cloud);
                recycledCount++;
            }

            _warnedRecycleLimit = false;
        }

        private void RecycleCloud(CloudRuntime cloud)
        {
            float highestTop = GetHighestCloudTop(cloud);
            float cameraSpawnTop =
                GetCameraTop(cloud.Renderer) + spawnMargin;
            float desiredBottom =
                Mathf.Max(highestTop, cameraSpawnTop)
                + NextFloat(
                    minimumVerticalSpacing,
                    maximumVerticalSpacing);

            RandomizePresentation(cloud);
            PlaceAtBottom(cloud, desiredBottom);
            RecycledCloudCount++;
        }

        private float GetHighestCloudTop(CloudRuntime excludedCloud)
        {
            float highestTop = float.NegativeInfinity;
            for (int index = 0; index < _clouds.Count; index++)
            {
                CloudRuntime candidate = _clouds[index];
                if (candidate == excludedCloud)
                {
                    continue;
                }

                highestTop = Mathf.Max(
                    highestTop,
                    candidate.Renderer.bounds.max.y);
            }

            return float.IsNegativeInfinity(highestTop)
                ? GetCameraTop(excludedCloud.Renderer) + spawnMargin
                : highestTop;
        }

        private void RandomizePresentation(CloudRuntime cloud)
        {
            float scale = NextFloat(scaleRange.x, scaleRange.y);
            cloud.Root.localScale = cloud.InitialLocalScale * scale;
            cloud.SpeedMultiplier = NextFloat(
                speedMultiplierRange.x,
                speedMultiplierRange.y);
            cloud.Renderer.flipX =
                allowHorizontalFlip && _random.Next(0, 2) == 1;

            Color color = cloud.Renderer.color;
            color.a = NextFloat(alphaRange.x, alphaRange.y);
            cloud.Renderer.color = color;
            cloud.Renderer.sortingOrder = sortingOrder;
        }

        private void PlaceAtBottom(
            CloudRuntime cloud,
            float desiredBottom)
        {
            ViewportBounds viewport =
                GetViewportBounds(cloud.Renderer);
            GetHorizontalCenterRange(
                cloud.Renderer,
                viewport,
                out float minimumCenterX,
                out float maximumCenterX);
            float targetCenterX = minimumCenterX <= maximumCenterX
                ? NextFloat(minimumCenterX, maximumCenterX)
                : (viewport.Left + viewport.Right) * 0.5f;

            Vector3 position = cloud.Root.position;
            position.x += targetCenterX
                - cloud.Renderer.bounds.center.x;
            position.y += desiredBottom
                - cloud.Renderer.bounds.min.y;
            cloud.Root.position = position;
        }

        private void ClampCloudsToHorizontalViewport()
        {
            for (int index = 0; index < _clouds.Count; index++)
            {
                CloudRuntime cloud = _clouds[index];
                ViewportBounds viewport =
                    GetViewportBounds(cloud.Renderer);
                GetHorizontalCenterRange(
                    cloud.Renderer,
                    viewport,
                    out float minimumCenterX,
                    out float maximumCenterX);

                float targetCenterX = minimumCenterX <= maximumCenterX
                    ? Mathf.Clamp(
                        cloud.Renderer.bounds.center.x,
                        minimumCenterX,
                        maximumCenterX)
                    : (viewport.Left + viewport.Right) * 0.5f;
                float correction =
                    targetCenterX - cloud.Renderer.bounds.center.x;
                if (Mathf.Approximately(correction, 0f))
                {
                    continue;
                }

                Vector3 position = cloud.Root.position;
                position.x += correction;
                cloud.Root.position = position;
            }
        }

        private void GetHorizontalCenterRange(
            SpriteRenderer renderer,
            ViewportBounds viewport,
            out float minimumCenterX,
            out float maximumCenterX)
        {
            float halfWidth = renderer.bounds.extents.x;
            minimumCenterX =
                viewport.Left + horizontalMargin + halfWidth;
            maximumCenterX =
                viewport.Right - horizontalMargin - halfWidth;
        }

        private float GetCameraBottom()
        {
            return GetViewportBounds(_clouds[0].Renderer).Bottom;
        }

        private float GetCameraTop(SpriteRenderer renderer)
        {
            return GetViewportBounds(renderer).Top;
        }

        private ViewportBounds GetViewportBounds(
            SpriteRenderer renderer)
        {
            Vector3 rendererCenter = renderer.bounds.center;
            float cameraDepth = Mathf.Abs(Vector3.Dot(
                rendererCenter - targetCamera.transform.position,
                targetCamera.transform.forward));
            cameraDepth = Mathf.Max(
                cameraDepth,
                targetCamera.nearClipPlane);

            Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, cameraDepth));
            Vector3 topRight = targetCamera.ViewportToWorldPoint(
                new Vector3(1f, 1f, cameraDepth));

            return new ViewportBounds(
                Mathf.Min(bottomLeft.x, topRight.x),
                Mathf.Max(bottomLeft.x, topRight.x),
                Mathf.Min(bottomLeft.y, topRight.y),
                Mathf.Max(bottomLeft.y, topRight.y));
        }

        private float NextFloat(float minimum, float maximum)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            return minimum
                + (float)_random.NextDouble() * (maximum - minimum);
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

        private static bool TryGetPrefabRenderer(
            GameObject prefab,
            out SpriteRenderer renderer)
        {
            return TryGetUsableRenderer(prefab, out renderer);
        }

        private static bool TryGetUsableRenderer(
            GameObject root,
            out SpriteRenderer renderer)
        {
            renderer = null;
            if (root == null)
            {
                return false;
            }

            SpriteRenderer[] renderers =
                root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer candidate = renderers[index];
                if (candidate == null
                    || !candidate.enabled
                    || candidate.sprite == null)
                {
                    continue;
                }

                renderer = candidate;
                return true;
            }

            return false;
        }

        private static int CompareCloudBottoms(
            CloudRuntime first,
            CloudRuntime second)
        {
            return first.Renderer.bounds.min.y.CompareTo(
                second.Renderer.bounds.min.y);
        }

        private void WarnInvalidConfigurationOnce(string reason)
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(RocketCloudFieldController)} on '{name}' is "
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
                $"{nameof(RocketCloudFieldController)} on '{name}' reached "
                + "its per-frame recycle safety limit after an unusually "
                + "large frame step.",
                this);
            _warnedRecycleLimit = true;
        }

        private void OnValidate()
        {
            cloudCount = Mathf.Clamp(
                cloudCount,
                MinimumCloudCount,
                MaximumCloudCount);
            minimumVerticalSpacing =
                Mathf.Max(0f, minimumVerticalSpacing);
            maximumVerticalSpacing = Mathf.Max(
                minimumVerticalSpacing,
                maximumVerticalSpacing);
            horizontalMargin = Mathf.Max(0f, horizontalMargin);
            spawnMargin = Mathf.Max(0f, spawnMargin);
            recycleMargin = Mathf.Max(0f, recycleMargin);
            ClampRange(ref scaleRange, MinimumScale, float.MaxValue);
            ClampRange(
                ref speedMultiplierRange,
                0f,
                float.MaxValue);
            ClampRange(ref alphaRange, 0f, 1f);
            maxRecyclesPerFrame = Mathf.Clamp(
                maxRecyclesPerFrame,
                1,
                MaximumCloudCount);
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

        private sealed class CloudRuntime
        {
            public CloudRuntime(
                Transform root,
                SpriteRenderer renderer,
                Vector3 initialLocalScale)
            {
                Root = root;
                Renderer = renderer;
                InitialLocalScale = initialLocalScale;
                SpeedMultiplier = 1f;
            }

            public Transform Root { get; }
            public SpriteRenderer Renderer { get; }
            public Vector3 InitialLocalScale { get; }
            public float SpeedMultiplier { get; set; }
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
