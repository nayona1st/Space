using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceGame.CommonUI.Audio;
using SpaceGame.CommonUI.Display;
using SpaceGame.CommonUI.Input;
using SpaceGame.CommonUI.Modal;
using SpaceGame.CommonUI.Pause;
using SpaceGame.CommonUI.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SpaceGame.CommonUI.Tests
{
    public sealed class CommonUISystemTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly List<InputDevice> devices = new List<InputDevice>();
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject target in objects)
            {
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }

            foreach (InputDevice device in devices)
            {
                if (device != null && device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            objects.Clear();
            devices.Clear();
            Time.timeScale = originalTimeScale;
        }

        [Test]
        public void PauseRequests_ReleaseOnlyTheirOwnLease()
        {
            Time.timeScale = 0.75f;
            PauseRequestService service =
                CreateComponent<PauseRequestService>("PauseService");

            IDisposable first = service.Acquire("settings");
            IDisposable second = service.Acquire("tutorial");
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(service.RequestCount, Is.EqualTo(2));

            first.Dispose();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(service.RequestCount, Is.EqualTo(1));

            second.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(0.75f));
            Assert.That(service.RequestCount, Is.Zero);
        }

        [Test]
        public void CancelRouter_InvokesOnlyHighestPriorityHandler()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            devices.Add(keyboard);
            ModalCancelRouter router =
                CreateComponent<ModalCancelRouter>("CancelRouter");
            router.Configure(null, "<Keyboard>/escape");

            int lowCount = 0;
            int highCount = 0;
            using IDisposable low = router.Push(
                () =>
                {
                    lowCount++;
                    return true;
                },
                100);
            using IDisposable high = router.Push(
                () =>
                {
                    highCount++;
                    return true;
                },
                400);

            InputSystem.QueueStateEvent(
                keyboard,
                new KeyboardState(Key.Escape));
            InputSystem.Update();

            Assert.That(highCount, Is.EqualTo(1));
            Assert.That(lowCount, Is.Zero);
        }

        [Test]
        public void InputGate_PreservesNestedActionMapState()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap gameplay = asset.AddActionMap("Gameplay");
            gameplay.AddAction("Jump", binding: "<Keyboard>/space");
            InputActionMap initiallyDisabled = asset.AddActionMap("Other");
            initiallyDisabled.AddAction("Use", binding: "<Keyboard>/e");
            gameplay.Enable();

            ModalInputGate gate =
                CreateComponent<ModalInputGate>("InputGate");
            gate.Configure(
                asset,
                new[] {gameplay.id.ToString(), initiallyDisabled.id.ToString()});

            IDisposable first = gate.Acquire();
            IDisposable second = gate.Acquire();
            Assert.That(gameplay.enabled, Is.False);
            Assert.That(initiallyDisabled.enabled, Is.False);

            first.Dispose();
            Assert.That(gameplay.enabled, Is.False);

            second.Dispose();
            Assert.That(gameplay.enabled, Is.True);
            Assert.That(initiallyDisabled.enabled, Is.False);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void BindingOverrides_RestoreAndNotifyImmediately()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = asset.AddActionMap("Player");
            InputAction action = map.AddAction(
                "Jump",
                binding: "<Keyboard>/space");
            var definition = new InputBindingDefinition();
            definition.Configure(
                "Jump",
                InputActionReference.Create(action),
                action.bindings[0].id.ToString(),
                "Keyboard&Mouse");
            var catalog = ScriptableObject.CreateInstance<InputBindingCatalog>();
            catalog.Configure(
                asset,
                null,
                new[] {definition},
                Array.Empty<string>(),
                new[] {map.id.ToString()});

            action.ApplyBindingOverride(0, "<Keyboard>/j");
            string json = asset.SaveBindingOverridesAsJson();
            action.RemoveBindingOverride(0);
            int notificationCount = 0;
            catalog.BindingsChanged += () => notificationCount++;

            InputBindingOverrideUtility.Restore(catalog, json);

            Assert.That(
                action.bindings[0].effectivePath,
                Is.EqualTo("<Keyboard>/j"));
            Assert.That(definition.GetDisplayString(), Is.Not.Empty);
            Assert.That(notificationCount, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void BindingCatalog_RejectsForbiddenAndDuplicateControls()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            devices.Add(keyboard);
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = asset.AddActionMap("Player");
            InputAction first = map.AddAction(
                "First",
                binding: "<Keyboard>/a");
            InputAction second = map.AddAction(
                "Second",
                binding: "<Keyboard>/b");
            InputActionReference firstReference =
                InputActionReference.Create(first);
            InputActionReference secondReference =
                InputActionReference.Create(second);
            var firstDefinition = new InputBindingDefinition();
            firstDefinition.Configure(
                "First",
                firstReference,
                first.bindings[0].id.ToString(),
                "Keyboard&Mouse");
            var secondDefinition = new InputBindingDefinition();
            secondDefinition.Configure(
                "Second",
                secondReference,
                second.bindings[0].id.ToString(),
                "Keyboard&Mouse");
            var catalog = ScriptableObject.CreateInstance<InputBindingCatalog>();
            catalog.Configure(
                asset,
                null,
                new[] {firstDefinition, secondDefinition},
                new[] {"<Keyboard>/escape"},
                new[] {map.id.ToString()});

            Assert.That(catalog.IsForbidden(keyboard.escapeKey), Is.True);
            Assert.That(
                catalog.HasDuplicate(
                    firstDefinition,
                    keyboard.bKey,
                    out InputBindingDefinition duplicate),
                Is.True);
            Assert.That(duplicate, Is.SameAs(secondDefinition));

            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(firstReference);
            UnityEngine.Object.DestroyImmediate(secondReference);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void SettingsCoordinator_PreviewsCommitsAndRestores()
        {
            var repository = new MemoryRepository();
            var audio = new RecordingAudioAdapter();
            var screen = new RecordingScreenApplier();
            var coordinator =
                new SettingsCoordinator(repository, audio, screen);
            coordinator.LoadAndApply();

            GameSettingsData snapshot = coordinator.BeginEdit();
            GameSettingsData working = snapshot.Clone();
            working.masterVolume = 0.25f;
            coordinator.PreviewAudio(working);
            Assert.That(
                audio.LastApplied.masterVolume,
                Is.EqualTo(0.25f));

            coordinator.RestorePreview(snapshot);
            Assert.That(
                audio.LastApplied.masterVolume,
                Is.EqualTo(snapshot.masterVolume));

            coordinator.Commit(working);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
            Assert.That(
                repository.Stored.masterVolume,
                Is.EqualTo(0.25f));
            Assert.That(
                screen.LastApplied.masterVolume,
                Is.EqualTo(0.25f));
        }

        private T CreateComponent<T>(string name)
            where T : Component
        {
            var target = new GameObject(name);
            objects.Add(target);
            return target.AddComponent<T>();
        }

        private sealed class MemoryRepository : ISettingsRepository
        {
            public int SaveCount { get; private set; }
            public GameSettingsData Stored { get; private set; }

            public bool TryLoad(out GameSettingsData settings)
            {
                settings = GameSettingsData.CreateDefault();
                return true;
            }

            public void Save(GameSettingsData settings)
            {
                SaveCount++;
                Stored = settings.Clone();
            }
        }

        private sealed class RecordingAudioAdapter : IAudioSettingsAdapter
        {
            public GameSettingsData LastApplied { get; private set; }

            public void Apply(GameSettingsData settings)
            {
                LastApplied = settings.Clone();
            }
        }

        private sealed class RecordingScreenApplier :
            IScreenSettingsApplier
        {
            public GameSettingsData LastApplied { get; private set; }

            public IReadOnlyList<ResolutionOption>
                GetAvailableResolutions()
            {
                return Array.Empty<ResolutionOption>();
            }

            public void Apply(GameSettingsData settings)
            {
                LastApplied = settings.Clone();
            }
        }
    }
}
