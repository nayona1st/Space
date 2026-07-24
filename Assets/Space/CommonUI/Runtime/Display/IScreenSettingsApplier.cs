using System.Collections.Generic;
using SpaceGame.CommonUI.Settings;

namespace SpaceGame.CommonUI.Display
{
    public interface IScreenSettingsApplier
    {
        IReadOnlyList<ResolutionOption> GetAvailableResolutions();
        void Apply(GameSettingsData settings);
    }
}
