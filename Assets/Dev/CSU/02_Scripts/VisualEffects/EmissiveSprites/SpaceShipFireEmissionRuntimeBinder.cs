using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CSU.VisualEffects.EmissiveSprites
{
    internal static class SpaceShipFireEmissionRuntimeBinder
    {
        private const string SettingsResourcePath =
            "EmissiveSprites/EmissiveSpriteSettings";
        private const string FireTextureName =
            "SpaceShip_Fire-Sheet";
        private const string FireSpritePrefix =
            "SpaceShip_Fire-Sheet_";

        private static EmissiveSpriteSettings settings;
        private static bool missingSettingsWasReported;
        private static bool conflictingPresenterWasReported;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            BindScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode)
        {
            BindScene(scene);
        }

        private static void BindScene(Scene scene)
        {
            EmissiveSpriteProfile profile = ResolveProfile();

            if (profile == null || !scene.IsValid())
            {
                return;
            }

            SpriteRenderer[] renderers =
                UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];

                if (renderer == null
                    || renderer.gameObject.scene != scene
                    || !UsesSpaceShipFireSprite(renderer))
                {
                    continue;
                }

                BindRenderer(renderer, profile);
            }
        }

        private static EmissiveSpriteProfile ResolveProfile()
        {
            if (settings == null)
            {
                settings =
                    Resources.Load<EmissiveSpriteSettings>(
                        SettingsResourcePath);
            }

            if (settings != null
                && settings.SpaceShipFireProfile != null)
            {
                return settings.SpaceShipFireProfile;
            }

            if (!missingSettingsWasReported)
            {
                missingSettingsWasReported = true;
                Debug.LogWarning(
                    "SpaceShip fire emission could not resolve its "
                    + $"profile from Resources/{SettingsResourcePath}.");
            }

            return null;
        }

        private static bool UsesSpaceShipFireSprite(
            SpriteRenderer renderer)
        {
            Sprite sprite = renderer.sprite;

            if (sprite == null)
            {
                return false;
            }

            if (sprite.name.StartsWith(
                FireSpritePrefix,
                StringComparison.Ordinal))
            {
                return true;
            }

            Texture2D texture = sprite.texture;
            return texture != null
                && string.Equals(
                    texture.name,
                    FireTextureName,
                    StringComparison.Ordinal);
        }

        private static void BindRenderer(
            SpriteRenderer renderer,
            EmissiveSpriteProfile profile)
        {
            EmissiveSpritePresenter presenter =
                renderer.GetComponent<EmissiveSpritePresenter>();

            if (presenter != null
                && presenter.Profile != null
                && presenter.Profile != profile)
            {
                if (!conflictingPresenterWasReported)
                {
                    conflictingPresenterWasReported = true;
                    Debug.LogWarning(
                        "SpaceShip fire emission found a renderer with "
                        + "a different emissive profile and left it "
                        + "unchanged.",
                        renderer);
                }

                return;
            }

            if (presenter == null)
            {
                presenter =
                    renderer.gameObject.AddComponent<
                        EmissiveSpritePresenter>();
            }

            presenter.Initialize(
                profile,
                new[] { renderer });
        }
    }
}
