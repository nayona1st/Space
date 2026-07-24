using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts.Health
{
    public abstract class DamageTask : MonoBehaviour, IDamageable
    {
        [field:SerializeField] public HealthDataSo Data { get; private set; }
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider bgHealthSlider;
        
        public event Action DeadEvent;
        
        public void HealthInit()
        {
            MaxHealth = Data.maxHealth;
            CurrentHealth = Data.currentHealth;
        }

        public void SetHealth(float health)
        {
            MaxHealth = health;
        }

        public virtual void Awake()
        {
            HealthInit();
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float damage)
        {
            CurrentHealth -= damage;
            
            SetSliderFill(MaxHealth, CurrentHealth);
            
            if(CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Dead();
            }
        }

        public void SetSliderFill(float maxHealth, float currentHealth)
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(healthSlider.DOValue(currentHealth, 0.1f).SetEase(Ease.OutCubic));
            seq.AppendInterval(0.15f);
            seq.Append(bgHealthSlider.DOValue(currentHealth, 0.1f).SetEase(Ease.OutCubic));

        }

        public virtual void Dead()
        {
            DeadEvent?.Invoke();
        }
    }
}