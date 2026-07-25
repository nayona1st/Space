using UnityEngine;

namespace Dev.NKY.Scripts
{
    [DefaultExecutionOrder(-32100)]
    [DisallowMultipleComponent]
    public sealed class SoundManagerSceneBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SoundManager.EnsureRuntimeInstance();
        }

        private void Awake()
        {
            SoundManager.EnsureRuntimeInstance();
        }
    }
}
