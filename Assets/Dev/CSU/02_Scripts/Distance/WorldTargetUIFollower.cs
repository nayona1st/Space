using UnityEngine;
using UnityEngine.Serialization;

namespace Dev.CSU.Scripts.Distance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldTargetUIFollower : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        [Tooltip("World object that this UI element follows.")]
        private Transform target;

        [SerializeField]
        [FormerlySerializedAs("worldOffset")]
        [Tooltip("Local-space offset from the target. Positive Y places the UI above its tilted direction.")]
        private Vector3 localOffset = new Vector3(0f, 3f, 0f);

        [Header("Camera")]
        [SerializeField]
        [Tooltip("Camera used to convert the target position to screen space. Uses the Main Camera when empty.")]
        private Camera targetCamera;

        [SerializeField]
        [Tooltip("Rounds the UI position to whole screen pixels to keep pixel fonts crisp and stable.")]
        private bool snapToPixels = true;

        [Header("Rotation")]
        [SerializeField]
        [Tooltip("Rotates the entire UI element by the target's tilt relative to its starting rotation.")]
        private bool matchTargetTilt = true;

        private RectTransform _rectTransform;
        private Vector3 _initialUiLocalEulerAngles;
        private float _initialTargetRotationZ;
        private bool _hasRotationBaseline;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ResolveCamera();
            CaptureRotationBaseline();
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += UpdatePosition;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= UpdatePosition;
        }

        private void UpdatePosition()
        {
            if (target == null)
            {
                return;
            }

            ResolveCamera();
            if (targetCamera == null)
            {
                return;
            }

            CaptureRotationBaseline();

            Vector3 worldPosition = target.TransformPoint(localOffset);
            Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z < 0f)
            {
                return;
            }

            if (snapToPixels)
            {
                screenPosition.x = Mathf.Round(screenPosition.x);
                screenPosition.y = Mathf.Round(screenPosition.y);
            }

            _rectTransform.position = new Vector3(
                screenPosition.x,
                screenPosition.y,
                _rectTransform.position.z);

            ApplyTargetTilt();
        }

        private void CaptureRotationBaseline()
        {
            if (_hasRotationBaseline || target == null || _rectTransform == null)
            {
                return;
            }

            _initialTargetRotationZ = target.eulerAngles.z;
            _initialUiLocalEulerAngles = _rectTransform.localEulerAngles;
            _hasRotationBaseline = true;
        }

        private void ApplyTargetTilt()
        {
            if (!_hasRotationBaseline)
            {
                return;
            }

            float targetTilt = matchTargetTilt
                ? Mathf.DeltaAngle(_initialTargetRotationZ, target.eulerAngles.z)
                : 0f;

            _rectTransform.localRotation = Quaternion.Euler(
                _initialUiLocalEulerAngles.x,
                _initialUiLocalEulerAngles.y,
                _initialUiLocalEulerAngles.z + targetTilt);
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
    }
}
