using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.Starfield
{
    internal readonly struct StarfieldViewport
    {
        public StarfieldViewport(
            float width,
            float height,
            float orthographicSize,
            float aspect)
        {
            Width = width;
            Height = height;
            OrthographicSize = orthographicSize;
            Aspect = aspect;
        }

        public float Width { get; }
        public float Height { get; }
        public float OrthographicSize { get; }
        public float Aspect { get; }

        public bool ApproximatelyEquals(StarfieldViewport other)
        {
            return Mathf.Approximately(Width, other.Width)
                && Mathf.Approximately(Height, other.Height)
                && Mathf.Approximately(
                    OrthographicSize,
                    other.OrthographicSize)
                && Mathf.Approximately(Aspect, other.Aspect);
        }
    }

    internal sealed class OrthographicCameraViewportProvider
    {
        private readonly Camera _camera;

        public OrthographicCameraViewportProvider(Camera camera)
        {
            _camera = camera;
        }

        public bool TryGetViewport(out StarfieldViewport viewport)
        {
            viewport = default;
            if (_camera == null
                || !_camera.isActiveAndEnabled
                || !_camera.orthographic)
            {
                return false;
            }

            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            if (width <= Mathf.Epsilon || height <= Mathf.Epsilon)
            {
                return false;
            }

            viewport = new StarfieldViewport(
                width,
                height,
                _camera.orthographicSize,
                _camera.aspect);
            return true;
        }
    }
}
