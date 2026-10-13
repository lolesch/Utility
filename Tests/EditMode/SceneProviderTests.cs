using NUnit.Framework;
using Submodules.Utility.UI;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// The refusals of <see cref="SceneProvider.LoadScene"/>: a request it cannot start answers <c>false</c> at
    /// once, before any coroutine or fade. The accepted path and the refusal of a second request while one
    /// loads need a real scene change, so they are covered by the game's Play Mode tests.
    /// </summary>
    [TestFixture]
    public sealed class SceneProviderTests
    {
        private SceneProvider provider;

        [SetUp]
        public void SetUp() => provider = new GameObject("scene provider").AddComponent<SceneProvider>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(provider.gameObject);

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void LoadScene_WithABlankName_IsRefusedAtOnce(string blank)
        {
            LogAssert.Expect(LogType.Error, new Regex("Cant load"));

            Assert.That(provider.LoadScene(blank), Is.False);
            Assert.That(provider.IsLoading, Is.False, "a refusal starts nothing");
        }

        [Test]
        public void LoadScene_OfASceneTheBuildDoesNotHold_IsRefusedAtOnce()
        {
            LogAssert.Expect(LogType.Error, new Regex("not in the build"));

            Assert.That(provider.LoadScene("NoSuchScene"), Is.False);
            Assert.That(provider.IsLoading, Is.False, "an unloadable scene never fades the screen in");
        }
    }
}
