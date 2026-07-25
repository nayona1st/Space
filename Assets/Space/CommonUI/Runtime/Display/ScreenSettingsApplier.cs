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
                .GroupBy(option => new { option.width, option.height })
                .Select(group => group
                    .OrderByDescending(GetRefreshRate)
                    .First())
                .OrderBy(option => option.width)
                .ThenBy(option => option.height)
                .ToList();

            cached.Clear();
            cached.AddRange(distinct);
            return cached;
        }

        public void Apply(GameSettingsData settings)
        {
            ResolutionOption safeResolution =
                FindSupportedOrCurrentResolution(settings);
            settings.resolutionWidth = safeResolution.width;
            settings.resolutionHeight = safeResolution.height;
            settings.refreshRateNumerator =
                safeResolution.refreshRateNumerator;
            settings.refreshRateDenominator =
                safeResolution.refreshRateDenominator;

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

        private ResolutionOption FindSupportedOrCurrentResolution(
            GameSettingsData settings)
        {
            IReadOnlyList<ResolutionOption> available =
                GetAvailableResolutions();
            ResolutionOption exact = available.FirstOrDefault(option =>
                option.width == settings.resolutionWidth &&
                option.height == settings.resolutionHeight);
            if (exact.width > 0 && exact.height > 0)
            {
                return exact;
            }

            Resolution current = Screen.currentResolution;
            ResolutionOption currentOption = available.FirstOrDefault(option =>
                option.width == current.width &&
                option.height == current.height);
            if (currentOption.width > 0 && currentOption.height > 0)
            {
                return currentOption;
            }

            return available
                .OrderBy(option =>
                {
                    long widthDelta =
                        (long)option.width - settings.resolutionWidth;
                    long heightDelta =
                        (long)option.height - settings.resolutionHeight;
                    return widthDelta * widthDelta +
                           heightDelta * heightDelta;
                })
                .First();
        }

        private static double GetRefreshRate(ResolutionOption option)
        {
            return option.refreshRateNumerator <= 0
                ? 0d
                : (double)option.refreshRateNumerator /
                  Mathf.Max(1, option.refreshRateDenominator);
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
