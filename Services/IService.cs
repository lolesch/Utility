namespace Submodules.Utility.Services
{
    /// <summary>
    /// Marks a plain class as a game-wide service: one instance per registered type, held by a
    /// <see cref="ServiceRegistry"/> and reached through <see cref="ServiceLocator"/> from the
    /// Unity edge only. It carries no members on purpose - config services (built from authored
    /// data) and state services (operating on a hero's state) share nothing but the registry, so
    /// a base class would be empty, and a service must stay constructible in a test with a fake.
    /// Register a service under the interface callers depend on
    /// (<c>registry.Register&lt;IThing&gt;(new Thing())</c>), not the concrete type.
    /// </summary>
    public interface IService { }
}
