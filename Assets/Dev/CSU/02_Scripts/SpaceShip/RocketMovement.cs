using System;
using UnityEngine;

namespace Dev.CSU.Scripts
{
    public class RocketMovement : MonoBehaviour
    {
        [Header("Rocket Movement")]
        [SerializeField] private float rocketSpeed;

        [SerializeField] private Vector2 moveDir;
        
        [Header("References")]
        private Rigidbody2D _rigidbody;

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
