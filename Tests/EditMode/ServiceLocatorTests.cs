using NUnit.Framework;
using Submodules.Utility.Services;
using System;
using UnityEditor;
using UnityEngine;

namespace Submodules.Utility.Tests.EditMode
{
    [TestFixture]
    public sealed class ServiceLocatorTests
    {
        [SetUp]
        public void SetUp() => ServiceLocator.Reset();

        [TearDown]
        public void TearDown() => ServiceLocator.Reset();

        [Test]
        public void Current_WhenNothingIsArmed_Throws_AndIsArmedIsFalse()
        {
            Assert.That(ServiceLocator.IsArmed, Is.False);
            _ = Assert.Throws<InvalidOperationException>(() => _ = ServiceLocator.Current);
            _ = Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<FakeCounter>());
        }

        [Test]
        public void Install_ArmsTheRegistry_AndGetResolvesThroughIt()
        {
            var registry = new ServiceRegistry();
            var counter = new FakeCounter();
            registry.Register(counter);

            ServiceLocator.Install(registry);

            Assert.That(ServiceLocator.IsArmed, Is.True);
            Assert.That(ServiceLocator.Current, Is.SameAs(registry));
            Assert.That(ServiceLocator.Get<FakeCounter>(), Is.SameAs(counter));
        }

        [Test]
        public void Install_WhileArmed_Throws_AndKeepsTheFirstRegistry()
        {
            var first = new ServiceRegistry();
            ServiceLocator.Install(first);

            _ = Assert.Throws<InvalidOperationException>(() => ServiceLocator.Install(new ServiceRegistry()));

            Assert.That(ServiceLocator.Current, Is.SameAs(first));
        }

        [Test]
        public void Install_Null_Throws()
        {
            _ = Assert.Throws<ArgumentNullException>(() => ServiceLocator.Install(null));
        }

        [Test]
        public void Reset_DisarmsIt_SoASecondPlayEntryStartsClean()
        {
            var first = new ServiceRegistry();
            first.Register(new FakeCounter());
            ServiceLocator.Install(first);

            ServiceLocator.Reset();

            Assert.That(ServiceLocator.IsArmed, Is.False);

            var second = new ServiceRegistry();
            ServiceLocator.Install(second);
            Assert.That(ServiceLocator.Current, Is.SameAs(second));
            Assert.That(ServiceLocator.Current.Contains<FakeCounter>(), Is.False);
        }

        [Test]
        public void EnteringEditMode_DisarmsIt_SoEditModeNeverSeesTheLastSessionsServices()
        {
            ServiceLocator.Install(new ServiceRegistry());

            ServiceLocator.OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            Assert.That(ServiceLocator.IsArmed, Is.True);

            ServiceLocator.OnPlayModeStateChanged(PlayModeStateChange.EnteredEditMode);
            Assert.That(ServiceLocator.IsArmed, Is.False);
        }

        [Test]
        public void ExitingPlayMode_StaysArmed_SoTheScenesTeardownCanStillReadAService()
        {
            var registry = new ServiceRegistry();
            var counter = new FakeCounter();
            registry.Register(counter);
            ServiceLocator.Install(registry);

            // The scene is still alive here: every OnDisable and OnDestroy runs after this event.
            ServiceLocator.OnPlayModeStateChanged(PlayModeStateChange.ExitingPlayMode);

            Assert.That(ServiceLocator.IsArmed, Is.True);
            Assert.That(ServiceLocator.Get<FakeCounter>(), Is.SameAs(counter));
        }

        [Test]
        public void ReadingTheLocator_CreatesNoGameObject()
        {
            var before = Resources.FindObjectsOfTypeAll<GameObject>().Length;

            _ = ServiceLocator.IsArmed;
            ServiceLocator.Install(new ServiceRegistry());
            _ = ServiceLocator.Current;
            ServiceLocator.Reset();

            Assert.That(Resources.FindObjectsOfTypeAll<GameObject>().Length, Is.EqualTo(before));
        }
    }
}
