using System.Collections.Generic;
using System.Linq;
using SpaceGame.CommonUI.Settings;
using UnityEngine;

namespace SpaceGame.CommonUI.Display
{
    [DisallowMultipleComponent]
    public sealed class ScreenSettingsApplier : MonoBehaviour, IScreenSettingsApplier
    {
        [SerializeField] private bool usePlatformSupportedResolutions = true;
        [SerializeField] private List<ResolutionOption> serializedResolutions =
            new List<ResolutionOption>();

        private readonly List<ResolutionOption> cached =
            new List<ResolutionOption>();

        public IReadOnlyList<ResolutionOption> GetAvailableResolutions()
        {
            cached.Clear();

            if (usePlatformSupportedResolutions && Screen.resolutions.Length > 0)
            {
                foreach (Resolution resolution in Screen.resolutions)
                {
                    cached.Add(new ResolutionOption(
                        resolution.width,
                        resolution.height,
                        (int)resolution.refreshRateRatio.numerator,
                        (int)resolution.refreshRateRatio.denominator));
                }
            }
            else
            {
                cached.AddRange(serializedResolutions);
            }

            if (cached.Count == 0)
            {
                Resolution current = Screen.currentResolution;
                cached.Add(new ResolutionOption(
                    current.width,
                    current.height,
                    (int)current.refreshRateRatio.numerator,
                    (int)current.refreshRateRatio.denominator));
            }

            List<ResolutionOption> distinct = cached
                .GroupBy(option => new
                {
                    option.width,
                    option.height,
                    option.refreshRateNumerator,
                    option.refreshRateDenominator
                })
                .Select(group => group.First())
                .OrderBy(option => option.width)
                .ThenBy(option => option.height)
                .ThenBy(option => option.refreshRateNumerator)
                .ToList();

            cached.Clear();
            cached.AddRange(distinct);
            return cached;
        }

        public void Apply(GameSettingsData settings)
        {
            FullScreenMode mode = settings.fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            RefreshRate refreshRate = new RefreshRate
            {
                numerator = (uint)Mathf.Max(0, settings.refreshRateNumerator),
                denominator = (uint)Mathf.Max(1, settings.refreshRateDenominator)
            };

            if (refreshRate.numerator == 0)
            {
                Screen.SetResolution(
                    settings.resolutionWidth,
                    settings.resolutionHeight,
                    mode);
                return;
            }

            Screen.SetResolution(
                settings.resolutionWidth,
                settings.resolutionHeight,
                mode,
                refreshRate);
        }

        public void Configure(
            bool useSupportedResolutions,
            IEnumerable<ResolutionOption> fallbackResolutions)
        {
            usePlatformSupportedResolutions = useSupportedResolutions;
            serializedResolutions = fallbackResolutions == null
                ? new List<ResolutionOption>()
                : new List<ResolutionOption>(fallbackResolutions);
        }
    }
}
