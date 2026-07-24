using System;
using UnityEngine;

namespace CSU.VisualEffects.EmissiveSprites
{
    [CreateAssetMenu(
        fileName = "EmissiveSpriteProfile",
        menuName = "CSU/Visual Effects/Emissive Sprite Profile")]
    public sealed class EmissiveSpriteProfile : ScriptableObject
    {
        [Header("Target")]
        [SerializeField] private EmissiveSpriteTarget target;
        [SerializeField] private bool effectEnabled = true;
        [SerializeField] private Material sharedMaterial;

        [Header("Color and opacity")]
        [SerializeField] private Color baseColor = Color.white;
        [ColorUsage(false, true)]
        [SerializeField] private Color emissionColor = Color.white;
        [Min(0f)]
        [SerializeField] private float emissionStrength = 2f;
        [Range(0f, 1f)]
        [SerializeField] private float overallOpacity = 1f;

        [Header("Emission mask")]
        [SerializeField] private EmissiveSpriteMaskMode maskMode =
            EmissiveSpriteMaskMode.Brightness;
        [Range(0f, 1f)]
        [SerializeField] private float brightnessThreshold = 0.55f;
        [Range(0.001f, 0.5f)]
        [SerializeField] private float thresholdSoftness = 0.12f;
        [Range(1f, 10f)]
        [SerializeField] private float outputClamp = 4f;

        [Header("UFO body and aura")]
        [Min(0f)]
        [SerializeField] private float bodyEmissionStrength = 2f;
        [SerializeField] private Vector2 bodyCenter =
            new Vector2(0.5f, 0.55f);
        [SerializeField] private Vector2 bodyHalfSize =
            new Vector2(0.3f, 0.24f);
        [Min(0f)]
        [SerializeField] private float pulseSpeed = 1.1f;
        [Range(0f, 1f)]
        [SerializeField] private float pulseAmount = 0.15f;

        [Header("Sorting")]
        [Tooltip(
            "Enabled profiles never change the SpriteRenderer's existing "
            + "Sorting Layer or Order.")]
        [SerializeField] private bool preserveRendererSorting = true;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder;

        [Header("Edit mode preview")]
        [Tooltip(
            "Multiplies emission only while previewing outside Play Mode. "
            + "Runtime always uses the profile's exact strength.")]
        [Range(0f, 2f)]
        [SerializeField] private float previewEmissionMultiplier = 1f;

        public event Action Changed;

        public EmissiveSpriteTarget Target => target;
        public bool EffectEnabled => effectEnabled;
        public Material SharedMaterial => sharedMaterial;
        public Color BaseColor => baseColor;
        public Color EmissionColor => emissionColor;
        public float EmissionStrength => emissionStrength;
        public float OverallOpacity => overallOpacity;
        public EmissiveSpriteMaskMode MaskMode => maskMode;
        public float BrightnessThreshold => brightnessThreshold;
        public float ThresholdSoftness => thresholdSoftness;
        public float OutputClamp => outputClamp;
        public float BodyEmissionStrength => bodyEmissionStrength;
        public Vector2 BodyCenter => bodyCenter;
        public Vector2 BodyHalfSize => bodyHalfSize;
        public float PulseSpeed => pulseSpeed;
        public float PulseAmount => pulseAmount;
        public bool PreserveRendererSorting => preserveRendererSorting;
        public string SortingLayerName => sortingLayerName;
        public int SortingOrder => sortingOrder;
        public float PreviewEmissionMultiplier =>
            previewEmissionMultiplier;

        private void OnValidate()
        {
            emissionStrength = Mathf.Max(0f, emissionStrength);
            overallOpacity = Mathf.Clamp01(overallOpacity);
            brightnessThreshold = Mathf.Clamp01(brightnessThreshold);
            thresholdSoftness = Mathf.Clamp(
                thresholdSoftness,
                0.001f,
                0.5f);
            outputClamp = Mathf.Clamp(outputClamp, 1f, 10f);
            bodyEmissionStrength = Mathf.Max(0f, bodyEmissionStrength);
            bodyHalfSize = new Vector2(
                Mathf.Max(0.001f, bodyHalfSize.x),
                Mathf.Max(0.001f, bodyHalfSize.y));
            pulseSpeed = Mathf.Max(0f, pulseSpeed);
            pulseAmount = Mathf.Clamp01(pulseAmount);
            previewEmissionMultiplier = Mathf.Clamp(
                previewEmissionMultiplier,
                0f,
                2f);
            Changed?.Invoke();
        }
    }
}
