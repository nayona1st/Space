using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    public enum LaunchPhase
    {
        Idle,
        Ignition,
        LiftOff,
        Cruise
    }

    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class RocketShootingDirector : MonoBehaviour
    {
        private const int MaxPhaseStepsPerFrame = 4;

        [Header("References")]
        [Tooltip("Rocket presentation controlled by this launch sequence.")]
        [SerializeField] private RocketView rocketView;

        [Tooltip("Vertical world scroller driven by the launch speed.")]
        [SerializeField] private VerticalBackgroundScroller backgroundScroller;

        [Header("Sequence")]
        [Tooltip("Starts the launch automatically when the scene begins.")]
        [SerializeField] private bool autoStart;

        [Tooltip("Time spent playing the ignition presentation before lift-off.")]
        [Min(0f)]
        [SerializeField] private float ignitionDuration = 0.75f;

        [Tooltip("Time taken for the rocket to move from Launch Start to Cruise Anchor.")]
        [Min(0f)]
        [SerializeField] private float liftOffDuration = 2f;

        [Tooltip("Background speed, in world units per second, after lift-off finishes.")]
        [Min(0f)]
        [SerializeField] private float cruiseSpeed = 8f;

        [Tooltip("Normalized vertical rocket movement during lift-off.")]
        [SerializeField] private AnimationCurve liftCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Normalized background acceleration during lift-off.")]
        [SerializeField] private AnimationCurve speedCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float _phaseElapsed;
        private float _currentScrollSpeed;
        private bool _launchStarted;
        private bool _warnedInvalidConfiguration;

        public LaunchPhase Phase { get; private set; } = LaunchPhase.Idle;
        public double Altitude { get; private set; }
        public float CurrentScrollSpeed => _currentScrollSpeed;

        public event Action<LaunchPhase> PhaseChanged;

        private void Awake()
        {
            Phase = LaunchPhase.Idle;
            Altitude = 0d;
            _phaseElapsed = 0f;
            _currentScrollSpeed = 0f;
            _launchStarted = false;

            if (rocketView != null)
            {
                rocketView.ResetToIdle();
            }

            if (backgroundScroller != null)
            {
                backgroundScroller.SetScrollSpeed(0f);
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                BeginLaunch();
            }
        }

        private void Update()
        {
            float deltaTime = Mathf.Max(0f, Time.deltaTime);
            double frameDistance = AdvanceSequence(deltaTime);

            if (backgroundScroller != null)
            {
                float effectiveFrameSpeed = deltaTime > 0f
                    ? (float)(frameDistance / deltaTime)
                    : 0f;
                backgroundScroller.SetScrollSpeed(effectiveFrameSpeed);
            }

            Altitude += frameDistance;
        }

        private void OnDisable()
        {
            if (backgroundScroller != null)
            {
                backgroundScroller.SetScrollSpeed(0f);
            }
        }

        public void BeginLaunch()
        {
            if (_launchStarted || Phase != LaunchPhase.Idle)
            {
                return;
            }

            if (!HasValidConfiguration())
            {
                WarnInvalidConfigurationOnce();
                return;
            }

            _warnedInvalidConfiguration = false;
            _launchStarted = true;
            Altitude = 0d;
            _currentScrollSpeed = 0f;

            backgroundScroller.ResetPassedBackgroundCount();
            rocketView.ResetToIdle();
            TransitionTo(LaunchPhase.Ignition);
        }

        private double AdvanceSequence(float deltaTime)
        {
            float remainingDelta = deltaTime;
            double frameDistance = 0d;

            for (int step = 0;
                 step < MaxPhaseStepsPerFrame && remainingDelta > 0f;
                 step++)
            {
                switch (Phase)
                {
                    case LaunchPhase.Idle:
                        _currentScrollSpeed = 0f;
                        remainingDelta = 0f;
                        break;

                    case LaunchPhase.Ignition:
                        ConsumeIgnitionTime(ref remainingDelta);
                        break;

                    case LaunchPhase.LiftOff:
                        frameDistance +=
                            ConsumeLiftOffTime(ref remainingDelta);
                        break;

                    case LaunchPhase.Cruise:
                        frameDistance += ConsumeCruiseTime(remainingDelta);
                        remainingDelta = 0f;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return Math.Max(0d, frameDistance);
        }

        private void ConsumeIgnitionTime(ref float remainingDelta)
        {
            _currentScrollSpeed = 0f;

            if (ignitionDuration <= 0f)
            {
                EnterLiftOff();
                return;
            }

            float phaseRemaining =
                Mathf.Max(0f, ignitionDuration - _phaseElapsed);
            if (phaseRemaining <= 0f)
            {
                EnterLiftOff();
                return;
            }

            float consumedDelta = Mathf.Min(remainingDelta, phaseRemaining);
            _phaseElapsed += consumedDelta;
            remainingDelta =
                Mathf.Max(0f, remainingDelta - consumedDelta);

            if (consumedDelta >= phaseRemaining)
            {
                EnterLiftOff();
            }
        }

        private void EnterLiftOff()
        {
            TransitionTo(LaunchPhase.LiftOff);
            rocketView.SetLiftProgress(0f, liftCurve);
            _currentScrollSpeed =
                cruiseSpeed * EvaluateNormalizedCurve(speedCurve, 0f);
        }

        private double ConsumeLiftOffTime(ref float remainingDelta)
        {
            if (liftOffDuration <= 0f)
            {
                rocketView.SetLiftProgress(1f, liftCurve);
                rocketView.SnapToCruiseAnchor();
                _currentScrollSpeed = cruiseSpeed;
                TransitionTo(LaunchPhase.Cruise);
                return 0d;
            }

            float phaseRemaining =
                Mathf.Max(0f, liftOffDuration - _phaseElapsed);
            if (phaseRemaining <= 0f)
            {
                rocketView.SnapToCruiseAnchor();
                _currentScrollSpeed = cruiseSpeed;
                TransitionTo(LaunchPhase.Cruise);
                return 0d;
            }

            float consumedDelta = Mathf.Min(remainingDelta, phaseRemaining);
            float startNormalizedTime =
                Mathf.Clamp01(_phaseElapsed / liftOffDuration);
            float endElapsed = Mathf.Min(
                liftOffDuration,
                _phaseElapsed + consumedDelta);
            float endNormalizedTime =
                Mathf.Clamp01(endElapsed / liftOffDuration);

            float startSpeed = cruiseSpeed
                * EvaluateNormalizedCurve(
                    speedCurve,
                    startNormalizedTime);
            float endSpeed = cruiseSpeed
                * EvaluateNormalizedCurve(
                    speedCurve,
                    endNormalizedTime);

            double travelledDistance =
                (double)(startSpeed + endSpeed) * 0.5d * consumedDelta;

            _phaseElapsed = endElapsed;
            remainingDelta =
                Mathf.Max(0f, remainingDelta - consumedDelta);
            _currentScrollSpeed = endSpeed;
            rocketView.SetLiftProgress(endNormalizedTime, liftCurve);

            if (consumedDelta >= phaseRemaining)
            {
                rocketView.SnapToCruiseAnchor();
                _currentScrollSpeed = cruiseSpeed;
                TransitionTo(LaunchPhase.Cruise);
            }

            return travelledDistance;
        }

        private double ConsumeCruiseTime(float deltaTime)
        {
            _currentScrollSpeed = cruiseSpeed;
            rocketView.SnapToCruiseAnchor();
            return (double)cruiseSpeed * deltaTime;
        }

        private void TransitionTo(LaunchPhase nextPhase)
        {
            if (Phase == nextPhase)
            {
                return;
            }

            Phase = nextPhase;
            _phaseElapsed = 0f;
            rocketView.SetPresentationPhase(nextPhase);
            PhaseChanged?.Invoke(nextPhase);
        }

        private bool HasValidConfiguration()
        {
            return rocketView != null
                && rocketView.isActiveAndEnabled
                && rocketView.IsConfigured
                && backgroundScroller != null
                && backgroundScroller.isActiveAndEnabled
                && backgroundScroller.IsConfigured;
        }

        private void WarnInvalidConfigurationOnce()
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(RocketShootingDirector)} on '{name}' could not begin "
                + "the launch because Rocket View or Background Scroller is not "
                + "fully configured.",
                this);
            _warnedInvalidConfiguration = true;
        }

        private static float EvaluateNormalizedCurve(
            AnimationCurve curve,
            float normalizedTime)
        {
            float value = curve != null
                ? curve.Evaluate(Mathf.Clamp01(normalizedTime))
                : normalizedTime;
            return Mathf.Clamp01(value);
        }

        private void OnValidate()
        {
            ignitionDuration = Mathf.Max(0f, ignitionDuration);
            liftOffDuration = Mathf.Max(0f, liftOffDuration);
            cruiseSpeed = Mathf.Max(0f, cruiseSpeed);

            if (liftCurve == null || liftCurve.length == 0)
            {
                liftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (speedCurve == null || speedCurve.length == 0)
            {
                speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
}
