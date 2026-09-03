using System;
using UnityEngine;

namespace Submodules.Utility.Tools.Tweening
{
    /// <summary>
    /// Evaluates an <see cref="Ease"/> at a normalised time. Pure: no state, no allocation,
    /// no engine calls beyond <see cref="Mathf"/>. <paramref name="t"/> is clamped to
    /// [0, 1], and the endpoints are returned exactly so the exponential curves (which
    /// only approach their bounds) still start at 0 and finish at 1.
    /// </summary>
    public static class Easing
    {
        private const float HalfPi = Mathf.PI * 0.5f;

        public static float Evaluate( Ease ease, float t )
        {
            t = Mathf.Clamp01( t );

            if ( t <= 0f )
                return 0f;

            if ( t >= 1f )
                return 1f;

            return ease switch
            {
                Ease.Linear => t,

                Ease.InSine => 1f - Mathf.Cos( t * HalfPi ),
                Ease.OutSine => Mathf.Sin( t * HalfPi ),
                Ease.InOutSine => -( Mathf.Cos( Mathf.PI * t ) - 1f ) * 0.5f,

                Ease.InQuad => t * t,
                Ease.OutQuad => 1f - ( 1f - t ) * ( 1f - t ),
                Ease.InOutQuad => t < 0.5f
                    ? 2f * t * t
                    : 1f - Mathf.Pow( -2f * t + 2f, 2f ) * 0.5f,

                Ease.InCubic => t * t * t,
                Ease.OutCubic => 1f - Mathf.Pow( 1f - t, 3f ),
                Ease.InOutCubic => t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow( -2f * t + 2f, 3f ) * 0.5f,

                Ease.InExpo => Mathf.Pow( 2f, 10f * ( t - 1f ) ),
                Ease.OutExpo => 1f - Mathf.Pow( 2f, -10f * t ),
                Ease.InOutExpo => t < 0.5f
                    ? Mathf.Pow( 2f, 20f * t - 10f ) * 0.5f
                    : ( 2f - Mathf.Pow( 2f, -20f * t + 10f ) ) * 0.5f,

                _ => throw new ArgumentOutOfRangeException( nameof( ease ), ease, null ),
            };
        }
    }
}
