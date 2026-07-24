using System.Collections.Generic;
using UnityEngine;

namespace CSU.VisualEffects.EmissiveSprites
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class EmissiveSpritePresenter : MonoBehaviour
    {
        [SerializeField] private EmissiveSpriteProfile profile;
        [SerializeField] private SpriteRenderer[] targetRenderers;

        private readonly List<EmissiveSpritePropertyBlockApplier> appliers =
            new List<EmissiveSpritePropertyBlockApplier>();
        private EmissiveSpriteProfile subscribedProfile;

        public EmissiveSpriteProfile Profile => profile;
        public IReadOnlyList<SpriteRenderer> TargetRenderers =>
            targetRenderers;

        private void OnEnable()
        {
            SubscribeToProfile();
            RebuildAppliersIfNeeded();
            ApplyImmediately();
        }

        private void OnDisable()
        {
            UnsubscribeFromProfile();
            RestoreAll();
        }

        private void OnDestroy()
        {
            UnsubscribeFromProfile();
            RestoreAll();
        }

        private void OnValidate()
        {
            SubscribeToProfile();
            RebuildAppliersIfNeeded();

            if (isActiveAndEnabled)
            {
                ApplyImmediately();
            }
        }

        public void Initialize(
            EmissiveSpriteProfile newProfile,
            SpriteRenderer[] renderers)
        {
            bool profileChanged = profile != newProfile;
            bool renderersChanged = !HaveSameRenderers(
                targetRenderers,
                renderers);

            if (!profileChanged && !renderersChanged)
            {
                ApplyImmediately();
                return;
            }

            UnsubscribeFromProfile();
            RestoreAll();
            profile = newProfile;
            targetRenderers = renderers;
            appliers.Clear();
            SubscribeToProfile();
            RebuildAppliersIfNeeded();
            ApplyImmediately();
        }

        [ContextMenu("Apply Emission Profile")]
        public void ApplyImmediately()
        {
            if (profile == null)
            {
                return;
            }

            RebuildAppliersIfNeeded();
            float multiplier = Application.isPlaying
                ? 1f
                : profile.PreviewEmissionMultiplier;

            for (int index = 0; index < appliers.Count; index++)
            {
                appliers[index].Apply(profile, multiplier);
            }
        }

        [ContextMenu("Restore Original Renderer State")]
        public void RestoreAll()
        {
            for (int index = 0; index < appliers.Count; index++)
            {
                appliers[index].Restore();
            }
        }

        private void RebuildAppliersIfNeeded()
        {
            EnsureTargetRenderers();

            if (AppliersMatchTargets())
            {
                return;
            }

            RestoreAll();
            appliers.Clear();

            if (targetRenderers == null)
            {
                return;
            }

            for (int index = 0;
                 index < targetRenderers.Length;
                 index++)
            {
                SpriteRenderer renderer = targetRenderers[index];

                if (renderer != null)
                {
                    appliers.Add(
                        new EmissiveSpritePropertyBlockApplier(renderer));
                }
            }
        }

        private void EnsureTargetRenderers()
        {
            if (targetRenderers != null
                && targetRenderers.Length > 0)
            {
                return;
            }

            targetRenderers =
                GetComponentsInChildren<SpriteRenderer>(true);
        }

        private bool AppliersMatchTargets()
        {
            if (targetRenderers == null
                || appliers.Count != targetRenderers.Length)
            {
                return false;
            }

            for (int index = 0;
                 index < targetRenderers.Length;
                 index++)
            {
                if (appliers[index].TargetRenderer
                    != targetRenderers[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void SubscribeToProfile()
        {
            if (subscribedProfile == profile)
            {
                return;
            }

            UnsubscribeFromProfile();
            subscribedProfile = profile;

            if (subscribedProfile != null)
            {
                subscribedProfile.Changed += ApplyImmediately;
            }
        }

        private void UnsubscribeFromProfile()
        {
            if (subscribedProfile != null)
            {
                subscribedProfile.Changed -= ApplyImmediately;
            }

            subscribedProfile = null;
        }

        private static bool HaveSameRenderers(
            SpriteRenderer[] first,
            SpriteRenderer[] second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (first == null
                || second == null
                || first.Length != second.Length)
            {
                return false;
            }

            for (int index = 0; index < first.Length; index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
