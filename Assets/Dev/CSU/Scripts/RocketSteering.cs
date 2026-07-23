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
        private const float SnapEpsilon = 0.05f;

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

        [Header("Response")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Exponential response strength while turning. Higher values respond faster.")]
        private float turnResponse = 7f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Exponential response strength while returning to the base direction.")]
        private float returnResponse = 9f;

        private Rigidbody2D _rigidbody;
        private RocketTurnInput _turnInput;
        private Vector2 _baseTravelDirection;
        private float _baseRotation;
        private float _currentTilt;
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
            float response = Mathf.Approximately(targetTilt, 0f)
                ? returnResponse
                : turnResponse;

            float alpha = 1f - Mathf.Exp(-response * Time.fixedDeltaTime);
            float angleDelta = Mathf.DeltaAngle(_currentTilt, targetTilt);
            _currentTilt += angleDelta * alpha;

            if (Mathf.Abs(Mathf.DeltaAngle(_currentTilt, targetTilt)) <= SnapEpsilon)
            {
                _currentTilt = targetTilt;
            }

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
            turnResponse = Mathf.Max(0f, turnResponse);
            returnResponse = Mathf.Max(0f, returnResponse);
        }
    }
}
