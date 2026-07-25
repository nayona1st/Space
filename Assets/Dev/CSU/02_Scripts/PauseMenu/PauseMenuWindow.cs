using System;
using System.Collections;
using Dev.CSU._02_Scripts.SceneTransition;
using SpaceGame.CommonUI;
using SpaceGame.CommonUI.Views;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.PauseMenu
{
    [DefaultExecutionOrder(-31000)]
    [DisallowMultipleComponent]
    public sealed class PauseMenuWindow : ModalWindowBase
    {
        private const int GlobalCancelPriority = -100;
        private const string DefaultGameplaySceneName = "InGame";
        private const string DefaultMainMenuSceneName = "MainMenu";

        [Header("Integration")]
        [SerializeField] private CommonUIRoot commonUIRoot;
        [SerializeField] private string gameplaySceneName =
            DefaultGameplaySceneName;
        [SerializeField] private string mainMenuSceneName =
            DefaultMainMenuSceneName;

        [Header("Panels")]
        [SerializeField] private GameObject mainPausePanel;
        [SerializeField] private GameObject exitChoicePanel;
        [SerializeField] private CanvasGroup mainPauseCanvasGroup;
        [SerializeField] private CanvasGroup exitChoiceCanvasGroup;

        [Header("Main Pause Actions")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Exit Actions")]
        [SerializeField] private Button returnToMainMenuButton;
        [SerializeField] private Button quitGameButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Default Selection")]
        [SerializeField] private Button mainDefaultSelection;
        [SerializeField] private Button exitDefaultSelection;

        [Header("Animation")]
        [Min(0f)]
        [SerializeField] private float panelFadeDuration = 0.18f;

        [Header("Responsive Layout")]
        [SerializeField] private Vector2 responsiveSafePadding =
            new Vector2(24f, 24f);

        private IDisposable _globalCancelRegistration;
        private Coroutine _panelTransitionRoutine;
        private Coroutine _settingsReturnRoutine;
        private Coroutine _selectionRoutine;
        private IGameQuitService _quitService;
        private GameObject _selectionBeforeOpen;
        private bool _isInitialized;
        private bool _showingExitChoices;
        private bool _waitingForSettings;
        private bool _transitionRequested;
        private bool _quitRequested;

        public bool IsShowingExitChoices => _showingExitChoices;
        public bool IsWaitingForSettings => _waitingForSettings;

        public void ConfigureView(
            CommonUIRoot root,
            GameObject pausePanel,
            GameObject choicesPanel,
            CanvasGroup pauseGroup,
            CanvasGroup choicesGroup,
            Button continueAction,
            Button settingsAction,
            Button exitAction,
            Button mainMenuAction,
            Button quitAction,
            Button pauseDefault,
            Button choicesDefault,
            TMP_Text status,
            float transitionDuration)
        {
            commonUIRoot = root;
            mainPausePanel = pausePanel;
            exitChoicePanel = choicesPanel;
            mainPauseCanvasGroup = pauseGroup;
            exitChoiceCanvasGroup = choicesGroup;
            continueButton = continueAction;
            settingsButton = settingsAction;
            exitButton = exitAction;
            returnToMainMenuButton = mainMenuAction;
            quitGameButton = quitAction;
            mainDefaultSelection = pauseDefault;
            exitDefaultSelection = choicesDefault;
            statusText = status;
            panelFadeDuration = Mathf.Max(0f, transitionDuration);
        }

        public void ConfigureSceneRouting(
            string gameplayScene,
            string mainMenuScene = DefaultMainMenuSceneName)
        {
            gameplaySceneName = string.IsNullOrWhiteSpace(gameplayScene)
                ? DefaultGameplaySceneName
                : gameplayScene;
            mainMenuSceneName = string.IsNullOrWhiteSpace(mainMenuScene)
                ? DefaultMainMenuSceneName
                : mainMenuScene;
        }

        public void SetQuitServiceForTests(IGameQuitService quitService)
        {
            _quitService = quitService;
        }

        private void Awake()
        {
            if (commonUIRoot == null)
            {
                commonUIRoot = GetComponentInParent<CommonUIRoot>(true);
            }

            if (commonUIRoot == null || commonUIRoot.Context == null)
            {
                Debug.LogError(
                    $"{nameof(PauseMenuWindow)} requires an initialized "
                    + $"{nameof(CommonUIRoot)} parent.",
                    this);
                enabled = false;
                return;
            }

            _quitService ??= new ApplicationGameQuitService();
            Initialize(commonUIRoot.Context);
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                RegisterGlobalCancelHandler();
            }
        }

        private void OnDisable()
        {
            DisposeGlobalCancelHandler();
            StopManagedCoroutines();
            if (IsOpen)
            {
                CloseDirect();
            }
        }

        public override void Open()
        {
            if (!IsOpen)
            {
                _selectionBeforeOpen =
                    EventSystem.current != null
                        ? EventSystem.current.currentSelectedGameObject
                        : null;
            }

            base.Open();
        }

        protected override void OnInitialized()
        {
            _isInitialized = true;
            continueButton.onClick.AddListener(ContinueGame);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(ShowExitChoices);
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            quitGameButton.onClick.AddListener(QuitGame);
            SetStatus(string.Empty);
            SetPanelStateImmediate(showExitChoices: false);
            RegisterGlobalCancelHandler();
        }

        protected override void OnOpened()
        {
            transform.SetAsLastSibling();
            _transitionRequested = false;
            _quitRequested = false;
            _waitingForSettings = false;
            ApplyResponsivePanelScale();
            SetStatus(string.Empty);
            SetPanelStateImmediate(showExitChoices: false);
            ScheduleSelection(mainDefaultSelection);
        }

        protected override bool HandleCancel()
        {
            if (_waitingForSettings)
            {
                return false;
            }

            if (_showingExitChoices)
            {
                ShowMainPauseMenu();
                return true;
            }

            RequestClose();
            return true;
        }

        protected override void OnCloseRequested()
        {
            if (_waitingForSettings || _transitionRequested)
            {
                return;
            }

            CloseDirect();
        }

        protected override void OnClosed()
        {
            StopManagedCoroutines();
            _showingExitChoices = false;
            _waitingForSettings = false;
            _transitionRequested = false;
            SetPanelStateImmediate(showExitChoices: false);

            if (EventSystem.current != null
                && _selectionBeforeOpen != null
                && _selectionBeforeOpen.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(
                    _selectionBeforeOpen);
            }

            _selectionBeforeOpen = null;
        }

        protected override void OnDestroy()
        {
            DisposeGlobalCancelHandler();
            StopManagedCoroutines();

            if (_isInitialized)
            {
                continueButton.onClick.RemoveListener(ContinueGame);
                settingsButton.onClick.RemoveListener(OpenSettings);
                exitButton.onClick.RemoveListener(ShowExitChoices);
                returnToMainMenuButton.onClick.RemoveListener(
                    ReturnToMainMenu);
                quitGameButton.onClick.RemoveListener(QuitGame);
            }

            base.OnDestroy();
        }

        private bool HandleGlobalCancel()
        {
            if (IsOpen
                || !isActiveAndEnabled
                || SceneTransitions.IsTransitioning
                || !string.Equals(
                    SceneManager.GetActiveScene().name,
                    gameplaySceneName,
                    StringComparison.Ordinal))
            {
                return false;
            }

            Open();
            return IsOpen;
        }

        private void RegisterGlobalCancelHandler()
        {
            if (_globalCancelRegistration != null || Context == null)
            {
                return;
            }

            _globalCancelRegistration = Context.CancelRouter.Push(
                HandleGlobalCancel,
                GlobalCancelPriority);
        }

        private void DisposeGlobalCancelHandler()
        {
            _globalCancelRegistration?.Dispose();
            _globalCancelRegistration = null;
        }

        private void ContinueGame()
        {
            if (!_waitingForSettings && !_transitionRequested)
            {
                RequestClose();
            }
        }

        private void OpenSettings()
        {
            if (_waitingForSettings
                || _transitionRequested
                || commonUIRoot == null
                || commonUIRoot.SettingsWindow == null)
            {
                return;
            }

            _waitingForSettings = true;
            SetCurrentPanelInteraction(false);
            commonUIRoot.SettingsWindow.transform.SetAsLastSibling();
            commonUIRoot.OpenSettings();

            if (!commonUIRoot.SettingsWindow.IsOpen)
            {
                _waitingForSettings = false;
                SetCurrentPanelInteraction(true);
                SetStatus("설정창을 열 수 없습니다.");
                Debug.LogError(
                    "Pause menu could not open the CommonUI settings window.",
                    this);
                return;
            }

            _settingsReturnRoutine = StartCoroutine(
                WaitForSettingsToClose());
        }

        private IEnumerator WaitForSettingsToClose()
        {
            while (commonUIRoot != null
                   && commonUIRoot.SettingsWindow != null
                   && commonUIRoot.SettingsWindow.IsOpen)
            {
                yield return null;
            }

            _settingsReturnRoutine = null;
            if (!IsOpen)
            {
                yield break;
            }

            _waitingForSettings = false;
            SetCurrentPanelInteraction(true);
            ScheduleSelection(settingsButton);
        }

        private void ShowExitChoices()
        {
            if (_waitingForSettings || _transitionRequested)
            {
                return;
            }

            SetStatus(string.Empty);
            StartPanelTransition(
                showExitChoices: true,
                exitDefaultSelection);
        }

        private void ShowMainPauseMenu()
        {
            SetStatus(string.Empty);
            StartPanelTransition(
                showExitChoices: false,
                exitButton);
        }

        private void StartPanelTransition(
            bool showExitChoices,
            Button selectionAfterTransition)
        {
            if (_panelTransitionRoutine != null)
            {
                StopCoroutine(_panelTransitionRoutine);
            }

            _panelTransitionRoutine = StartCoroutine(
                CrossFadePanels(
                    showExitChoices,
                    selectionAfterTransition));
        }

        private IEnumerator CrossFadePanels(
            bool showExitChoices,
            Button selectionAfterTransition)
        {
            _showingExitChoices = showExitChoices;
            mainPausePanel.SetActive(true);
            exitChoicePanel.SetActive(true);
            SetPanelGroupsInteraction(false);

            float mainStart = mainPauseCanvasGroup.alpha;
            float exitStart = exitChoiceCanvasGroup.alpha;
            float mainTarget = showExitChoices ? 0f : 1f;
            float exitTarget = showExitChoices ? 1f : 0f;
            float elapsed = 0f;

            if (panelFadeDuration > 0f)
            {
                while (elapsed < panelFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(
                        elapsed / panelFadeDuration);
                    progress = progress * progress * (3f - 2f * progress);
                    mainPauseCanvasGroup.alpha = Mathf.Lerp(
                        mainStart,
                        mainTarget,
                        progress);
                    exitChoiceCanvasGroup.alpha = Mathf.Lerp(
                        exitStart,
                        exitTarget,
                        progress);
                    yield return null;
                }
            }

            SetPanelStateImmediate(showExitChoices);
            _panelTransitionRoutine = null;
            ScheduleSelection(selectionAfterTransition);
        }

        private void ReturnToMainMenu()
        {
            if (_transitionRequested || _quitRequested)
            {
                return;
            }

            SetPanelGroupsInteraction(false);
            if (!SceneTransitions.TryLoadScene(mainMenuSceneName))
            {
                SetCurrentPanelInteraction(true);
                SetStatus("메인 메뉴로 이동할 수 없습니다.");
                Debug.LogError(
                    "Pause menu failed to request the existing scene "
                    + $"transition service to load '{mainMenuSceneName}'.",
                    this);
                return;
            }

            _transitionRequested = true;
            SetStatus("메인 메뉴로 이동 중...");
        }

        private void QuitGame()
        {
            if (_quitRequested || _transitionRequested)
            {
                return;
            }

            _quitRequested = true;
            SetPanelGroupsInteraction(false);
            SetStatus("게임 종료 요청 중...");
            _quitService.RequestQuit();
        }

        private void SetPanelStateImmediate(bool showExitChoices)
        {
            _showingExitChoices = showExitChoices;
            mainPausePanel.SetActive(!showExitChoices);
            exitChoicePanel.SetActive(showExitChoices);
            mainPauseCanvasGroup.alpha = showExitChoices ? 0f : 1f;
            exitChoiceCanvasGroup.alpha = showExitChoices ? 1f : 0f;
            SetPanelGroupsInteraction(true);
        }

        private void SetPanelGroupsInteraction(bool enabled)
        {
            bool mainEnabled =
                enabled && !_showingExitChoices && !_waitingForSettings;
            bool exitEnabled =
                enabled && _showingExitChoices && !_waitingForSettings;
            SetCanvasGroupInteraction(
                mainPauseCanvasGroup,
                mainEnabled);
            SetCanvasGroupInteraction(
                exitChoiceCanvasGroup,
                exitEnabled);
        }

        private void SetCurrentPanelInteraction(bool enabled)
        {
            SetCanvasGroupInteraction(
                _showingExitChoices
                    ? exitChoiceCanvasGroup
                    : mainPauseCanvasGroup,
                enabled);
        }

        private static void SetCanvasGroupInteraction(
            CanvasGroup group,
            bool enabled)
        {
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }

        private void ScheduleSelection(Button target)
        {
            if (_selectionRoutine != null)
            {
                StopCoroutine(_selectionRoutine);
            }

            _selectionRoutine = StartCoroutine(SelectNextFrame(target));
        }

        private IEnumerator SelectNextFrame(Button target)
        {
            yield return null;
            _selectionRoutine = null;
            if (IsOpen
                && target != null
                && target.isActiveAndEnabled
                && target.IsInteractable()
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    target.gameObject);
            }
        }

        private void StopManagedCoroutines()
        {
            if (_panelTransitionRoutine != null)
            {
                StopCoroutine(_panelTransitionRoutine);
                _panelTransitionRoutine = null;
            }

            if (_settingsReturnRoutine != null)
            {
                StopCoroutine(_settingsReturnRoutine);
                _settingsReturnRoutine = null;
            }

            if (_selectionRoutine != null)
            {
                StopCoroutine(_selectionRoutine);
                _selectionRoutine = null;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyResponsivePanelScale();
        }

        private void ApplyResponsivePanelScale()
        {
            if (transform is not RectTransform rootRect)
            {
                return;
            }

            Vector2 availableSize = rootRect.rect.size
                - responsiveSafePadding * 2f;
            FitPanelInside(
                mainPausePanel != null
                    ? mainPausePanel.transform as RectTransform
                    : null,
                availableSize);
            FitPanelInside(
                exitChoicePanel != null
                    ? exitChoicePanel.transform as RectTransform
                    : null,
                availableSize);
        }

        private static void FitPanelInside(
            RectTransform panelRect,
            Vector2 availableSize)
        {
            if (panelRect == null
                || availableSize.x <= 0f
                || availableSize.y <= 0f
                || panelRect.rect.width <= 0f
                || panelRect.rect.height <= 0f)
            {
                return;
            }

            float scale = Mathf.Clamp01(
                Mathf.Min(
                    availableSize.x / panelRect.rect.width,
                    availableSize.y / panelRect.rect.height));
            panelRect.localScale = Vector3.one * scale;
        }

        private void OnValidate()
        {
            panelFadeDuration = Mathf.Max(0f, panelFadeDuration);
            responsiveSafePadding = new Vector2(
                Mathf.Max(0f, responsiveSafePadding.x),
                Mathf.Max(0f, responsiveSafePadding.y));
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                gameplaySceneName = DefaultGameplaySceneName;
            }

            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                mainMenuSceneName = DefaultMainMenuSceneName;
            }
        }
    }

    public interface IGameQuitService
    {
        void RequestQuit();
    }

    public sealed class ApplicationGameQuitService : IGameQuitService
    {
        public void RequestQuit()
        {
            Debug.Log("Application.Quit() was requested by the pause menu.");
            Application.Quit();
        }
    }
}
