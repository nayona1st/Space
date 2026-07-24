using UnityEngine;

namespace Dev.CSU._02_Scripts.Planet
{
    [DisallowMultipleComponent]
    public sealed class ParallaxPlanet : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private float _initialWorldZ;
        private bool _isInitialized;

        public GameObject RootObject => gameObject;

        public bool Initialize()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_spriteRenderer == null)
            {
                return false;
            }

            _initialWorldZ = transform.position.z;
            DisablePhysics();
            _isInitialized = true;
            return true;
        }

        public void ActivateAtCameraRight(
            Camera targetCamera,
            float viewportY,
            float rightPadding)
        {
            if (!_isInitialized || targetCamera == null)
            {
                return;
            }

            gameObject.SetActive(true);

            float depth = GetCameraDepth(targetCamera);
            Vector3 viewportPosition = targetCamera.ViewportToWorldPoint(
                new Vector3(1f, viewportY, depth));

            Vector3 position = transform.position;
            position.x = viewportPosition.x + rightPadding;
            position.y = viewportPosition.y;
            position.z = _initialWorldZ;
            transform.position = position;

            Bounds bounds = _spriteRenderer.bounds;
            position = transform.position;
            position.x += viewportPosition.x + rightPadding - bounds.min.x;
            position.y += viewportPosition.y - bounds.center.y;
            transform.position = position;
        }

        public void ActivateAtViewport(
            Camera targetCamera,
            float viewportX,
            float viewportY)
        {
            if (!_isInitialized || targetCamera == null)
            {
                return;
            }

            gameObject.SetActive(true);
            AlignCenterToViewport(targetCamera, viewportX, viewportY);
        }

        public void AlignCenterToViewport(
            Camera targetCamera,
            float viewportX,
            float viewportY)
        {
            if (!_isInitialized || targetCamera == null)
            {
                return;
            }

            float depth = GetCameraDepth(targetCamera);
            Vector3 targetCenter = targetCamera.ViewportToWorldPoint(
                new Vector3(viewportX, viewportY, depth));

            Vector3 position = transform.position;
            position.x += targetCenter.x - _spriteRenderer.bounds.center.x;
            position.y += targetCenter.y - _spriteRenderer.bounds.center.y;
            position.z = _initialWorldZ;
            transform.position = position;
        }

        public void ApplyParallax(float cameraDeltaX, float parallaxFollow)
        {
            if (!_isInitialized)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x += cameraDeltaX * parallaxFollow;
            transform.position = position;
        }

        public void MaintainViewportY(Camera targetCamera, float viewportY)
        {
            if (!_isInitialized || targetCamera == null)
            {
                return;
            }

            float depth = GetCameraDepth(targetCamera);
            float targetCenterY = targetCamera.ViewportToWorldPoint(
                new Vector3(0.5f, viewportY, depth)).y;

            Vector3 position = transform.position;
            position.y += targetCenterY - _spriteRenderer.bounds.center.y;
            transform.position = position;
        }

        public bool IsCompletelyLeftOf(Camera targetCamera, float leftPadding)
        {
            if (!_isInitialized || targetCamera == null)
            {
                return false;
            }

            float depth = GetCameraDepth(targetCamera);
            float cameraLeftEdge = targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0.5f, depth)).x;

            return _spriteRenderer.bounds.max.x < cameraLeftEdge - leftPadding;
        }

        public void ReturnToPool()
        {
            gameObject.SetActive(false);
        }

        private float GetCameraDepth(Camera targetCamera)
        {
            float depth = Vector3.Dot(
                transform.position - targetCamera.transform.position,
                targetCamera.transform.forward);

            if (depth > 0f)
            {
                return depth;
            }

            return Mathf.Abs(transform.position.z - targetCamera.transform.position.z);
        }

        private void DisablePhysics()
        {
            Collider2D[] colliders2D = GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders2D.Length; index++)
            {
                colliders2D[index].enabled = false;
            }

            Rigidbody2D[] rigidbodies2D = GetComponentsInChildren<Rigidbody2D>(true);
            for (int index = 0; index < rigidbodies2D.Length; index++)
            {
                rigidbodies2D[index].simulated = false;
            }

            Collider[] colliders3D = GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders3D.Length; index++)
            {
                colliders3D[index].enabled = false;
            }

            Rigidbody[] rigidbodies3D = GetComponentsInChildren<Rigidbody>(true);
            for (int index = 0; index < rigidbodies3D.Length; index++)
            {
                rigidbodies3D[index].isKinematic = true;
                rigidbodies3D[index].detectCollisions = false;
            }
        }
    }
}
