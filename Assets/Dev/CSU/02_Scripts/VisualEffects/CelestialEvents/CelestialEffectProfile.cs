using System;
using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    [Serializable]
    public sealed class CelestialEffectProfile
    {
        private const float MinimumPositiveValue = 0.0001f;

        [Header("Identity")]
        [SerializeField] private string displayName =
            "Celestial Effect";
        [SerializeField] private CelestialEffectType effectType;
        [SerializeField] private bool enabled = true;
        [SerializeField] private CelestialEffectPresenter prefab;

        [Header("Schedule")]
        [Tooltip("Seconds between probability checks.")]
        [SerializeField] private Vector2 checkIntervalRange =
            new Vector2(3f, 8f);
        [Range(0f, 1f)]
        [SerializeField] private float spawnProbability = 0.12f;
        [Min(1)]
        [SerializeField] private int maximumActive = 2;
        [Min(1)]
        [SerializeField] private int poolCapacity = 3;

        [Header("Camera-relative Spawn")]
        [SerializeField] private CelestialSpawnEdgeMode spawnEdgeMode =
            CelestialSpawnEdgeMode.RandomRightOrTop;
        [Tooltip("0 is the bottom and 1 is the top of the camera viewport.")]
        [SerializeField] private Vector2 normalizedVerticalStartRange =
            new Vector2(0.2f, 1f);
        [Tooltip("0 is the left and 1 is the right of the camera viewport.")]
        [SerializeField] private Vector2 normalizedTopStartRange =
            new Vector2(0.35f, 1f);

        [Header("Motion")]
        [Tooltip("Downward distance per one unit of leftward distance.")]
        [SerializeField] private Vector2 downwardSlopeRange =
            new Vector2(0.35f, 0.75f);
        [Tooltip("Desired time from off-screen spawn to off-screen exit.")]
        [SerializeField] private Vector2 crossingDurationRange =
            new Vector2(0.8f, 1.8f);
        [SerializeField] private Vector2 scaleRange =
            new Vector2(0.8f, 1.25f);

        [Header("Head")]
        [SerializeField] private Sprite[] headSprites;
        [SerializeField] private Material headMaterial;
        [SerializeField] private Color minimumHeadColor =
            new Color(0.9f, 0.97f, 1f, 1f);
        [SerializeField] private Color maximumHeadColor = Color.white;
        [ColorUsage(true, true)]
        [SerializeField] private Color headEmissionColor =
            new Color(0.88f, 0.97f, 1f, 1f);
        [SerializeField] private Vector2 headEmissionStrengthRange =
            new Vector2(4f, 7f);

        [Header("Trails")]
        [SerializeField] private CelestialTrailSettings coreTrail;
        [SerializeField] private CelestialTrailSettings outerTrail;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [Tooltip("Starfield is -1. The default 0 draws above it while gameplay sorting layers remain in front.")]
        [SerializeField] private int sortingOrder;
        [Tooltip("Distance forward from the camera. It remains behind world objects near Z=0.")]
        [Min(0.31f)]
        [SerializeField] private float cameraDepth = 13f;

        public string DisplayName => displayName;
        public CelestialEffectType EffectType => effectType;
        public bool Enabled => enabled;
        public CelestialEffectPresenter Prefab => prefab;
        public Vector2 CheckIntervalRange => checkIntervalRange;
        public float SpawnProbability => spawnProbability;
        public int MaximumActive => maximumActive;
        public int PoolCapacity => poolCapacity;
        public CelestialSpawnEdgeMode SpawnEdgeMode =>
            spawnEdgeMode;
        public Vector2 NormalizedVerticalStartRange =>
            normalizedVerticalStartRange;
        public Vector2 NormalizedTopStartRange =>
            normalizedTopStartRange;
        public Vector2 DownwardSlopeRange => downwardSlopeRange;
        public Vector2 CrossingDurationRange =>
            crossingDurationRange;
        public Vector2 ScaleRange => scaleRange;
        public Sprite[] HeadSprites => headSprites;
        public Material HeadMaterial => headMaterial;
        public Color MinimumHeadColor => minimumHeadColor;
        public Color MaximumHeadColor => maximumHeadColor;
        public Color HeadEmissionColor => headEmissionColor;
        public Vector2 HeadEmissionStrengthRange =>
            headEmissionStrengthRange;
        public CelestialTrailSettings CoreTrail => coreTrail;
        public CelestialTrailSettings OuterTrail => outerTrail;
        public string SortingLayerName => sortingLayerName;
        public int SortingOrder => sortingOrder;
        public float CameraDepth => cameraDepth;

        public static CelestialEffectProfile
            CreateShootingStarDefaults()
        {
            return new CelestialEffectProfile
            {
                displayName = "Shooting Star",
                effectType = CelestialEffectType.ShootingStar,
                checkIntervalRange = new Vector2(3f, 8f),
                spawnProbability = 0.12f,
                maximumActive = 2,
                poolCapacity = 3,
                spawnEdgeMode =
                    CelestialSpawnEdgeMode.RandomRightOrTop,
                normalizedVerticalStartRange =
                    new Vector2(0.2f, 1f),
                normalizedTopStartRange =
                    new Vector2(0.35f, 1f),
                downwardSlopeRange =
                    new Vector2(0.35f, 0.75f),
                crossingDurationRange =
                    new Vector2(0.8f, 1.8f),
                scaleRange = new Vector2(0.8f, 1.25f),
                minimumHeadColor =
                    new Color(0.9f, 0.97f, 1f, 1f),
                maximumHeadColor = Color.white,
                headEmissionColor =
                    new Color(0.88f, 0.97f, 1f, 1f),
                headEmissionStrengthRange =
                    new Vector2(4f, 7f),
                coreTrail =
                    CelestialTrailSettings
                        .CreateShootingStarCoreDefaults(),
                outerTrail =
                    CelestialTrailSettings
                        .CreateDisabledOuterDefaults(),
                sortingOrder = 0,
                cameraDepth = 13f
            };
        }

        public static CelestialEffectProfile CreateCometDefaults()
        {
            return new CelestialEffectProfile
            {
                displayName = "Comet",
                effectType = CelestialEffectType.Comet,
                checkIntervalRange = new Vector2(5f, 12f),
                spawnProbability = 0.02f,
                maximumActive = 1,
                poolCapacity = 1,
                spawnEdgeMode =
                    CelestialSpawnEdgeMode.RandomRightOrTop,
                normalizedVerticalStartRange =
                    new Vector2(0.45f, 1f),
                normalizedTopStartRange =
                    new Vector2(0.65f, 1f),
                downwardSlopeRange =
                    new Vector2(0.12f, 0.32f),
                crossingDurationRange =
                    new Vector2(4f, 8f),
                scaleRange = new Vector2(1.5f, 2.25f),
                minimumHeadColor =
                    HexColor("EAFBFF"),
                maximumHeadColor = Color.white,
                headEmissionColor =
                    HexColor("BDEFFF"),
                headEmissionStrengthRange =
                    new Vector2(6f, 10f),
                coreTrail =
                    CelestialTrailSettings
                        .CreateCometCoreDefaults(),
                outerTrail =
                    CelestialTrailSettings
                        .CreateCometOuterDefaults(),
                sortingOrder = 0,
                cameraDepth = 12.75f
            };
        }

        public bool HasRequiredAssets()
        {
            return prefab != null
                && headMaterial != null
                && headSprites != null
                && headSprites.Length > 0
                && coreTrail != null
                && (!coreTrail.Enabled
                    || coreTrail.Material != null)
                && outerTrail != null
                && (!outerTrail.Enabled
                    || outerTrail.Material != null);
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = effectType.ToString();
            }

            checkIntervalRange.x = Mathf.Max(
                MinimumPositiveValue,
                checkIntervalRange.x);
            checkIntervalRange.y = Mathf.Max(
                checkIntervalRange.x,
                checkIntervalRange.y);
            spawnProbability = Mathf.Clamp01(spawnProbability);
            maximumActive = Mathf.Max(1, maximumActive);
            poolCapacity = Mathf.Max(maximumActive, poolCapacity);

            NormalizeRange(
                ref normalizedVerticalStartRange,
                0f,
                1f);
            NormalizeRange(
                ref normalizedTopStartRange,
                0f,
                1f);
            NormalizeRange(
                ref downwardSlopeRange,
                MinimumPositiveValue,
                4f);
            NormalizeRange(
                ref crossingDurationRange,
                MinimumPositiveValue,
                float.MaxValue);
            NormalizeRange(
                ref scaleRange,
                MinimumPositiveValue,
                float.MaxValue);
            NormalizeRange(
                ref headEmissionStrengthRange,
                0f,
                float.MaxValue);

            coreTrail ??=
                CelestialTrailSettings
                    .CreateShootingStarCoreDefaults();
            outerTrail ??=
                CelestialTrailSettings
                    .CreateDisabledOuterDefaults();
            coreTrail.Validate();
            outerTrail.Validate();

            if (string.IsNullOrWhiteSpace(sortingLayerName))
            {
                sortingLayerName = "Default";
            }

            cameraDepth = Mathf.Max(0.31f, cameraDepth);
        }

        private static void NormalizeRange(
            ref Vector2 range,
            float minimum,
            float maximum)
        {
            range.x = Mathf.Clamp(range.x, minimum, maximum);
            range.y = Mathf.Clamp(range.y, minimum, maximum);
            if (range.x > range.y)
            {
                (range.x, range.y) = (range.y, range.x);
            }
        }

        private static Color HexColor(string html)
        {
            ColorUtility.TryParseHtmlString(
                $"#{html}",
                out Color color);
            return color;
        }
    }
}
