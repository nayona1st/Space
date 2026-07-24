using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    [Serializable]
    public sealed class CelestialTrailSettings
    {
        private const float MinimumPositiveValue = 0.0001f;

        [SerializeField] private bool enabled = true;
        [SerializeField] private Material material;
        [SerializeField] private Vector2 timeRange =
            new Vector2(0.25f, 0.7f);
        [SerializeField] private AnimationCurve widthCurve;
        [Min(MinimumPositiveValue)]
        [SerializeField] private float widthMultiplier = 0.18f;
        [Min(MinimumPositiveValue)]
        [SerializeField] private float minimumVertexDistance = 0.08f;
        [SerializeField] private Gradient colorGradient;
        [ColorUsage(true, true)]
        [SerializeField] private Color emissionColor = Color.white;
        [SerializeField] private Vector2 emissionStrengthRange =
            new Vector2(4f, 7f);

        public bool Enabled => enabled;
        public Material Material => material;
        public Vector2 TimeRange => timeRange;
        public AnimationCurve WidthCurve => widthCurve;
        public float WidthMultiplier => widthMultiplier;
        public float MinimumVertexDistance => minimumVertexDistance;
        public Gradient ColorGradient => colorGradient;
        public Color EmissionColor => emissionColor;
        public Vector2 EmissionStrengthRange =>
            emissionStrengthRange;

        public static CelestialTrailSettings
            CreateShootingStarCoreDefaults()
        {
            return new CelestialTrailSettings
            {
                enabled = true,
                timeRange = new Vector2(0.25f, 0.7f),
                widthMultiplier = 0.18f,
                minimumVertexDistance = 0.08f,
                colorGradient = CreateGradient(
                    new Color(0.92f, 0.98f, 1f, 0.95f),
                    new Color(0.66f, 0.88f, 1f, 0f)),
                emissionColor =
                    new Color(0.85f, 0.96f, 1f, 1f),
                emissionStrengthRange = new Vector2(4f, 7f)
            };
        }

        public static CelestialTrailSettings
            CreateDisabledOuterDefaults()
        {
            return new CelestialTrailSettings
            {
                enabled = false,
                timeRange = new Vector2(0.25f, 0.7f),
                widthMultiplier = 0.28f,
                minimumVertexDistance = 0.1f,
                colorGradient = CreateGradient(
                    new Color(0.75f, 0.93f, 1f, 0.35f),
                    new Color(0.4f, 0.75f, 1f, 0f)),
                emissionColor =
                    new Color(0.45f, 0.82f, 1f, 1f),
                emissionStrengthRange = new Vector2(2f, 4f)
            };
        }

        public static CelestialTrailSettings
            CreateCometCoreDefaults()
        {
            return new CelestialTrailSettings
            {
                enabled = true,
                timeRange = new Vector2(1.5f, 3f),
                widthMultiplier = 0.34f,
                minimumVertexDistance = 0.12f,
                colorGradient = CreateGradient(
                    HexColor("BDEFFF", 0.95f),
                    HexColor("67CFFF", 0f)),
                emissionColor = HexColor("BDEFFF", 1f),
                emissionStrengthRange = new Vector2(6f, 10f)
            };
        }

        public static CelestialTrailSettings
            CreateCometOuterDefaults()
        {
            return new CelestialTrailSettings
            {
                enabled = true,
                timeRange = new Vector2(1.8f, 3f),
                widthMultiplier = 0.82f,
                minimumVertexDistance = 0.18f,
                colorGradient = CreateGradient(
                    HexColor("67CFFF", 0.42f),
                    HexColor("67CFFF", 0f)),
                emissionColor = HexColor("67CFFF", 1f),
                emissionStrengthRange = new Vector2(2f, 5f)
            };
        }

        public void Validate()
        {
            timeRange.x = Mathf.Max(
                MinimumPositiveValue,
                timeRange.x);
            timeRange.y = Mathf.Max(timeRange.x, timeRange.y);
            widthMultiplier = Mathf.Max(
                MinimumPositiveValue,
                widthMultiplier);
            minimumVertexDistance = Mathf.Max(
                MinimumPositiveValue,
                minimumVertexDistance);
            emissionStrengthRange.x = Mathf.Max(
                0f,
                emissionStrengthRange.x);
            emissionStrengthRange.y = Mathf.Max(
                emissionStrengthRange.x,
                emissionStrengthRange.y);

            widthCurve ??=
                AnimationCurve.Linear(0f, 1f, 1f, 0f);
            colorGradient ??= CreateWhiteFadeGradient();
        }

        private static Gradient CreateWhiteFadeGradient()
        {
            return CreateGradient(
                new Color(1f, 1f, 1f, 0.9f),
                new Color(0.7f, 0.9f, 1f, 0f));
        }

        private static Gradient CreateGradient(
            Color headColor,
            Color tailColor)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(headColor, 0f),
                    new GradientColorKey(tailColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(headColor.a, 0f),
                    new GradientAlphaKey(
                        Mathf.Min(headColor.a, 0.5f),
                        0.45f),
                    new GradientAlphaKey(tailColor.a, 1f)
                });
            return gradient;
        }

        private static Color HexColor(string html, float alpha)
        {
            ColorUtility.TryParseHtmlString(
                $"#{html}",
                out Color color);
            color.a = alpha;
            return color;
        }
    }
}
