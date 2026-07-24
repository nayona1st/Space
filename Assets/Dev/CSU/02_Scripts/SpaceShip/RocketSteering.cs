using UnityEngine;
using UnityEngine.Serialization;

namespace Dev.CSU._02_Scripts.SpaceShip
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

        [Header("Tilt Speed")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Tilt speed in degrees per second when A or D is first pressed.")]
        private float initialTiltSpeed = 30f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum tilt speed in degrees per second while A or D is held.")]
        private float maximumTiltSpeed = 120f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Amount the tilt speed increases per second while A or D is held.")]
        private float tiltAcceleration = 60f;

        [Header("Response")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Exponential response strength while returning to the base direction.")]
        private float returnResponse = 9f;

        private Rigidbody2D _rigidbody;
        private RocketTurnInput _turnInput;
        private Vector2 _baseTravelDirection;
        private float _baseRotation;
        private float _currentTilt;
        private float _currentTiltSpeed;
        private int _lastTurnDirection;
        private bool _hasBaseTravelDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _turnInput = GetComponent<RocketTurnInput>();
            _baseRotation = _rigidbody.rotation;
            ResetTurnAcceleration();
        }

        private void FixedUpdate()
        {
            float speed = _rigidbody.linearVelocity.magnitude;

            if (!_hasBaseTravelDirection && speed >= MinimumSpeed)
            {
                _baseTravelDirection = _rigidbody.linearVelocity / speed;
                _hasBaseTravelDirection = true;
            }

            float turnInput = _turnInput.TurnInput;
            float targetTilt = GetTargetTilt(turnInput);
            float deltaTime = Time.deltaTime;

            if (Mathf.Approximately(turnInput, 0f))
            {
                ResetTurnAcceleration();

                float alpha = 1f - Mathf.Exp(-returnResponse * deltaTime);
                float angleDelta = Mathf.DeltaAngle(_currentTilt, targetTilt);
                _currentTilt += angleDelta * alpha;
            }
            else
            {
                int turnDirection = turnInput < 0f ? -1 : 1;
                if (turnDirection != _lastTurnDirection)
                {
                    _currentTiltSpeed = initialTiltSpeed;
                }

                _currentTilt = Mathf.MoveTowards(
                    _currentTilt,
                    targetTilt,
                    _currentTiltSpeed * deltaTime);

                _currentTiltSpeed = Mathf.MoveTowards(
                    _currentTiltSpeed,
                    maximumTiltSpeed,
                    tiltAcceleration * deltaTime);

                _lastTurnDirection = turnDirection;
            }

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

        private float GetTargetTilt(float turnInput)
        {
            if (turnInput < 0f)
            {
                return leftMaxAngle;
            }

            if (turnInput > 0f)
            {
                return -rightMaxAngle;
            }

            return 0f;
        }

        private void ResetTurnAcceleration()
        {
            _currentTiltSpeed = initialTiltSpeed;
            _lastTurnDirection = 0;
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
            initialTiltSpeed = Mathf.Max(0f, initialTiltSpeed);
            maximumTiltSpeed = Mathf.Max(initialTiltSpeed, maximumTiltSpeed);
            tiltAcceleration = Mathf.Max(0f, tiltAcceleration);
            returnResponse = Mathf.Max(0f, returnResponse);
            _currentTiltSpeed = Mathf.Clamp(
                _currentTiltSpeed,
                initialTiltSpeed,
                maximumTiltSpeed);
        }
    }
}
