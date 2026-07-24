using System;
using System.Collections.Generic;
using CSU.VisualEffects.EmissiveSprites;
using UnityEditor;
using UnityEngine;

namespace CSU.VisualEffects.EmissiveSprites.Editor
{
    public static class EmissiveSpriteAssetFactory
    {
        private const string ShaderPath =
            "Assets/Dev/CSU/05_Shaders/EmissiveSprites/"
            + "SelectiveSpriteEmission.shader";
        private const string MaterialPath =
            "Assets/Dev/CSU/08_Material/EmissiveSprites/"
            + "M_SelectiveSpriteEmission.mat";
        private const string ProfileDirectory =
            "Assets/Resources/EmissiveSprites/Profiles";
        private const string SettingsPath =
            "Assets/Resources/EmissiveSprites/"
            + "EmissiveSpriteSettings.asset";

        private const string StarCloudProfilePath =
            ProfileDirectory + "/StarCloudEmissionProfile.asset";
        private const string UfoAuraProfilePath =
            ProfileDirectory + "/UfoAuraEmissionProfile.asset";
        private const string UfoBackgroundProfilePath =
            ProfileDirectory + "/UfoBackgroundEmissionProfile.asset";
        private const string SpaceShipFireProfilePath =
            ProfileDirectory + "/SpaceShipFireEmissionProfile.asset";

        private const string StarCloudPrefabPath =
            "Assets/Dev/CSU/03_Prefabs/Present_0.prefab";
        private const string UfoAuraPrefabPath =
            "Assets/Dev/CSU/03_Prefabs/UFO&Aura.prefab";
        private const string UfoBackgroundPrefabPath =
            "Assets/Dev/CSU/03_Prefabs/UFO Bckground_0.prefab";

        [MenuItem("Tools/Space/Build Emissive Sprite Assets")]
        public static void Build()
        {
            EnsureFolder("Assets/Dev/CSU/08_Material/EmissiveSprites");
            EnsureFolder(ProfileDirectory);
            EnsureFolder("Assets/Resources/EmissiveSprites");

            Shader shader =
                AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            if (shader == null)
            {
                Debug.LogError(
                    $"Emissive sprite shader is missing at '{ShaderPath}'.");
                return;
            }

            Material material = CreateOrLoadMaterial(shader);
            EmissiveSpriteProfile starCloud = CreateOrLoadProfile(
                StarCloudProfilePath,
                serializedProfile => ConfigureStarCloud(
                    serializedProfile,
                    material));
            EmissiveSpriteProfile ufoAura = CreateOrLoadProfile(
                UfoAuraProfilePath,
                serializedProfile => ConfigureUfoAura(
                    serializedProfile,
                    material));
            EmissiveSpriteProfile ufoBackground = CreateOrLoadProfile(
                UfoBackgroundProfilePath,
                serializedProfile => ConfigureUfoBackground(
                    serializedProfile,
                    material));
            EmissiveSpriteProfile spaceShipFire = CreateOrLoadProfile(
                SpaceShipFireProfilePath,
                serializedProfile => ConfigureSpaceShipFire(
                    serializedProfile,
                    material));

            ConfigureSettings(
                starCloud,
                ufoAura,
                ufoBackground,
                spaceShipFire);
            ConfigurePrefab(
                StarCloudPrefabPath,
                starCloud,
                disableLegacyUfoComponent: false);
            ConfigurePrefab(
                UfoAuraPrefabPath,
                ufoAura,
                disableLegacyUfoComponent: true);
            ConfigurePrefab(
                UfoBackgroundPrefabPath,
                ufoBackground,
                disableLegacyUfoComponent: false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Emissive sprite assets built for Present_0, UFO&Aura, "
                + "UFO Bckground_0, and SpaceShip_Fire. Existing "
                + "Sorting values were preserved.");
        }

        [MenuItem("Tools/Space/Validate Emissive Sprite Setup")]
        public static void ValidateSetup()
        {
            List<string> errors = new List<string>();
            ValidatePrefab(
                StarCloudPrefabPath,
                EmissiveSpriteTarget.StarCloud,
                errors);
            ValidatePrefab(
                UfoAuraPrefabPath,
                EmissiveSpriteTarget.UfoAura,
                errors);
            ValidatePrefab(
                UfoBackgroundPrefabPath,
                EmissiveSpriteTarget.UfoBackground,
                errors);
            ValidateProfile(
                SpaceShipFireProfilePath,
                EmissiveSpriteTarget.SpaceShipFire,
                errors);

            Material sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (sharedMaterial == null)
            {
                errors.Add($"Missing shared material: {MaterialPath}");
            }

            if (errors.Count > 0)
            {
                Debug.LogError(
                    "Emissive sprite validation failed:\n- "
                    + string.Join("\n- ", errors));
                return;
            }

            int runtimeInstances = 0;
            int boundFirePresenters = 0;
            Material[] loadedMaterials =
                Resources.FindObjectsOfTypeAll<Material>();

            for (int index = 0;
                 index < loadedMaterials.Length;
                 index++)
            {
                Material loadedMaterial = loadedMaterials[index];

                if (loadedMaterial != null
                    && loadedMaterial.name.Contains(
                        "M_SelectiveSpriteEmission (Instance)"))
                {
                    runtimeInstances++;
                }
            }

            if (Application.isPlaying)
            {
                EmissiveSpritePresenter[] presenters =
                    Resources.FindObjectsOfTypeAll<
                        EmissiveSpritePresenter>();

                for (int index = 0;
                     index < presenters.Length;
                     index++)
                {
                    EmissiveSpritePresenter presenter =
                        presenters[index];

                    if (presenter != null
                        && presenter.Profile != null
                        && presenter.Profile.Target
                            == EmissiveSpriteTarget.SpaceShipFire)
                    {
                        boundFirePresenters++;
                    }
                }

                if (boundFirePresenters == 0)
                {
                    Debug.LogError(
                        "Emissive sprite runtime validation did not "
                        + "find a bound SpaceShip_Fire renderer.");
                    return;
                }
            }

            Debug.Log(
                "Emissive sprite validation passed. Actual runtime "
                + "prefabs and SpaceShip_Fire use their intended "
                + $"profiles. Bound fire presenters: {boundFirePresenters}. "
                + "Generated material instances found: "
                + $"{runtimeInstances}.");
        }

        private static Material CreateOrLoadMaterial(Shader shader)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (material != null)
            {
                if (material.shader != shader)
                {
                    material.shader = shader;
                    EditorUtility.SetDirty(material);
                }

                return material;
            }

            material = new Material(shader)
            {
                name = "M_SelectiveSpriteEmission"
            };
            material.SetFloat("_EmissionStrength", 0f);
            material.SetFloat("_BodyEmissionStrength", 0f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static EmissiveSpriteProfile CreateOrLoadProfile(
            string assetPath,
            Action<SerializedObject> configureNewProfile)
        {
            EmissiveSpriteProfile profile =
                AssetDatabase.LoadAssetAtPath<EmissiveSpriteProfile>(
                    assetPath);

            if (profile != null)
            {
                return profile;
            }

            profile =
                ScriptableObject.CreateInstance<EmissiveSpriteProfile>();
            SerializedObject serializedProfile =
                new SerializedObject(profile);
            configureNewProfile(serializedProfile);
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, assetPath);
            return profile;
        }

        private static void ConfigureStarCloud(
            SerializedObject profile,
            Material material)
        {
            SetCommonProfileValues(
                profile,
                EmissiveSpriteTarget.StarCloud,
                material,
                new Color(1f, 1f, 1f, 1f),
                new Color(0.22f, 0.55f, 0.95f, 1f),
                2f,
                1f,
                EmissiveSpriteMaskMode.Brightness,
                0.58f,
                0.14f,
                3.5f);
            SetFloat(profile, "bodyEmissionStrength", 2f);
            SetFloat(profile, "pulseSpeed", 0f);
            SetFloat(profile, "pulseAmount", 0f);
        }

        private static void ConfigureUfoAura(
            SerializedObject profile,
            Material material)
        {
            SetCommonProfileValues(
                profile,
                EmissiveSpriteTarget.UfoAura,
                material,
                new Color(1f, 1f, 1f, 1f),
                new Color(0.12f, 0.85f, 0.65f, 1f),
                3.5f,
                1f,
                EmissiveSpriteMaskMode.UfoBodyAndAura,
                0.55f,
                0.13f,
                4.5f);
            SetFloat(profile, "bodyEmissionStrength", 1.6f);
            SetVector2(
                profile,
                "bodyCenter",
                new Vector2(0.5f, 0.55f));
            SetVector2(
                profile,
                "bodyHalfSize",
                new Vector2(0.3f, 0.24f));
            SetFloat(profile, "pulseSpeed", 1.1f);
            SetFloat(profile, "pulseAmount", 0.15f);
        }

        private static void ConfigureUfoBackground(
            SerializedObject profile,
            Material material)
        {
            SetCommonProfileValues(
                profile,
                EmissiveSpriteTarget.UfoBackground,
                material,
                new Color(1f, 1f, 1f, 1f),
                new Color(0.2f, 0.6f, 0.9f, 1f),
                1.5f,
                1f,
                EmissiveSpriteMaskMode.Brightness,
                0.62f,
                0.12f,
                3f);
            SetFloat(profile, "bodyEmissionStrength", 1.5f);
            SetFloat(profile, "pulseSpeed", 0f);
            SetFloat(profile, "pulseAmount", 0f);
        }

        private static void ConfigureSpaceShipFire(
            SerializedObject profile,
            Material material)
        {
            SetCommonProfileValues(
                profile,
                EmissiveSpriteTarget.SpaceShipFire,
                material,
                Color.white,
                new Color(0.55f, 0.9f, 1f, 1f),
                3.4f,
                1f,
                EmissiveSpriteMaskMode.Brightness,
                0.58f,
                0.12f,
                4.5f);
            SetFloat(profile, "bodyEmissionStrength", 3.4f);
            SetFloat(profile, "pulseSpeed", 0f);
            SetFloat(profile, "pulseAmount", 0f);
        }

        private static void SetCommonProfileValues(
            SerializedObject profile,
            EmissiveSpriteTarget target,
            Material material,
            Color baseColor,
            Color emissionColor,
            float emissionStrength,
            float opacity,
            EmissiveSpriteMaskMode maskMode,
            float threshold,
            float softness,
            float outputClamp)
        {
            SetEnum(profile, "target", (int)target);
            SetBool(profile, "effectEnabled", true);
            SetObject(profile, "sharedMaterial", material);
            SetColor(profile, "baseColor", baseColor);
            SetColor(profile, "emissionColor", emissionColor);
            SetFloat(profile, "emissionStrength", emissionStrength);
            SetFloat(profile, "overallOpacity", opacity);
            SetEnum(profile, "maskMode", (int)maskMode);
            SetFloat(profile, "brightnessThreshold", threshold);
            SetFloat(profile, "thresholdSoftness", softness);
            SetFloat(profile, "outputClamp", outputClamp);
            SetBool(profile, "preserveRendererSorting", true);
            SetFloat(profile, "previewEmissionMultiplier", 1f);
        }

        private static void ConfigureSettings(
            EmissiveSpriteProfile starCloud,
            EmissiveSpriteProfile ufoAura,
            EmissiveSpriteProfile ufoBackground,
            EmissiveSpriteProfile spaceShipFire)
        {
            EmissiveSpriteSettings settings =
                AssetDatabase.LoadAssetAtPath<EmissiveSpriteSettings>(
                    SettingsPath);

            if (settings == null)
            {
                settings =
                    ScriptableObject.CreateInstance<
                        EmissiveSpriteSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            SerializedObject serializedSettings =
                new SerializedObject(settings);
            SetObject(
                serializedSettings,
                "starCloudProfile",
                starCloud);
            SetObject(
                serializedSettings,
                "ufoAuraProfile",
                ufoAura);
            SetObject(
                serializedSettings,
                "ufoBackgroundProfile",
                ufoBackground);
            SetObject(
                serializedSettings,
                "spaceShipFireProfile",
                spaceShipFire);
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void ConfigurePrefab(
            string prefabPath,
            EmissiveSpriteProfile profile,
            bool disableLegacyUfoComponent)
        {
            GameObject root =
                PrefabUtility.LoadPrefabContents(prefabPath);

            if (root == null)
            {
                Debug.LogError(
                    $"Could not load emissive target prefab '{prefabPath}'.");
                return;
            }

            try
            {
                SpriteRenderer[] renderers =
                    root.GetComponentsInChildren<SpriteRenderer>(true);
                int[] sortingLayers = new int[renderers.Length];
                int[] sortingOrders = new int[renderers.Length];

                for (int index = 0;
                     index < renderers.Length;
                     index++)
                {
                    sortingLayers[index] =
                        renderers[index].sortingLayerID;
                    sortingOrders[index] =
                        renderers[index].sortingOrder;
                }

                if (disableLegacyUfoComponent)
                {
                    CSU.VisualEffects.UfoAuraShaderSettings legacy =
                        root.GetComponent<
                            CSU.VisualEffects.UfoAuraShaderSettings>();

                    if (legacy != null)
                    {
                        legacy.enabled = false;
                        EditorUtility.SetDirty(legacy);
                    }
                }

                EmissiveSpritePresenter presenter =
                    root.GetComponent<EmissiveSpritePresenter>();

                if (presenter == null)
                {
                    presenter =
                        root.AddComponent<EmissiveSpritePresenter>();
                }

                presenter.Initialize(profile, renderers);
                EditorUtility.SetDirty(presenter);

                for (int index = 0;
                     index < renderers.Length;
                     index++)
                {
                    if (renderers[index].sortingLayerID
                            != sortingLayers[index]
                        || renderers[index].sortingOrder
                            != sortingOrders[index])
                    {
                        throw new InvalidOperationException(
                            $"Sorting changed while configuring "
                            + $"'{prefabPath}'.");
                    }

                    EditorUtility.SetDirty(renderers[index]);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidatePrefab(
            string prefabPath,
            EmissiveSpriteTarget expectedTarget,
            List<string> errors)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                errors.Add($"Missing prefab: {prefabPath}");
                return;
            }

            EmissiveSpritePresenter presenter =
                prefab.GetComponent<EmissiveSpritePresenter>();

            if (presenter == null)
            {
                errors.Add(
                    $"Missing presenter on prefab: {prefabPath}");
                return;
            }

            if (presenter.Profile == null)
            {
                errors.Add(
                    $"Missing profile on prefab: {prefabPath}");
            }
            else if (presenter.Profile.Target != expectedTarget)
            {
                errors.Add(
                    $"Unexpected profile target on prefab: {prefabPath}");
            }

            if (presenter.TargetRenderers == null
                || presenter.TargetRenderers.Count == 0)
            {
                errors.Add(
                    $"No target SpriteRenderer on prefab: {prefabPath}");
            }
        }

        private static void ValidateProfile(
            string profilePath,
            EmissiveSpriteTarget expectedTarget,
            List<string> errors)
        {
            EmissiveSpriteProfile profile =
                AssetDatabase.LoadAssetAtPath<EmissiveSpriteProfile>(
                    profilePath);

            if (profile == null)
            {
                errors.Add($"Missing profile: {profilePath}");
                return;
            }

            if (profile.Target != expectedTarget)
            {
                errors.Add(
                    $"Unexpected profile target: {profilePath}");
            }

            if (profile.SharedMaterial == null)
            {
                errors.Add(
                    $"Missing shared material: {profilePath}");
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void SetBool(
            SerializedObject target,
            string propertyName,
            bool value)
        {
            target.FindProperty(propertyName).boolValue = value;
        }

        private static void SetFloat(
            SerializedObject target,
            string propertyName,
            float value)
        {
            target.FindProperty(propertyName).floatValue = value;
        }

        private static void SetEnum(
            SerializedObject target,
            string propertyName,
            int value)
        {
            target.FindProperty(propertyName).enumValueIndex = value;
        }

        private static void SetColor(
            SerializedObject target,
            string propertyName,
            Color value)
        {
            target.FindProperty(propertyName).colorValue = value;
        }

        private static void SetVector2(
            SerializedObject target,
            string propertyName,
            Vector2 value)
        {
            target.FindProperty(propertyName).vector2Value = value;
        }

        private static void SetObject(
            SerializedObject target,
            string propertyName,
            UnityEngine.Object value)
        {
            target.FindProperty(propertyName).objectReferenceValue = value;
        }
    }
}
