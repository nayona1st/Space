using System;
using UnityEngine;

namespace SpaceGame.CommonUI.Settings
{
    [Serializable]
    public sealed class GameSettingsData
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float uiVolume = 0.8f;
        [Range(0f, 1f)] public float ambienceVolume = 0.8f;
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public int refreshRateNumerator;
        public int refreshRateDenominator = 1;

        public static GameSettingsData CreateDefault()
        {
            return new GameSettingsData();
        }

        public GameSettingsData Clone()
        {
            return new GameSettingsData
            {
                masterVolume = masterVolume,
                bgmVolume = bgmVolume,
                sfxVolume = sfxVolume,
                uiVolume = uiVolume,
                ambienceVolume = ambienceVolume,
                fullscreen = fullscreen,
                resolutionWidth = resolutionWidth,
                resolutionHeight = resolutionHeight,
                refreshRateNumerator = refreshRateNumerator,
                refreshRateDenominator = refreshRateDenominator
            };
        }

        public void Sanitize()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            bgmVolume = Mathf.Clamp01(bgmVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            uiVolume = Mathf.Clamp01(uiVolume);
            ambienceVolume = Mathf.Clamp01(ambienceVolume);
            resolutionWidth = Mathf.Max(320, resolutionWidth);
            resolutionHeight = Mathf.Max(200, resolutionHeight);
            refreshRateNumerator = Mathf.Max(0, refreshRateNumerator);
            refreshRateDenominator = Mathf.Max(1, refreshRateDenominator);
        }
    }
}
