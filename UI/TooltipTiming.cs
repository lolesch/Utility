using System;

namespace Submodules.Utility.UI
{
    public enum TooltipVisibility
    {
        Hidden,
        PendingShow,
        Visible,
        PendingHide,
    }

    /// <summary>
    /// The <see cref="TooltipHost{T}"/> mechanism with no <see cref="UnityEngine.MonoBehaviour"/>
    /// attached: a show-delay before a hint appears, and a hide-debounce so leaving and
    /// re-entering within a short window swaps content instead of flashing the panel out
    /// and back in. One <typeparamref name="T"/> at a time - a request while a hint is
    /// already visible (or fading out) replaces it immediately rather than restarting the
    /// show-delay. Driven by hand via <see cref="Tick"/>, the same manual-tick shape
    /// <see cref="Submodules.Utility.Tools.Tweening.Tween"/> uses.
    /// </summary>
    public sealed class TooltipTiming<T>
    {
        private readonly float showDelay;
        private readonly float hideDebounce;

        private float elapsed;
        private T pending;

        public TooltipTiming(float showDelay, float hideDebounce)
        {
            this.showDelay = showDelay;
            this.hideDebounce = hideDebounce;
        }

        public TooltipVisibility State { get; private set; } = TooltipVisibility.Hidden;

        /// <summary>The content shown while <see cref="Visible"/> or <see cref="PendingHide"/>.</summary>
        public T Content { get; private set; }

        public event Action<T> OnShow;
        public event Action OnHide;

        /// <summary>
        /// A hover / select. From <see cref="TooltipVisibility.Hidden"/> or
        /// <see cref="TooltipVisibility.PendingShow"/> this (re)starts the show-delay
        /// countdown for <paramref name="content"/>. From <see cref="TooltipVisibility.Visible"/>
        /// or <see cref="TooltipVisibility.PendingHide"/> a hint is already up, or on its way
        /// out within the debounce window, so it swaps content in immediately instead.
        /// </summary>
        public void Request(T content)
        {
            switch (State)
            {
                case TooltipVisibility.Hidden:
                case TooltipVisibility.PendingShow:
                    pending = content;
                    elapsed = 0f;
                    State = TooltipVisibility.PendingShow;
                    break;

                case TooltipVisibility.Visible:
                case TooltipVisibility.PendingHide:
                    Content = content;
                    State = TooltipVisibility.Visible;
                    OnShow?.Invoke(Content);
                    break;
            }
        }

        /// <summary>
        /// An exit / deselect. Cancels a pending show outright - nothing was ever shown, so
        /// there is nothing to debounce. Starts the hide-debounce instead of hiding
        /// synchronously when a hint is visible, so a <see cref="Request"/> landing before
        /// the debounce elapses swaps content instead of flashing the panel.
        /// </summary>
        public void Cancel()
        {
            switch (State)
            {
                case TooltipVisibility.PendingShow:
                    State = TooltipVisibility.Hidden;
                    break;

                case TooltipVisibility.Visible:
                    elapsed = 0f;
                    State = TooltipVisibility.PendingHide;
                    break;
            }
        }

        public void Tick(float deltaTime)
        {
            if (State != TooltipVisibility.PendingShow && State != TooltipVisibility.PendingHide)
                return;

            elapsed += deltaTime;

            if (State == TooltipVisibility.PendingShow)
            {
                if (elapsed < showDelay)
                    return;

                Content = pending;
                State = TooltipVisibility.Visible;
                OnShow?.Invoke(Content);
            }
            else
            {
                if (elapsed < hideDebounce)
                    return;

                State = TooltipVisibility.Hidden;
                OnHide?.Invoke();
            }
        }
    }
}
