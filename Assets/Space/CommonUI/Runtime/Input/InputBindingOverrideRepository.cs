using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceGame.CommonUI.Input
{
    public interface IInputBindingOverrideRepository
    {
        string LoadJson();
        void SaveJson(string json);
    }

    public sealed class PlayerPrefsInputBindingOverrideRepository :
        IInputBindingOverrideRepository
    {
        public const string OverridesKey =
            "SpaceGame.CommonUI.v1.InputBindings";

        public string LoadJson()
        {
            return PlayerPrefs.GetString(OverridesKey, string.Empty);
        }

        public void SaveJson(string json)
        {
            PlayerPrefs.SetString(OverridesKey, json ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    public static class InputBindingOverrideUtility
    {
        public static void Restore(InputBindingCatalog catalog, string json)
        {
            if (catalog == null || catalog.ActionAsset == null)
            {
                return;
            }

            catalog.ActionAsset.RemoveAllBindingOverrides();
            if (!string.IsNullOrWhiteSpace(json))
            {
                catalog.ActionAsset.LoadBindingOverridesFromJson(json);
            }

            catalog.NotifyBindingsChanged();
        }
    }
}
