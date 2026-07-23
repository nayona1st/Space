using UnityEngine;
using UnityEngine.Serialization;

namespace Dev.CSU.Scripts
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(RocketTurnInput))]
    public sealed class RocketSteering : MonoBehaviour
    {
        private const float MinimumSpeed = 0.0001f;

        [Header("Tilt Settings")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum counterclockwise tilt in degrees while A is held.")]
        [FormerlySerializedAs("leftTurnAngle")]
        private float leftMaxAngle = 30f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum clockwise tilt in degrees while D is held.")]
        [FormerlySerializedAs("rightTurnAngle")]
        private float rightMaxAngle = 30f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum tilt speed in degrees per second while A or D is held.")]
        private float tiltSpeed = 90f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum return speed in degrees per second when the input is neutral.")]
        private float returnSpeed = 120f;

        [Header("Smoothing")]
        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Time in seconds used to ease into a turn. Higher values feel softer.")]
        private float tiltSmoothTime = 0.18f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Time in seconds used to ease back to the base direction.")]
        private float returnSmoothTime = 0.24f;

        private Rigidbody2D _rigidbody;
        private RocketTurnInput _turnInput;
        private Vector2 _baseTravelDirection;
        private float _baseRotation;
        private float _currentTilt;
        private float _tiltVelocity;
        private bool _hasBaseTravelDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _turnInput = GetComponent<RocketTurnInput>();
            _baseRotation = _rigidbody.rotation;
        }

        private void FixedUpdate()
        {
            float speed = _rigidbody.linearVelocity.magnitude;

            if (!_hasBaseTravelDirection && speed >= MinimumSpeed)
            {
                _baseTravelDirection = _rigidbody.linearVelocity / speed;
                _hasBaseTravelDirection = true;
            }

            float targetTilt = GetTargetTilt();
            bool isReturning = Mathf.Approximately(targetTilt, 0f);
            float maxSpeed = isReturning
                ? returnSpeed
                : tiltSpeed;
            float smoothTime = isReturning
                ? returnSmoothTime
                : tiltSmoothTime;

            _currentTilt = Mathf.SmoothDampAngle(
                _currentTilt,
                targetTilt,
                ref _tiltVelocity,
                smoothTime,
                maxSpeed,
                Time.fixedDeltaTime);
            _currentTilt = Mathf.Clamp(_currentTilt, -rightMaxAngle, leftMaxAngle);

            _rigidbody.MoveRotation(_baseRotation + _currentTilt);

            if (_hasBaseTravelDirection && speed >= MinimumSpeed)
            {
                Vector2 currentDirection = Rotate(_baseTravelDirection, _currentTilt);
                _rigidbody.linearVelocity = currentDirection * speed;
            }
        }

        private float GetTargetTilt()
        {
            if (_turnInput.TurnInput < 0f)
            {
                return leftMaxAngle;
            }

            if (_turnInput.TurnInput > 0f)
            {
                return -rightMaxAngle;
            }

            return 0f;
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);

            return new Vector2(
                (direction.x * cosine) - (direction.y * sine),
                (direction.x * sine) + (direction.y * cosine));
        }

        private void OnValidate()
        {
            leftMaxAngle = Mathf.Max(0f, leftMaxAngle);
            rightMaxAngle = Mathf.Max(0f, rightMaxAngle);
            tiltSpeed = Mathf.Max(0f, tiltSpeed);
            returnSpeed = Mathf.Max(0f, returnSpeed);
            tiltSmoothTime = Mathf.Max(0.01f, tiltSmoothTime);
            returnSmoothTime = Mathf.Max(0.01f, returnSmoothTime);
        }
    }
}
