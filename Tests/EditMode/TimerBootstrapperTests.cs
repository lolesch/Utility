using NUnit.Framework;
using Submodules.Utility.Tools.Tweening;
using Submodules.Utility.Tools.Timer;
using UnityEditor;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// When the timer and tween registrations are cleared on leaving Play Mode. They are cleared on
    /// entering Edit Mode, after the scene is torn down, so an owner can still deregister from its
    /// <c>OnDisable</c> / <c>OnDestroy</c>; the loop system is removed earlier, on
    /// <c>ExitingPlayMode</c>, so nothing ticks during that teardown.
    /// </summary>
    [TestFixture]
    public sealed class TimerBootstrapperTests
    {
        [SetUp]
        public void SetUp() => TimerTicker.Clear();

        [TearDown]
        public void TearDown()
        {
            Tween.KillAll();
            TimerTicker.Clear();
        }

        [Test]
        public void ExitingPlayMode_KeepsTheRegistrations_BecauseTheSceneIsStillBeingTornDown()
        {
            _ = Tween.Play( 1f, Ease.Linear, _ => { } );
            Assert.That( TimerTicker.Tweens, Has.Count.EqualTo( 1 ), "the tween registered itself" );

            TimerBootstrapper.OnPlayModeStateChanged( PlayModeStateChange.ExitingPlayMode );

            Assert.That( TimerTicker.Tweens, Has.Count.EqualTo( 1 ) );
        }

        [Test]
        public void EnteringEditMode_ClearsTheRegistrations_SoEditModeNeverSeesTheLastSessions()
        {
            _ = Tween.Play( 1f, Ease.Linear, _ => { } );

            TimerBootstrapper.OnPlayModeStateChanged( PlayModeStateChange.EnteredEditMode );

            Assert.That( TimerTicker.Tweens, Is.Empty );
        }

        [Test]
        public void EnteringPlayMode_LeavesTheRegistrationsAlone()
        {
            _ = Tween.Play( 1f, Ease.Linear, _ => { } );

            TimerBootstrapper.OnPlayModeStateChanged( PlayModeStateChange.EnteredPlayMode );

            Assert.That( TimerTicker.Tweens, Has.Count.EqualTo( 1 ) );
        }
    }
}
