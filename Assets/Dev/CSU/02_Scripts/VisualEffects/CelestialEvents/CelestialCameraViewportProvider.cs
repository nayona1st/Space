using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    public readonly struct CelestialViewport
    {
        public CelestialViewport(float halfWidth, float halfHeight)
        {
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
        }

        public float HalfWidth { get; }
        public float HalfHeight { get; }
    }

    internal sealed class CelestialCameraViewportProvider
    {
        private readonly Camera _camera;

        public CelestialCameraViewportProvider(Camera camera)
        {
            _camera = camera;
        }

        public bool TryGetViewport(out CelestialViewport viewport)
        {
            viewport = default;
            if (_camera == null
                || !_camera.isActiveAndEnabled
                || !_camera.orthographic)
            {
                return false;
            }

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            if (halfWidth <= Mathf.Epsilon
                || halfHeight <= Mathf.Epsilon)
            {
                return false;
            }

            viewport = new CelestialViewport(
                halfWidth,
                halfHeight);
            return true;
        }
    }
}
