using System;
using UnityEngine;

namespace Dev.CSU.Scripts.Stage
{
    [DefaultExecutionOrder(1050)]
    public abstract class StageParallaxDecoration : MonoBehaviour
    {
        private const float MinimumScale = 0.0001f;

        [Header("References")]
        [Tooltip("Prefab instantiated once and reused whenever this stage presentation is active.")]
        [SerializeField] private GameObject decorationPrefab;

        [Tooltip("Camera used for viewport placement and horizontal parallax. The stage controller camera or Camera.main is used when empty.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Transform whose X movement drives parallax. The Target Camera transform is used when empty.")]
        [SerializeField] private Transform horizontalMovementSource;

        [Header("Viewport Placement")]
        [Tooltip("Horizontal viewport position used when the decoration is activated. 0 is left and 1 is right.")]
        [SerializeField] private float viewportX = 0.5f;

        [Tooltip("Vertical viewport position kept throughout the stage. 0 is bottom and 1 is top.")]
        [SerializeField] private float viewportY = 0.5f;

        [Tooltip("Additional horizontal world-space offset from the configured viewport position.")]
        [SerializeField] private float horizontalOffset;

        [Tooltip("Additional vertical world-space offset from the configured viewport position.")]
        [SerializeField] private float verticalOffset;

        [Tooltip("Uniform scale applied to the instantiated decoration.")]
        [Min(MinimumScale)]
        [SerializeField] private float uniformScale = 1f;

        [Tooltip("Sprite sorting order applied to every SpriteRenderer under the decoration.")]
        [SerializeField] private int sortingOrder = -1;

        [Header("Horizontal Parallax")]
        [Tooltip("Screen-space horizontal parallax strength. 0 keeps the decoration fixed on screen, while 1 leaves it fixed in world X.")]
        [Min(0f)]
        [SerializeField] private float horizontalParallaxStrength = 0.5f;

        [Tooltip("A one-frame X movement at or above this distance is treated as a teleport and only resets the movement sample.")]
        [Min(0f)]
        [SerializeField] private float horizontalTeleportThreshold = 50f;

        [Tooltip("Rightward camera travel distance for which this stage presentation remains active.")]
        [Min(0f)]
        [SerializeField] private float stageTravelDistance = 200f;

        [Tooltip("Additional world-space padding outside the camera's right edge when recycling the decoration.")]
        [Min(0f)]
        [SerializeField] private float spawnRightPadding = 5f;

        [Tooltip("Distance past the camera's left edge before recycling the decoration.")]
        [Min(0f)]
        [SerializeField] private float recycleLeftPadding = 5f;

        [Tooltip("Realigns the decoration to its configured viewport position each time the stage starts.")]
        [SerializeField] private bool resetPositionOnStageStart = true;

        private GameObject _instance;
        private SpriteRenderer _primaryRenderer;
        private float _previousCameraX;
        private float _previousHorizontalSourceX;
        private float _travelledDistance;
        private bool _hasBeenPositioned;
        private bool _isRunning;
        private bool _warnedMissingPrefab;
        private bool _warnedMissingCamera;
        private bool _warnedMissingRenderer;

        public bool IsConfigured => decorationPrefab != null;
        public bool IsRunning => _isRunning;

        public event Action Completed;

        public bool BeginStage(Camera fallbackCamera)
        {
            StopStage();

            if (!TryResolveCamera(fallbackCamera) || !EnsureInstance())
            {
                return false;
            }

            _instance.SetActive(true);
            ApplyVisualSettings();

            if (resetPositionOnStageStart || !_hasBeenPositioned)
            {
                AlignCenterToViewport();
                _hasBeenPositioned = true;
            }
            else
            {
                MaintainViewportY();
            }

            ResetHorizontalSamples();
            _travelledDistance = 0f;
            _isRunning = true;
            return true;
        }

        public void StopStage()
        {
            _isRunning = false;
            _travelledDistance = 0f;

            if (_instance != null)
            {
                _instance.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (!_isRunning || _instance == null)
            {
                return;
            }

            if (!TryResolveCamera(null))
            {
                StopStage();
                return;
            }

            float currentCameraX = targetCamera.transform.position.x;
            float cameraDeltaX = currentCameraX - _previousCameraX;
            _previousCameraX = currentCameraX;

            float currentHorizontalSourceX = GetHorizontalSourceX();
            float horizontalSourceDeltaX =
                currentHorizontalSourceX - _previousHorizontalSourceX;
            _previousHorizontalSourceX = currentHorizontalSourceX;

            if (horizontalTeleportThreshold > 0f
                && (Mathf.Abs(cameraDeltaX) >= horizontalTeleportThreshold
                    || Mathf.Abs(horizontalSourceDeltaX)
                    >= horizontalTeleportThreshold))
            {
                AlignCenterToViewport();
                return;
            }

            _travelledDistance += Mathf.Max(
                0f,
                horizontalSourceDeltaX);

            ApplyHorizontalParallax(
                cameraDeltaX,
                horizontalSourceDeltaX);
            MaintainViewportY();
            RecycleIfCompletelyLeft();

            if (_travelledDistance >= stageTravelDistance)
            {
                CompleteStage();
            }
        }

        private bool EnsureInstance()
        {
            if (_instance != null && _primaryRenderer != null)
            {
                return true;
            }

            if (decorationPrefab == null)
            {
                if (!_warnedMissingPrefab)
                {
                    Debug.LogWarning(
                        $"{GetType().Name} on '{name}' has no decoration Prefab.",
                        this);
                    _warnedMissingPrefab = true;
                }

                return false;
            }

            _instance = Instantiate(decorationPrefab, transform);
            _instance.name = $"{decorationPrefab.name}_{GetType().Name}";
            _primaryRenderer =
                _instance.GetComponentInChildren<SpriteRenderer>(true);

            if (_primaryRenderer == null)
            {
                if (!_warnedMissingRenderer)
                {
                    Debug.LogWarning(
                        $"{GetType().Name} on '{name}' could not find a "
                        + $"SpriteRenderer under '{decorationPrefab.name}'.",
                        this);
                    _warnedMissingRenderer = true;
                }

                _instance.SetActive(false);
                return false;
            }

            DisablePhysics(_instance);
            _instance.SetActive(false);
            _warnedMissingPrefab = false;
            _warnedMissingRenderer = false;
            return true;
        }

        private bool TryResolveCamera(Camera fallbackCamera)
        {
            if (targetCamera == null)
            {
                targetCamera = fallbackCamera != null
                    ? fallbackCamera
                    : Camera.main;
            }

            if (targetCamera != null)
            {
                if (horizontalMovementSource == null)
                {
                    horizontalMovementSource = targetCamera.transform;
                }

                _warnedMissingCamera = false;
                return true;
            }

            if (!_warnedMissingCamera)
            {
                Debug.LogWarning(
                    $"{GetType().Name} on '{name}' could not find a camera.",
                    this);
                _warnedMissingCamera = true;
            }

            return false;
        }

        private float GetHorizontalSourceX()
        {
            return horizontalMovementSource != null
                ? horizontalMovementSource.position.x
                : targetCamera.transform.position.x;
        }

        private void ResetHorizontalSamples()
        {
            _previousCameraX = targetCamera.transform.position.x;
            _previousHorizontalSourceX = GetHorizontalSourceX();
        }

        private void ApplyVisualSettings()
        {
            _instance.transform.localScale = Vector3.one * uniformScale;

            SpriteRenderer[] renderers =
                _instance.GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].sortingOrder = sortingOrder;
            }
        }

        private void AlignCenterToViewport()
        {
            float depth = GetCameraDepth();
            Vector3 targetCenter = targetCamera.ViewportToWorldPoint(
                new Vector3(viewportX, viewportY, depth));

            targetCenter.x += horizontalOffset;
            targetCenter.y += verticalOffset;

            Vector3 position = _instance.transform.position;
            position.x += targetCenter.x - _primaryRenderer.bounds.center.x;
            position.y += targetCenter.y - _primaryRenderer.bounds.center.y;
            _instance.transform.position = position;
        }

        private void ApplyHorizontalParallax(
            float cameraDeltaX,
            float horizontalSourceDeltaX)
        {
            Vector3 position = _instance.transform.position;
            position.x += cameraDeltaX
                - horizontalSourceDeltaX
                * horizontalParallaxStrength;
            _instance.transform.position = position;
        }

        private void MaintainViewportY()
        {
            float depth = GetCameraDepth();
            float targetCenterY = targetCamera.ViewportToWorldPoint(
                new Vector3(0.5f, viewportY, depth)).y + verticalOffset;

            Vector3 position = _instance.transform.position;
            position.y += targetCenterY - _primaryRenderer.bounds.center.y;
            _instance.transform.position = position;
        }

        private void RecycleIfCompletelyLeft()
        {
            float depth = GetCameraDepth();
            float cameraLeftEdge = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, viewportY, depth)).x;

            if (_primaryRenderer.bounds.max.x
                >= cameraLeftEdge - recycleLeftPadding)
            {
                return;
            }

            float cameraRightEdge = targetCamera.ViewportToWorldPoint(
                new Vector3(1f, viewportY, depth)).x;

            Vector3 position = _instance.transform.position;
            position.x += cameraRightEdge
                + spawnRightPadding
                - _primaryRenderer.bounds.min.x;
            _instance.transform.position = position;
        }

        private float GetCameraDepth()
        {
            float depth = Vector3.Dot(
                _instance.transform.position
                - targetCamera.transform.position,
                targetCamera.transform.forward);

            if (depth > 0f)
            {
                return depth;
            }

            return Mathf.Abs(
                _instance.transform.position.z
                - targetCamera.transform.position.z);
        }

        private void CompleteStage()
        {
            _isRunning = false;

            if (_instance != null)
            {
                _instance.SetActive(false);
            }

            Completed?.Invoke();
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

        protected virtual void OnDisable()
        {
            StopStage();
        }

        protected virtual void OnValidate()
        {
            viewportX = Mathf.Clamp01(viewportX);
            viewportY = Mathf.Clamp01(viewportY);
            uniformScale = Mathf.Max(MinimumScale, uniformScale);
            horizontalParallaxStrength =
                Mathf.Max(0f, horizontalParallaxStrength);
            horizontalTeleportThreshold =
                Mathf.Max(0f, horizontalTeleportThreshold);
            stageTravelDistance = Mathf.Max(0f, stageTravelDistance);
            spawnRightPadding = Mathf.Max(0f, spawnRightPadding);
            recycleLeftPadding = Mathf.Max(0f, recycleLeftPadding);
        }
    }
}
