using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.Distance
{
    [DisallowMultipleComponent]
    public sealed class HorizontalDistanceTracker : MonoBehaviour
    {
        [Header("Distance Tracking")]
        [SerializeField]
        [Tooltip("Transform whose world-space X movement is measured. Uses this object when left empty.")]
        private Transform target;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Meters represented by one Unity world unit.")]
        private float metersPerUnityUnit = 1f;

        public float DistanceMeters { get; private set; }

        public event Action<float> DistanceChanged;

        private float _previousX;
        private bool _hasPreviousPosition;

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }
        }

        private void OnEnable()
        {
            CaptureCurrentPosition();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                _hasPreviousPosition = false;
                return;
            }

            float currentX = target.position.x;
            if (!_hasPreviousPosition)
            {
                _previousX = currentX;
                _hasPreviousPosition = true;
                return;
            }

            float deltaX = currentX - _previousX;
            _previousX = currentX;

            float addedDistance = Mathf.Max(0f, deltaX) * metersPerUnityUnit;
            if (addedDistance <= 0f)
            {
                return;
            }

            DistanceMeters += addedDistance;
            DistanceChanged?.Invoke(DistanceMeters);
        }

        public void ResetDistance()
        {
            DistanceMeters = 0f;
            CaptureCurrentPosition();
            DistanceChanged?.Invoke(DistanceMeters);
        }

        private void CaptureCurrentPosition()
        {
            if (target == null)
            {
                _hasPreviousPosition = false;
                return;
            }

            _previousX = target.position.x;
            _hasPreviousPosition = true;
        }

        private void OnValidate()
        {
            metersPerUnityUnit = Mathf.Max(0f, metersPerUnityUnit);
        }
    }
}
