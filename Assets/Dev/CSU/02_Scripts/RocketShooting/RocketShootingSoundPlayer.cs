using System.Collections.Generic;
using Dev.NKY.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    public enum RocketShootingSoundCue
    {
        RocketLaunch,
        RocketPreLaunch,
        UIClick,
        UIHover,
        PartEquip,
        PartEquipFailed,
        PartDelete,
        PartStatsOpen,
        PartStatsClose,
        PartUpgrade,
        PartDraw,
        UpgradePopupOpen,
        UpgradePopupClose
    }

    public static class RocketShootingSoundPlayer
    {
        private const string RocketSceneName = "Rocket Shooting";
        private const string ResourceRoot =
            "SFX/RocketShooting/";

        private static readonly Dictionary<
            RocketShootingSoundCue,
            AudioClip> Clips =
            new Dictionary<RocketShootingSoundCue, AudioClip>();

        private static readonly HashSet<RocketShootingSoundCue>
            MissingClipWarnings =
                new HashSet<RocketShootingSoundCue>();

        private static AudioSource _rocketLaunchLoopSource;
        private static int _rocketLaunchSceneHandle = -1;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Clips.Clear();
            MissingClipWarnings.Clear();
            _rocketLaunchLoopSource = null;
            _rocketLaunchSceneHandle = -1;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneListener()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public static void Play(RocketShootingSoundCue cue)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != RocketSceneName)
            {
                return;
            }

            AudioClip clip = GetClip(cue);
            if (clip == null)
            {
                WarnMissingClipOnce(cue);
                return;
            }

            SoundManager.EnsureRuntimeInstance();
            SoundManager soundManager = SoundManager.Instance;
            if (soundManager == null)
            {
                return;
            }

            float volume = GetVolume(cue);
            if (cue == RocketShootingSoundCue.RocketLaunch)
            {
                PlayRocketLaunchLoop(
                    soundManager,
                    clip,
                    volume,
                    activeScene.handle);
            }
            else if (cue == RocketShootingSoundCue.UIClick
                || cue == RocketShootingSoundCue.UIHover)
            {
                soundManager.PlayUI(clip, volume);
            }
            else
            {
                soundManager.PlaySFX(clip, volume);
            }
        }

        private static void PlayRocketLaunchLoop(
            SoundManager soundManager,
            AudioClip clip,
            float volume,
            int sceneHandle)
        {
            if (_rocketLaunchLoopSource != null
                && _rocketLaunchLoopSource.isPlaying
                && _rocketLaunchLoopSource.clip == clip)
            {
                return;
            }

            StopRocketLaunchLoop();
            _rocketLaunchLoopSource =
                soundManager.PlayLoopingSFX(clip, volume);
            _rocketLaunchSceneHandle =
                _rocketLaunchLoopSource != null
                    ? sceneHandle
                    : -1;
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode)
        {
            if (_rocketLaunchLoopSource == null)
            {
                return;
            }

            if (scene.name != RocketSceneName
                || scene.handle != _rocketLaunchSceneHandle)
            {
                StopRocketLaunchLoop();
            }
        }

        private static void StopRocketLaunchLoop()
        {
            if (_rocketLaunchLoopSource != null)
            {
                SoundManager soundManager = SoundManager.Instance;
                if (soundManager != null)
                {
                    soundManager.StopSFX(
                        _rocketLaunchLoopSource);
                }
                else
                {
                    _rocketLaunchLoopSource.Stop();
                    _rocketLaunchLoopSource.loop = false;
                    _rocketLaunchLoopSource.clip = null;
                }
            }

            _rocketLaunchLoopSource = null;
            _rocketLaunchSceneHandle = -1;
        }

        private static AudioClip GetClip(
            RocketShootingSoundCue cue)
        {
            if (Clips.TryGetValue(cue, out AudioClip clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>(
                ResourceRoot + GetResourceName(cue));
            Clips[cue] = clip;
            return clip;
        }

        private static string GetResourceName(
            RocketShootingSoundCue cue)
        {
            switch (cue)
            {
                case RocketShootingSoundCue.RocketLaunch:
                    return "RocketLaunch";
                case RocketShootingSoundCue.RocketPreLaunch:
                    return "RocketPreLaunch";
                case RocketShootingSoundCue.UIClick:
                    return "UIClick";
                case RocketShootingSoundCue.UIHover:
                    return "UIHover";
                case RocketShootingSoundCue.PartEquip:
                    return "PartEquip";
                case RocketShootingSoundCue.PartEquipFailed:
                    return "PartEquipFailed";
                case RocketShootingSoundCue.PartDelete:
                    return "PartDelete";
                case RocketShootingSoundCue.PartStatsOpen:
                    return "PartStatsOpen";
                case RocketShootingSoundCue.PartStatsClose:
                    return "PartStatsClose";
                case RocketShootingSoundCue.PartUpgrade:
                    return "PartUpgrade";
                case RocketShootingSoundCue.PartDraw:
                    return "PartDraw";
                case RocketShootingSoundCue.UpgradePopupOpen:
                case RocketShootingSoundCue.UpgradePopupClose:
                    return "UpgradePopup";
                default:
                    return string.Empty;
            }
        }

        private static float GetVolume(
            RocketShootingSoundCue cue)
        {
            switch (cue)
            {
                case RocketShootingSoundCue.RocketLaunch:
                    return 0.75f;
                case RocketShootingSoundCue.UIHover:
                    return 0.35f;
                case RocketShootingSoundCue.UIClick:
                    return 0.55f;
                case RocketShootingSoundCue.PartStatsOpen:
                case RocketShootingSoundCue.PartStatsClose:
                    return 0.5f;
                default:
                    return 0.7f;
            }
        }

        private static void WarnMissingClipOnce(
            RocketShootingSoundCue cue)
        {
            if (!MissingClipWarnings.Add(cue))
            {
                return;
            }

            Debug.LogWarning(
                "[RocketShootingSoundPlayer] Missing Resources/"
                + ResourceRoot
                + GetResourceName(cue)
                + " audio clip.");
        }
    }
}
