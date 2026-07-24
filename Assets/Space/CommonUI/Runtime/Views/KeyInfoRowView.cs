using SpaceGame.CommonUI.Input;
using TMPro;
using UnityEngine;

namespace SpaceGame.CommonUI.Views
{
    [DisallowMultipleComponent]
    public sealed class KeyInfoRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text displayNameText;
        [SerializeField] private TMP_Text bindingText;

        private InputBindingDefinition definition;

        public void ConfigureView(TMP_Text nameLabel, TMP_Text bindingLabel)
        {
            displayNameText = nameLabel;
            bindingText = bindingLabel;
        }

        public void Initialize(InputBindingDefinition bindingDefinition)
        {
            definition = bindingDefinition;
            displayNameText.text = definition.DisplayName;
            Refresh();
        }

        public void Refresh()
        {
            if (definition != null)
            {
                displayNameText.SetText(definition.DisplayName);
                bindingText.SetText(definition.GetDisplayString());
                displayNameText.SetLayoutDirty();
                displayNameText.SetVerticesDirty();
                bindingText.SetLayoutDirty();
                bindingText.SetVerticesDirty();
                displayNameText.ForceMeshUpdate(true, true);
                bindingText.ForceMeshUpdate(true, true);
            }
        }
    }
}
