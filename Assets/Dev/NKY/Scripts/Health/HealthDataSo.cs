using UnityEngine;

namespace Dev.NKY.Scripts.Health
{
    [CreateAssetMenu(fileName = "Health", menuName = "So/HealthData", order = 0)]
    public class HealthDataSo : ScriptableObject
    {
        public float maxHealth;
        public float currentHealth;
    }
}