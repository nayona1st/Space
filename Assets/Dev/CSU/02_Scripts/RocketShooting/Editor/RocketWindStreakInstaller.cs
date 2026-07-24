using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.RocketShooting.Editor
{
    public static class RocketWindStreakInstaller
    {
        private const string TimedVerificationSessionKey =
            "CSU.RocketWindStreak.TimedVerification";
        private const string RocketScenePath =
            "Assets/Dev/CSU/01_Scenes/Rocket Shooting.unity";
        private const string RainTexturePath =
            "Assets/Dev/CSU/00_Assets/Rain.png";
        private const string StreakPrefabPath =
            "Assets/Dev/CSU/03_Prefabs/RocketWindStreak.prefab";
        private const int ExpectedSpriteCount = 18;

        [InitializeOnLoadMethod]
        private static void InitializeTimedVerificationHooks()
        {
            SceneManager.sceneLoaded -= HandleTimedSceneLoaded;
            SceneManager.sceneLoaded += HandleTimedSceneLoaded;
            EditorApplication.playModeStateChanged -=
                HandleTimedPlayModeChanged;
            EditorApplication.playModeStateChanged +=
                HandleTimedPlayModeChanged;
        }

        [MenuItem(
            "Tools/CSU/Rocket Shooting/Install Wind Streak Field")]
        public static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsRocketScene(scene))
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] Installation is allowed only "
                    + "while Rocket Shooting is the active scene. No scene "
                    + "was changed.");
                return;
            }

            RocketShootingDirector director =
                FindSingleSceneComponent<RocketShootingDirector>(
                    scene);
            if (director == null)
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] Expected exactly one "
                    + "RocketShootingDirector in Rocket Shooting.");
                return;
            }

            var serializedDirector = new SerializedObject(director);
            VerticalBackgroundScroller scroller =
                serializedDirector.FindProperty("backgroundScroller")
                    .objectReferenceValue
                as VerticalBackgroundScroller;
            if (scroller == null)
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] The Director has no explicit "
                    + "Background Scroller reference.");
                return;
            }

            var serializedScroller = new SerializedObject(scroller);
            Camera targetCamera = serializedScroller
                .FindProperty("targetCamera")
                .objectReferenceValue as Camera;
            if (targetCamera == null)
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] The Background Scroller has no "
                    + "explicit Target Camera reference.");
                return;
            }

            Sprite[] rainSprites = LoadOrderedRainSprites();
            if (rainSprites == null)
            {
                return;
            }

            SpriteRenderer prefab =
                LoadOrCreateStreakPrefab(rainSprites[0]);
            if (prefab == null)
            {
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Rocket Wind Streak Field");

            RocketWindStreakController controller =
                FindSingleSceneComponent<RocketWindStreakController>(
                    scene);
            bool createdController = controller == null;

            if (createdController)
            {
                Transform windField = FindDirectChild(
                    scroller.transform,
                    "WindField");
                if (windField == null)
                {
                    GameObject windFieldObject =
                        new GameObject("WindField");
                    Undo.RegisterCreatedObjectUndo(
                        windFieldObject,
                        "Create Rocket Wind Field");
                    windFieldObject.transform.SetParent(
                        scroller.transform,
                        false);
                    windField = windFieldObject.transform;
                }

                controller =
                    windField.GetComponent<RocketWindStreakController>();
                if (controller == null)
                {
                    controller =
                        Undo.AddComponent<RocketWindStreakController>(
                            windField.gameObject);
                }
            }

            Transform instancesRoot =
                ResolveInstancesRoot(controller);
            if (instancesRoot == null)
            {
                instancesRoot = FindDirectChild(
                    controller.transform,
                    "WindInstances");
            }

            if (instancesRoot == null)
            {
                GameObject instances =
                    new GameObject("WindInstances");
                Undo.RegisterCreatedObjectUndo(
                    instances,
                    "Create Rocket Wind Instances Root");
                instances.transform.SetParent(
                    controller.transform,
                    false);
                instancesRoot = instances.transform;
            }

            Undo.RecordObject(
                controller,
                "Configure Rocket Wind Streak Field");
            var serializedController =
                new SerializedObject(controller);
            AssignIfNull(
                serializedController,
                "launchDirector",
                director);
            AssignIfNull(
                serializedController,
                "backgroundScroller",
                scroller);
            AssignIfNull(
                serializedController,
                "targetCamera",
                targetCamera);
            AssignIfNull(
                serializedController,
                "streakPrefab",
                prefab);
            AssignIfNull(
                serializedController,
                "instancesRoot",
                instancesRoot);
            AssignMissingSprites(
                serializedController,
                rainSprites);
            serializedController.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                createdController
                    ? "[Rocket Wind Streaks] Installed WindField/"
                      + "WindInstances with explicit Director, Scroller, "
                      + "Camera, prefab, root, and Rain_0..Rain_17 "
                      + "references. Existing scene values were preserved."
                    : "[Rocket Wind Streaks] Reused the existing "
                      + "installation and filled only missing references.");
        }

        [MenuItem(
            "Tools/CSU/Rocket Shooting/Verify Wind Streak Field")]
        public static void VerifyScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsRocketScene(scene))
            {
                Debug.LogError(
                    "[Rocket Wind Streak Verifier] Rocket Shooting must be "
                    + "the active scene.");
                return;
            }

            RocketWindStreakController controller =
                FindSingleSceneComponent<RocketWindStreakController>(
                    scene);
            if (controller == null)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Verifier] Expected exactly one "
                    + "controller.");
                return;
            }

            var serialized = new SerializedObject(controller);
            string[] requiredReferences =
            {
                "launchDirector",
                "backgroundScroller",
                "targetCamera",
                "streakPrefab",
                "instancesRoot"
            };

            for (int index = 0;
                 index < requiredReferences.Length;
                 index++)
            {
                string propertyName = requiredReferences[index];
                SerializedProperty property =
                    serialized.FindProperty(propertyName);
                if (property == null
                    || property.objectReferenceValue == null)
                {
                    Debug.LogError(
                        "[Rocket Wind Streak Verifier] Missing reference: "
                        + propertyName);
                    return;
                }
            }

            SerializedProperty sprites =
                serialized.FindProperty("streakSprites");
            if (sprites == null
                || sprites.arraySize != ExpectedSpriteCount)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Verifier] Expected 18 Rain "
                    + "Sprites.");
                return;
            }

            for (int index = 0;
                 index < ExpectedSpriteCount;
                 index++)
            {
                Sprite sprite = sprites.GetArrayElementAtIndex(index)
                    .objectReferenceValue as Sprite;
                if (sprite == null
                    || sprite.name != $"Rain_{index}")
                {
                    Debug.LogError(
                        "[Rocket Wind Streak Verifier] Sprite list order "
                        + $"mismatch at index {index}.");
                    return;
                }
            }

            Transform instancesRoot = serialized
                .FindProperty("instancesRoot")
                .objectReferenceValue as Transform;
            if (controller.transform.name != "WindField"
                || controller.transform.parent == null
                || controller.transform.parent.name != "ScrollingWorld"
                || instancesRoot == null
                || instancesRoot.name != "WindInstances"
                || instancesRoot.parent != controller.transform)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Verifier] Expected hierarchy is "
                    + "ScrollingWorld/WindField/WindInstances.");
                return;
            }

            Debug.Log(
                "[Rocket Wind Streak Verifier] PASS: one controller, "
                + "explicit references, ordered Rain_0..Rain_17 Sprites, "
                + "and the expected hierarchy are connected.");
        }

        [MenuItem(
            "Tools/CSU/Rocket Shooting/Verify Runtime Wind Streak Pool")]
        public static void VerifyRuntimePool()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Runtime Verifier] Enter Play Mode "
                    + "before running this verification.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            RocketWindStreakController controller =
                FindSingleSceneComponent<RocketWindStreakController>(
                    scene);
            if (controller == null || !controller.IsInitialized)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Runtime Verifier] The active scene "
                    + "does not contain one initialized controller.");
                return;
            }

            if (controller.PoolCount
                != controller.ConfiguredPoolSize)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Runtime Verifier] Fixed pool size "
                    + $"mismatch. Configured={controller.ConfiguredPoolSize}, "
                    + $"Actual={controller.PoolCount}.");
                return;
            }

            if (!controller.HasGroundExited)
            {
                if (controller.ActiveStreakCount != 0)
                {
                    Debug.LogError(
                        "[Rocket Wind Streak Runtime Verifier] Streaks must "
                        + "remain inactive before Ground exits.");
                    return;
                }

                Debug.Log(
                    "[Rocket Wind Streak Runtime Verifier] PASS before "
                    + $"Ground exit: pool={controller.PoolCount}, active=0.");
                return;
            }

            if (controller.CurrentIntensity < 0f
                || controller.CurrentIntensity > 1f
                || controller.ActiveStreakCount < 0
                || controller.ActiveStreakCount
                    > controller.PoolCount)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Runtime Verifier] Runtime density "
                    + "or clamped intensity is invalid.");
                return;
            }

            Debug.Log(
                "[Rocket Wind Streak Runtime Verifier] PASS after Ground "
                + $"exit: pool={controller.PoolCount}, "
                + $"active={controller.ActiveStreakCount}, "
                + $"intensity={controller.CurrentIntensity:F3}, "
                + $"recycled={controller.RecycledStreakCount}.");
        }

        [MenuItem(
            "Tools/CSU/Rocket Shooting/Run Timed Transition Verification")]
        public static void RunTimedTransitionVerification()
        {
            if (EditorApplication.isPlaying
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[Rocket Wind Streak Timed Verifier] Stop Play Mode "
                    + "before starting a timed run.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!IsRocketScene(scene))
            {
                Debug.LogError(
                    "[Rocket Wind Streak Timed Verifier] Rocket Shooting "
                    + "must be the active scene.");
                return;
            }

            SessionState.SetBool(
                TimedVerificationSessionKey,
                true);
            Debug.Log(
                "[Rocket Wind Streak Timed Verifier] Starting a timed "
                + "Play Mode run. The existing transition configuration "
                + "will not be changed.");
            EditorApplication.EnterPlaymode();
        }

        private static void HandleTimedSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            if (!SessionState.GetBool(
                    TimedVerificationSessionKey,
                    false))
            {
                return;
            }

            if (scene.path == RocketScenePath)
            {
                Debug.Log(
                    "[Rocket Wind Streak Timed Verifier] Rocket Shooting "
                    + "runtime started.");
                return;
            }

            if (scene.name != "InGame")
            {
                return;
            }

            float transitionTime = Mathf.Max(0f, Time.time);
            Debug.Log(
                "[Rocket Wind Streak Timed Verifier] PASS: InGame loaded "
                + $"at game time {transitionTime:F3} seconds.");
            SessionState.SetBool(
                TimedVerificationSessionKey,
                false);
        }

        private static void HandleTimedPlayModeChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode
                || state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetBool(
                    TimedVerificationSessionKey,
                    false);
            }
        }

        private static Sprite[] LoadOrderedRainSprites()
        {
            Sprite[] sprites = AssetDatabase
                .LoadAllAssetsAtPath(RainTexturePath)
                .OfType<Sprite>()
                .Where(sprite =>
                    sprite.name.StartsWith(
                        "Rain_",
                        StringComparison.Ordinal))
                .OrderBy(GetRainSpriteIndex)
                .ToArray();

            if (sprites.Length != ExpectedSpriteCount)
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] Rain.png must provide exactly "
                    + $"Rain_0..Rain_17. Found {sprites.Length} Sprites.");
                return null;
            }

            for (int index = 0;
                 index < ExpectedSpriteCount;
                 index++)
            {
                if (sprites[index].name != $"Rain_{index}")
                {
                    Debug.LogError(
                        "[Rocket Wind Streaks] Rain Sprite sequence is "
                        + $"missing Rain_{index}.");
                    return null;
                }
            }

            return sprites;
        }

        private static SpriteRenderer LoadOrCreateStreakPrefab(
            Sprite firstSprite)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                StreakPrefabPath);
            if (prefab == null)
            {
                GameObject temporary =
                    new GameObject("RocketWindStreak");
                try
                {
                    SpriteRenderer renderer =
                        temporary.AddComponent<SpriteRenderer>();
                    renderer.sprite = firstSprite;
                    renderer.color =
                        new Color(1f, 1f, 1f, 0.38f);
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = 1;
                    prefab = PrefabUtility.SaveAsPrefabAsset(
                        temporary,
                        StreakPrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }

            if (prefab == null)
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] Could not create the streak "
                    + "prefab.");
                return null;
            }

            SpriteRenderer prefabRenderer =
                prefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer == null
                || prefabRenderer.sprite == null)
            {
                Debug.LogError(
                    "[Rocket Wind Streaks] Existing streak prefab must "
                    + "contain one SpriteRenderer with a Sprite. It was not "
                    + "overwritten.");
                return null;
            }

            return prefabRenderer;
        }

        private static int GetRainSpriteIndex(Sprite sprite)
        {
            string suffix = sprite.name.Substring("Rain_".Length);
            return int.TryParse(
                suffix,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int index)
                ? index
                : int.MaxValue;
        }

        private static void AssignMissingSprites(
            SerializedObject serialized,
            Sprite[] sprites)
        {
            SerializedProperty property =
                serialized.FindProperty("streakSprites");
            if (property == null)
            {
                return;
            }

            if (property.arraySize < sprites.Length)
            {
                property.arraySize = sprites.Length;
            }

            for (int index = 0;
                 index < sprites.Length;
                 index++)
            {
                SerializedProperty element =
                    property.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue == null)
                {
                    element.objectReferenceValue = sprites[index];
                }
            }
        }

        private static Transform ResolveInstancesRoot(
            RocketWindStreakController controller)
        {
            var serialized = new SerializedObject(controller);
            SerializedProperty property =
                serialized.FindProperty("instancesRoot");
            return property?.objectReferenceValue as Transform;
        }

        private static void AssignIfNull(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null
                && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName)
        {
            for (int index = 0;
                 index < parent.childCount;
                 index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool IsRocketScene(Scene scene)
        {
            return scene.IsValid()
                && scene.isLoaded
                && scene.path == RocketScenePath;
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
