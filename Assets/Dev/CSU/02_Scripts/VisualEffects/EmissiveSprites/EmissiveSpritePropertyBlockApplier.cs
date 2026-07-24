using UnityEngine;

namespace CSU.VisualEffects.EmissiveSprites
{
    internal sealed class EmissiveSpritePropertyBlockApplier
    {
        private static readonly int EffectEnabledId =
            Shader.PropertyToID("_EffectEnabled");
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId =
            Shader.PropertyToID("_EmissionStrength");
        private static readonly int BodyEmissionStrengthId =
            Shader.PropertyToID("_BodyEmissionStrength");
        private static readonly int OverallOpacityId =
            Shader.PropertyToID("_OverallOpacity");
        private static readonly int MaskModeId =
            Shader.PropertyToID("_MaskMode");
        private static readonly int BrightnessThresholdId =
            Shader.PropertyToID("_BrightnessThreshold");
        private static readonly int ThresholdSoftnessId =
            Shader.PropertyToID("_ThresholdSoftness");
        private static readonly int OutputClampId =
            Shader.PropertyToID("_OutputClamp");
        private static readonly int BodyCenterId =
            Shader.PropertyToID("_BodyCenter");
        private static readonly int BodyHalfSizeId =
            Shader.PropertyToID("_BodyHalfSize");
        private static readonly int PulseSpeedId =
            Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountId =
            Shader.PropertyToID("_PulseAmount");

        private readonly SpriteRenderer targetRenderer;
        private readonly Material originalMaterial;
        private readonly int originalSortingLayerId;
        private readonly int originalSortingOrder;
        private MaterialPropertyBlock propertyBlock;
        private bool sortingWasOverridden;

        public EmissiveSpritePropertyBlockApplier(
            SpriteRenderer targetRenderer)
        {
            this.targetRenderer = targetRenderer;
            originalMaterial = targetRenderer != null
                ? targetRenderer.sharedMaterial
                : null;
            originalSortingLayerId = targetRenderer != null
                ? targetRenderer.sortingLayerID
                : 0;
            originalSortingOrder = targetRenderer != null
                ? targetRenderer.sortingOrder
                : 0;
        }

        public SpriteRenderer TargetRenderer => targetRenderer;

        public void Apply(
            EmissiveSpriteProfile profile,
            float emissionMultiplier)
        {
            if (targetRenderer == null || profile == null)
            {
                return;
            }

            EnsurePropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);

            if (!profile.EffectEnabled
                || profile.SharedMaterial == null)
            {
                propertyBlock.SetFloat(EffectEnabledId, 0f);
                targetRenderer.SetPropertyBlock(propertyBlock);
                RestoreMaterialAndSorting();
                return;
            }

            if (targetRenderer.sharedMaterial
                != profile.SharedMaterial)
            {
                targetRenderer.sharedMaterial =
                    profile.SharedMaterial;
            }

            propertyBlock.SetFloat(EffectEnabledId, 1f);
            propertyBlock.SetColor(BaseColorId, profile.BaseColor);
            propertyBlock.SetColor(
                EmissionColorId,
                profile.EmissionColor);
            propertyBlock.SetFloat(
                EmissionStrengthId,
                profile.EmissionStrength
                * Mathf.Max(0f, emissionMultiplier));
            propertyBlock.SetFloat(
                BodyEmissionStrengthId,
                profile.BodyEmissionStrength
                * Mathf.Max(0f, emissionMultiplier));
            propertyBlock.SetFloat(
                OverallOpacityId,
                profile.OverallOpacity);
            propertyBlock.SetFloat(
                MaskModeId,
                (float)profile.MaskMode);
            propertyBlock.SetFloat(
                BrightnessThresholdId,
                profile.BrightnessThreshold);
            propertyBlock.SetFloat(
                ThresholdSoftnessId,
                profile.ThresholdSoftness);
            propertyBlock.SetFloat(
                OutputClampId,
                profile.OutputClamp);
            propertyBlock.SetVector(
                BodyCenterId,
                profile.BodyCenter);
            propertyBlock.SetVector(
                BodyHalfSizeId,
                profile.BodyHalfSize);
            propertyBlock.SetFloat(PulseSpeedId, profile.PulseSpeed);
            propertyBlock.SetFloat(PulseAmountId, profile.PulseAmount);
            targetRenderer.SetPropertyBlock(propertyBlock);

            ApplySorting(profile);
        }

        public void Restore()
        {
            if (targetRenderer == null)
            {
                return;
            }

            EnsurePropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(EffectEnabledId, 0f);
            targetRenderer.SetPropertyBlock(propertyBlock);
            RestoreMaterialAndSorting();
        }

        private void ApplySorting(EmissiveSpriteProfile profile)
        {
            if (profile.PreserveRendererSorting)
            {
                RestoreSortingIfNeeded();
                return;
            }

            targetRenderer.sortingLayerName =
                profile.SortingLayerName;
            targetRenderer.sortingOrder = profile.SortingOrder;
            sortingWasOverridden = true;
        }

        private void RestoreMaterialAndSorting()
        {
            if (targetRenderer.sharedMaterial != originalMaterial)
            {
                targetRenderer.sharedMaterial = originalMaterial;
            }

            RestoreSortingIfNeeded();
        }

        private void RestoreSortingIfNeeded()
        {
            if (!sortingWasOverridden)
            {
                return;
            }

            targetRenderer.sortingLayerID = originalSortingLayerId;
            targetRenderer.sortingOrder = originalSortingOrder;
            sortingWasOverridden = false;
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
}
