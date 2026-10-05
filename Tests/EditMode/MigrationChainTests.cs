using NUnit.Framework;
using Submodules.Utility.Persistence;
using System;

namespace Submodules.Utility.Tests.EditMode
{
    [TestFixture]
    public sealed class MigrationChainTests
    {
        [Test]
        public void Upgrade_ReturnsThePayloadUnchanged_WhenThereIsNothingToUpgrade()
        {
            Assert.That(new MigrationChain().Upgrade("p", 2, 2), Is.EqualTo("p"));
        }

        [Test]
        public void Upgrade_RunsEachStepInVersionOrder()
        {
            var chain = new MigrationChain()
                .Add(2, p => p + "2")
                .Add(1, p => p + "1")
                .Add(3, p => p + "3");

            Assert.That(chain.Upgrade("p", 1, 4), Is.EqualTo("p123"));
        }

        [Test]
        public void Upgrade_Throws_WhenAStepInBetweenIsMissing()
        {
            var chain = new MigrationChain().Add(1, p => p);

            var thrown = Assert.Throws<InvalidOperationException>(() => chain.Upgrade("p", 1, 3));
            StringAssert.Contains("version 2", thrown.Message);
        }

        [Test]
        public void Add_Throws_ForAVersionThatAlreadyHasAStep()
        {
            var chain = new MigrationChain().Add(1, p => p);

            Assert.Throws<InvalidOperationException>(() => chain.Add(1, p => p));
        }
    }
}
