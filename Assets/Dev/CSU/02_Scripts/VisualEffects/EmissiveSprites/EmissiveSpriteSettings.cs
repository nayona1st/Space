using UnityEngine;

namespace CSU.VisualEffects.EmissiveSprites
{
    [CreateAssetMenu(
        fileName = "EmissiveSpriteSettings",
        menuName = "CSU/Visual Effects/Emissive Sprite Settings")]
    public sealed class EmissiveSpriteSettings : ScriptableObject
    {
        [SerializeField] private EmissiveSpriteProfile starCloudProfile;
        [SerializeField] private EmissiveSpriteProfile ufoAuraProfile;
        [SerializeField] private EmissiveSpriteProfile ufoBackgroundProfile;
        [SerializeField] private EmissiveSpriteProfile spaceShipFireProfile;

        public EmissiveSpriteProfile StarCloudProfile => starCloudProfile;
        public EmissiveSpriteProfile UfoAuraProfile => ufoAuraProfile;
        public EmissiveSpriteProfile UfoBackgroundProfile =>
            ufoBackgroundProfile;
        public EmissiveSpriteProfile SpaceShipFireProfile =>
            spaceShipFireProfile;

        public EmissiveSpriteProfile GetProfile(
            EmissiveSpriteTarget target)
        {
            switch (target)
            {
                case EmissiveSpriteTarget.StarCloud:
                    return starCloudProfile;
                case EmissiveSpriteTarget.UfoAura:
                    return ufoAuraProfile;
                case EmissiveSpriteTarget.UfoBackground:
                    return ufoBackgroundProfile;
                case EmissiveSpriteTarget.SpaceShipFire:
                    return spaceShipFireProfile;
                default:
                    return null;
            }
        }
    }
}
