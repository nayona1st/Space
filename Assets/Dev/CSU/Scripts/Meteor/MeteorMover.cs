using UnityEngine;

namespace Dev.CSU.Scripts.Meteor
{
    [DisallowMultipleComponent]
    public sealed class MeteorMover : MonoBehaviour
    {
        private Rigidbody2D _rigidbody2D;
        private Vector2 _direction = Vector2.left;
        private float _speed;
        private float _remainingLifetime;
        private bool _initialized;

        private void Awake()
        {
            TryGetComponent(out _rigidbody2D);
        }

        public void Initialize(Vector2 direction, float speed, float lifetime)
        {
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
            _speed = Mathf.Max(0f, speed);
            _remainingLifetime = Mathf.Max(0f, lifetime);
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

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
