using NUnit.Framework;
using Submodules.Utility.Provider;
using UnityEngine;

namespace Submodules.Utility.Tests.EditMode
{
    // One throwaway subclass per test: AbstractSceneSingleton<T>.Instance is backed by a
    // static field per closed generic type, so sharing a type across tests would leak the
    // resolved instance from one test into the next. A fresh type per test sidesteps that
    // without having to reach into the static state to reset it.
    internal sealed class FakeSceneSingletonA : AbstractSceneSingleton<FakeSceneSingletonA> { }
    internal sealed class FakeSceneSingletonB : AbstractSceneSingleton<FakeSceneSingletonB> { }
    internal sealed class FakeProviderA : AbstractProvider<FakeProviderA> { }

    /// <summary>
    /// <see cref="AbstractSceneSingleton{T}"/> is the whole of the scene-singleton contract
    /// (exactly one enabled <c>T</c>, whichever is found or created first); <see cref="AbstractProvider{T}"/>
    /// layers the cross-scene persistence promise on top via <see cref="AbstractProvider{T}.OnResolved"/>
    /// (root-reparenting a found instance, per InventoryTetris's own <c>DragProvider</c>/<c>PreviewProvider</c>
    /// split rework). Neither subclass overrides anything, so this covers the base contract
    /// both consumers of it rely on.
    /// </summary>
    [TestFixture]
    public sealed class ProviderTests
    {
        private readonly System.Collections.Generic.List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = spawned.Count - 1; 0 <= i; i--)
                if (spawned[i] != null)
                    Object.DestroyImmediate(spawned[i]);

            spawned.Clear();
        }

        [Test]
        public void Instance_WhenNoneExistsInTheScene_CreatesOneNamedAfterTheType()
        {
            var instance = FakeSceneSingletonA.Instance;
            spawned.Add(instance.gameObject);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.name, Is.EqualTo("FAKE_SCENE_SINGLETON_A"));
        }

        [Test]
        public void Instance_WhenTwoAlreadyExistInTheScene_ResolvesToExactlyOneOfThem_AndDisablesTheOther()
        {
            // Which of the two wins is whatever order FindObjectsOfType happens to return -
            // not a documented guarantee - so this asserts the actual contract (exactly one
            // survivor, the rest disabled not destroyed) rather than an assumed ordering.
            var first = new GameObject("first").AddComponent<FakeSceneSingletonB>();
            var second = new GameObject("second").AddComponent<FakeSceneSingletonB>();
            spawned.Add(first.gameObject);
            spawned.Add(second.gameObject);

            var resolved = FakeSceneSingletonB.Instance;

            Assert.That(resolved, Is.EqualTo(first).Or.EqualTo(second));
            Assert.That(resolved.enabled, Is.True);

            var other = resolved == first ? second : first;
            Assert.That(other.enabled, Is.False, "every later duplicate is disabled, not destroyed");
        }

        [Test]
        public void AbstractProvider_Instance_WhenFoundUnderAParent_InEditModeDoesNotReparent()
        {
            // The reparent-to-root rescue is a Play-mode-only belt-and-braces (see
            // AbstractProvider.OnResolved) - an edit-mode Instance read (this test, an
            // [InitializeOnLoad] hook, an inspector) must never mutate the scene. OnValidate
            // is the author-time guard for a mis-authored provider instead.
            var parent = new GameObject("parent");
            var child = new GameObject("child").AddComponent<FakeProviderA>();
            child.transform.SetParent(parent.transform);
            spawned.Add(parent);
            spawned.Add(child.gameObject);

            var resolved = FakeProviderA.Instance;

            Assert.That(resolved.transform.parent, Is.EqualTo(parent.transform),
                "edit-mode access must not mutate the scene, even for a mis-authored provider");
        }
    }
}
