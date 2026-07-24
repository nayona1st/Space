using System;
using Dev.CSU._02_Scripts.Planet;
using UnityEngine;

namespace Dev.CSU._02_Scripts.Stage
{
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public sealed class StageThemeController : MonoBehaviour
    {
        [Tooltip("Stage presentation controller that reports completion and starts the next configured stage.")]
        [SerializeField] private PlanetParallaxController planetController;

        private bool _warnedMissingController;

        public int CurrentStageNumber { get; private set; }

        public event Action<int> StageBackgroundReady;

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            if (planetController == null)
            {
                WarnMissingControllerOnce();
                return;
            }

            CurrentStageNumber = Mathf.Max(1, planetController.CurrentStageNumber);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (planetController != null)
            {
                planetController.StageCompleted += HandleStageCompleted;
            }
        }

        private void UnsubscribeEvents()
        {
            if (planetController != null)
            {
                planetController.StageCompleted -= HandleStageCompleted;
            }
        }

        private void HandleStageCompleted(int completedStageNumber)
        {
            if (planetController == null || completedStageNumber != CurrentStageNumber)
            {
                return;
            }

            int nextStageNumber = completedStageNumber + 1;
            
            if (!planetController.HasStage(nextStageNumber)) return;
            

            CurrentStageNumber = nextStageNumber;
            StageBackgroundReady?.Invoke(nextStageNumber);
            planetController.StartStage(nextStageNumber);
        }

        private void WarnMissingControllerOnce()
        {
            if (_warnedMissingController) return;
            

            Debug.LogWarning($"{nameof(StageThemeController)} on '{name}' is inactive: " + "Planet Parallax Controller is not assigned.", this);
            _warnedMissingController = true;
        }
    }
}
