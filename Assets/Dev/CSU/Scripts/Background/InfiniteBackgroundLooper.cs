using System;
using UnityEngine;

namespace Dev.CSU.Scripts.Background
{
    [DisallowMultipleComponent]
    public sealed class InfiniteBackgroundLooper : MonoBehaviour
    {
        private const int RequiredSegmentCount = 2;
        private const int MaxRecyclesPerFrame = 16;

        [Tooltip("무한 반복에 사용할 두 배경 Transform입니다. 런타임에 월드 X 위치를 기준으로 정렬됩니다.")]
        [SerializeField] private Transform[] segments;

        [Tooltip("배경 재배치 판정에 사용할 카메라입니다. 비어 있으면 Camera.main을 사용합니다.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("배경이 카메라 왼쪽을 이 거리만큼 더 벗어난 뒤 재배치됩니다.")]
        [Min(0f)]
        [SerializeField] private float recycleMargin;

        [Tooltip("배경 사이의 미세한 틈을 막기 위해 서로 겹치게 할 월드 거리입니다.")]
        [Min(0f)]
        [SerializeField] private float seamOverlap = 0.01f;

        private readonly Transform[] _orderedSegments = new Transform[RequiredSegmentCount];
        private readonly SpriteRenderer[] _orderedRenderers =
            new SpriteRenderer[RequiredSegmentCount];
        private readonly int[] _orderedOriginalIndices = new int[RequiredSegmentCount];

        private bool _isInitialized;
        private bool _warnedInvalidConfiguration;
        private bool _warnedMissingCamera;
        private bool _warnedInsufficientCoverage;
        private bool _warnedRecycleLimit;

        public int SegmentCount => RequiredSegmentCount;

        public event Action<Transform, int> SegmentWillRecycle;
        public event Action<Transform, int> SegmentRecycled;

        private void OnEnable()
        {
            _isInitialized = TryInitialize();
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (!TryResolveCamera())
            {
                return;
            }

            float cameraLeftEdge = GetCameraLeftEdgeX();
            float recycleThreshold = cameraLeftEdge - recycleMargin;
            int recycleCount = 0;

            while (_orderedRenderers[0].bounds.max.x < recycleThreshold
                   && recycleCount < MaxRecyclesPerFrame)
            {
                RecycleLeftSegment();
                recycleCount++;
            }

            if (recycleCount == MaxRecyclesPerFrame
                && _orderedRenderers[0].bounds.max.x < recycleThreshold
                && !_warnedRecycleLimit)
            {
                Debug.LogWarning(
                    $"{nameof(InfiniteBackgroundLooper)} on '{name}' reached its "
                    + "per-frame recycle safety limit after an unusually large camera jump.",
                    this);
                _warnedRecycleLimit = true;
            }
            else if (_orderedRenderers[0].bounds.max.x >= recycleThreshold)
            {
                _warnedRecycleLimit = false;
            }
        }

        private bool TryInitialize()
        {
            if (segments == null || segments.Length != RequiredSegmentCount)
            {
                WarnInvalidConfigurationOnce(
                    $"Exactly {RequiredSegmentCount} background segments are required.");
                return false;
            }

            if (segments[0] == null || segments[1] == null || segments[0] == segments[1])
            {
                WarnInvalidConfigurationOnce(
                    "Both background segment references must be assigned and unique.");
                return false;
            }

            for (int index = 0; index < RequiredSegmentCount; index++)
            {
                SpriteRenderer renderer =
                    segments[index].GetComponentInChildren<SpriteRenderer>(true);

                if (renderer == null)
                {
                    WarnInvalidConfigurationOnce(
                        $"Segment '{segments[index].name}' has no SpriteRenderer.");
                    return false;
                }

                _orderedSegments[index] = segments[index];
                _orderedRenderers[index] = renderer;
                _orderedOriginalIndices[index] = index;
            }

            SortSegmentsByWorldX();
            _warnedInvalidConfiguration = false;

            if (TryResolveCamera())
            {
                ValidateCameraCoverageOnce();
            }

            return true;
        }

        private void SortSegmentsByWorldX()
        {
            if (_orderedRenderers[0].bounds.min.x <= _orderedRenderers[1].bounds.min.x)
            {
                return;
            }

            SwapOrderedSegments();
        }

        private void RecycleLeftSegment()
        {
            Transform segmentToMove = _orderedSegments[0];
            SpriteRenderer movingRenderer = _orderedRenderers[0];
            SpriteRenderer rightRenderer = _orderedRenderers[1];
            int originalIndex = _orderedOriginalIndices[0];

            SegmentWillRecycle?.Invoke(segmentToMove, originalIndex);

            float desiredLeftEdge = rightRenderer.bounds.max.x - seamOverlap;
            float positionDeltaX = desiredLeftEdge - movingRenderer.bounds.min.x;

            Vector3 position = segmentToMove.position;
            position.x += positionDeltaX;
            segmentToMove.position = position;

            SegmentRecycled?.Invoke(segmentToMove, originalIndex);
            SwapOrderedSegments();
        }

        public bool TryGetSegment(int originalIndex, out Transform segment)
        {
            segment = null;

            if (!_isInitialized)
            {
                _isInitialized = TryInitialize();
            }

            if (!_isInitialized
                || originalIndex < 0
                || originalIndex >= RequiredSegmentCount)
            {
                return false;
            }

            for (int orderedIndex = 0;
                 orderedIndex < RequiredSegmentCount;
                 orderedIndex++)
            {
                if (_orderedOriginalIndices[orderedIndex] != originalIndex)
                {
                    continue;
                }

                segment = _orderedSegments[orderedIndex];
                return segment != null;
            }

            return false;
        }

        private void SwapOrderedSegments()
        {
            (_orderedSegments[0], _orderedSegments[1]) =
                (_orderedSegments[1], _orderedSegments[0]);
            (_orderedRenderers[0], _orderedRenderers[1]) =
                (_orderedRenderers[1], _orderedRenderers[0]);
            (_orderedOriginalIndices[0], _orderedOriginalIndices[1]) =
                (_orderedOriginalIndices[1], _orderedOriginalIndices[0]);
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
                    $"{nameof(InfiniteBackgroundLooper)} on '{name}' could not find a camera.",
                    this);
                _warnedMissingCamera = true;
            }

            return false;
        }

        private float GetCameraLeftEdgeX()
        {
            if (targetCamera.orthographic)
            {
                return targetCamera.transform.position.x
                    - targetCamera.orthographicSize * targetCamera.aspect;
            }

            float segmentPlaneDistance = Mathf.Abs(
                targetCamera.transform.position.z
                - _orderedRenderers[0].bounds.center.z);

            return targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0.5f, segmentPlaneDistance)).x;
        }

        private void ValidateCameraCoverageOnce()
        {
            if (_warnedInsufficientCoverage)
            {
                return;
            }

            float cameraWidth;
            if (targetCamera.orthographic)
            {
                cameraWidth = targetCamera.orthographicSize
                    * 2f
                    * targetCamera.aspect;
            }
            else
            {
                float segmentPlaneDistance = Mathf.Abs(
                    targetCamera.transform.position.z
                    - _orderedRenderers[0].bounds.center.z);
                Vector3 left = targetCamera.ViewportToWorldPoint(
                    new Vector3(0f, 0.5f, segmentPlaneDistance));
                Vector3 right = targetCamera.ViewportToWorldPoint(
                    new Vector3(1f, 0.5f, segmentPlaneDistance));
                cameraWidth = Mathf.Abs(right.x - left.x);
            }

            float availableWidth = _orderedRenderers[0].bounds.size.x
                + _orderedRenderers[1].bounds.size.x
                - seamOverlap;

            if (availableWidth + Mathf.Epsilon < cameraWidth)
            {
                Debug.LogWarning(
                    $"{nameof(InfiniteBackgroundLooper)} on '{name}' has background "
                    + $"coverage ({availableWidth:F2}) smaller than the camera width "
                    + $"({cameraWidth:F2}).",
                    this);
                _warnedInsufficientCoverage = true;
            }
        }

        private void WarnInvalidConfigurationOnce(string reason)
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(InfiniteBackgroundLooper)} on '{name}' is disabled: {reason}",
                this);
            _warnedInvalidConfiguration = true;
        }

        private void OnValidate()
        {
            recycleMargin = Mathf.Max(0f, recycleMargin);
            seamOverlap = Mathf.Max(0f, seamOverlap);
        }
    }
}
