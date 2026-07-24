using Dev.CSU._02_Scripts.SceneTransition;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.MainMenu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuRocketLaunchButton : MonoBehaviour
    {
        private const string DefaultLaunchSceneName = "Rocket Shooting";

        [Header("References")]
        [SerializeField] private Button launchButton;

        [Header("Transition")]
        [SerializeField] private string launchSceneName =
            DefaultLaunchSceneName;

        private bool _launchRequested;

        public bool LaunchRequested => _launchRequested;

        public void Launch()
        {
            if (_launchRequested)
            {
                return;
            }

            if (launchButton == null)
            {
                launchButton = GetComponent<Button>();
            }

            _launchRequested = true;
            launchButton.interactable = false;

            if (SceneTransitions.TryLoadScene(launchSceneName))
            {
                return;
            }

            _launchRequested = false;
            launchButton.interactable = true;
            Debug.LogError(
                $"{nameof(MainMenuRocketLaunchButton)} on '{name}' could not "
                + $"start the existing fade transition to "
                + $"'{launchSceneName}'.",
                this);
        }

        private void Reset()
        {
            launchButton = GetComponent<Button>();
            launchSceneName = DefaultLaunchSceneName;
        }

        private void OnValidate()
        {
            if (launchButton == null)
            {
                launchButton = GetComponent<Button>();
            }

            launchSceneName = string.IsNullOrWhiteSpace(launchSceneName)
                ? DefaultLaunchSceneName
                : launchSceneName.Trim();
        }
    }
}
