using SpaceGame.CommonUI.Input;
using SpaceGame.CommonUI.Modal;
using SpaceGame.CommonUI.Pause;
using SpaceGame.CommonUI.Settings;
using SpaceGame.CommonUI.Display;

namespace SpaceGame.CommonUI.Views
{
    public sealed class CommonUIContext
    {
        public SettingsCoordinator Settings { get; }
        public IInputBindingOverrideRepository BindingRepository { get; }
        public InputBindingCatalog BindingCatalog { get; }
        public IScreenSettingsApplier ScreenApplier { get; }
        public PauseRequestService PauseService { get; }
        public ModalCancelRouter CancelRouter { get; }
        public ModalInputGate InputGate { get; }

        public CommonUIContext(
            SettingsCoordinator settings,
            IInputBindingOverrideRepository bindingRepository,
            InputBindingCatalog bindingCatalog,
            IScreenSettingsApplier screenApplier,
            PauseRequestService pauseService,
            ModalCancelRouter cancelRouter,
            ModalInputGate inputGate)
        {
            Settings = settings;
            BindingRepository = bindingRepository;
            BindingCatalog = bindingCatalog;
            ScreenApplier = screenApplier;
            PauseService = pauseService;
            CancelRouter = cancelRouter;
            InputGate = inputGate;
        }
    }
}
