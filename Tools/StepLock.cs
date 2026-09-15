using System.Collections.Generic;
using System.Linq;

namespace Submodules.Utility.Tools
{
    public class StepLock
    {
        private readonly List<object> lockers = new();
        public bool IsLocked => lockers.Any();
        public bool IsOpen => !IsLocked;

        public void Lock(object locker) => lockers.Add(locker);
        public void Unlock(object unlocker) => lockers.Remove(unlocker);
        public void ForceUnlock() => lockers.Clear();
    }
}
