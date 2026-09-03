namespace Submodules.Utility.Tools.Tweening
{
    /// <summary>
    /// The standard easing curves, named to match the convention DOTween and easings.net
    /// use so a call site reads the same after the port. Every curve is monotone on
    /// [0, 1] and maps 0 to 0 and 1 to 1 — overshoot / elastic / bounce are intentionally
    /// out of scope (see dev/specs/2026-09-03-shared-ui-components-design.md).
    /// </summary>
    public enum Ease
    {
        Linear,

        InSine,
        OutSine,
        InOutSine,

        InQuad,
        OutQuad,
        InOutQuad,

        InCubic,
        OutCubic,
        InOutCubic,

        InExpo,
        OutExpo,
        InOutExpo,
    }
}
