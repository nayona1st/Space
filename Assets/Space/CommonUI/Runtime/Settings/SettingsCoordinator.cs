using SpaceGame.CommonUI.Audio;
using SpaceGame.CommonUI.Display;

namespace SpaceGame.CommonUI.Settings
{
    public sealed class SettingsCoordinator
    {
        private readonly ISettingsRepository repository;
        private readonly IAudioSettingsAdapter audioAdapter;
        private readonly IScreenSettingsApplier screenApplier;

        public GameSettingsData Current { get; private set; }

        public SettingsCoordinator(
            ISettingsRepository repository,
            IAudioSettingsAdapter audioAdapter,
            IScreenSettingsApplier screenApplier)
        {
            this.repository = repository;
            this.audioAdapter = audioAdapter;
            this.screenApplier = screenApplier;
        }

        public void LoadAndApply()
        {
            if (!repository.TryLoad(out GameSettingsData loaded))
            {
                loaded = GameSettingsData.CreateDefault();
            }

            loaded.Sanitize();
            Current = loaded.Clone();
            Apply(Current);
        }

        public GameSettingsData BeginEdit()
        {
            return Current.Clone();
        }

        public void PreviewAudio(GameSettingsData workingCopy)
        {
            audioAdapter.Apply(workingCopy);
        }

        public void Commit(GameSettingsData workingCopy)
        {
            workingCopy.Sanitize();
            Current = workingCopy.Clone();
            Apply(Current);
            repository.Save(Current);
        }

        public void RestorePreview(GameSettingsData snapshot)
        {
            snapshot.Sanitize();
            Apply(snapshot);
        }

        private void Apply(GameSettingsData settings)
        {
            audioAdapter.Apply(settings);
            screenApplier.Apply(settings);
        }
    }
}
