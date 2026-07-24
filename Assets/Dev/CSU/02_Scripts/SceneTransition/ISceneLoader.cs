using UnityEngine;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    public interface ISceneLoader
    {
        bool CanLoadScene(string sceneName);
        AsyncOperation LoadSceneAsync(string sceneName);
    }
}
