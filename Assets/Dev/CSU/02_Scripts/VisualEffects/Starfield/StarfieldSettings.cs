using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.Starfield
{
    public enum StarfieldVisibilityMode
    {
        AllStages,
        SelectedStages
    }

    [CreateAssetMenu(
        fileName = "StarfieldSettings",
        menuName = "Space/Starfield Settings")]
    public sealed class StarfieldSettings : ScriptableObject
    {
        public const string ResourcePath =
            "Starfield/StarfieldSettings";

        private const float MinimumReferenceSize = 0.01f;
        private const float MinimumEmitterWidth = 0.01f;

        [Header("System")]
        [SerializeField] private bool systemEnabled = true;
        [SerializeField] private bool prewarm = true;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Sprite[] starSprites;

        [Header("Camera-relative Layout")]
        [Tooltip("Velocity values are scaled against this orthographic size so screen-space motion remains stable when the camera zoom changes.")]
        [Min(MinimumReferenceSize)]
        [SerializeField] private float referenceOrthographicSize = 5f;
        [Min(MinimumEmitterWidth)]
        [SerializeField] private float emitterWidth = 0.2f;
        [Min(0f)]
        [SerializeField] private float verticalPadding = 2f;
        [Min(0f)]
        [SerializeField] private float spawnRightPadding = 1f;
        [Min(0f)]
        [SerializeField] private float despawnLeftPadding = 1f;

        [Header("Visibility")]
        [SerializeField] private StarfieldVisibilityMode visibilityMode =
            StarfieldVisibilityMode.AllStages;
        [SerializeField] private int[] visibleStageNumbers;

        [Header("Layers")]
        [SerializeField] private StarfieldLayerSettings farLayer =
            StarfieldLayerSettings.CreateFarDefaults();
        [SerializeField] private StarfieldLayerSettings nearLayer =
            StarfieldLayerSettings.CreateNearDefaults();

        public bool SystemEnabled => systemEnabled;
        public bool Prewarm => prewarm;
        public Material ParticleMaterial => particleMaterial;
        public Sprite[] StarSprites => starSprites;
        public float ReferenceOrthographicSize =>
            referenceOrthographicSize;
        public float EmitterWidth => emitterWidth;
        public float VerticalPadding => verticalPadding;
        public float SpawnRightPadding => spawnRightPadding;
        public float DespawnLeftPadding => despawnLeftPadding;
        public StarfieldVisibilityMode VisibilityMode => visibilityMode;
        public StarfieldLayerSettings FarLayer => farLayer;
        public StarfieldLayerSettings NearLayer => nearLayer;

        public bool IsVisibleAtStage(int stageNumber)
        {
            if (visibilityMode == StarfieldVisibilityMode.AllStages)
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
            return particleMaterial != null
                && starSprites != null
                && starSprites.Length > 0;
        }

        private void OnValidate()
        {
            referenceOrthographicSize = Mathf.Max(
                MinimumReferenceSize,
                referenceOrthographicSize);
            emitterWidth = Mathf.Max(
                MinimumEmitterWidth,
                emitterWidth);
            verticalPadding = Mathf.Max(0f, verticalPadding);
            spawnRightPadding = Mathf.Max(0f, spawnRightPadding);
            despawnLeftPadding = Mathf.Max(0f, despawnLeftPadding);

            farLayer ??= StarfieldLayerSettings.CreateFarDefaults();
            nearLayer ??= StarfieldLayerSettings.CreateNearDefaults();
            farLayer.Validate();
            nearLayer.Validate();

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
    }
}
