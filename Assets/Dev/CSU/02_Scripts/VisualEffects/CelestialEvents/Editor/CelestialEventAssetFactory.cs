using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents.Editor
{
    [InitializeOnLoad]
    internal static class CelestialEventAssetFactory
    {
        private const string StarsPath =
            "Assets/Dev/CSU/00_Assets/Stars.png";
        private const string MaterialFolder =
            "Assets/Dev/CSU/08_Material/CelestialEvents";
        private const string PrefabFolder =
            "Assets/Dev/CSU/03_Prefabs/VisualEffects/CelestialEvents";
        private const string SettingsFolder =
            "Assets/Resources/CelestialEvents";
        private const string SettingsPath =
            SettingsFolder + "/CelestialEventSettings.asset";
        private const string ShaderName =
            "Space/Celestial Emissive Unlit";

        static CelestialEventAssetFactory()
        {
            EditorApplication.delayCall += BuildMissingAssets;
        }

        [MenuItem(
            "Tools/Space/Rebuild Celestial Event Assets",
            false,
            40)]
        private static void RebuildAssets()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(SettingsFolder);

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Shader '{ShaderName}' could not be found.");
            }

            Sprite[] stars = AssetDatabase
                .LoadAllAssetsAtPath(StarsPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            if (stars.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No sliced sprites were found at '{StarsPath}'.");
            }

            Material shootingStarHead = CreateOrUpdateMaterial(
                MaterialFolder + "/ShootingStarHead.mat",
                shader,
                new Color(0.9f, 0.97f, 1f, 1f),
                new Color(0.88f, 0.97f, 1f, 1f),
                5.5f);
            Material shootingStarTrail = CreateOrUpdateMaterial(
                MaterialFolder + "/ShootingStarTrail.mat",
                shader,
                Color.white,
                new Color(0.85f, 0.96f, 1f, 1f),
                5f);
            Material cometHead = CreateOrUpdateMaterial(
                MaterialFolder + "/CometHead.mat",
                shader,
                HtmlColor("EAFBFF"),
                HtmlColor("BDEFFF"),
                8f);
            Material cometCore = CreateOrUpdateMaterial(
                MaterialFolder + "/CometCoreTrail.mat",
                shader,
                Color.white,
                HtmlColor("BDEFFF"),
                8f);
            Material cometOuter = CreateOrUpdateMaterial(
                MaterialFolder + "/CometOuterTrail.mat",
                shader,
                Color.white,
                HtmlColor("67CFFF"),
                3.5f);

            CelestialEffectPresenter shootingStarPrefab =
                CreateOrUpdatePrefab(
                    PrefabFolder + "/ShootingStar.prefab",
                    "ShootingStar",
                    stars[0],
                    shootingStarHead,
                    shootingStarTrail,
                    shootingStarTrail);
            CelestialEffectPresenter cometPrefab =
                CreateOrUpdatePrefab(
                    PrefabFolder + "/Comet.prefab",
                    "Comet",
                    stars[0],
                    cometHead,
                    cometCore,
                    cometOuter);

            CelestialEventSettings settings =
                AssetDatabase.LoadAssetAtPath<
                    CelestialEventSettings>(SettingsPath);
            if (settings == null)
            {
                settings =
                    ScriptableObject.CreateInstance<
                        CelestialEventSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serializedSettings =
                new SerializedObject(settings);
            AssignObject(
                serializedSettings,
                "shootingStar.prefab",
                shootingStarPrefab);
            AssignObject(
                serializedSettings,
                "shootingStar.headMaterial",
                shootingStarHead);
            AssignObjects(
                serializedSettings,
                "shootingStar.headSprites",
                stars);
            AssignObject(
                serializedSettings,
                "shootingStar.coreTrail.material",
                shootingStarTrail);
            AssignObject(
                serializedSettings,
                "shootingStar.outerTrail.material",
                shootingStarTrail);

            AssignObject(
                serializedSettings,
                "comet.prefab",
                cometPrefab);
            AssignObject(
                serializedSettings,
                "comet.headMaterial",
                cometHead);
            AssignObjects(
                serializedSettings,
                "comet.headSprites",
                stars);
            AssignObject(
                serializedSettings,
                "comet.coreTrail.material",
                cometCore);
            AssignObject(
                serializedSettings,
                "comet.outerTrail.material",
                cometOuter);
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = settings;
            Debug.Log(
                "Celestial event assets rebuilt successfully. "
                + "Production probabilities remain unchanged.",
                settings);
        }

        private static void BuildMissingAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || AssetDatabase.LoadAssetAtPath<
                    CelestialEventSettings>(SettingsPath) != null)
            {
                return;
            }

            RebuildAssets();
        }

        private static Material CreateOrUpdateMaterial(
            string path,
            Shader shader,
            Color baseColor,
            Color emissionColor,
            float emissionStrength)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emissionColor);
            material.SetFloat(
                "_EmissionStrength",
                emissionStrength);
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static CelestialEffectPresenter
            CreateOrUpdatePrefab(
                string path,
                string objectName,
                Sprite sprite,
                Material headMaterial,
                Material coreMaterial,
                Material outerMaterial)
        {
            var root = new GameObject(objectName);
            try
            {
                SpriteRenderer head =
                    root.AddComponent<SpriteRenderer>();
                head.sprite = sprite;
                head.sharedMaterial = headMaterial;
                head.shadowCastingMode = ShadowCastingMode.Off;
                head.receiveShadows = false;

                var coreObject = new GameObject("CoreTrail");
                coreObject.transform.SetParent(
                    root.transform,
                    false);
                TrailRenderer core =
                    coreObject.AddComponent<TrailRenderer>();
                ConfigureTrailDefaults(core, coreMaterial);

                var outerObject = new GameObject("OuterTrail");
                outerObject.transform.SetParent(
                    root.transform,
                    false);
                TrailRenderer outer =
                    outerObject.AddComponent<TrailRenderer>();
                ConfigureTrailDefaults(outer, outerMaterial);

                CelestialEffectPresenter presenter =
                    root.AddComponent<CelestialEffectPresenter>();
                var serializedPresenter =
                    new SerializedObject(presenter);
                AssignObject(
                    serializedPresenter,
                    "headRenderer",
                    head);
                AssignObject(
                    serializedPresenter,
                    "coreTrailRenderer",
                    core);
                AssignObject(
                    serializedPresenter,
                    "outerTrailRenderer",
                    outer);
                serializedPresenter
                    .ApplyModifiedPropertiesWithoutUndo();

                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null
                ? prefab.GetComponent<CelestialEffectPresenter>()
                : null;
        }

        private static void ConfigureTrailDefaults(
            TrailRenderer trail,
            Material material)
        {
            trail.sharedMaterial = material;
            trail.time = 1f;
            trail.widthMultiplier = 0.2f;
            trail.minVertexDistance = 0.08f;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
        }

        private static void AssignObject(
            SerializedObject serializedObject,
            string propertyPath,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property '{propertyPath}' was not found.");
            }

            property.objectReferenceValue = value;
        }

        private static void AssignObjects(
            SerializedObject serializedObject,
            string propertyPath,
            UnityEngine.Object[] values)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyPath);
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException(
                    $"Serialized array '{propertyPath}' was not found.");
            }

            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue = values[index];
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string currentPath = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath =
                    $"{currentPath}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        parts[index]);
                }

                currentPath = nextPath;
            }
        }

        private static Color HtmlColor(string html)
        {
            ColorUtility.TryParseHtmlString(
                $"#{html}",
                out Color color);
            return color;
        }
    }
}
