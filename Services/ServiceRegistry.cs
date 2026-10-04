using System;
using System.Collections.Generic;

namespace Submodules.Utility.Services
{
    /// <summary>
    /// The services of one boot, keyed by the type they were registered under. A plain object:
    /// no statics, no scene, no Unity types - build one in a test and register fakes. The
    /// one-instance-per-type rule is enforced here, so a duplicate is unrepresentable and no
    /// scan-and-disable guard is needed. <see cref="ServiceLocator"/> is the static holder for
    /// the one the game boots with.
    /// </summary>
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, IService> services = new();

        /// <summary>Registers <paramref name="service"/> under <typeparamref name="T"/>. Throws
        /// when <typeparamref name="T"/> is already registered or the service is null.</summary>
        public void Register<T>(T service) where T : class, IService
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service), $"Cannot register a null {typeof(T).Name}.");

            if (!services.TryAdd(typeof(T), service))
                throw new InvalidOperationException($"{typeof(T).Name} is already registered; one service type maps to exactly one instance.");
        }

        /// <summary>The service registered under <typeparamref name="T"/>. Throws when there is
        /// none, naming the type, so a missing registration fails at the call that needs it.</summary>
        public T Get<T>() where T : class, IService =>
            TryGet<T>(out var service)
                ? service
                : throw new InvalidOperationException($"No {typeof(T).Name} is registered. Register it in the boot, before anything asks for it.");

        public bool TryGet<T>(out T service) where T : class, IService
        {
            if (services.TryGetValue(typeof(T), out var found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        public bool Contains<T>() where T : class, IService => services.ContainsKey(typeof(T));
    }
}
