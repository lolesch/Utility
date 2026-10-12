using Submodules.Utility.Services;
using System;
using UnityEngine;

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
        /// <summary>A button action went down. Not raised for a value action (the pointer).</summary>
        event Action<TAction> Performed;

        /// <summary>Whether the action's control is down right now.</summary>
        bool IsHeld(TAction action);

        /// <summary>The pointer (mouse, pen or touch) in screen pixels; zero when there is none.</summary>
        Vector2 PointerPosition { get; }

        /// <summary>
        /// Listens for the next control the player presses and binds <paramref name="action"/>'s
        /// binding at <paramref name="bindingIndex"/> to it, saving the result. Escape cancels.
        /// <paramref name="completed"/> is told whether a new control was bound.
        /// </summary>
        void Rebind(TAction action, int bindingIndex, Action<bool> completed);

        /// <summary>Drops every rebinding, back to the asset's defaults, and saves that.</summary>
        void ResetBindings();
    }
}
