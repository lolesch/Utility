using System;
using UnityEngine;

namespace Submodules.Utility.Services
{
    /// <summary>
    /// The static holder for the <see cref="ServiceRegistry"/> the game booted with. Called only
    /// from the Unity edge (views, <c>MonoBehaviour</c>s); anything engine-free takes its
    /// services by constructor. It never touches the scene: reading it cannot create, reparent
    /// or dirty anything, so an EditMode test or an <c>[InitializeOnLoad]</c> hook can ask
    /// <see cref="IsArmed"/> safely.
    ///
    /// Armed once by the boot (<see cref="Install"/>) and cleared in
    /// <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>, which fires on every Play
    /// entry even with domain reload disabled, so a second Play starts from a clean boot instead
    /// of inheriting the previous one's services. Reading it unarmed throws rather than returning
    /// null: a missed boot should fail at the read, not as an NRE far from the cause.
    /// </summary>
    public static class ServiceLocator
    {
        private static ServiceRegistry current;

        public static bool IsArmed => current != null;

        /// <summary>The armed registry. Throws when nothing is armed (boot has not run, or a test
        /// forgot <see cref="Install"/>).</summary>
        public static ServiceRegistry Current =>
            current ?? throw new InvalidOperationException(
                $"No {nameof(ServiceRegistry)} is armed. The game boot arms it before the first scene (Play Mode only); a test calls {nameof(ServiceLocator)}.{nameof(Install)}.");

        /// <summary>Arms <paramref name="registry"/>. Throws when one is already armed: a boot
        /// that installs twice is a bug, and <see cref="Reset"/> is the only way out.</summary>
        public static void Install(ServiceRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            if (current != null)
                throw new InvalidOperationException($"A {nameof(ServiceRegistry)} is already armed; call {nameof(Reset)} before installing another.");

            current = registry;
        }

        public static T Get<T>() where T : class, IService => Current.Get<T>();

        public static void Reset() => current = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayEntry() => Reset();
    }
}
