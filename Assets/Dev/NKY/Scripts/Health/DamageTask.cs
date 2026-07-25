using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts.Health
{
    public abstract class DamageTask : MonoBehaviour, IDamageable
    {
        [field:SerializeField] public HealthDataSo Data { get; private set; }
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        
        
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider bgHealthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        
        public event Action DeadEvent;
        public event Action<float> DamageTaken;
        
        public void HealthInit()
        {
            MaxHealth = Data.maxHealth;
            CurrentHealth = Data.currentHealth;
            IsDead = false;
        }

        public void SetHealth(float health)
        {
            MaxHealth = Mathf.Max(1f, health);
            CurrentHealth = MaxHealth;
            IsDead = false;
            OnHealthReset();
            SetUi(MaxHealth, CurrentHealth);
        }

        public virtual void Awake()
        {
            HealthInit();
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
            IsDead = false;
            OnHealthReset();
            SetUi(MaxHealth, CurrentHealth);
        }

        protected virtual void OnHealthReset()
        {
        }

        public void TakeDamage(float damage)
        {
            if (IsDead
                || damage <= 0f
                || float.IsNaN(damage)
                || float.IsInfinity(damage))
            {
                return;
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            float appliedDamage = previousHealth - CurrentHealth;
            if (appliedDamage <= 0f)
            {
                return;
            }

            SetUi(MaxHealth, CurrentHealth);
            DamageTaken?.Invoke(appliedDamage);

            if (CurrentHealth <= 0f)
            {
                Dead();
            }
        }

        public void SetUi(float maxHealth, float currentHealth)
        {
            if (healthText != null)
            {
                healthText.text =
                    $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }

            Sequence seq = DOTween.Sequence();
            float normalizedHealth = maxHealth > 0f
                ? Mathf.Clamp01(currentHealth / maxHealth)
                : 0f;

            if (healthSlider != null)
            {
                seq.Append(healthSlider.DOValue(normalizedHealth, 0.1f).SetEase(Ease.OutCubic));
            }

            if (bgHealthSlider != null)
            {
                seq.AppendInterval(0.15f);
                seq.Append(bgHealthSlider.DOValue(normalizedHealth, 0.1f).SetEase(Ease.OutCubic));
            }
        }

        public virtual void Dead()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            DeadEvent?.Invoke();
        }
    }
}
