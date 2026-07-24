using UnityEngine;

namespace Dev.CSU._02_Scripts.Map
{
    [DefaultExecutionOrder(2000)]
    [DisallowMultipleComponent]
    public sealed class MapBorderController : MonoBehaviour
    {
        private const float MinimumColliderThickness = 0.01f;

        [Header("References")]
        [Tooltip("Camera whose viewport determines the visible top, bottom, and horizontal boundary coverage.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Transform whose X position the MapBoarder follows. The Target Camera transform is used when empty.")]
        [SerializeField] private Transform horizontalFollowTarget;

        [Tooltip("Non-trigger BoxCollider2D used as the upper boundary.")]
        [SerializeField] private BoxCollider2D topBoundary;

        [Tooltip("Non-trigger BoxCollider2D used as the lower boundary.")]
        [SerializeField] private BoxCollider2D bottomBoundary;

        [Header("Boundary Placement")]
        [Tooltip("Distance in Unity Units that the upper playable limit is moved inward from the viewport top.")]
        [Min(0f)]
        [SerializeField] private float topPadding;

        [Tooltip("Distance in Unity Units that the lower playable limit is moved inward from the viewport bottom.")]
        [Min(0f)]
        [SerializeField] private float bottomPadding;

        [Tooltip("World-space thickness of the upper and lower colliders in Unity Units.")]
        [Min(MinimumColliderThickness)]
        [SerializeField] private float colliderThickness = 1f;

        [Header("Following")]
        [Tooltip("Moves MapBoarder on X with the Horizontal Follow Target. Its world Y remains locked.")]
        [SerializeField] private bool followHorizontalPosition = true;

        [Tooltip("Recalculates collider width and viewport height when the Game View size, aspect ratio, or orthographic size changes.")]
        [SerializeField] private bool updateBoundsOnResolutionChange = true;

        [Header("Collision Filtering")]
        [Tooltip("Layers allowed to collide with the boundaries. Other layers are excluded by Collider2D layer overrides.")]
        [SerializeField] private LayerMask playerCollisionLayers;

        private float _lockedWorldY;
        private int _lastPixelWidth;
        private int _lastPixelHeight;
        private float _lastAspect;
        private float _lastOrthographicSize;
        private float _topPlayableWorldY;
        private float _bottomPlayableWorldY;
        private bool _initialized;
        private bool _warnedInvalidConfiguration;

        public float LockedWorldY => _lockedWorldY;
        public float TopPlayableWorldY => _topPlayableWorldY;
        public float BottomPlayableWorldY => _bottomPlayableWorldY;
        public float BoundsCenterY =>
            (_topPlayableWorldY + _bottomPlayableWorldY) * 0.5f;
        public float BoundsHeight => Mathf.Max(
            0f,
            _topPlayableWorldY - _bottomPlayableWorldY);

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                Initialize();
                return;
            }

            UpdateBounds();
        }

        private void LateUpdate()
        {
            if (!TryResolveReferences())
            {
                return;
            }

            KeepHorizontalPosition();

            if (updateBoundsOnResolutionChange
                && HaveViewportDimensionsChanged())
            {
                UpdateBounds();
            }
        }

        public void UpdateBounds()
        {
            if (!TryResolveReferences())
            {
                return;
            }

            Vector3 parentPosition = transform.position;
            parentPosition.y = _lockedWorldY;

            if (followHorizontalPosition)
            {
                parentPosition.x = horizontalFollowTarget.position.x;
            }

            transform.position = parentPosition;

            float viewportDepth = Mathf.Max(
                0.01f,
                Mathf.Abs(transform.position.z
                    - targetCamera.transform.position.z));
            Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, viewportDepth));
            Vector3 topRight = targetCamera.ViewportToWorldPoint(
                new Vector3(1f, 1f, viewportDepth));

            float viewportWidth = Mathf.Abs(topRight.x - bottomLeft.x);
            float viewportHeight = Mathf.Abs(topRight.y - bottomLeft.y);
            float maximumPadding = Mathf.Max(
                0f,
                (viewportHeight * 0.5f)
                - MinimumColliderThickness);
            float safeTopPadding = Mathf.Min(
                topPadding,
                maximumPadding);
            float safeBottomPadding = Mathf.Min(
                bottomPadding,
                maximumPadding);

            _topPlayableWorldY = _lockedWorldY
                + (viewportHeight * 0.5f)
                - safeTopPadding;
            _bottomPlayableWorldY = _lockedWorldY
                - (viewportHeight * 0.5f)
                + safeBottomPadding;

            ConfigureBoundary(
                topBoundary,
                _topPlayableWorldY + (colliderThickness * 0.5f),
                viewportWidth);
            ConfigureBoundary(
                bottomBoundary,
                _bottomPlayableWorldY - (colliderThickness * 0.5f),
                viewportWidth);
            CacheViewportDimensions();
        }

        private void Initialize()
        {
            _lockedWorldY = transform.position.y;
            _initialized = true;

            if (TryResolveReferences())
            {
                ApplyCollisionFilters();
                UpdateBounds();
            }
        }

        private bool TryResolveReferences()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (horizontalFollowTarget == null && targetCamera != null)
            {
                horizontalFollowTarget = targetCamera.transform;
            }

            bool isValid = targetCamera != null
                && horizontalFollowTarget != null
                && topBoundary != null
                && bottomBoundary != null
                && topBoundary != bottomBoundary;

            if (isValid)
            {
                _warnedInvalidConfiguration = false;
                return true;
            }

            if (!_warnedInvalidConfiguration)
            {
                Debug.LogWarning(
                    $"{nameof(MapBorderController)} on '{name}' requires "
                    + "a camera, a horizontal target, and two unique "
                    + "BoxCollider2D boundary references.",
                    this);
                _warnedInvalidConfiguration = true;
            }

            return false;
        }

        private void KeepHorizontalPosition()
        {
            Vector3 position = transform.position;
            position.y = _lockedWorldY;

            if (followHorizontalPosition)
            {
                position.x = horizontalFollowTarget.position.x;
            }

            transform.position = position;
        }

        private void ConfigureBoundary(
            BoxCollider2D boundary,
            float worldCenterY,
            float viewportWidth)
        {
            Transform boundaryTransform = boundary.transform;
            boundaryTransform.localRotation = Quaternion.identity;
            boundaryTransform.localScale = Vector3.one;

            float parentScaleX = Mathf.Max(
                MinimumColliderThickness,
                Mathf.Abs(transform.lossyScale.x));
            float parentScaleY = Mathf.Max(
                MinimumColliderThickness,
                Mathf.Abs(transform.lossyScale.y));

            boundaryTransform.localPosition = new Vector3(
                0f,
                (worldCenterY - _lockedWorldY) / parentScaleY,
                0f);
            boundary.offset = Vector2.zero;
            boundary.size = new Vector2(
                viewportWidth / parentScaleX,
                colliderThickness / parentScaleY);
            boundary.isTrigger = false;
            boundary.enabled = true;
        }

        private void ApplyCollisionFilters()
        {
            int includedLayers = playerCollisionLayers.value;
            int excludedLayers = ~includedLayers;

            ApplyCollisionFilter(topBoundary, includedLayers, excludedLayers);
            ApplyCollisionFilter(
                bottomBoundary,
                includedLayers,
                excludedLayers);
        }

        private static void ApplyCollisionFilter(
            Collider2D boundary,
            int includedLayers,
            int excludedLayers)
        {
            boundary.layerOverridePriority = 1;
            boundary.includeLayers = includedLayers;
            boundary.excludeLayers = excludedLayers;
        }

        private bool HaveViewportDimensionsChanged()
        {
            return targetCamera.pixelWidth != _lastPixelWidth
                || targetCamera.pixelHeight != _lastPixelHeight
                || !Mathf.Approximately(
                    targetCamera.aspect,
                    _lastAspect)
                || !Mathf.Approximately(
                    targetCamera.orthographicSize,
                    _lastOrthographicSize);
        }

        private void CacheViewportDimensions()
        {
            _lastPixelWidth = targetCamera.pixelWidth;
            _lastPixelHeight = targetCamera.pixelHeight;
            _lastAspect = targetCamera.aspect;
            _lastOrthographicSize = targetCamera.orthographicSize;
        }

        private void OnValidate()
        {
            topPadding = Mathf.Max(0f, topPadding);
            bottomPadding = Mathf.Max(0f, bottomPadding);
            colliderThickness = Mathf.Max(
                MinimumColliderThickness,
                colliderThickness);

            if (!Application.isPlaying)
            {
                _lockedWorldY = transform.position.y;
                _initialized = true;
            }

            if (targetCamera != null
                && horizontalFollowTarget != null
                && topBoundary != null
                && bottomBoundary != null
                && topBoundary != bottomBoundary)
            {
                ApplyCollisionFilters();
                UpdateBounds();
            }
        }
    }
}
