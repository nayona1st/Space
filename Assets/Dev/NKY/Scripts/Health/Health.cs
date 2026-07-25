using Dev.CSU._02_Scripts.Distance;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dev.NKY.Scripts.Health
{
    public class Health : DamageTask
    {
        private const float MetersPerResource = 1000f;

        [Header("Death Reward")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private HorizontalDistanceTracker distanceTracker;

        private bool _deathRewardGranted;

        public int LastDeathReward { get; private set; }

        public override void Awake()
        {
            base.Awake();

            if (resourceManager == null)
            {
                resourceManager = FindFirstObjectByType<ResourceManager>();
            }

            if (distanceTracker == null)
            {
                distanceTracker = GetComponent<HorizontalDistanceTracker>();
            }
        }

        private void OnEnable()
        {
            DeadEvent += HandleDeathReward;
        }

        private void OnDisable()
        {
            DeadEvent -= HandleDeathReward;
        }

        private void Update()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TakeDamage(2);
            }
        }

        public int CalculateDeathReward()
        {
            if (distanceTracker == null)
            {
                return 0;
            }

            return Mathf.FloorToInt(
                Mathf.Max(0f, distanceTracker.DistanceMeters)
                / MetersPerResource);
        }

        protected override void OnHealthReset()
        {
            _deathRewardGranted = false;
            LastDeathReward = 0;
        }

        private void HandleDeathReward()
        {
            if (_deathRewardGranted)
            {
                return;
            }

            _deathRewardGranted = true;
            LastDeathReward = CalculateDeathReward();

            if (resourceManager == null)
            {
                Debug.LogError(
                    "[Health] Cannot grant the death reward because no ResourceManager is available.",
                    this);
                return;
            }

            resourceManager.AddResource(LastDeathReward);
            Debug.Log(
                $"[Health] Death reward granted once: {LastDeathReward} "
                + $"(distance: {distanceTracker?.DistanceMeters ?? 0f:F1} m, "
                + $"balance: {resourceManager.CurrentResource}).",
                this);
        }
    }
}
