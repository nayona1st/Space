using UnityEngine;

namespace Dev.CSU.Scripts.Meteor
{
    [DisallowMultipleComponent]
    public sealed class MeteorMover : MonoBehaviour
    {
        private Rigidbody2D _rigidbody2D;
        private Vector2 _direction = Vector2.left;
        private float _speed;
        private float _rotationSpeed;
        private float _remainingLifetime;
        private bool _initialized;

        private void Awake()
        {
            TryGetComponent(out _rigidbody2D);
        }

        public void Initialize(
            Vector2 direction,
            float speed,
            float lifetime,
            float rotationSpeed,
            float spawnRotation)
        {
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
            _speed = Mathf.Max(0f, speed);
            _rotationSpeed = rotationSpeed;
            _remainingLifetime = Mathf.Max(0f, lifetime);
            SetZRotation(spawnRotation);
            _initialized = true;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _direction * _speed;
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (_rigidbody2D == null)
            {
                transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));
            }

            transform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime, Space.Self);

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            _direction = Vector2.left;
            _speed = 0f;
            _rotationSpeed = 0f;
            _remainingLifetime = 0f;
            _initialized = false;
            SetZRotation(0f);

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private void SetZRotation(float zRotation)
        {
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = zRotation;
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
    }
}
