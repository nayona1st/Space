using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dev.NKY.Scripts.Health
{
    public class Health : DamageTask
    {
        private void Update()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TakeDamage(2);
            }
        }
    }
}