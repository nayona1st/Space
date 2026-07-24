using UnityEngine;

namespace Dev.NKY.Scripts.Health
{
    public interface IDamageable
    {
        public HealthDataSo Data { get; }
        
        public float MaxHealth { get; }
        public float CurrentHealth { get; }

        public void HealthInit();
        
        
        
    }
}