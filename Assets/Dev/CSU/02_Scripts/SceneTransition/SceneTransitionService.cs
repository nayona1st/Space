using System;
using System.Collections;
using UnityEngine;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class SceneTransitionService :
        MonoBehaviour,
        ISceneTransitionService
    {
        private static SceneTransitionService _instance;

        [Header("Dependencies")]
        [Tooltip("MonoBehaviour that implements IScreenFader.")]
        [SerializeField] private MonoBehaviour screenFaderSource;

        [Tooltip("MonoBehaviour that implements ISceneLoader.")]
        [SerializeField] private MonoBehaviour sceneLoaderSource;

        [Header("Timing")]
        [Tooltip("Seconds used to cover the current scene before loading.")]
        [Min(0f)]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Tooltip("Seconds used to reveal the initial or newly loaded scene.")]
        [Min(0f)]
        [SerializeField] private float fadeInDuration = 0.5f;

        private IScreenFader _screenFader;
        private ISceneLoader _sceneLoader;
        private Coroutine _activeCoroutine;
        private bool _isOwner;
        private bool _warnedInvalidConfiguration;
        private bool _warnedLoadFailure;

        public bool IsTransitioning { get; private set; }

        public event Action<string> TransitionStarted;
        public event Action<string> TransitionCompleted;

        internal static bool HasActiveInstance =>
            _instance != null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _isOwner = true;

            if (!SceneTransitions.Register(this))
            {
                _isOwner = false;
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            if (!ResolveDependencies())
            {
                IsTransitioning = false;
                WarnInvalidConfigurationOnce();
                ClearFadeIfPossible();
                return;
            }

            _screenFader.SetAlpha(1f);
            IsTransitioning = true;
        }

        private void Start()
        {
            if (!_isOwner)
            {
                return;
            }

            if (!ResolveDependencies())
            {
                IsTransitioning = false;
                WarnInvalidConfigurationOnce();
                ClearFadeIfPossible();
                return;
            }

            _activeCoroutine = StartCoroutine(InitialFadeInRoutine());
        }

        public bool TryLoadScene(string sceneName)
        {
            if (!_isOwner || IsTransitioning)
            {
                return false;
            }

            if (!ResolveDependencies())
            {
                WarnInvalidConfigurationOnce();
                ClearFadeIfPossible();
                return false;
            }

            string normalizedSceneName = string.IsNullOrWhiteSpace(sceneName)
                ? string.Empty
                : sceneName.Trim();

            try
            {
                if (!_sceneLoader.CanLoadScene(normalizedSceneName))
                {
                    WarnLoadFailureOnce(
                        normalizedSceneName,
                        "The scene is not available in the player build.");
                    return false;
                }
            }
            catch (Exception exception)
            {
                WarnLoadFailureOnce(
                    normalizedSceneName,
                    exception.Message);
                return false;
            }

            IsTransitioning = true;
            TransitionStarted?.Invoke(normalizedSceneName);
            _activeCoroutine = StartCoroutine(
                TransitionRoutine(normalizedSceneName));
            return true;
        }

        private IEnumerator InitialFadeInRoutine()
        {
            yield return null;
            yield return _screenFader.FadeTo(0f, fadeInDuration);

            IsTransitioning = false;
            _activeCoroutine = null;
        }

        private IEnumerator TransitionRoutine(string sceneName)
        {
            yield return _screenFader.FadeTo(1f, fadeOutDuration);

            AsyncOperation loadOperation = null;
            string loadFailureReason = null;

            try
            {
                loadOperation = _sceneLoader.LoadSceneAsync(sceneName);
            }
            catch (Exception exception)
            {
                loadFailureReason = exception.Message;
            }

            if (!string.IsNullOrEmpty(loadFailureReason))
            {
                yield return RecoverFromLoadFailure(
                    sceneName,
                    loadFailureReason);
                yield break;
            }

            if (loadOperation == null)
            {
                yield return RecoverFromLoadFailure(
                    sceneName,
                    "The scene loader returned no operation.");
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return _screenFader.FadeTo(0f, fadeInDuration);

            IsTransitioning = false;
            _activeCoroutine = null;
            TransitionCompleted?.Invoke(sceneName);
        }

        private IEnumerator RecoverFromLoadFailure(
            string sceneName,
            string reason)
        {
            WarnLoadFailureOnce(sceneName, reason);
            yield return _screenFader.FadeTo(0f, fadeInDuration);

            IsTransitioning = false;
            _activeCoroutine = null;
        }

        private bool ResolveDependencies()
        {
            _screenFader = ResolveDependency<IScreenFader>(
                ref screenFaderSource);
            _sceneLoader = ResolveDependency<ISceneLoader>(
                ref sceneLoaderSource);

            return _screenFader != null && _sceneLoader != null;
        }

        private T ResolveDependency<T>(
            ref MonoBehaviour source)
            where T : class
        {
            if (source != null && source is T assignedDependency)
            {
                return assignedDependency;
            }

            MonoBehaviour[] candidates =
                GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate is T dependency)
                {
                    source = candidate;
                    return dependency;
                }
            }

            source = null;
            return null;
        }

        private void ClearFadeIfPossible()
        {
            if (_screenFader != null)
            {
                _screenFader.SetAlpha(0f);
            }
        }

        private void WarnInvalidConfigurationOnce()
        {
            if (_warnedInvalidConfiguration)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(SceneTransitionService)} on '{name}' requires "
                + "components implementing IScreenFader and ISceneLoader.",
                this);
            _warnedInvalidConfiguration = true;
        }

        private void WarnLoadFailureOnce(
            string sceneName,
            string reason)
        {
            if (_warnedLoadFailure)
            {
                return;
            }

            string displayName = string.IsNullOrEmpty(sceneName)
                ? "<empty>"
                : sceneName;

            Debug.LogWarning(
                $"{nameof(SceneTransitionService)} could not load scene "
                + $"'{displayName}'. {reason}",
                this);
            _warnedLoadFailure = true;
        }

        private void OnDestroy()
        {
            if (!_isOwner)
            {
                return;
            }

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            IsTransitioning = false;
            SceneTransitions.Unregister(this);

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnValidate()
        {
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
        }

        internal static void ResetStaticState()
        {
            _instance = null;
        }
    }
}
