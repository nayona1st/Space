using UnityEngine;

namespace CSU.VisualEffects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StarCloudShaderSettings : MonoBehaviour
    {
        [Header("초보자용 기본 설정")]
        [Tooltip(
            "별구름 셰이더 효과를 켜거나 끕니다. 끄면 원본 "
            + "스프라이트와 같은 색상 및 형태로 표시됩니다.")]
        [SerializeField] private bool effectEnabled = true;

        [Tooltip(
            "별구름에 사용할 전용 머티리얼입니다. 일반적으로 "
            + "변경하지 않아도 됩니다.")]
        [SerializeField] private Material effectMaterial;

        [Header("색상과 발광")]
        [Tooltip(
            "별구름에 곱할 전체 색상입니다. 흰색에 가까울수록 "
            + "원본 색상을 유지합니다.")]
        [SerializeField] private Color cloudTint =
            new Color(0.78f, 0.9f, 1f, 1f);

        [Tooltip(
            "별구름의 밝은 부분에서 나오는 빛의 색상입니다. "
            + "HDR 색상을 사용할 수 있습니다.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color emissionColor =
            new Color(0.1f, 0.65f, 1.2f, 1f);

        [Tooltip(
            "별구름이 얼마나 밝게 빛나는지 조절합니다. 너무 "
            + "높으면 플레이어와 장애물이 잘 보이지 않을 수 있습니다.")]
        [Range(0f, 2f)]
        [SerializeField] private float emissionStrength = 0.28f;

        [Header("느린 구름 움직임")]
        [Tooltip(
            "구름의 일렁이는 무늬가 가로로 흐르는 속도입니다. "
            + "프리팹의 실제 이동 속도에는 영향을 주지 않습니다.")]
        [Range(-0.1f, 0.1f)]
        [SerializeField] private float horizontalFlowSpeed = 0.008f;

        [Tooltip(
            "구름의 일렁이는 무늬가 세로로 흐르는 속도입니다. "
            + "작은 값을 사용할수록 자연스럽습니다.")]
        [Range(-0.1f, 0.1f)]
        [SerializeField] private float verticalFlowSpeed = 0.003f;

        [Tooltip(
            "구름 형태가 일렁이는 정도입니다. 0이면 원본 형태를 "
            + "그대로 유지합니다.")]
        [Range(0f, 0.02f)]
        [SerializeField] private float distortionStrength = 0.002f;

        [Tooltip(
            "일렁이는 무늬의 크기를 조절합니다. 작은 값은 큰 "
            + "흐름, 큰 값은 잘게 움직이는 효과를 만듭니다.")]
        [Range(0.25f, 12f)]
        [SerializeField] private float noiseScale = 3.5f;

        [Header("투명도")]
        [Tooltip(
            "별구름 전체의 투명도입니다. SpriteRenderer의 전환 "
            + "페이드와 함께 곱해져 자연스럽게 사라집니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float overallOpacity = 0.95f;

        private static readonly int EffectEnabledId =
            Shader.PropertyToID("_EffectEnabled");
        private static readonly int CloudTintId =
            Shader.PropertyToID("_CloudTint");
        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId =
            Shader.PropertyToID("_EmissionStrength");
        private static readonly int FlowSpeedId =
            Shader.PropertyToID("_FlowSpeed");
        private static readonly int DistortionStrengthId =
            Shader.PropertyToID("_DistortionStrength");
        private static readonly int NoiseScaleId =
            Shader.PropertyToID("_NoiseScale");
        private static readonly int OverallOpacityId =
            Shader.PropertyToID("_OverallOpacity");

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
            cloudTint = new Color(0.78f, 0.9f, 1f, 1f);
            emissionColor = new Color(0.1f, 0.65f, 1.2f, 1f);
            emissionStrength = 0.28f;
            horizontalFlowSpeed = 0.008f;
            verticalFlowSpeed = 0.003f;
            distortionStrength = 0.002f;
            noiseScale = 3.5f;
            overallOpacity = 0.95f;
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
            propertyBlock.SetColor(CloudTintId, cloudTint);
            propertyBlock.SetColor(EmissionColorId, emissionColor);
            propertyBlock.SetFloat(
                EmissionStrengthId,
                emissionStrength);
            propertyBlock.SetVector(
                FlowSpeedId,
                new Vector4(
                    horizontalFlowSpeed,
                    verticalFlowSpeed,
                    0f,
                    0f));
            propertyBlock.SetFloat(
                DistortionStrengthId,
                distortionStrength);
            propertyBlock.SetFloat(NoiseScaleId, noiseScale);
            propertyBlock.SetFloat(OverallOpacityId, overallOpacity);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
