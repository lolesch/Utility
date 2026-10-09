using System.Reflection;
using NUnit.Framework;
using Submodules.Utility.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// A checkmark mirrors its toggle's bool onto the <see cref="Image"/> beside it. It is not
    /// <c>[ExecuteAlways]</c>, so <c>Awake</c> never runs in EditMode and the test calls it by hand, the way
    /// the player does when the checkmark is built at runtime: nothing has serialized its image reference.
    /// </summary>
    [TestFixture]
    public sealed class ToggleCheckmarkTests
    {
        private UiTestScene scene;

        [SetUp]
        public void SetUp() => scene = new UiTestScene();

        [TearDown]
        public void TearDown() => scene.Dispose();

        private static void Awake(ToggleCheckmark checkmark) =>
            typeof(ToggleCheckmark).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(checkmark, null);

        private ToggleCheckmark CheckmarkUnder(AbstractToggle toggle)
        {
            var go = new GameObject("checkmark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(toggle.transform, false);

            var checkmark = go.AddComponent<ToggleCheckmark>();

            // The Editor runs OnValidate on AddComponent (and on every serialized write), which fills the image
            // reference. A player never does, so a checkmark built at runtime starts with it empty.
            typeof(ToggleCheckmark).GetField("image", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(checkmark, null);

            return checkmark;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Awake_WithNoSerializedImage_ShowsTheTogglesState(bool isOn)
        {
            var toggle = scene.Toggle();
            toggle.SetToggle(isOn);
            var checkmark = CheckmarkUnder(toggle);

            Assert.That(() => Awake(checkmark), Throws.Nothing);

            Assert.That(checkmark.GetComponent<Image>().enabled, Is.EqualTo(isOn));
        }
    }
}