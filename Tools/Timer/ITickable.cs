namespace Submodules.Utility.Tools.Timer
{
    /// <summary>
    /// Something the <see cref="TimerTicker"/> advances once per frame off the shared
    /// <c>Update</c>-loop insertion. <see cref="Timer"/> is registered directly; other
    /// clock-driven helpers (the tween) register as an <see cref="ITickable"/> so the
    /// ticker never needs to know their concrete type.
    /// </summary>
    internal interface ITickable
    {
        void Tick( float deltaTime );
    }
}
