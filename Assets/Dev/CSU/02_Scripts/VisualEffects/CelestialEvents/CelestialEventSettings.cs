using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    [CreateAssetMenu(
        fileName = "CelestialEventSettings",
        menuName = "Space/Celestial Event Settings")]
    public sealed class CelestialEventSettings : ScriptableObject
    {
        public const string ResourcePath =
            "CelestialEvents/CelestialEventSettings";

        [Header("System")]
        [SerializeField] private bool systemEnabled = true;
        [Tooltip("False uses scaled time, so effects pause with Time.timeScale.")]
        [SerializeField] private bool useUnscaledTime;
        [Tooltip("Enables URP camera post processing at runtime and restores the previous value when this system is removed.")]
        [SerializeField] private bool enableCameraPostProcessing = true;

        [Header("Camera-relative Margins")]
        [Min(0f)]
        [SerializeField] private float spawnOutsideMargin = 1.5f;
        [Min(0f)]
        [SerializeField] private float despawnOutsideMargin = 1.5f;

        [Header("Visibility")]
        [SerializeField] private CelestialVisibilityMode visibilityMode =
            CelestialVisibilityMode.AllStages;
        [SerializeField] private int[] visibleStageNumbers;

        [Header("Profiles")]
        [SerializeField] private CelestialEffectProfile shootingStar;
        [SerializeField] private CelestialEffectProfile comet;

        [Header("Testing")]
        [Tooltip("Keeps production defaults intact while allowing rapid visual checks in Play Mode.")]
        [SerializeField] private bool testMode;
        [Min(1f)]
        [SerializeField] private float testProbabilityMultiplier = 1f;

        public bool SystemEnabled => systemEnabled;
        public bool UseUnscaledTime => useUnscaledTime;
        public bool EnableCameraPostProcessing =>
            enableCameraPostProcessing;
        public float SpawnOutsideMargin => spawnOutsideMargin;
        public float DespawnOutsideMargin => despawnOutsideMargin;
        public CelestialVisibilityMode VisibilityMode =>
            visibilityMode;
        public CelestialEffectProfile ShootingStar => shootingStar;
        public CelestialEffectProfile Comet => comet;
        public bool TestMode => testMode;
        public float TestProbabilityMultiplier =>
            testMode ? testProbabilityMultiplier : 1f;

        public bool IsVisibleAtStage(int stageNumber)
        {
            if (visibilityMode == CelestialVisibilityMode.AllStages)
            {
                return true;
            }

            if (visibleStageNumbers == null)
            {
                return false;
            }

            for (int index = 0;
                 index < visibleStageNumbers.Length;
                 index++)
            {
                if (visibleStageNumbers[index] == stageNumber)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasRequiredAssets()
        {
            return shootingStar != null
                && comet != null
                && (!shootingStar.Enabled
                    || shootingStar.HasRequiredAssets())
                && (!comet.Enabled || comet.HasRequiredAssets());
        }

        private void OnEnable()
        {
            EnsureProfiles();
        }

        private void OnValidate()
        {
            spawnOutsideMargin = Mathf.Max(
                0f,
                spawnOutsideMargin);
            despawnOutsideMargin = Mathf.Max(
                0f,
                despawnOutsideMargin);
            testProbabilityMultiplier = Mathf.Max(
                1f,
                testProbabilityMultiplier);

            EnsureProfiles();
            shootingStar.Validate();
            comet.Validate();

            if (visibleStageNumbers == null)
            {
                return;
            }

            for (int index = 0;
                 index < visibleStageNumbers.Length;
                 index++)
            {
                visibleStageNumbers[index] =
                    Mathf.Max(1, visibleStageNumbers[index]);
            }
        }

        private void EnsureProfiles()
        {
            shootingStar ??=
                CelestialEffectProfile
                    .CreateShootingStarDefaults();
            comet ??= CelestialEffectProfile.CreateCometDefaults();
        }
    }
}
