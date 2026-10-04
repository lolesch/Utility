using NUnit.Framework;
using Submodules.Utility.Tools;
using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Submodules.Utility.Tests.EditMode
{
    [TestFixture]
    public sealed class PlayerLoopHookTests
    {
        private sealed class HookMarker { }

        [TearDown]
        public void TearDown() => PlayerLoopHook.Remove(typeof(HookMarker));

        [Test]
        public void Install_PutsOneSystemInTheLoop_AndRemoveTakesItOut()
        {
            Assert.That(PlayerLoopHook.IsInstalled(typeof(HookMarker)), Is.False);

            PlayerLoopHook.Install<Update>(typeof(HookMarker), () => { });

            Assert.That(PlayerLoopHook.IsInstalled(typeof(HookMarker)), Is.True);
            Assert.That(CountOf(typeof(HookMarker)), Is.EqualTo(1));

            PlayerLoopHook.Remove(typeof(HookMarker));

            Assert.That(CountOf(typeof(HookMarker)), Is.Zero);
        }

        [Test]
        public void Install_Twice_StillLeavesExactlyOne_AndTheLatestDelegateWins()
        {
            var calls = 0;
            PlayerLoopHook.Install<Update>(typeof(HookMarker), () => calls += 100);
            PlayerLoopHook.Install<Update>(typeof(HookMarker), () => calls++);

            Assert.That(CountOf(typeof(HookMarker)), Is.EqualTo(1));

            FindSystem(PlayerLoop.GetCurrentPlayerLoop(), typeof(HookMarker)).updateDelegate();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Install_AppendsToTheEndOfThePhase_SoItRunsAfterTheScriptUpdates()
        {
            PlayerLoopHook.Install<Update>(typeof(HookMarker), () => { });

            var update = FindSystem(PlayerLoop.GetCurrentPlayerLoop(), typeof(Update));

            Assert.That(update.subSystemList[^1].type, Is.EqualTo(typeof(HookMarker)));
        }

        [Test]
        public void Install_IntoAPhaseTheLoopLacks_Throws_AndLeavesTheLoopUntouched()
        {
            _ = Assert.Throws<InvalidOperationException>(() =>
                PlayerLoopHook.Install<HookMarker>(typeof(HookMarker), () => { }));

            Assert.That(CountOf(typeof(HookMarker)), Is.Zero);
        }

        private static int CountOf(Type type) => Count(PlayerLoop.GetCurrentPlayerLoop(), type);

        private static int Count(PlayerLoopSystem system, Type type)
        {
            var count = system.type == type ? 1 : 0;

            if (system.subSystemList != null)
                foreach (var sub in system.subSystemList)
                    count += Count(sub, type);

            return count;
        }

        private static PlayerLoopSystem FindSystem(PlayerLoopSystem system, Type type)
        {
            if (system.type == type)
                return system;

            if (system.subSystemList != null)
                foreach (var sub in system.subSystemList)
                {
                    var found = FindSystem(sub, type);
                    if (found.type == type)
                        return found;
                }

            return default;
        }
    }
}
