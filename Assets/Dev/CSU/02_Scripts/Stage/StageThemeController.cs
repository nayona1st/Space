using System;
using Dev.CSU._02_Scripts.Planet;
using Dev.CSU._02_Scripts.SceneTransition;
using UnityEngine;

namespace Dev.CSU._02_Scripts.Stage
{
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public sealed class StageThemeController : MonoBehaviour
    {
        private const string DefaultEndingSceneName = "Ending";

        [Tooltip("Stage presentation controller that reports completion and starts the next configured stage.")]
        [SerializeField] private PlanetParallaxController planetController;

        [Header("Run Completion")]
        [Tooltip("Scene loaded through the shared fade transition after the final configured stage completes.")]
        [SerializeField] private string endingSceneName =
            DefaultEndingSceneName;

        private bool _warnedMissingController;
        private bool _endingTransitionRequested;

        public int CurrentStageNumber { get; private set; }
        public bool EndingTransitionRequested =>
            _endingTransitionRequested;

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
            if (!planetController.HasStage(nextStageNumber))
            {
                RequestEndingTransition(completedStageNumber);
                return;
            }

            CurrentStageNumber = nextStageNumber;
            StageBackgroundReady?.Invoke(nextStageNumber);
            planetController.StartStage(nextStageNumber);
        }

        private void RequestEndingTransition(int completedStageNumber)
        {
            if (_endingTransitionRequested)
            {
                return;
            }

            if (SceneTransitions.TryLoadScene(endingSceneName))
            {
                _endingTransitionRequested = true;
                return;
            }

            Debug.LogError(
                $"{nameof(StageThemeController)} on '{name}' completed final "
                + $"stage {completedStageNumber}, but the shared scene "
                + $"transition service could not load '{endingSceneName}'.",
                this);
        }

        private void WarnMissingControllerOnce()
        {
            if (_warnedMissingController) return;

            Debug.LogWarning($"{nameof(StageThemeController)} on '{name}' is inactive: " + "Planet Parallax Controller is not assigned.", this);
            _warnedMissingController = true;
        }

        private void OnValidate()
        {
            endingSceneName = string.IsNullOrWhiteSpace(endingSceneName)
                ? DefaultEndingSceneName
                : endingSceneName.Trim();
        }
    }
}
