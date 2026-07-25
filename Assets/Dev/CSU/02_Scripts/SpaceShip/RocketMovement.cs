using Dev.NKY.Scripts;
using UnityEngine;

namespace Dev.CSU._02_Scripts.SpaceShip
{
    public class RocketMovement : MonoBehaviour
    {
        [SerializeField] private float rocketSpeed;

        private Vector2 moveDir;

        public float Speed => rocketSpeed;
        
        [Header("References")]
        private Rigidbody2D _rigidbody;

        public void ChangeSpeed(float speed)
        {
            rocketSpeed = Mathf.Max(0f, speed);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            moveDir = Vector2.right;
        }

        private void FixedUpdate()
        {
            _rigidbody.linearVelocity = moveDir * rocketSpeed;
        }
    }
}
