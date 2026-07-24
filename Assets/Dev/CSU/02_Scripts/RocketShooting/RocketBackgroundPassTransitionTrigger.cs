using Dev.CSU._02_Scripts.SceneTransition;
using UnityEngine;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    [DisallowMultipleComponent]
    public sealed class RocketBackgroundPassTransitionTrigger : MonoBehaviour
    {
        private const string DefaultTargetSceneName = "Seong Uk";

        [Header("Transition")]
        [Tooltip("Allows the background pass condition to request a scene transition.")]
        [SerializeField] private bool transitionEnabled = true;

        [Tooltip("Number of recycled sky backgrounds required before transitioning.")]
        [Min(1)]
        [SerializeField] private int requiredBackgroundPasses = 3;

        [Tooltip("Scene loaded after the launch reaches Cruise and the pass threshold.")]
        [SerializeField] private string targetSceneName = DefaultTargetSceneName;

        [Header("References")]
        [Tooltip("Background scroller that reports each recycled sky tile.")]
        [SerializeField] private VerticalBackgroundScroller backgroundScroller;

        [Tooltip("Launch director that reports the current launch phase.")]
        [SerializeField] private RocketShootingDirector launchDirector;

        private ISceneTransitionService _transitionService;
        private bool _transitionRequestIssued;

        private void OnEnable()
        {
            if (backgroundScroller != null)
            {
                backgroundScroller.BackgroundPassed +=
                    HandleBackgroundPassed;
            }

            if (launchDirector != null)
            {
                launchDirector.PhaseChanged += HandlePhaseChanged;
            }

            TryRequestTransition();
        }

        private void OnDisable()
        {
            if (backgroundScroller != null)
            {
                backgroundScroller.BackgroundPassed -=
                    HandleBackgroundPassed;
            }

            if (launchDirector != null)
            {
                launchDirector.PhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandleBackgroundPassed(int passedBackgroundCount)
        {
            TryRequestTransition();
        }

        private void HandlePhaseChanged(LaunchPhase phase)
        {
            TryRequestTransition();
        }

        private void TryRequestTransition()
        {
            if (_transitionRequestIssued
                || !transitionEnabled
                || backgroundScroller == null
                || launchDirector == null
                || launchDirector.Phase != LaunchPhase.Cruise
                || backgroundScroller.PassedBackgroundCount
                    < requiredBackgroundPasses)
            {
                return;
            }

            if (_transitionService == null
                && !SceneTransitions.TryGetService(
                    out _transitionService))
            {
                return;
            }

            if (_transitionService.TryLoadScene(targetSceneName))
            {
                _transitionRequestIssued = true;
            }
        }

        private void Reset()
        {
            backgroundScroller =
                GetComponentInChildren<VerticalBackgroundScroller>(true);
            launchDirector = GetComponent<RocketShootingDirector>();
        }

        private void OnValidate()
        {
            requiredBackgroundPasses =
                Mathf.Max(1, requiredBackgroundPasses);
        }
    }
}
