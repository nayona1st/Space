using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.Starfield
{
    [Serializable]
    public sealed class StarfieldLayerSettings
    {
        private const float MinimumPositiveValue = 0.0001f;

        [Header("Identity")]
        [SerializeField] private string layerName = "Starfield Layer";
        [SerializeField] private bool layerEnabled = true;

        [Header("Appearance")]
        [SerializeField] private Vector2 sizeRange =
            new Vector2(0.015f, 0.04f);
        [SerializeField] private Color minimumColor =
            new Color(0.85f, 0.93f, 1f, 0.2f);
        [SerializeField] private Color maximumColor =
            new Color(1f, 1f, 1f, 0.5f);

        [Header("Motion")]
        [Tooltip("Local X velocity authored for the reference orthographic size. Negative values move stars left.")]
        [SerializeField] private Vector2 horizontalVelocityRange =
            new Vector2(-0.3f, -0.23f);
        [SerializeField] private Vector2 lifetimeRange =
            new Vector2(70f, 90f);

        [Header("Population")]
        [Min(0f)]
        [SerializeField] private float emissionRate = 3f;
        [Min(1)]
        [SerializeField] private int maximumParticles = 200;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = -1;
        [Tooltip("Distance forward from the camera. Larger values render farther from the camera when sorting values match.")]
        [Min(0.31f)]
        [SerializeField] private float cameraDepth = 15f;

        public string LayerName => layerName;
        public bool LayerEnabled => layerEnabled;
        public Vector2 SizeRange => sizeRange;
        public Color MinimumColor => minimumColor;
        public Color MaximumColor => maximumColor;
        public Vector2 HorizontalVelocityRange =>
            horizontalVelocityRange;
        public Vector2 LifetimeRange => lifetimeRange;
        public float EmissionRate => emissionRate;
        public int MaximumParticles => maximumParticles;
        public string SortingLayerName => sortingLayerName;
        public int SortingOrder => sortingOrder;
        public float CameraDepth => cameraDepth;

        public static StarfieldLayerSettings CreateFarDefaults()
        {
            return new StarfieldLayerSettings
            {
                layerName = "StarField_Far",
                sizeRange = new Vector2(0.015f, 0.04f),
                minimumColor =
                    new Color(0.82f, 0.91f, 1f, 0.2f),
                maximumColor =
                    new Color(1f, 1f, 1f, 0.5f),
                horizontalVelocityRange =
                    new Vector2(-0.3f, -0.23f),
                lifetimeRange = new Vector2(70f, 90f),
                emissionRate = 3f,
                maximumParticles = 200,
                sortingOrder = -1,
                cameraDepth = 15f
            };
        }

        public static StarfieldLayerSettings CreateNearDefaults()
        {
            return new StarfieldLayerSettings
            {
                layerName = "StarField_Near",
                sizeRange = new Vector2(0.04f, 0.09f),
                minimumColor =
                    new Color(0.88f, 0.94f, 1f, 0.45f),
                maximumColor =
                    new Color(1f, 1f, 1f, 0.8f),
                horizontalVelocityRange =
                    new Vector2(-0.7f, -0.45f),
                lifetimeRange = new Vector2(35f, 50f),
                emissionRate = 1f,
                maximumParticles = 100,
                sortingOrder = -1,
                cameraDepth = 14.5f
            };
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                layerName = "Starfield Layer";
            }

            sizeRange.x = Mathf.Max(MinimumPositiveValue, sizeRange.x);
            sizeRange.y = Mathf.Max(sizeRange.x, sizeRange.y);

            horizontalVelocityRange.x =
                Mathf.Min(-MinimumPositiveValue, horizontalVelocityRange.x);
            horizontalVelocityRange.y =
                Mathf.Min(-MinimumPositiveValue, horizontalVelocityRange.y);
            if (horizontalVelocityRange.x > horizontalVelocityRange.y)
            {
                (horizontalVelocityRange.x, horizontalVelocityRange.y) =
                    (horizontalVelocityRange.y, horizontalVelocityRange.x);
            }

            lifetimeRange.x =
                Mathf.Max(MinimumPositiveValue, lifetimeRange.x);
            lifetimeRange.y =
                Mathf.Max(lifetimeRange.x, lifetimeRange.y);
            emissionRate = Mathf.Max(0f, emissionRate);
            maximumParticles = Mathf.Max(1, maximumParticles);
            cameraDepth = Mathf.Max(0.31f, cameraDepth);

            if (string.IsNullOrWhiteSpace(sortingLayerName))
            {
                sortingLayerName = "Default";
            }
        }
    }
}
