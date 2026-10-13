using Submodules.Utility.Services;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Submodules.Utility.Bindings
{
    /// <summary>
    /// The game's input as named actions rather than keys. <typeparamref name="TAction"/> is the
    /// game's own catalog (an enum whose default member means "no action"), so this mechanism never
    /// learns a game concept: what <c>QuickMove</c> is stays with the game, which key it starts on
    /// is its <c>.inputactions</c> asset, and which key it is now is the player's rebinding.
    /// Callers subscribe to <see cref="Performed"/> for a press and ask <see cref="IsHeld"/> for a
    /// modifier, instead of polling a <c>KeyCode</c>.
    /// </summary>
    public interface IInputService<TAction> : IService where TAction : struct, Enum
    {
        /// <summary>A button action went down. Not raised for a value action (the pointer), nor
        /// again for the same action within the service's repeat guard.</summary>
        event Action<TAction> Performed;

        /// <summary>Whether the action's control is down right now.</summary>
        bool IsHeld(TAction action);

        /// <summary>
        /// Whether the keyboard's <paramref name="key"/> is down right now, whatever it is bound to. For a rule
        /// about the keys themselves rather than the game's actions: the operating system's own chords (Alt+Tab)
        /// are made of keys, and rebinding an action does not move them.
        /// </summary>
        bool IsKeyHeld(Key key);

        /// <summary>The pointer (mouse, pen or touch) in screen pixels; zero when there is none.</summary>
        Vector2 PointerPosition { get; }

        /// <summary>
        /// Listens for the next key the player presses and binds <paramref name="action"/>'s
        /// binding at <paramref name="bindingIndex"/> to it, saving the result. Escape cancels; a
        /// key another action already uses is refused, bar one the two actions are authored to share.
        /// <paramref name="completed"/> is told which of those happened.
        /// </summary>
        void Rebind(TAction action, int bindingIndex, Action<RebindResult> completed);

        /// <summary>Drops every rebinding, back to the asset's defaults, and saves that.</summary>
        void ResetBindings();
    }
}
