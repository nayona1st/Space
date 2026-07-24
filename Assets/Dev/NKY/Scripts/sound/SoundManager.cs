using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Dev.NKY.Scripts
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Mixer Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("BGM Channel")]
        [SerializeField] private AudioSource bgmSource;

        [Header("SFX Pool Settings")]
        [SerializeField] private int initialPoolSize = 10;
        private readonly List<AudioSource> sfxPool = new List<AudioSource>();

        // AudioMixer에 노출시킨 파라미터 이름
        private const string MASTER_PARAM = "Master";
        private const string BGM_PARAM = "BGM";
        private const string SFX_PARAM = "SFX";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitSFXPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 저장된 볼륨 값 불러오기 및 적용 (기본값 1f = 100%)
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

            SetMasterVolume(masterVol);
            SetBGMVolume(bgmVol);
            SetSFXVolume(sfxVol);
        }

        private void InitSFXPool()
        {
            GameObject poolParent = new GameObject("SFX_Pool");
            poolParent.transform.SetParent(transform);

            for (int i = 0; i < initialPoolSize; i++)
            {
                AudioSource source = poolParent.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = sfxGroup; // SFX 믹서 그룹 연결
                sfxPool.Add(source);
            }

            if (bgmSource != null)
            {
                bgmSource.outputAudioMixerGroup = bgmGroup; // BGM 믹서 그룹 연결
            }
        }

        /// <summary>
        /// 효과음(SFX) 재생
        /// </summary>
        public void PlaySFX(SoundDataSO soundData)
        {
            if (soundData == null) return;
            AudioSource availableSource = GetAvailableSFXSource();
            soundData.Play(availableSource);
        }

        /// <summary>
        /// 배경음(BGM) 재생
        /// </summary>
        public void PlayBGM(SoundDataSO soundData)
        {
            if (soundData == null || bgmSource == null) return;
            soundData.Play(bgmSource);
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in sfxPool)
            {
                if (!source.isPlaying) return source;
            }

            AudioSource newSource = sfxPool[0].transform.parent.gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.outputAudioMixerGroup = sfxGroup;
            sfxPool.Add(newSource);
            return newSource;
        }

        // ==========================================
        // ★ 음량 조절 메서드 (UI Slider OnValueChanged 연결용)
        // ==========================================

        /// <summary> 마스터 볼륨 조절 (값 범위: 0.0001 ~ 1) </summary>
        public void SetMasterVolume(float volume)
        {
            SetMixerVolume(MASTER_PARAM, volume);
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }

        /// <summary> BGM 볼륨 조절 (값 범위: 0.0001 ~ 1) </summary>
        public void SetBGMVolume(float volume)
        {
            SetMixerVolume(BGM_PARAM, volume);
            PlayerPrefs.SetFloat("BGMVolume", volume);
        }

        /// <summary> SFX 볼륨 조절 (값 범위: 0.0001 ~ 1) </summary>
        public void SetSFXVolume(float volume)
        {
            SetMixerVolume(SFX_PARAM, volume);
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }

        /// <summary>
        /// 0~1 비율 수치를 AudioMixer 데시벨(dB, -80 ~ 0) 수치로 변환하여 적용합니다.
        /// </summary>
        private void SetMixerVolume(string parameterName, float volume)
        {
            if (audioMixer == null) return;

            // 0일 때 Log10 연산 에러 방지를 위해 0.0001f로 제한 (약 -80dB)
            float clampedVolume = Mathf.Clamp(volume, 0.0001f, 1f);
            float dB = Mathf.Log10(clampedVolume) * 20f;

            audioMixer.SetFloat(parameterName, dB);
        }
    }
}