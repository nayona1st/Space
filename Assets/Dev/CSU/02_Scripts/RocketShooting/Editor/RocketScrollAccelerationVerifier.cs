using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.RocketShooting.Editor
{
    public static class RocketScrollAccelerationVerifier
    {
        private const float SpeedTolerance = 0.01f;
        private const int CurveSamples = 100;

        [MenuItem(
            "Tools/CSU/Rocket Shooting/Verify Runtime Scroll Acceleration")]
        public static void VerifyRuntimeAcceleration()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError(
                    "[Rocket Scroll Acceleration Verifier] Enter Play Mode "
                    + "before running this verification.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            RocketShootingDirector director =
                FindSingleSceneComponent<RocketShootingDirector>(activeScene);
            if (director == null)
            {
                Debug.LogError(
                    "[Rocket Scroll Acceleration Verifier] Expected exactly "
                    + "one RocketShootingDirector in the active scene.");
                return;
            }

            var serializedDirector = new SerializedObject(director);
            VerticalBackgroundScroller scroller =
                serializedDirector.FindProperty("backgroundScroller")
                    .objectReferenceValue as VerticalBackgroundScroller;
            if (scroller == null)
            {
                Debug.LogError(
                    "[Rocket Scroll Acceleration Verifier] The background "
                    + "scroller reference is missing.");
                return;
            }

            if (!VerifyConfiguredCurve(director))
            {
                return;
            }

            if (director.Phase == LaunchPhase.Idle
                || director.Phase == LaunchPhase.Ignition)
            {
                if (director.CurrentScrollSpeed > SpeedTolerance)
                {
                    Debug.LogError(
                        "[Rocket Scroll Acceleration Verifier] Scroll speed "
                        + "must remain zero before LiftOff.");
                    return;
                }
            }
            else
            {
                if (director.CurrentScrollSpeed
                        < director.StartScrollSpeed - SpeedTolerance
                    || director.CurrentScrollSpeed
                        > director.MaximumScrollSpeed + SpeedTolerance)
                {
                    Debug.LogError(
                        "[Rocket Scroll Acceleration Verifier] Runtime speed "
                        + "is outside its configured range.");
                    return;
                }
            }

            if (Mathf.Abs(
                    scroller.ScrollSpeed
                    - director.CurrentScrollSpeed)
                > SpeedTolerance)
            {
                Debug.LogError(
                    "[Rocket Scroll Acceleration Verifier] Background speed "
                    + "does not match CurrentScrollSpeed.");
                return;
            }

            Debug.Log(
                "[Rocket Scroll Acceleration Verifier] PASS: "
                + $"phase={director.Phase}, "
                + $"speed={director.CurrentScrollSpeed:F2}, "
                + $"progress={director.AccelerationProgress:P0}, "
                + $"altitude={director.Altitude:F2}.");
        }

        private static bool VerifyConfiguredCurve(
            RocketShootingDirector director)
        {
            float previousSpeed =
                director.EvaluateConfiguredScrollSpeed(0f);
            if (Mathf.Abs(
                    previousSpeed - director.StartScrollSpeed)
                > SpeedTolerance)
            {
                Debug.LogError(
                    "[Rocket Scroll Acceleration Verifier] The curve does "
                    + "not begin at the configured start speed.");
                return false;
            }

            for (int sample = 1; sample <= CurveSamples; sample++)
            {
                float normalizedProgress =
                    (float)sample / CurveSamples;
                float sampledSpeed =
                    director.EvaluateConfiguredScrollSpeed(
                        normalizedProgress);
                if (sampledSpeed + SpeedTolerance < previousSpeed)
                {
                    Debug.LogError(
                        "[Rocket Scroll Acceleration Verifier] The configured "
                        + $"curve decreases near {normalizedProgress:P0}.");
                    return false;
                }

                previousSpeed = sampledSpeed;
            }

            if (Mathf.Abs(
                    previousSpeed - director.MaximumScrollSpeed)
                > SpeedTolerance)
            {
                Debug.LogError(
                    "[Rocket Scroll Acceleration Verifier] The curve does "
                    + "not finish at the configured maximum speed.");
                return false;
            }

            return true;
        }

        private static T FindSingleSceneComponent<T>(Scene scene)
            where T : Component
        {
            T found = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                T[] candidates =
                    roots[rootIndex].GetComponentsInChildren<T>(true);
                for (int candidateIndex = 0;
                     candidateIndex < candidates.Length;
                     candidateIndex++)
                {
                    T candidate = candidates[candidateIndex];
                    if (found != null && found != candidate)
                    {
                        return null;
                    }

                    found = candidate;
                }
            }

            return found;
        }
    }
}
