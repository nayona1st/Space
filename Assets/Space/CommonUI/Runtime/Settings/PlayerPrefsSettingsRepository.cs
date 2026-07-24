using UnityEngine;

namespace SpaceGame.CommonUI.Settings
{
    public sealed class PlayerPrefsSettingsRepository : ISettingsRepository
    {
        public const string Prefix = "SpaceGame.CommonUI.v1.";
        public const string InitializedKey = Prefix + "Settings.Initialized";
        public const string MasterVolumeKey = Prefix + "Audio.Master";
        public const string BgmVolumeKey = Prefix + "Audio.BGM";
        public const string SfxVolumeKey = Prefix + "Audio.SFX";
        public const string UiVolumeKey = Prefix + "Audio.UI";
        public const string AmbienceVolumeKey = Prefix + "Audio.Ambience";
        public const string FullscreenKey = Prefix + "Display.Fullscreen";
        public const string ResolutionWidthKey = Prefix + "Display.Width";
        public const string ResolutionHeightKey = Prefix + "Display.Height";
        public const string RefreshNumeratorKey = Prefix + "Display.RefreshNumerator";
        public const string RefreshDenominatorKey = Prefix + "Display.RefreshDenominator";

        public bool TryLoad(out GameSettingsData settings)
        {
            if (PlayerPrefs.GetInt(InitializedKey, 0) == 0)
            {
                settings = null;
                return false;
            }

            settings = new GameSettingsData
            {
                masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f),
                bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 0.8f),
                sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f),
                uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, 0.8f),
                ambienceVolume = PlayerPrefs.GetFloat(AmbienceVolumeKey, 0.8f),
                fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) != 0,
                resolutionWidth = PlayerPrefs.GetInt(ResolutionWidthKey, 1920),
                resolutionHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080),
                refreshRateNumerator = PlayerPrefs.GetInt(RefreshNumeratorKey, 0),
                refreshRateDenominator = PlayerPrefs.GetInt(RefreshDenominatorKey, 1)
            };
            settings.Sanitize();
            return true;
        }

        public void Save(GameSettingsData settings)
        {
            settings.Sanitize();
            PlayerPrefs.SetInt(InitializedKey, 1);
            PlayerPrefs.SetFloat(MasterVolumeKey, settings.masterVolume);
            PlayerPrefs.SetFloat(BgmVolumeKey, settings.bgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, settings.sfxVolume);
            PlayerPrefs.SetFloat(UiVolumeKey, settings.uiVolume);
            PlayerPrefs.SetFloat(AmbienceVolumeKey, settings.ambienceVolume);
            PlayerPrefs.SetInt(FullscreenKey, settings.fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(ResolutionWidthKey, settings.resolutionWidth);
            PlayerPrefs.SetInt(ResolutionHeightKey, settings.resolutionHeight);
            PlayerPrefs.SetInt(RefreshNumeratorKey, settings.refreshRateNumerator);
            PlayerPrefs.SetInt(RefreshDenominatorKey, settings.refreshRateDenominator);
            PlayerPrefs.Save();
        }
    }
}
