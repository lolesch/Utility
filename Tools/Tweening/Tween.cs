using System;
using System.Collections.Generic;
using Submodules.Utility.Tools.Timer;
using UnityEngine;

namespace Submodules.Utility.Tools.Tweening
{
    /// <summary>
    /// A single-property lerp on an <see cref="Ease"/> curve. It is advanced by the same
    /// <c>Update</c>-loop insertion <see cref="Timer"/> rides — it registers with
    /// <see cref="TimerTicker"/> the same way, so there is no second PlayerLoop system.
    ///
    /// A tween is a duration, an ease, and a per-tick applier that is handed the eased
    /// 0..1 fraction; the caller turns that into a value (see
    /// <see cref="Submodules.Utility.Extensions.TweenExtensions"/> for the UI helpers).
    ///
    /// Handles are pooled: a completed or killed tween resets and returns to a shared
    /// stack, so a screen that fades a hundred times reuses one handle rather than
    /// allocating a hundred. The only per-start allocation is the applier / OnComplete
    /// delegate; there is none per tick.
    /// </summary>
    public sealed class Tween
    {
        private static readonly Stack<Tween> pool = new();

        private float duration;
        private float elapsed;
        private Ease ease;
        private Action<float> apply;
        private Action onComplete;
        private object target;
        private UnityEngine.Object link;
        private bool linked;

        private Tween() { }

        public bool IsRunning { get; private set; }

        /// <summary>The linear (un-eased) progress, 0..1.</summary>
        public float Progress => duration <= 0f ? 1f : Mathf.Clamp01( elapsed / duration );

        /// <summary>Count of running tweens, across every target.</summary>
        public static int ActiveCount => TimerTicker.Tweens.Count;

        /// <summary>
        /// Starts a tween. <paramref name="onUpdate"/> is handed the eased fraction
        /// (<see cref="Easing.Evaluate"/> of the linear progress) once per tick and
        /// finishes on exactly 1. A non-positive <paramref name="duration"/> completes on
        /// the next tick.
        /// </summary>
        public static Tween Play( float duration, Ease ease, Action<float> onUpdate )
        {
            var tween = pool.Count > 0 ? pool.Pop() : new Tween();

            tween.duration = Mathf.Max( 0f, duration );
            tween.elapsed = 0f;
            tween.ease = ease;
            tween.apply = onUpdate;
            tween.onComplete = null;
            tween.target = null;
            tween.link = null;
            tween.linked = false;
            tween.IsRunning = true;

            TimerTicker.RegisterTween( tween );

            return tween;
        }

        /// <summary>A callback for natural completion. Not called on <see cref="Kill()"/>.</summary>
        public Tween OnComplete( Action callback )
        {
            onComplete = callback;
            return this;
        }

        /// <summary>
        /// Tags the tween so <see cref="Kill(object)"/> and <see cref="IsTweening(object)"/>
        /// can find it — the <c>DOTween.Kill(transform)</c> replacement.
        /// </summary>
        public Tween SetTarget( object owner )
        {
            target = owner;
            return this;
        }

        /// <summary>
        /// Binds the tween to a Unity object: once that object is destroyed the tween
        /// cancels silently on its next tick instead of the applier throwing.
        /// </summary>
        public Tween LinkTo( UnityEngine.Object unityObject )
        {
            link = unityObject;
            linked = true;
            return this;
        }

        /// <summary>Cancels the tween. Silent by contract — does not fire <see cref="OnComplete"/>.</summary>
        public void Kill()
        {
            if ( !IsRunning )
                return;

            Release();
        }

        public void Tick( float deltaTime )
        {
            if ( !IsRunning )
                return;

            if ( linked && link == null )
            {
                Release();
                return;
            }

            elapsed += deltaTime;

            apply?.Invoke( Easing.Evaluate( ease, Progress ) );

            if ( elapsed < duration )
                return;

            var callback = onComplete;
            Release();
            callback?.Invoke();
        }

        private void Release()
        {
            IsRunning = false;
            apply = null;
            onComplete = null;
            target = null;
            link = null;
            linked = false;

            TimerTicker.DeregisterTween( this );
            pool.Push( this );
        }

        /// <summary>Cancels every running tween tagged with <paramref name="target"/>. Silent.</summary>
        public static void Kill( object target )
        {
            if ( target == null )
                return;

            var tweens = TimerTicker.Tweens;
            for ( var i = tweens.Count - 1; i >= 0; i-- )
                if ( Equals( tweens[i].target, target ) )
                    tweens[i].Kill();
        }

        /// <summary>True while a running tween is tagged with <paramref name="target"/>.</summary>
        public static bool IsTweening( object target )
        {
            if ( target == null )
                return false;

            var tweens = TimerTicker.Tweens;
            for ( var i = 0; i < tweens.Count; i++ )
                if ( Equals( tweens[i].target, target ) )
                    return true;

            return false;
        }

        /// <summary>Cancels every running tween. Silent. For play-mode exit and between test cases.</summary>
        public static void KillAll()
        {
            var tweens = TimerTicker.Tweens;
            for ( var i = tweens.Count - 1; i >= 0; i-- )
                tweens[i].Kill();
        }
    }
}
