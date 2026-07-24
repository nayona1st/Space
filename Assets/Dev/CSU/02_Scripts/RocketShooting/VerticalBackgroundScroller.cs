using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    [DisallowMultipleComponent]
    public sealed class VerticalBackgroundScroller : MonoBehaviour
    {
        private const int RequiredSkyTileCount = 2;
        private const float CoverageEpsilon = 0.0001f;

        [Header("World References")]
        [Tooltip("Ground artwork that exits once and is never recycled.")]
        [SerializeField] private Transform ground;

        [Tooltip("Exactly two sky tiles that recycle vertically.")]
        [SerializeField] private Transform[] skyTiles;

        [Tooltip("Camera used to determine the lower recycle edge.")]
        [SerializeField] private Camera targetCamera;

        [Header("Recycling")]
        [Tooltip("Extra distance below the camera before an item is recycled or disabled.")]
        [Min(0f)]
        [SerializeField] private float recycleMargin;

        [Tooltip("Small world-space overlap between sky tiles to prevent visible seams.")]
        [Min(0f)]
        [SerializeField] private float seamOverlap = 0.01f;

        [Tooltip("Maximum number of sky reposition operations allowed in one frame.")]
        [Min(1)]
        [SerializeField] private int maxRecyclesPerFrame = 32;

        private readonly Transform[] _orderedTiles =
            new Transform[RequiredSkyTileCount];
        private readonly SpriteRenderer[] _orderedRenderers =
            new SpriteRenderer[RequiredSkyTileCount];

        private SpriteRenderer _groundRenderer;
        private float _scrollSpeed;
        private bool _isInitialized;
        private bool _warnedInvalidConfiguration;
        private bool _warnedMissingCamera;
        private bool _warnedRecycleLimit;

        public float ScrollSpeed => _scrollSpeed;
        public int PassedBackgroundCount { get; private set; }
        public bool HasGroundExited { get; private set; }

        public event Action<int> BackgroundPassed;

        public bool IsConfigured
        {
            get
            {
                if (!HasUniqueRootReferences())
                {
                    return false;
                }

                return TryGetRenderer(
                        ground,
                        false,
                        out _)
                    && TryGetUsableRenderer(skyTiles[0], out _)
                    && TryGetUsableRenderer(skyTiles[1], out _);
            }
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = TryInitialize();

            if (!_isInitialized)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_isInitialized || _scrollSpeed <= 0f)
            {
                return;
            }

            float distance = _scrollSpeed * Mathf.Max(0f, Time.deltaTime);
            if (distance <= 0f)
            {
                return;
            }

            Vector3 movement = Vector3.down * distance;

            if (ground.gameObject.activeSelf)
            {
                ground.position += movement;
            }

            for (int index = 0; index < RequiredSkyTileCount; index++)
            {
                _orderedTiles[index].position += movement;
            }
        }

        private void LateUpdate()
        {
            if (!_isInitialized || _scrollSpeed <= 0f)
            {
                return;
            }

            if (!TryResolveCamera())
            {
                return;
            }

            float cameraBottom = GetCameraBottomEdgeY();
            float recycleThreshold = cameraBottom - recycleMargin;

            DisableGroundIfBelow(recycleThreshold);
            RecycleSkyTiles(recycleThreshold);
        }

        public void SetScrollSpeed(float worldUnitsPerSecond)
        {
            _scrollSpeed = Mathf.Max(0f, worldUnitsPerSecond);
        }

        public void ResetPassedBackgroundCount()
        {
            PassedBackgroundCount = 0;
        }

        private bool TryInitialize()
        {
            if (!HasUniqueRootReferences())
            {
                WarnInvalidConfigurationOnce(
                    "assign one Ground and exactly two unique Sky Tiles.");
                return false;
            }

            if (!TryGetRenderer(
                    ground,
                    false,
                    out _groundRenderer))
            {
                WarnInvalidConfigurationOnce(
                    "Ground must contain an enabled SpriteRenderer "
                    + "with a Sprite.");
                return false;
            }

            HasGroundExited = !ground.gameObject.activeSelf;

            for (int index = 0; index < RequiredSkyTileCount; index++)
            {
                if (!TryGetUsableRenderer(
                        skyTiles[index],
                        out SpriteRenderer renderer))
                {
                    WarnInvalidConfigurationOnce(
                        $"Sky Tile {index} must be active and contain an "
                        + "enabled SpriteRenderer with a Sprite.");
                    return false;
                }

                _orderedTiles[index] = skyTiles[index];
                _orderedRenderers[index] = renderer;
            }

            SortTilesByWorldY();

            if (!TryResolveCamera())
            {
                return false;
            }

            if (!ValidateVerticalCoverage())
            {
                return false;
            }

            _warnedInvalidConfiguration = false;
            return true;
        }

        private bool HasUniqueRootReferences()
        {
            return ground != null
                && skyTiles != null
                && skyTiles.Length == RequiredSkyTileCount
                && skyTiles[0] != null
                && skyTiles[1] != null
                && skyTiles[0] != skyTiles[1]
                && skyTiles[0] != ground
                && skyTiles[1] != ground;
        }

        private static bool TryGetUsableRenderer(
            Transform root,
            out SpriteRenderer renderer)
        {
            return TryGetRenderer(root, true, out renderer);
        }

        private static bool TryGetRenderer(
            Transform root,
            bool requireActiveInHierarchy,
            out SpriteRenderer renderer)
        {
            renderer = null;

            if (root == null
                || (requireActiveInHierarchy
                    && !root.gameObject.activeInHierarchy))
            {
                return false;
            }

            SpriteRenderer[] renderers =
                root.GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer candidate = renderers[index];
                if (candidate == null
                    || (requireActiveInHierarchy
                        && !candidate.gameObject.activeInHierarchy)
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

        private bool ValidateVerticalCoverage()
        {
            float firstTileHeight = _orderedRenderers[0].bounds.size.y;
            float secondTileHeight = _orderedRenderers[1].bounds.size.y;
            float minimumTileHeight =
                Mathf.Min(firstTileHeight, secondTileHeight);

            if (minimumTileHeight <= CoverageEpsilon)
            {
                WarnInvalidConfigurationOnce(
                    "Sky Tile renderer bounds must have a positive height.");
                return false;
            }

            float maximumSafeOverlap =
                Mathf.Max(0f, minimumTileHeight - CoverageEpsilon);
            seamOverlap = Mathf.Clamp(
                seamOverlap,
                0f,
                maximumSafeOverlap);

            float cameraHeight = Mathf.Max(
                GetCameraHeightAtRenderer(_orderedRenderers[0]),
                GetCameraHeightAtRenderer(_orderedRenderers[1]));
            float requiredTileHeight =
                cameraHeight + recycleMargin + seamOverlap;

            if (cameraHeight <= CoverageEpsilon)
            {
                WarnInvalidConfigurationOnce(
                    "the target Camera has no measurable vertical coverage.");
                return false;
            }

            if (minimumTileHeight + CoverageEpsilon
                >= requiredTileHeight)
            {
                return true;
            }

            WarnInvalidConfigurationOnce(
                $"each Sky Tile must be at least {requiredTileHeight:F3} "
                + "world units high to cover the Camera, recycle margin, and "
                + $"seam overlap, but the smaller tile is "
                + $"{minimumTileHeight:F3}.");
            return false;
        }

        private void SortTilesByWorldY()
        {
            if (_orderedRenderers[0].bounds.min.y
                <= _orderedRenderers[1].bounds.min.y)
            {
                return;
            }

            SwapOrderedTiles();
        }

        private void DisableGroundIfBelow(float recycleThreshold)
        {
            if (!ground.gameObject.activeSelf)
            {
                HasGroundExited = true;
                return;
            }

            if (_groundRenderer.bounds.max.y < recycleThreshold)
            {
                ground.gameObject.SetActive(false);
                HasGroundExited = true;
            }
        }

        private void RecycleSkyTiles(float recycleThreshold)
        {
            int recycleCount = 0;

            while (_orderedRenderers[0].bounds.max.y < recycleThreshold
                   && recycleCount < maxRecyclesPerFrame)
            {
                RecycleLowestTile();
                recycleCount++;
            }

            bool stillBelowThreshold =
                _orderedRenderers[0].bounds.max.y < recycleThreshold;

            if (stillBelowThreshold && !_warnedRecycleLimit)
            {
                Debug.LogWarning(
                    $"{nameof(VerticalBackgroundScroller)} on '{name}' reached "
                    + "its per-frame recycle safety limit after an unusually "
                    + "large frame step.",
                    this);
                _warnedRecycleLimit = true;
            }
            else if (!stillBelowThreshold)
            {
                _warnedRecycleLimit = false;
            }
        }

        private void RecycleLowestTile()
        {
            Transform tileToMove = _orderedTiles[0];
            SpriteRenderer movingRenderer = _orderedRenderers[0];
            SpriteRenderer upperRenderer = _orderedRenderers[1];

            float desiredBottomEdge =
                upperRenderer.bounds.max.y - seamOverlap;
            float positionDeltaY =
                desiredBottomEdge - movingRenderer.bounds.min.y;

            Vector3 position = tileToMove.position;
            position.y += positionDeltaY;
            tileToMove.position = position;

            SwapOrderedTiles();

            PassedBackgroundCount++;
            BackgroundPassed?.Invoke(PassedBackgroundCount);
        }

        private void SwapOrderedTiles()
        {
            (_orderedTiles[0], _orderedTiles[1]) =
                (_orderedTiles[1], _orderedTiles[0]);
            (_orderedRenderers[0], _orderedRenderers[1]) =
                (_orderedRenderers[1], _orderedRenderers[0]);
        }

        private bool TryResolveCamera()
        {
            if (targetCamera != null)
            {
                _warnedMissingCamera = false;
                return true;
            }

            if (!_warnedMissingCamera)
            {
                Debug.LogWarning(
                    $"{nameof(VerticalBackgroundScroller)} on '{name}' could "
                    + "not resolve its explicitly assigned Target Camera.",
                    this);
                _warnedMissingCamera = true;
            }

            return false;
        }

        private float GetCameraBottomEdgeY()
        {
            if (targetCamera.orthographic)
            {
                return targetCamera.transform.position.y
                    - targetCamera.orthographicSize;
            }

            float tilePlaneDistance = Mathf.Abs(
                targetCamera.transform.position.z
                - _orderedRenderers[0].bounds.center.z);

            return targetCamera.ViewportToWorldPoint(
                new Vector3(0.5f, 0f, tilePlaneDistance)).y;
        }

        private float GetCameraHeightAtRenderer(
            SpriteRenderer renderer)
        {
            if (targetCamera.orthographic)
            {
                return targetCamera.orthographicSize * 2f;
            }

            float tilePlaneDistance = Mathf.Abs(
                targetCamera.transform.position.z
                - renderer.bounds.center.z);
            Vector3 bottom = targetCamera.ViewportToWorldPoint(
                new Vector3(0.5f, 0f, tilePlaneDistance));
            Vector3 top = targetCamera.ViewportToWorldPoint(
                new Vector3(0.5f, 1f, tilePlaneDistance));
            return Mathf.Abs(top.y - bottom.y);
        }

        private void WarnInvalidConfigurationOnce(string reason)
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(VerticalBackgroundScroller)} on '{name}' is inactive: "
                + reason,
                this);
            _warnedInvalidConfiguration = true;
        }

        private void OnValidate()
        {
            recycleMargin = Mathf.Max(0f, recycleMargin);
            seamOverlap = Mathf.Max(0f, seamOverlap);
            maxRecyclesPerFrame = Mathf.Clamp(maxRecyclesPerFrame, 1, 256);

            if (skyTiles == null
                || skyTiles.Length != RequiredSkyTileCount
                || skyTiles[0] == null
                || skyTiles[1] == null)
            {
                return;
            }

            SpriteRenderer firstRenderer =
                skyTiles[0].GetComponentInChildren<SpriteRenderer>(true);
            SpriteRenderer secondRenderer =
                skyTiles[1].GetComponentInChildren<SpriteRenderer>(true);

            if (firstRenderer == null || secondRenderer == null)
            {
                return;
            }

            float minimumTileHeight = Mathf.Min(
                firstRenderer.bounds.size.y,
                secondRenderer.bounds.size.y);
            float maximumSafeOverlap =
                Mathf.Max(0f, minimumTileHeight - CoverageEpsilon);
            seamOverlap = Mathf.Min(seamOverlap, maximumSafeOverlap);
        }
    }
}
