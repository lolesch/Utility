using NUnit.Framework;
using Submodules.Utility.Services;
using System;

namespace Submodules.Utility.Tests.EditMode
{
    internal interface IFakeGreeter : IService { string Greet(); }

    internal sealed class FakeGreeter : IFakeGreeter
    {
        public string Greet() => "hello";
    }

    internal sealed class FakeCounter : IService { }

    [TestFixture]
    public sealed class ServiceRegistryTests
    {
        [Test]
        public void Get_ReturnsTheInstanceRegisteredUnderThatType()
        {
            var registry = new ServiceRegistry();
            var greeter = new FakeGreeter();

            registry.Register<IFakeGreeter>(greeter);

            Assert.That(registry.Get<IFakeGreeter>(), Is.SameAs(greeter));
        }

        [Test]
        public void Get_WhenNothingIsRegistered_ThrowsNamingTheType()
        {
            var registry = new ServiceRegistry();

            var e = Assert.Throws<InvalidOperationException>(() => registry.Get<IFakeGreeter>());

            Assert.That(e.Message, Does.Contain(nameof(IFakeGreeter)));
        }

        [Test]
        public void Register_TheSameTypeTwice_Throws_AndKeepsTheFirstInstance()
        {
            var registry = new ServiceRegistry();
            var first = new FakeGreeter();
            registry.Register<IFakeGreeter>(first);

            _ = Assert.Throws<InvalidOperationException>(() => registry.Register<IFakeGreeter>(new FakeGreeter()));

            Assert.That(registry.Get<IFakeGreeter>(), Is.SameAs(first));
        }

        [Test]
        public void Register_Null_Throws()
        {
            var registry = new ServiceRegistry();

            _ = Assert.Throws<ArgumentNullException>(() => registry.Register<IFakeGreeter>(null));
        }

        [Test]
        public void Register_DifferentTypes_AreIndependent()
        {
            var registry = new ServiceRegistry();
            var counter = new FakeCounter();

            registry.Register<IFakeGreeter>(new FakeGreeter());
            registry.Register(counter);

            Assert.That(registry.Get<FakeCounter>(), Is.SameAs(counter));
            Assert.That(registry.Contains<IFakeGreeter>(), Is.True);
        }

        [Test]
        public void TryGet_WhenMissing_ReturnsFalse_AndNull()
        {
            var registry = new ServiceRegistry();

            Assert.That(registry.TryGet<IFakeGreeter>(out var service), Is.False);
            Assert.That(service, Is.Null);
        }

        [Test]
        public void TwoRegistries_DoNotShareServices()
        {
            var a = new ServiceRegistry();
            var b = new ServiceRegistry();

            a.Register<IFakeGreeter>(new FakeGreeter());

            Assert.That(b.Contains<IFakeGreeter>(), Is.False);
        }
    }
}
