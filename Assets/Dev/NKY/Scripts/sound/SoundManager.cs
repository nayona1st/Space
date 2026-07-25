using System.Collections;
using System.Collections.Generic;
using Dev.CSU._02_Scripts.SceneTransition;
using SpaceGame.CommonUI.Settings;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Dev.NKY.Scripts
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Mixer Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;

        [Header("BGM Channel")]
        [SerializeField] private AudioSource bgmSource;

        [Header("Scene BGM Transition")]
        [Min(0f)]
        [SerializeField] private float bgmFadeOutDuration = 0.5f;
        [Min(0f)]
        [SerializeField] private float bgmFadeInDuration = 0.8f;

        [Header("SFX Pool Settings")]
        [SerializeField] private int initialPoolSize = 10;
        private readonly List<AudioSource> sfxPool = new List<AudioSource>();

        private AudioClip _mainMenuBgm;
        private AudioClip _inGameBgm;
        private AudioClip _endingBgm;
        private Coroutine _bgmTransitionRoutine;
        private ISceneTransitionService _sceneTransitionService;
        private string _pendingSceneName;
        private bool _sceneTransitionPending;

        // AudioMixer에 노출시킨 파라미터 이름
        private const string MASTER_PARAM = "Master";
        private const string BGM_PARAM = "BGM";
        private const string SFX_PARAM = "SFX";
        private const string UI_PARAM = "UI";
        private const string AMBIENCE_PARAM = "Ambience";
        private const string MAIN_MENU_SCENE = "MainMenu";
        private const string ROCKET_SHOOTING_SCENE = "Rocket Shooting";
        private const string IN_GAME_SCENE = "InGame";
        private const string ENDING_SCENE = "Ending";
        private const string MAIN_MENU_BGM_PATH = "BGM/MainMenuBGM";
        private const string IN_GAME_BGM_PATH = "BGM/InGameBGM";
        private const string ENDING_BGM_PATH = "BGM/EndingBGM";

        internal static void EnsureRuntimeInstance()
        {
            if (Instance != null)
            {
                return;
            }

            AudioMixer mixer = Resources.Load<AudioMixer>("Audio");
            if (mixer == null)
            {
                Debug.LogError(
                    "[SoundManager] Resources/Audio.mixer를 찾을 수 없습니다.");
                return;
            }

            GameObject managerObject = new GameObject(nameof(SoundManager));
            managerObject.SetActive(false);
            AudioSource musicSource =
                managerObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            SoundManager manager =
                managerObject.AddComponent<SoundManager>();
            manager.ConfigureRuntime(
                mixer,
                FindGroup(mixer, "BGM"),
                FindGroup(mixer, "SFX"),
                FindGroup(mixer, "UI"),
                FindGroup(mixer, "Ambience"),
                musicSource);
            managerObject.SetActive(true);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitSFXPool();
                SceneManager.sceneLoaded += HandleSceneLoaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            bool hasCommonSettings =
                PlayerPrefs.GetInt(
                    PlayerPrefsSettingsRepository.InitializedKey,
                    0) != 0;
            float masterVol = GetSavedVolume(
                hasCommonSettings,
                PlayerPrefsSettingsRepository.MasterVolumeKey,
                "MasterVolume",
                1f);
            float bgmVol = GetSavedVolume(
                hasCommonSettings,
                PlayerPrefsSettingsRepository.BgmVolumeKey,
                "BGMVolume",
                0.8f);
            float sfxVol = GetSavedVolume(
                hasCommonSettings,
                PlayerPrefsSettingsRepository.SfxVolumeKey,
                "SFXVolume",
                0.8f);
            float uiVol = GetSavedVolume(
                hasCommonSettings,
                PlayerPrefsSettingsRepository.UiVolumeKey,
                string.Empty,
                0.8f);
            float ambienceVol = GetSavedVolume(
                hasCommonSettings,
                PlayerPrefsSettingsRepository.AmbienceVolumeKey,
                string.Empty,
                0.8f);

            SetMixerVolume(MASTER_PARAM, masterVol);
            SetMixerVolume(BGM_PARAM, bgmVol);
            SetMixerVolume(SFX_PARAM, sfxVol);
            SetMixerVolume(UI_PARAM, uiVol);
            SetMixerVolume(AMBIENCE_PARAM, ambienceVol);

            LoadSceneBgmClips();
            TryBindSceneTransitionService();
            PlaySceneBgm(
                SceneManager.GetActiveScene().name,
                false);
        }

        private void Update()
        {
            TryBindSceneTransitionService();

            if (!_sceneTransitionPending
                || _sceneTransitionService == null
                || _sceneTransitionService.IsTransitioning)
            {
                return;
            }

            string activeSceneName =
                SceneManager.GetActiveScene().name;
            if (activeSceneName == _pendingSceneName)
            {
                return;
            }

            _sceneTransitionPending = false;
            _pendingSceneName = string.Empty;
            PlaySceneBgm(activeSceneName, false);
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

        private void ConfigureRuntime(
            AudioMixer mixer,
            AudioMixerGroup musicGroup,
            AudioMixerGroup effectsGroup,
            AudioMixerGroup interfaceGroup,
            AudioMixerGroup environmentGroup,
            AudioSource musicSource)
        {
            audioMixer = mixer;
            bgmGroup = musicGroup;
            sfxGroup = effectsGroup;
            uiGroup = interfaceGroup;
            ambienceGroup = environmentGroup;
            bgmSource = musicSource;

            foreach (AudioSource source in sfxPool)
            {
                source.outputAudioMixerGroup = sfxGroup;
            }

            if (bgmSource != null)
            {
                bgmSource.outputAudioMixerGroup = bgmGroup;
            }
        }

        private static AudioMixerGroup FindGroup(
            AudioMixer mixer,
            string groupName)
        {
            AudioMixerGroup[] groups =
                mixer.FindMatchingGroups(groupName);
            foreach (AudioMixerGroup group in groups)
            {
                if (group != null && group.name == groupName)
                {
                    return group;
                }
            }

            Debug.LogError(
                $"[SoundManager] Audio Mixer 그룹을 찾을 수 없습니다: {groupName}");
            return null;
        }

        /// <summary>
        /// 효과음(SFX) 재생
        /// </summary>
        public void PlaySFX(SoundDataSO soundData)
        {
            if (soundData == null) return;
            AudioSource availableSource =
                GetAvailableSFXSource(sfxGroup);
            soundData.Play(availableSource);
        }

        public void PlaySFX(
            AudioClip clip,
            float volume = 1f)
        {
            PlayClip(
                clip,
                sfxGroup,
                volume);
        }

        public AudioSource PlayLoopingSFX(
            AudioClip clip,
            float volume = 1f)
        {
            if (clip == null)
            {
                return null;
            }

            AudioSource source =
                GetAvailableSFXSource(sfxGroup);
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = 1f;
            source.loop = true;
            source.Play();
            return source;
        }

        public void StopSFX(AudioSource source)
        {
            if (source == null || !sfxPool.Contains(source))
            {
                return;
            }

            source.Stop();
            source.loop = false;
            source.clip = null;
        }

        public void PlayUI(SoundDataSO soundData)
        {
            if (soundData == null) return;
            AudioSource availableSource =
                GetAvailableSFXSource(uiGroup != null ? uiGroup : sfxGroup);
            soundData.Play(availableSource);
        }

        public void PlayUI(
            AudioClip clip,
            float volume = 1f)
        {
            PlayClip(
                clip,
                uiGroup != null ? uiGroup : sfxGroup,
                volume);
        }

        public void PlayAmbience(SoundDataSO soundData)
        {
            if (soundData == null) return;
            AudioSource availableSource = GetAvailableSFXSource(
                ambienceGroup != null ? ambienceGroup : sfxGroup);
            soundData.Play(availableSource);
        }

        /// <summary>
        /// 배경음(BGM) 재생
        /// </summary>
        public void PlayBGM(SoundDataSO soundData)
        {
            if (soundData == null || bgmSource == null) return;
            StopBgmTransitionRoutine();
            soundData.Play(bgmSource);
        }

        private void LoadSceneBgmClips()
        {
            _mainMenuBgm ??=
                Resources.Load<AudioClip>(MAIN_MENU_BGM_PATH);
            _inGameBgm ??=
                Resources.Load<AudioClip>(IN_GAME_BGM_PATH);
            _endingBgm ??=
                Resources.Load<AudioClip>(ENDING_BGM_PATH);
        }

        private void TryBindSceneTransitionService()
        {
            if (!SceneTransitions.TryGetService(
                    out ISceneTransitionService service)
                || ReferenceEquals(_sceneTransitionService, service))
            {
                return;
            }

            UnbindSceneTransitionService();
            _sceneTransitionService = service;
            _sceneTransitionService.TransitionStarted +=
                HandleTransitionStarted;
        }

        private void UnbindSceneTransitionService()
        {
            if (_sceneTransitionService == null)
            {
                return;
            }

            _sceneTransitionService.TransitionStarted -=
                HandleTransitionStarted;
            _sceneTransitionService = null;
        }

        private void HandleTransitionStarted(string targetSceneName)
        {
            LoadSceneBgmClips();
            _pendingSceneName = targetSceneName;
            _sceneTransitionPending = true;

            AudioClip targetClip =
                GetSceneBgmClip(targetSceneName);
            if (bgmSource == null || bgmSource.clip == targetClip)
            {
                return;
            }

            StopBgmTransitionRoutine();
            _bgmTransitionRoutine =
                StartCoroutine(FadeOutCurrentBgm());
        }

        private void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            if (scene != SceneManager.GetActiveScene())
            {
                return;
            }

            bool managedTransition =
                _sceneTransitionPending
                && scene.name == _pendingSceneName;

            _sceneTransitionPending = false;
            _pendingSceneName = string.Empty;
            PlaySceneBgm(scene.name, managedTransition);
        }

        private void PlaySceneBgm(
            string sceneName,
            bool fadeOutAlreadyStarted)
        {
            if (bgmSource == null)
            {
                return;
            }

            LoadSceneBgmClips();
            AudioClip targetClip = GetSceneBgmClip(sceneName);

            if (bgmSource.clip == targetClip)
            {
                if (targetClip == null)
                {
                    return;
                }

                if (!bgmSource.isPlaying)
                {
                    bgmSource.volume = 0f;
                    bgmSource.Play();
                }

                if (bgmSource.volume < 1f)
                {
                    StopBgmTransitionRoutine();
                    _bgmTransitionRoutine =
                        StartCoroutine(FadeBgmVolume(1f, bgmFadeInDuration));
                }

                return;
            }

            StopBgmTransitionRoutine();
            _bgmTransitionRoutine = StartCoroutine(
                SwitchSceneBgm(targetClip, fadeOutAlreadyStarted));
        }

        private AudioClip GetSceneBgmClip(string sceneName)
        {
            switch (sceneName)
            {
                case MAIN_MENU_SCENE:
                case ROCKET_SHOOTING_SCENE:
                    return _mainMenuBgm;
                case IN_GAME_SCENE:
                    return _inGameBgm;
                case ENDING_SCENE:
                    return _endingBgm;
                default:
                    return null;
            }
        }

        private IEnumerator SwitchSceneBgm(
            AudioClip targetClip,
            bool fadeOutAlreadyStarted)
        {
            if (!fadeOutAlreadyStarted
                && bgmSource.isPlaying
                && bgmSource.volume > 0f)
            {
                yield return FadeBgmVolume(
                    0f,
                    bgmFadeOutDuration,
                    false);
            }

            bgmSource.Stop();
            bgmSource.clip = targetClip;
            bgmSource.pitch = 1f;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.ignoreListenerPause = true;
            bgmSource.volume = 0f;

            if (targetClip == null)
            {
                _bgmTransitionRoutine = null;
                yield break;
            }

            bgmSource.Play();
            yield return FadeBgmVolume(
                1f,
                bgmFadeInDuration,
                false);
            _bgmTransitionRoutine = null;
        }

        private IEnumerator FadeOutCurrentBgm()
        {
            yield return FadeBgmVolume(
                0f,
                bgmFadeOutDuration,
                false);
            _bgmTransitionRoutine = null;
        }

        private IEnumerator FadeBgmVolume(
            float targetVolume,
            float duration,
            bool clearRoutineWhenFinished = true)
        {
            float startVolume = bgmSource.volume;
            if (duration <= 0f)
            {
                bgmSource.volume = targetVolume;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(
                        startVolume,
                        targetVolume,
                        Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }

                bgmSource.volume = targetVolume;
            }

            if (clearRoutineWhenFinished)
            {
                _bgmTransitionRoutine = null;
            }
        }

        private void StopBgmTransitionRoutine()
        {
            if (_bgmTransitionRoutine == null)
            {
                return;
            }

            StopCoroutine(_bgmTransitionRoutine);
            _bgmTransitionRoutine = null;
        }

        private AudioSource GetAvailableSFXSource(
            AudioMixerGroup outputGroup)
        {
            foreach (var source in sfxPool)
            {
                if (!source.isPlaying)
                {
                    source.outputAudioMixerGroup = outputGroup;
                    return source;
                }
            }

            AudioSource newSource = sfxPool[0].transform.parent.gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.outputAudioMixerGroup = outputGroup;
            sfxPool.Add(newSource);
            return newSource;
        }

        private void PlayClip(
            AudioClip clip,
            AudioMixerGroup outputGroup,
            float volume)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source =
                GetAvailableSFXSource(outputGroup);
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = 1f;
            source.loop = false;
            source.Play();
        }

        // ==========================================
        // ★ 음량 조절 메서드 (UI Slider OnValueChanged 연결용)
        // ==========================================

        /// <summary> 마스터 볼륨 조절 (값 범위: 0.0001 ~ 1) </summary>
        public void SetMasterVolume(float volume)
        {
            SetMixerVolume(MASTER_PARAM, volume);
            SaveVolume(
                PlayerPrefsSettingsRepository.MasterVolumeKey,
                volume);
        }

        /// <summary> BGM 볼륨 조절 (값 범위: 0.0001 ~ 1) </summary>
        public void SetBGMVolume(float volume)
        {
            SetMixerVolume(BGM_PARAM, volume);
            SaveVolume(
                PlayerPrefsSettingsRepository.BgmVolumeKey,
                volume);
        }

        /// <summary> SFX 볼륨 조절 (값 범위: 0.0001 ~ 1) </summary>
        public void SetSFXVolume(float volume)
        {
            SetMixerVolume(SFX_PARAM, volume);
            SaveVolume(
                PlayerPrefsSettingsRepository.SfxVolumeKey,
                volume);
        }

        public void SetUIVolume(float volume)
        {
            SetMixerVolume(UI_PARAM, volume);
            SaveVolume(
                PlayerPrefsSettingsRepository.UiVolumeKey,
                volume);
        }

        public void SetAmbienceVolume(float volume)
        {
            SetMixerVolume(AMBIENCE_PARAM, volume);
            SaveVolume(
                PlayerPrefsSettingsRepository.AmbienceVolumeKey,
                volume);
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

        private static float GetSavedVolume(
            bool hasCommonSettings,
            string commonKey,
            string legacyKey,
            float defaultValue)
        {
            if (hasCommonSettings)
            {
                return PlayerPrefs.GetFloat(commonKey, defaultValue);
            }

            return string.IsNullOrWhiteSpace(legacyKey)
                ? defaultValue
                : PlayerPrefs.GetFloat(legacyKey, defaultValue);
        }

        private static void SaveVolume(string key, float volume)
        {
            PlayerPrefs.SetInt(
                PlayerPrefsSettingsRepository.InitializedKey,
                1);
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            StopBgmTransitionRoutine();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnbindSceneTransitionService();
            Instance = null;
        }

        private void OnValidate()
        {
            bgmFadeOutDuration = Mathf.Max(0f, bgmFadeOutDuration);
            bgmFadeInDuration = Mathf.Max(0f, bgmFadeInDuration);
        }
    }

}
