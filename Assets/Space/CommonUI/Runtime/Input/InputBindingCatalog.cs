using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceGame.CommonUI.Input
{
    [Serializable]
    public sealed class InputBindingDefinition
    {
        [SerializeField] private string displayName;
        [SerializeField] private InputActionReference action;
        [SerializeField] private string bindingId;
        [SerializeField] private string controlScheme;

        public string DisplayName => displayName;
        public InputActionReference ActionReference => action;
        public string BindingId => bindingId;
        public string ControlScheme => controlScheme;

        public void Configure(
            string name,
            InputActionReference actionReference,
            string id,
            string scheme)
        {
            displayName = name;
            action = actionReference;
            bindingId = id;
            controlScheme = scheme;
        }

        public bool TryGetBindingIndex(out int bindingIndex)
        {
            bindingIndex = -1;
            InputAction inputAction = action?.action;
            if (inputAction == null ||
                !Guid.TryParse(bindingId, out Guid parsedId))
            {
                return false;
            }

            bindingIndex = inputAction.bindings.IndexOf(binding =>
                binding.id == parsedId);
            return bindingIndex >= 0;
        }

        public string GetEffectivePath()
        {
            if (!TryGetBindingIndex(out int index))
            {
                return string.Empty;
            }

            return action.action.bindings[index].effectivePath;
        }

        public string GetDisplayString()
        {
            if (!TryGetBindingIndex(out int index))
            {
                return "미지정";
            }

            string value = action.action.GetBindingDisplayString(
                index,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
            return string.IsNullOrWhiteSpace(value) ? "미지정" : value;
        }
    }

    [CreateAssetMenu(
        fileName = "InputBindingCatalog",
        menuName = "Space/Common UI/Input Binding Catalog")]
    public sealed class InputBindingCatalog : ScriptableObject
    {
        [SerializeField] private InputActionAsset actionAsset;
        [SerializeField] private InputActionReference cancelAction;
        [SerializeField] private List<InputBindingDefinition> bindings =
            new List<InputBindingDefinition>();
        [SerializeField] private List<string> forbiddenControlPaths =
            new List<string>();
        [SerializeField] private List<string> gameplayActionMapIds =
            new List<string>();

        public event Action BindingsChanged;

        public InputActionAsset ActionAsset => actionAsset;
        public InputActionReference CancelAction => cancelAction;
        public IReadOnlyList<InputBindingDefinition> Bindings => bindings;
        public IReadOnlyList<string> ForbiddenControlPaths =>
            forbiddenControlPaths;
        public IReadOnlyList<string> GameplayActionMapIds =>
            gameplayActionMapIds;

        public void Configure(
            InputActionAsset asset,
            InputActionReference modalCancelAction,
            IEnumerable<InputBindingDefinition> definitions,
            IEnumerable<string> forbiddenPaths,
            IEnumerable<string> gameplayMapIds)
        {
            actionAsset = asset;
            cancelAction = modalCancelAction;
            bindings = definitions == null
                ? new List<InputBindingDefinition>()
                : new List<InputBindingDefinition>(definitions);
            forbiddenControlPaths = forbiddenPaths == null
                ? new List<string>()
                : new List<string>(forbiddenPaths);
            gameplayActionMapIds = gameplayMapIds == null
                ? new List<string>()
                : new List<string>(gameplayMapIds);
        }

        public bool IsForbidden(InputControl control)
        {
            if (control == null)
            {
                return true;
            }

            foreach (string forbiddenPath in forbiddenControlPaths)
            {
                if (InputControlPath.Matches(forbiddenPath, control))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasDuplicate(
            InputBindingDefinition source,
            InputControl control,
            out InputBindingDefinition duplicate)
        {
            foreach (InputBindingDefinition candidate in bindings)
            {
                if (ReferenceEquals(candidate, source))
                {
                    continue;
                }

                if (!string.Equals(
                        candidate.ControlScheme,
                        source.ControlScheme,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string effectivePath = candidate.GetEffectivePath();
                if (!string.IsNullOrWhiteSpace(effectivePath) &&
                    InputControlPath.Matches(effectivePath, control))
                {
                    duplicate = candidate;
                    return true;
                }
            }

            duplicate = null;
            return false;
        }

        public void RemoveAllOverrides()
        {
            actionAsset?.RemoveAllBindingOverrides();
            NotifyBindingsChanged();
        }

        public void NotifyBindingsChanged()
        {
            BindingsChanged?.Invoke();
        }
    }
}
