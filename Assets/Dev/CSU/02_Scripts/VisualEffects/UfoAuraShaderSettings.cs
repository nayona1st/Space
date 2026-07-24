using UnityEngine;

namespace CSU.VisualEffects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class UfoAuraShaderSettings : MonoBehaviour
    {
        [Header("초보자용 기본 설정")]
        [Tooltip(
            "UFO 주변의 에너지 Aura 효과를 켜거나 끕니다. "
            + "끄면 원본 스프라이트와 같은 모습으로 표시됩니다.")]
        [SerializeField] private bool effectEnabled = true;

        [Tooltip(
            "UFO와 Aura에 사용할 전용 머티리얼입니다. 일반적으로 "
            + "변경하지 않아도 됩니다.")]
        [SerializeField] private Material effectMaterial;

        [Header("Aura 색상과 발광")]
        [Tooltip(
            "UFO 주변 에너지에 더할 빛의 색상입니다. UFO 본체의 "
            + "기본 색상은 보호 영역을 통해 유지됩니다.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color auraColor =
            new Color(0.15f, 1.15f, 0.75f, 1f);

        [Tooltip(
            "Aura가 얼마나 밝게 빛나는지 조절합니다. UFO 본체에는 "
            + "효과가 거의 적용되지 않습니다.")]
        [Range(0f, 2f)]
        [SerializeField] private float emissionStrength = 0.35f;

        [Header("부드러운 펄스")]
        [Tooltip(
            "Aura가 밝아졌다 어두워지는 반복 속도입니다. 값이 "
            + "너무 높으면 빠르게 깜빡이는 것처럼 보일 수 있습니다.")]
        [Range(0f, 4f)]
        [SerializeField] private float pulseSpeed = 1.1f;

        [Tooltip(
            "한 번의 펄스에서 밝기가 변하는 폭입니다. 0이면 "
            + "밝기가 일정합니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float pulseAmount = 0.18f;

        [Tooltip(
            "펄스가 가장 어두운 순간의 밝기입니다. 값을 높이면 "
            + "Aura가 완전히 꺼져 보이는 현상을 막을 수 있습니다.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float minimumBrightness = 0.82f;

        [Header("에너지 일렁임")]
        [Tooltip(
            "Aura 표면의 에너지 무늬가 움직이는 속도입니다. "
            + "프리팹의 실제 이동 속도에는 영향을 주지 않습니다.")]
        [Range(-0.15f, 0.15f)]
        [SerializeField] private float flowSpeed = 0.035f;

        [Tooltip(
            "Aura 형태가 일렁이는 정도입니다. 너무 높으면 UFO "
            + "주변이 지저분하게 보일 수 있습니다.")]
        [Range(0f, 0.02f)]
        [SerializeField] private float distortionStrength = 0.002f;

        [Tooltip(
            "에너지 무늬의 크기입니다. 작은 값은 큰 흐름, 큰 "
            + "값은 잘게 움직이는 효과를 만듭니다.")]
        [Range(0.25f, 12f)]
        [SerializeField] private float noiseScale = 4.5f;

        [Tooltip(
            "Aura 바깥쪽이 투명하게 사라지는 범위입니다. 큰 "
            + "값일수록 가장자리가 부드러워집니다.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float edgeSoftness = 0.12f;

        [Tooltip(
            "Aura 부분의 전체 투명도입니다. UFO 본체와 스테이지 "
            + "전환 페이드는 별도로 유지됩니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float overallOpacity = 0.92f;

        [Header("고급 설정 - UFO 본체 보호")]
        [Tooltip(
            "원본 이미지 안에서 UFO 본체의 중심 위치입니다. 본체가 "
            + "펄스한다면 이 값을 조금씩 조절하세요.")]
        [SerializeField] private Vector2 bodyCenter =
            new Vector2(0.5f, 0.55f);

        [Tooltip(
            "셰이더 효과에서 보호할 UFO 본체 영역의 반쪽 크기입니다. "
            + "값을 키우면 더 넓은 부분이 원본 모습으로 유지됩니다.")]
        [SerializeField] private Vector2 bodyHalfSize =
            new Vector2(0.3f, 0.24f);

        private static readonly int EffectEnabledId =
            Shader.PropertyToID("_EffectEnabled");
        private static readonly int AuraColorId =
            Shader.PropertyToID("_AuraColor");
        private static readonly int EmissionStrengthId =
            Shader.PropertyToID("_EmissionStrength");
        private static readonly int PulseSpeedId =
            Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountId =
            Shader.PropertyToID("_PulseAmount");
        private static readonly int MinimumBrightnessId =
            Shader.PropertyToID("_MinimumBrightness");
        private static readonly int FlowSpeedId =
            Shader.PropertyToID("_FlowSpeed");
        private static readonly int DistortionStrengthId =
            Shader.PropertyToID("_DistortionStrength");
        private static readonly int NoiseScaleId =
            Shader.PropertyToID("_NoiseScale");
        private static readonly int EdgeSoftnessId =
            Shader.PropertyToID("_EdgeSoftness");
        private static readonly int OverallOpacityId =
            Shader.PropertyToID("_OverallOpacity");
        private static readonly int BodyCenterId =
            Shader.PropertyToID("_BodyCenter");
        private static readonly int BodyHalfSizeId =
            Shader.PropertyToID("_BodyHalfSize");

        private SpriteRenderer cachedRenderer;
        private MaterialPropertyBlock propertyBlock;

        private void OnEnable()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        [ContextMenu("셰이더 설정을 권장 기본값으로 되돌리기")]
        private void RestoreRecommendedDefaults()
        {
            effectEnabled = true;
            auraColor = new Color(0.15f, 1.15f, 0.75f, 1f);
            emissionStrength = 0.35f;
            pulseSpeed = 1.1f;
            pulseAmount = 0.18f;
            minimumBrightness = 0.82f;
            flowSpeed = 0.035f;
            distortionStrength = 0.002f;
            noiseScale = 4.5f;
            edgeSoftness = 0.12f;
            overallOpacity = 0.92f;
            bodyCenter = new Vector2(0.5f, 0.55f);
            bodyHalfSize = new Vector2(0.3f, 0.24f);
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<SpriteRenderer>();
            }

            if (cachedRenderer == null)
            {
                return;
            }

            if (effectMaterial != null
                && cachedRenderer.sharedMaterial != effectMaterial)
            {
                cachedRenderer.sharedMaterial = effectMaterial;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            cachedRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(
                EffectEnabledId,
                effectEnabled ? 1f : 0f);
            propertyBlock.SetColor(AuraColorId, auraColor);
            propertyBlock.SetFloat(
                EmissionStrengthId,
                emissionStrength);
            propertyBlock.SetFloat(PulseSpeedId, pulseSpeed);
            propertyBlock.SetFloat(PulseAmountId, pulseAmount);
            propertyBlock.SetFloat(
                MinimumBrightnessId,
                minimumBrightness);
            propertyBlock.SetFloat(FlowSpeedId, flowSpeed);
            propertyBlock.SetFloat(
                DistortionStrengthId,
                distortionStrength);
            propertyBlock.SetFloat(NoiseScaleId, noiseScale);
            propertyBlock.SetFloat(EdgeSoftnessId, edgeSoftness);
            propertyBlock.SetFloat(OverallOpacityId, overallOpacity);
            propertyBlock.SetVector(BodyCenterId, bodyCenter);
            propertyBlock.SetVector(BodyHalfSizeId, bodyHalfSize);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
