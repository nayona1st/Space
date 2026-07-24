namespace SpaceGame.CommonUI.Settings
{
    public interface ISettingsRepository
    {
        bool TryLoad(out GameSettingsData settings);
        void Save(GameSettingsData settings);
    }
}
