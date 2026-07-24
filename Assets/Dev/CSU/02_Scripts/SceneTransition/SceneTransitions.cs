using UnityEngine;

namespace Dev.CSU._02_Scripts.SceneTransition
{
    public static class SceneTransitions
    {
        private static ISceneTransitionService _service;

        public static bool IsTransitioning =>
            TryGetService(out ISceneTransitionService service)
            && service.IsTransitioning;

        public static bool TryGetService(
            out ISceneTransitionService service)
        {
            if (!IsServiceAlive(_service))
            {
                _service = null;
                service = null;
                return false;
            }

            service = _service;
            return true;
        }

        public static bool TryLoadScene(string sceneName)
        {
            return TryGetService(out ISceneTransitionService service)
                && service.TryLoadScene(sceneName);
        }

        internal static bool Register(
            ISceneTransitionService service)
        {
            if (!IsServiceAlive(service))
            {
                return false;
            }

            if (IsServiceAlive(_service)
                && !ReferenceEquals(_service, service))
            {
                return false;
            }

            _service = service;
            return true;
        }

        internal static void Unregister(
            ISceneTransitionService service)
        {
            if (ReferenceEquals(_service, service))
            {
                _service = null;
            }
        }

        internal static void ResetRegistration()
        {
            _service = null;
        }

        private static bool IsServiceAlive(
            ISceneTransitionService service)
        {
            if (service == null)
            {
                return false;
            }

            return !(service is Object unityObject)
                || unityObject != null;
        }
    }
}
