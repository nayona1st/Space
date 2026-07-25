using UnityEngine;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    [DisallowMultipleComponent]
    public sealed class RocketView : MonoBehaviour
    {
        [Header("Hierarchy")]
        [Tooltip("Transform whose world position is controlled by the launch sequence.")]
        [SerializeField] private Transform motionRoot;

        [Tooltip("Child transform that receives presentation-only shake and bob offsets.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("World-space marker for the rocket's initial vertical position.")]
        [SerializeField] private Transform launchStart;

        [Tooltip("World-space marker at which the rocket remains during cruise.")]
        [SerializeField] private Transform cruiseAnchor;

        [Header("Visual References")]
        [Tooltip("Main rocket body renderer. The launch code never animates its transform.")]
        [SerializeField] private SpriteRenderer bodyRenderer;

        [Tooltip("Root object containing the exhaust SpriteRenderer and Animator.")]
        [SerializeField] private GameObject exhaustRoot;

        [Tooltip("Additional exhaust objects that follow the primary exhaust active state.")]
        [SerializeField] private GameObject[] additionalExhaustRoots;

        [Header("Ignition Motion")]
        [Tooltip("Maximum local-space shake distance during ignition.")]
        [Min(0f)]
        [SerializeField] private float ignitionShakeAmplitude = 0.08f;

        [Tooltip("Ignition shake cycles per second.")]
        [Min(0f)]
        [SerializeField] private float ignitionShakeFrequency = 24f;

        [Header("Cruise Motion")]
        [Tooltip("Maximum local-space vertical bob distance during cruise.")]
        [Min(0f)]
        [SerializeField] private float cruiseBobAmplitude = 0.05f;

        [Tooltip("Cruise bob cycles per second.")]
        [Min(0f)]
        [SerializeField] private float cruiseBobFrequency = 1.5f;

        private Vector3 _visualBaseLocalPosition;
        private float _fixedMotionX;
        private float _fixedMotionZ;
        private float _launchY;
        private float _cruiseY;
        private float _presentationElapsed;
        private LaunchPhase _presentationPhase = LaunchPhase.Idle;
        private bool _isInitialized;

        public bool IsConfigured =>
            motionRoot != null
            && visualRoot != null
            && visualRoot != motionRoot
            && launchStart != null
            && cruiseAnchor != null
            && bodyRenderer != null
            && exhaustRoot != null;

        public SpriteRenderer BodyRenderer => bodyRenderer;
        public GameObject ExhaustRoot => exhaustRoot;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void LateUpdate()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _presentationElapsed += Mathf.Max(0f, Time.deltaTime);
            visualRoot.localPosition =
                _visualBaseLocalPosition + CalculatePresentationOffset();
        }

        private void OnDisable()
        {
            ResetVisualOffset();
        }

        public void ResetToIdle()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _fixedMotionX = motionRoot.position.x;
            _fixedMotionZ = motionRoot.position.z;
            _launchY = launchStart.position.y;
            _cruiseY = cruiseAnchor.position.y;

            SetMotionRootY(_launchY);
            SetExhaustActive(false);
            SetPresentationPhase(LaunchPhase.Idle);
        }

        public void SetLiftProgress(
            float normalizedProgress,
            AnimationCurve movementCurve)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            float time = Mathf.Clamp01(normalizedProgress);
            float curvedProgress = movementCurve != null
                ? movementCurve.Evaluate(time)
                : time;

            SetMotionRootY(
                Mathf.Lerp(_launchY, _cruiseY, Mathf.Clamp01(curvedProgress)));
        }

        public void SnapToCruiseAnchor()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _cruiseY = cruiseAnchor.position.y;
            SetMotionRootY(_cruiseY);
        }

        public void SetExhaustActive(bool isActive)
        {
            SetActiveIfNeeded(exhaustRoot, isActive);

            if (additionalExhaustRoots == null)
            {
                return;
            }

            foreach (GameObject additionalRoot in additionalExhaustRoots)
            {
                if (additionalRoot == exhaustRoot)
                {
                    continue;
                }

                SetActiveIfNeeded(additionalRoot, isActive);
            }
        }

        public void SetPresentationPhase(LaunchPhase phase)
        {
            _presentationPhase = phase;
            _presentationElapsed = 0f;

            SetExhaustActive(phase != LaunchPhase.Idle);

            if (phase == LaunchPhase.Idle)
            {
                ResetVisualOffset();
            }
        }

        private bool EnsureInitialized()
        {
            if (_isInitialized)
            {
                return true;
            }

            if (!IsConfigured)
            {
                return false;
            }

            _visualBaseLocalPosition = visualRoot.localPosition;
            _fixedMotionX = motionRoot.position.x;
            _fixedMotionZ = motionRoot.position.z;
            _launchY = launchStart.position.y;
            _cruiseY = cruiseAnchor.position.y;
            _isInitialized = true;
            return true;
        }

        private void SetMotionRootY(float worldY)
        {
            Vector3 position = motionRoot.position;
            position.x = _fixedMotionX;
            position.y = worldY;
            position.z = _fixedMotionZ;
            motionRoot.position = position;
        }

        private static void SetActiveIfNeeded(
            GameObject target,
            bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }

        private Vector3 CalculatePresentationOffset()
        {
            float fullCycle = Mathf.PI * 2f;

            switch (_presentationPhase)
            {
                case LaunchPhase.Ignition:
                {
                    float phase =
                        _presentationElapsed * ignitionShakeFrequency * fullCycle;
                    return new Vector3(
                        Mathf.Sin(phase) * ignitionShakeAmplitude,
                        Mathf.Sin(phase * 1.37f)
                            * ignitionShakeAmplitude
                            * 0.5f,
                        0f);
                }

                case LaunchPhase.LiftOff:
                {
                    float phase =
                        _presentationElapsed * ignitionShakeFrequency * fullCycle;
                    return new Vector3(
                        Mathf.Sin(phase) * ignitionShakeAmplitude * 0.35f,
                        Mathf.Sin(phase * 1.37f)
                            * ignitionShakeAmplitude
                            * 0.2f,
                        0f);
                }

                case LaunchPhase.Cruise:
                {
                    float phase =
                        _presentationElapsed * cruiseBobFrequency * fullCycle;
                    return new Vector3(
                        Mathf.Sin(phase * 0.5f)
                            * cruiseBobAmplitude
                            * 0.25f,
                        Mathf.Sin(phase) * cruiseBobAmplitude,
                        0f);
                }

                default:
                    return Vector3.zero;
            }
        }

        private void ResetVisualOffset()
        {
            if (visualRoot != null && _isInitialized)
            {
                visualRoot.localPosition = _visualBaseLocalPosition;
            }
        }

        private void OnValidate()
        {
            ignitionShakeAmplitude = Mathf.Max(0f, ignitionShakeAmplitude);
            ignitionShakeFrequency = Mathf.Max(0f, ignitionShakeFrequency);
            cruiseBobAmplitude = Mathf.Max(0f, cruiseBobAmplitude);
            cruiseBobFrequency = Mathf.Max(0f, cruiseBobFrequency);
        }
    }
}
