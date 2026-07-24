using System;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    public interface ISceneTransitionService
    {
        bool IsTransitioning { get; }

        event Action<string> TransitionStarted;
        event Action<string> TransitionCompleted;

        bool TryLoadScene(string sceneName);
    }
}
