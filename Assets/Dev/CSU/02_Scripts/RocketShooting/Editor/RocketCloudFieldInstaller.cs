using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.CSU._02_Scripts.RocketShooting.Editor
{
    public static class RocketCloudFieldInstaller
    {
        private const string RocketScenePath =
            "Assets/Dev/CSU/01_Scenes/Rocket Shooting.unity";
        private const string Cloud0Path =
            "Assets/Dev/CSU/03_Prefabs/Invironment/Cloud_0.prefab";
        private const string Cloud1Path =
            "Assets/Dev/CSU/03_Prefabs/Invironment/Cloud_1.prefab";

        [MenuItem("Tools/CSU/Rocket Shooting/Install Cloud Field")]
        public static void Install()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.SaveOpenScenes();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    RocketScenePath,
                    OpenSceneMode.Single);
                InstallIntoScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
            }
        }

        [MenuItem("Tools/CSU/Rocket Shooting/Verify Cloud Field")]
        public static void Verify()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    RocketScenePath,
                    OpenSceneMode.Single);
                VerifyScene(scene);
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
            }
        }

        [MenuItem(
            "Tools/CSU/Rocket Shooting/Verify Runtime Cloud Pool")]
        public static void VerifyRuntimePool()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Runtime Verifier] Enter Play Mode "
                    + "before running this verification.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            RocketCloudFieldController controller =
                FindSingleSceneComponent<RocketCloudFieldController>(
                    activeScene);
            if (controller == null)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Runtime Verifier] Expected exactly "
                    + "one active-scene controller.");
                return;
            }

            if (!controller.IsInitialized)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Runtime Verifier] The controller "
                    + "did not initialize.");
                return;
            }

            if (controller.PoolCount
                    != controller.ConfiguredCloudCount
                || controller.ActiveCloudCount
                    != controller.ConfiguredCloudCount)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Runtime Verifier] Pool count "
                    + $"mismatch. Configured={controller.ConfiguredCloudCount}, "
                    + $"Pool={controller.PoolCount}, "
                    + $"Active={controller.ActiveCloudCount}.");
                return;
            }

            if (controller.RecycledCloudCount <= 0)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Runtime Verifier] No cloud has "
                    + "recycled yet. Let Play Mode run longer and retry.");
                return;
            }

            Debug.Log(
                "[Rocket Cloud Field Runtime Verifier] PASS: "
                + $"{controller.PoolCount} active pooled clouds, "
                + $"{controller.RecycledCloudCount} recycle operation(s), "
                + "and no pool growth.");
        }

        private static void InstallIntoScene(Scene scene)
        {
            RocketShootingDirector director =
                FindSingleSceneComponent<RocketShootingDirector>(scene);
            if (director == null)
            {
                Debug.LogError(
                    "[Rocket Cloud Field] RocketShootingDirector was not "
                    + $"found in '{RocketScenePath}'.");
                return;
            }

            var directorObject = new SerializedObject(director);
            VerticalBackgroundScroller scroller =
                directorObject.FindProperty("backgroundScroller")
                    .objectReferenceValue
                as VerticalBackgroundScroller;
            if (scroller == null)
            {
                Debug.LogError(
                    "[Rocket Cloud Field] The director has no explicit "
                    + "VerticalBackgroundScroller reference.");
                return;
            }

            var scrollerObject = new SerializedObject(scroller);
            Camera targetCamera = scrollerObject
                .FindProperty("targetCamera")
                .objectReferenceValue as Camera;
            if (targetCamera == null)
            {
                Debug.LogError(
                    "[Rocket Cloud Field] The background scroller has no "
                    + "explicit Target Camera reference.");
                return;
            }

            GameObject cloud0 =
                AssetDatabase.LoadAssetAtPath<GameObject>(Cloud0Path);
            GameObject cloud1 =
                AssetDatabase.LoadAssetAtPath<GameObject>(Cloud1Path);
            if (cloud0 == null || cloud1 == null)
            {
                Debug.LogError(
                    "[Rocket Cloud Field] Cloud_0 or Cloud_1 prefab is "
                    + "missing.");
                return;
            }

            RocketCloudFieldController controller =
                FindSingleSceneComponent<RocketCloudFieldController>(
                    scene);
            bool createdController = controller == null;

            if (createdController)
            {
                GameObject cloudField = new GameObject("CloudField");
                Undo.RegisterCreatedObjectUndo(
                    cloudField,
                    "Create Rocket Cloud Field");
                cloudField.transform.SetParent(
                    scroller.transform,
                    false);
                controller =
                    Undo.AddComponent<RocketCloudFieldController>(
                        cloudField);
            }

            Transform instancesRoot = ResolveInstancesRoot(controller);
            if (instancesRoot == null)
            {
                GameObject instances =
                    new GameObject("CloudInstances");
                Undo.RegisterCreatedObjectUndo(
                    instances,
                    "Create Rocket Cloud Instances Root");
                instances.transform.SetParent(
                    controller.transform,
                    false);
                instancesRoot = instances.transform;
            }

            var serializedController =
                new SerializedObject(controller);
            AssignIfNull(
                serializedController,
                "launchDirector",
                director);
            AssignIfNull(
                serializedController,
                "targetCamera",
                targetCamera);
            AssignIfNull(
                serializedController,
                "cloud0Prefab",
                cloud0);
            AssignIfNull(
                serializedController,
                "cloud1Prefab",
                cloud1);
            AssignIfNull(
                serializedController,
                "cloudInstancesRoot",
                instancesRoot);

            if (createdController)
            {
                SetInt(serializedController, "cloudCount", 14);
                SetFloat(
                    serializedController,
                    "minimumVerticalSpacing",
                    2f);
                SetFloat(
                    serializedController,
                    "maximumVerticalSpacing",
                    5.5f);
                SetFloat(
                    serializedController,
                    "horizontalMargin",
                    1.5f);
                SetFloat(serializedController, "spawnMargin", 3f);
                SetFloat(serializedController, "recycleMargin", 4f);
                SetVector2(
                    serializedController,
                    "scaleRange",
                    new Vector2(0.8f, 1.35f));
                SetVector2(
                    serializedController,
                    "speedMultiplierRange",
                    new Vector2(0.72f, 1.08f));
                SetVector2(
                    serializedController,
                    "alphaRange",
                    new Vector2(0.72f, 1f));
                SetBool(
                    serializedController,
                    "allowHorizontalFlip",
                    true);
                SetBool(
                    serializedController,
                    "randomizeSeed",
                    true);
                SetInt(
                    serializedController,
                    "randomSeed",
                    20260725);
                SetInt(serializedController, "sortingOrder", 1);
                SetInt(
                    serializedController,
                    "maxRecyclesPerFrame",
                    32);
            }

            serializedController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(
                createdController
                    ? "[Rocket Cloud Field] Installed with explicit scene "
                      + "references. Existing scene objects were preserved."
                    : "[Rocket Cloud Field] Existing installation was "
                      + "reused. Missing references were repaired without "
                      + "overwriting tuned values.");
        }

        private static void VerifyScene(Scene scene)
        {
            RocketCloudFieldController[] controllers =
                FindSceneComponents<RocketCloudFieldController>(scene);
            if (controllers.Length != 1)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Verifier] Expected exactly one "
                    + $"controller, found {controllers.Length}.");
                return;
            }

            RocketCloudFieldController controller = controllers[0];
            var serialized = new SerializedObject(controller);
            string[] requiredReferenceNames =
            {
                "launchDirector",
                "targetCamera",
                "cloud0Prefab",
                "cloud1Prefab",
                "cloudInstancesRoot"
            };

            foreach (string propertyName in requiredReferenceNames)
            {
                SerializedProperty property =
                    serialized.FindProperty(propertyName);
                if (property == null
                    || property.objectReferenceValue == null)
                {
                    Debug.LogError(
                        "[Rocket Cloud Field Verifier] Missing reference: "
                        + propertyName);
                    return;
                }
            }

            Transform instancesRoot = serialized
                .FindProperty("cloudInstancesRoot")
                .objectReferenceValue as Transform;
            if (controller.transform.parent == null
                || instancesRoot == null
                || instancesRoot.parent != controller.transform)
            {
                Debug.LogError(
                    "[Rocket Cloud Field Verifier] CloudField hierarchy is "
                    + "not connected as expected.");
                return;
            }

            Debug.Log(
                "[Rocket Cloud Field Verifier] PASS: one controller, "
                + "explicit Director/Camera/prefab/root references, and "
                + "the expected scene hierarchy are connected.");
        }

        private static Transform ResolveInstancesRoot(
            RocketCloudFieldController controller)
        {
            var serialized = new SerializedObject(controller);
            SerializedProperty property =
                serialized.FindProperty("cloudInstancesRoot");
            return property?.objectReferenceValue as Transform;
        }

        private static void AssignIfNull(
            SerializedObject serialized,
            string propertyName,
            Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null
                && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetInt(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetFloat(
            SerializedObject serialized,
            string propertyName,
            float value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetVector2(
            SerializedObject serialized,
            string propertyName,
            Vector2 value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.vector2Value = value;
            }
        }

        private static T FindSingleSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            return components.Length == 1
                ? components[0]
                : null;
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
