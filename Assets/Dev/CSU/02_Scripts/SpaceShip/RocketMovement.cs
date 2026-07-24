using Dev.NKY.Scripts;
using UnityEngine;

namespace Dev.CSU._02_Scripts.SpaceShip
{
    public class RocketMovement : MonoBehaviour
    {
        [SerializeField] private float rocketSpeed;

        private Vector2 moveDir;
        
        [Header("References")]
        private Rigidbody2D _rigidbody;

        public void ChangeSpeed(float speed)
        {
            rocketSpeed = speed;
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
