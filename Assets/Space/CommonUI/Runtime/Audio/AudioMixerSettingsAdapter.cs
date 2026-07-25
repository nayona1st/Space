using SpaceGame.CommonUI.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace SpaceGame.CommonUI.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioMixerSettingsAdapter : MonoBehaviour, IAudioSettingsAdapter
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParameter = "Master";
        [SerializeField] private string bgmParameter = "BGM";
        [SerializeField] private string sfxParameter = "SFX";
        [SerializeField] private string uiParameter = "UI";
        [SerializeField] private string ambienceParameter = "Ambience";

        public AudioMixer Mixer => mixer;

        public void Apply(GameSettingsData settings)
        {
            if (mixer == null)
            {
                return;
            }

            SetVolume(masterParameter, settings.masterVolume);
            SetVolume(bgmParameter, settings.bgmVolume);
            SetVolume(sfxParameter, settings.sfxVolume);
            SetVolume(uiParameter, settings.uiVolume);
            SetVolume(ambienceParameter, settings.ambienceVolume);
        }

        public void Configure(
            AudioMixer targetMixer,
            string master,
            string bgm,
            string sfx,
            string ui,
            string ambience)
        {
            mixer = targetMixer;
            masterParameter = master;
            bgmParameter = bgm;
            sfxParameter = sfx;
            uiParameter = ui;
            ambienceParameter = ambience;
        }

        private void SetVolume(string parameter, float normalizedVolume)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            float decibels = normalizedVolume <= 0.0001f
                ? -80f
                : Mathf.Log10(Mathf.Clamp01(normalizedVolume)) * 20f;
            mixer.SetFloat(parameter, decibels);
        }
    }
}
