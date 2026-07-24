using System;
using Dev.CSU._02_Scripts.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.MainMenu.Editor
{
    internal static class MainMenuButtonHoverInstaller
    {
        private const string ScenePath =
            "Assets/Dev/CSU/01_Scenes/MainMenu.unity";
        private const string ShaderName =
            "Space/UI/Main Menu Hover Glow";
        private const string MaterialFolder =
            "Assets/Dev/CSU/08_Material/UI";
        private const string MaterialPath =
            MaterialFolder + "/M_MainMenuHoverGlow.mat";
        private const float GlowExpansion = 18f;

        private static readonly string[] ButtonNames =
        {
            "StartButton",
            "SettingButton",
            "ExitButton"
        };

        private static readonly Color DefaultGlowColor =
            new Color(0.1f, 0.92f, 0.96f, 0.78f);

        [MenuItem(
            "Tools/Main Menu/Install Button Hover Visuals",
            false,
            200)]
        private static void Install()
        {
            Scene scene = EnsureMainMenuSceneIsOpen();
            if (!scene.IsValid())
            {
                return;
            }

            Material material = GetOrCreateMaterial();
            if (material == null)
            {
                return;
            }

            Transform buttonsRoot = FindTransform(
                scene,
                "Canvas/Buttons");
            if (buttonsRoot == null)
            {
                Debug.LogError(
                    "MainMenu button hover installer could not find "
                    + "'Canvas/Buttons'.");
                return;
            }

            int installedCount = 0;
            foreach (string buttonName in ButtonNames)
            {
                Transform buttonTransform =
                    buttonsRoot.Find(buttonName);
                if (buttonTransform == null)
                {
                    Debug.LogError(
                        "MainMenu button hover installer could not "
                        + $"find '{buttonName}'.");
                    continue;
                }

                if (InstallOnButton(
                    buttonTransform.gameObject,
                    material))
                {
                    installedCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "MainMenu button hover visuals installed on "
                + $"{installedCount}/{ButtonNames.Length} buttons.");
        }

        [MenuItem(
            "Tools/Main Menu/Validate Button Hover Visuals",
            false,
            201)]
        private static void ValidateInstallation()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError(
                    "Open MainMenu.unity before validating its "
                    + "button hover visuals.");
                return;
            }

            Transform buttonsRoot = FindTransform(
                scene,
                "Canvas/Buttons");
            int validCount = 0;
            foreach (string buttonName in ButtonNames)
            {
                Transform buttonTransform =
                    buttonsRoot != null
                        ? buttonsRoot.Find(buttonName)
                        : null;
                if (IsButtonValid(buttonTransform))
                {
                    validCount++;
                }
            }

            if (validCount == ButtonNames.Length)
            {
                Debug.Log(
                    "MainMenu button hover validation passed for "
                    + "all three buttons.");
            }
            else
            {
                Debug.LogError(
                    "MainMenu button hover validation failed. "
                    + $"{validCount}/{ButtonNames.Length} buttons "
                    + "are configured correctly.");
            }
        }

        private static Scene EnsureMainMenuSceneIsOpen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
            {
                return activeScene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return default;
            }

            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
        }

        private static Material GetOrCreateMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError(
                    $"Could not find shader '{ShaderName}'.");
                return null;
            }

            EnsureFolder(MaterialFolder);
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M_MainMenuHoverGlow"
                };
                AssetDatabase.CreateAsset(
                    material,
                    MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            ConfigureMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureMaterial(Material material)
        {
            float width = 400f + GlowExpansion * 2f;
            float height = 100f + GlowExpansion * 2f;
            material.SetFloat("_AspectRatio", width / height);
            material.SetFloat(
                "_EdgeInset",
                GlowExpansion / height);
            material.SetFloat("_CornerRadius", 0.075f);
            material.SetFloat("_RingWidth", 0.026f);
            material.SetFloat("_GlowSpread", 0.12f);
            material.SetFloat("_Softness", 0.008f);
            material.SetFloat("_CoreIntensity", 0.9f);
            material.SetFloat("_GlowIntensity", 0.55f);
        }

        private static bool InstallOnButton(
            GameObject buttonObject,
            Material material)
        {
            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError(
                    $"'{buttonObject.name}' has no Button component.");
                return false;
            }

            Image glowImage = GetOrCreateGlowImage(
                buttonObject.transform,
                material);
            MainMenuButtonHoverVisual visual =
                buttonObject.GetComponent<
                    MainMenuButtonHoverVisual>();
            if (visual == null)
            {
                visual = Undo.AddComponent<
                    MainMenuButtonHoverVisual>(
                    buttonObject);
            }

            Undo.RecordObject(visual, "Configure button hover");
            visual.Configure(
                button,
                glowImage,
                DefaultGlowColor);
            EditorUtility.SetDirty(visual);
            return true;
        }

        private static Image GetOrCreateGlowImage(
            Transform buttonTransform,
            Material material)
        {
            Transform existing =
                buttonTransform.Find("HoverGlow");
            GameObject glowObject;
            if (existing == null)
            {
                glowObject = new GameObject(
                    "HoverGlow",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(
                    glowObject,
                    "Create button hover glow");
                glowObject.layer =
                    buttonTransform.gameObject.layer;
                glowObject.transform.SetParent(
                    buttonTransform,
                    false);
            }
            else
            {
                glowObject = existing.gameObject;
                Undo.RecordObject(
                    glowObject,
                    "Configure button hover glow");
            }

            glowObject.transform.SetAsFirstSibling();

            RectTransform rectTransform =
                glowObject.GetComponent<RectTransform>();
            Undo.RecordObject(
                rectTransform,
                "Resize button hover glow");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin =
                new Vector2(-GlowExpansion, -GlowExpansion);
            rectTransform.offsetMax =
                new Vector2(GlowExpansion, GlowExpansion);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            Image image = glowObject.GetComponent<Image>();
            Undo.RecordObject(image, "Configure button hover image");
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.material = material;
            image.color = new Color(
                DefaultGlowColor.r,
                DefaultGlowColor.g,
                DefaultGlowColor.b,
                0f);
            image.raycastTarget = false;
            image.maskable = true;
            EditorUtility.SetDirty(image);
            return image;
        }

        private static bool IsButtonValid(
            Transform buttonTransform)
        {
            if (buttonTransform == null)
            {
                return false;
            }

            Button button =
                buttonTransform.GetComponent<Button>();
            MainMenuButtonHoverVisual visual =
                buttonTransform.GetComponent<
                    MainMenuButtonHoverVisual>();
            Transform glowTransform =
                buttonTransform.Find("HoverGlow");
            Image glowImage = glowTransform != null
                ? glowTransform.GetComponent<Image>()
                : null;

            return button != null
                && visual != null
                && glowImage != null
                && !glowImage.raycastTarget
                && glowImage.material != null
                && glowImage.material.shader != null
                && glowImage.material.shader.name == ShaderName
                && glowTransform.GetSiblingIndex() == 0;
        }

        private static Transform FindTransform(
            Scene scene,
            string path)
        {
            string[] segments = path.Split('/');
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!string.Equals(
                    root.name,
                    segments[0],
                    StringComparison.Ordinal))
                {
                    continue;
                }

                Transform current = root.transform;
                for (int index = 1;
                     index < segments.Length;
                     index++)
                {
                    current = current.Find(segments[index]);
                    if (current == null)
                    {
                        return null;
                    }
                }

                return current;
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent =
                folderPath.Substring(
                    0,
                    folderPath.LastIndexOf(
                        "/",
                        StringComparison.Ordinal));
            string folderName =
                folderPath.Substring(parent.Length + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
