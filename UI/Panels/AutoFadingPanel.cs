using Submodules.Utility.Tools.Timer;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A <see cref="SimplePanel"/> that can delay its own appearance
    /// (<see cref="fadeInDelay"/>, via an <see cref="FadeIn"/> override) and automatically
    /// fade itself back out a fixed time after appearing (<see cref="fadeOutDelay"/>).
    /// Either pending timer is cancelled the moment the panel actually starts appearing or
    /// disappearing, so an explicit <see cref="SimplePanel.FadeOut"/> always wins over a
    /// stale scheduled fade-in and vice versa. Split out of <see cref="SimplePanel"/>
    /// because every panel in the project leaves both features unused — they don't belong
    /// on the base every panel pays for.
    /// </summary>
    public class AutoFadingPanel : SimplePanel
    {
        [SerializeField, Range(0, 10)] private float fadeInDelay = 0f;
        [Tooltip("The timespan to pass after fading in before automatically fading out again. \n0 means no autoFadeOut.")]
        [SerializeField, Range(0, 10)] private float fadeOutDelay = 0f;

        /// <summary>The delayed fade-in / auto-fade-out schedulers — a sequence-of-callbacks job, now a plain <see cref="Timer"/>.</summary>
        private Timer delayedFadeIn;
        private Timer autoFadeOut;
        
        public override void FadeIn()
        {
            if (fadeInDelay <= 0)
            {
                base.FadeIn();
                return;
            }

            delayedFadeIn?.Stop();
            delayedFadeIn = new Timer(fadeInDelay);
            delayedFadeIn.OnComplete += base.FadeIn;
            delayedFadeIn.Start();
        }

        protected override void BeforeAppear()
        {
            base.BeforeAppear();
            StopTimers();
        }

        protected override void OnAppear()
        {
            base.OnAppear();

            if (fadeOutDelay <= 0) return;
            
            autoFadeOut?.Stop();
            autoFadeOut = new Timer(fadeOutDelay);
            autoFadeOut.OnComplete += FadeOut;
            autoFadeOut.Start();
        }

        protected override void BeforeDisappear()
        {
            base.BeforeDisappear();
            StopTimers();
        }

        protected override void OnPanelDisable()
        {
            base.OnPanelDisable();
        
            StopTimers();
        }
        
        private void StopTimers()
        {
            delayedFadeIn?.Stop();
            autoFadeOut?.Stop();
        }
    }
}
