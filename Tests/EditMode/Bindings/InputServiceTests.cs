using NUnit.Framework;
using Submodules.Utility.Bindings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.Bindings
{
    // The Input System's own fixture: virtual devices, and a fresh input state per test.
    public sealed class InputServiceTests : InputTestFixture
    {
        // Hold and Twin are authored onto the same key, as QuickMove and SecondSlot are.
        private enum TestAction { None, Fire, Hold, Aim, Twin }

        private Keyboard keyboard;
        private Mouse mouse;
        private InputActionAsset source;
        private InMemoryBindingStore store;
        private InputService<TestAction> service;
        private readonly List<TestAction> performed = new();

        [SetUp]
        public void AddDevices()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            store = new InMemoryBindingStore();
            source = Author("Fire", "Hold", "Aim", "Twin");

            // One fixture instance serves every test, so what one raised must not reach the next.
            performed.Clear();
        }

        [TearDown]
        public void DisposeService()
        {
            service?.Dispose();
            service = null;
            Object.DestroyImmediate(source);
        }

        private static InputActionAsset Author(params string[] names)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "TestInput";
            var map = asset.AddActionMap("Test");

            foreach (var name in names)
            {
                var path = name switch
                {
                    "Fire" => "<Keyboard>/f",
                    "Hold" or "Twin" => "<Keyboard>/leftShift",
                    _ => "<Keyboard>/a",
                };

                _ = map.AddAction(name, InputActionType.Button, path);
            }

            return asset;
        }

        // The rebinding waits 0.05 s after a match for a better one; that is input time, not frame time.
        private void Settle()
        {
            currentTime += 0.1;
            InputSystem.Update();
        }

        private InputService<TestAction> Build(float minRepeatSeconds = 0f)
        {
            service = new InputService<TestAction>(source, store, minRepeatSeconds);
            service.Performed += performed.Add;

            return service;
        }

        [Test]
        public void APress_RaisesPerformed_OnceForTheActionItIsBoundTo()
        {
            _ = Build();

            Press(keyboard.fKey);
            Release(keyboard.fKey);

            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }));
        }

        [Test]
        public void IsHeld_FollowsTheKey()
        {
            _ = Build();
            Assert.That(service.IsHeld(TestAction.Hold), Is.False);

            Press(keyboard.leftShiftKey);
            Assert.That(service.IsHeld(TestAction.Hold), Is.True);
            Assert.That(service.IsHeld(TestAction.Fire), Is.False, "another action's key");

            Release(keyboard.leftShiftKey);
            Assert.That(service.IsHeld(TestAction.Hold), Is.False);
        }

        [Test]
        public void NoAction_IsNeverHeld()
        {
            _ = Build();

            Assert.That(service.IsHeld(TestAction.None), Is.False);
        }

        [Test]
        public void ThePointer_IsTheMousesPosition_AndZeroWithoutOne()
        {
            _ = Build();
            Set(mouse.position, new Vector2(120f, 45f));

            Assert.That(service.PointerPosition, Is.EqualTo(new Vector2(120f, 45f)));

            InputSystem.RemoveDevice(mouse);

            Assert.That(service.PointerPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ACatalogMemberTheAssetLacks_ThrowsAtConstruction_NamingIt()
        {
            Object.DestroyImmediate(source);
            source = Author("Fire", "Hold");

            var thrown = Assert.Throws<System.InvalidOperationException>(() => _ = Build());

            Assert.That(thrown.Message, Does.Contain("Aim"));
        }

        [Test]
        public void TheSourceAsset_IsNeverChanged_ByARebinding()
        {
            _ = Build();
            service.Rebind(TestAction.Fire, 0, null);
            Press(keyboard.gKey);
            Settle();

            Assert.That(source.FindAction("Fire").bindings[0].overridePath, Is.Null);
            Assert.That(source.FindAction("Fire").enabled, Is.False, "the clone was the one enabled");
        }

        [Test]
        public void Rebinding_MovesTheActionToTheNextKey_AndSavesIt()
        {
            RebindResult? bound = null;
            _ = Build();

            service.Rebind(TestAction.Fire, 0, result => bound = result);
            Press(keyboard.gKey);
            Settle();
            Release(keyboard.gKey);

            Assert.That(bound, Is.EqualTo(RebindResult.Bound));
            Assert.That(store.Saves, Is.EqualTo(1));
            performed.Clear();

            Press(keyboard.fKey);
            Release(keyboard.fKey);
            Assert.That(performed, Is.Empty, "the old key no longer fires it");

            Press(keyboard.gKey);
            Release(keyboard.gKey);
            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }));
        }

        [Test]
        public void Escape_CancelsARebinding_AndSavesNothing()
        {
            RebindResult? bound = null;
            _ = Build();

            service.Rebind(TestAction.Fire, 0, result => bound = result);
            Press(keyboard.escapeKey);
            Settle();
            Release(keyboard.escapeKey);

            Assert.That(bound, Is.EqualTo(RebindResult.Cancelled));
            Assert.That(store.Saves, Is.Zero);

            Press(keyboard.fKey);
            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }), "still bound to F, and enabled again");
        }

        [Test]
        public void AMouseClickOrAPadButton_IsNotAPick()
        {
            RebindResult? bound = null;
            var pad = InputSystem.AddDevice<Gamepad>();
            _ = Build();

            service.Rebind(TestAction.Fire, 0, result => bound = result);
            Press(mouse.leftButton);
            Press(pad.buttonSouth);
            Settle();

            Assert.That(bound, Is.Null, "still listening for a key");

            Press(keyboard.gKey);
            Settle();

            Assert.That(bound, Is.EqualTo(RebindResult.Bound));
        }

        [Test]
        public void AKeyAnotherActionUses_IsRefused_AndNothingChanges()
        {
            RebindResult? bound = null;
            _ = Build();

            service.Rebind(TestAction.Fire, 0, result => bound = result);
            Press(keyboard.aKey);
            Settle();
            Release(keyboard.aKey);

            Assert.That(bound, Is.EqualTo(RebindResult.Conflict));
            Assert.That(store.Saves, Is.Zero);
            Assert.That(source.FindAction("Fire").bindings[0].overridePath, Is.Null);

            Press(keyboard.fKey);
            Release(keyboard.fKey);
            Press(keyboard.aKey);

            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire, TestAction.Aim }), "each on its own key, both enabled again");
        }

        [Test]
        public void AKeyTwoActionsAreAuthoredOnto_CanBeRestoredToThem()
        {
            RebindResult? bound = null;
            _ = Build();

            service.Rebind(TestAction.Hold, 0, null);
            Press(keyboard.gKey);
            Settle();
            Release(keyboard.gKey);

            service.Rebind(TestAction.Hold, 0, result => bound = result);
            Press(keyboard.leftShiftKey);
            Settle();
            Release(keyboard.leftShiftKey);

            Assert.That(bound, Is.EqualTo(RebindResult.Bound), "Twin shares Shift by authoring, not by accident");
        }

        [Test]
        public void ASharedKey_IsRefused_ToAnActionNotAuthoredOnIt()
        {
            RebindResult? bound = null;
            _ = Build();

            service.Rebind(TestAction.Fire, 0, result => bound = result);
            Press(keyboard.leftShiftKey);
            Settle();

            Assert.That(bound, Is.EqualTo(RebindResult.Conflict));
        }

        [Test]
        public void ARepeatedPress_WithinTheGuard_IsDropped_AndAfterItIsNot()
        {
            _ = Build(minRepeatSeconds: 0.2f);

            Press(keyboard.fKey);
            Release(keyboard.fKey);
            currentTime += 0.05;
            Press(keyboard.fKey);
            Release(keyboard.fKey);
            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }), "the second came too soon");

            currentTime += 0.5;
            Press(keyboard.fKey);
            Release(keyboard.fKey);
            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire, TestAction.Fire }));
        }

        [Test]
        public void TheRepeatGuard_IsPerAction()
        {
            _ = Build(minRepeatSeconds: 0.2f);

            Press(keyboard.fKey);
            Press(keyboard.aKey);

            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire, TestAction.Aim }));
        }

        [Test]
        public void WhileRebinding_ThePickedKeyDoesNotFireOtherActions()
        {
            _ = Build();

            service.Rebind(TestAction.Fire, 0, null);
            Press(keyboard.leftShiftKey);
            Settle();

            Assert.That(performed, Is.Empty);
        }

        [Test]
        public void AnOlderRebinding_IsCancelled_ByANewerOne()
        {
            RebindResult? first = null;
            _ = Build();

            service.Rebind(TestAction.Fire, 0, result => first = result);
            service.Rebind(TestAction.Hold, 0, null);

            Assert.That(first, Is.EqualTo(RebindResult.Cancelled));
        }

        [Test]
        public void ABindingTheActionLacks_ThrowsBeforeAnythingIsSwitchedOff()
        {
            _ = Build();

            _ = Assert.Throws<System.ArgumentOutOfRangeException>(() => service.Rebind(TestAction.Fire, 7, null));

            Press(keyboard.fKey);
            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }), "Fire is still enabled");
        }

        [Test]
        public void NoAction_CannotBeRebound()
        {
            _ = Build();

            _ = Assert.Throws<System.ArgumentException>(() => service.Rebind(TestAction.None, 0, null));
        }

        [Test]
        public void SavedRebindings_AreAppliedAtConstruction()
        {
            _ = Build();
            service.Rebind(TestAction.Fire, 0, null);
            Press(keyboard.gKey);
            Settle();
            Release(keyboard.gKey);
            service.Dispose();
            performed.Clear();

            _ = Build();
            Press(keyboard.gKey);

            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }));
        }

        [Test]
        public void ResettingBindings_RestoresTheDefaults_AndSavesThat()
        {
            _ = Build();
            service.Rebind(TestAction.Fire, 0, null);
            Press(keyboard.gKey);
            Settle();
            Release(keyboard.gKey);

            service.ResetBindings();
            performed.Clear();
            Press(keyboard.fKey);

            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }));
            Assert.That(store.Saves, Is.EqualTo(2));
        }

        [Test]
        public void DamagedSavedBindings_LogAnError_AndTheDefaultsPlayOn()
        {
            store.Json = "{ not json";
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Could not apply the saved key bindings"));

            _ = Build();
            Press(keyboard.fKey);

            Assert.That(performed, Is.EqualTo(new[] { TestAction.Fire }));
        }
    }
}
