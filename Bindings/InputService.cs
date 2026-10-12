using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RebindingOperation = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

namespace Submodules.Utility.Bindings
{
    /// <summary>
    /// <see cref="IInputService{TAction}"/> over an <see cref="InputActionAsset"/>: each member of the
    /// <typeparamref name="TAction"/> catalog (bar the default, "no action") is the asset's action of
    /// the same name, and a catalog member the asset lacks throws at construction, not as a dead
    /// key in play.
    ///
    /// <para>It works on a clone of the asset. The Editor with domain reload disabled keeps a
    /// ScriptableObject alive across Play sessions, so rebindings and the enabled state of the
    /// shared asset itself would leak from one session into the next. The clone is enabled here and
    /// destroyed by <see cref="Dispose"/>.</para>
    /// </summary>
    public sealed class InputService<TAction> : IInputService<TAction>, IDisposable where TAction : struct, Enum
    {
        private readonly InputActionAsset asset;
        private readonly Dictionary<TAction, InputAction> actions = new();
        private readonly Dictionary<TAction, double> lastPerformed = new();
        private readonly IBindingOverrideStore store;
        private readonly float minRepeatSeconds;
        private RebindingOperation rebinding;

        public event Action<TAction> Performed;

        /// <param name="source">The authored actions; cloned, never changed.</param>
        /// <param name="store">Where rebindings are kept. Saved ones are applied here.</param>
        /// <param name="minRepeatSeconds">Key-spam guard: a press of an action within this many seconds
        /// (input time) of its last accepted press is dropped. Zero accepts every press.</param>
        public InputService(InputActionAsset source, IBindingOverrideStore store, float minRepeatSeconds = 0f)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.minRepeatSeconds = minRepeatSeconds;
            asset = UnityEngine.Object.Instantiate(source);

            foreach (var id in (TAction[])Enum.GetValues(typeof(TAction)))
            {
                if (EqualityComparer<TAction>.Default.Equals(id, default))
                    continue;

                var action = asset.FindAction(id.ToString())
                    ?? throw new InvalidOperationException($"{source.name} has no action '{id}'; every {typeof(TAction).Name} member needs one.");

                actions.Add(id, action);

                // A value action (the pointer) changes every frame; only a button is an event.
                if (action.type == InputActionType.Button)
                    action.performed += context => RaisePerformed(id, context.time);
            }

            ApplySavedOverrides();
            asset.Enable();
        }

        public bool IsHeld(TAction action) => actions.TryGetValue(action, out var found) && found.IsPressed();

        public Vector2 PointerPosition => Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

        public void Rebind(TAction action, int bindingIndex, Action<RebindResult> completed)
        {
            if (!actions.TryGetValue(action, out var target))
                throw new ArgumentException($"'{action}' is not an action of this service.", nameof(action));

            // One at a time: the older one is told it was cancelled.
            rebinding?.Cancel();

            // Checked before the action is switched off, so a wrong index leaves it working.
            if (bindingIndex < 0 || bindingIndex >= target.bindings.Count)
                throw new ArgumentOutOfRangeException(nameof(bindingIndex), $"'{action}' has {target.bindings.Count} binding(s); there is no binding {bindingIndex}.");

            // An enabled action cannot be rebound.
            target.Disable();

            // Keys only: a click or a pad button is the player reaching for the settings screen, not a pick.
            var conflicted = false;

            rebinding = target.PerformInteractiveRebinding(bindingIndex)
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnApplyBinding((operation, path) =>
                {
                    conflicted = IsTakenByAnother(target, bindingIndex, operation.selectedControl);

                    if (!conflicted)
                        target.ApplyBindingOverride(bindingIndex, path);
                })
                .OnComplete(operation => Finish(operation, target, conflicted ? RebindResult.Conflict : RebindResult.Bound, completed))
                .OnCancel(operation => Finish(operation, target, RebindResult.Cancelled, completed))
                .Start();
        }

        // Two actions on one key fire together. The exception is a pair authored onto the same key
        // (QuickMove and SecondSlot on Shift): putting either back on it is not a collision.
        private bool IsTakenByAnother(InputAction target, int bindingIndex, InputControl picked)
        {
            var authoredHere = InputControlPath.Matches(target.bindings[bindingIndex].path, picked);

            foreach (var other in actions.Values)
            {
                if (other == target)
                    continue;

                foreach (var binding in other.bindings)
                {
                    if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath))
                        continue;

                    if (!InputControlPath.Matches(binding.effectivePath, picked))
                        continue;

                    if (authoredHere && InputControlPath.Matches(binding.path, picked))
                        continue;

                    return true;
                }
            }

            return false;
        }

        public void ResetBindings()
        {
            asset.RemoveAllBindingOverrides();
            Save();
        }

        public void Dispose()
        {
            rebinding?.Dispose();
            rebinding = null;
            asset.Disable();

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(asset);
            else
                UnityEngine.Object.DestroyImmediate(asset);
        }

        // The key that is being picked for one action must not also fire the others.
        private void RaisePerformed(TAction id, double time)
        {
            if (rebinding != null)
                return;

            if (minRepeatSeconds > 0f && lastPerformed.TryGetValue(id, out var last) && time - last < minRepeatSeconds)
                return;

            lastPerformed[id] = time;
            Performed?.Invoke(id);
        }

        private void Finish(RebindingOperation operation, InputAction target, RebindResult result, Action<RebindResult> completed)
        {
            operation.Dispose();
            rebinding = null;
            target.Enable();

            if (result == RebindResult.Bound)
                Save();

            completed?.Invoke(result);
        }

        private void Save() => store.Save(asset.SaveBindingOverridesAsJson());

        // A damaged value must not stop the game booting: the defaults play on.
        private void ApplySavedOverrides()
        {
            var json = store.Load();

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                asset.LoadBindingOverridesFromJson(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not apply the saved key bindings; using the defaults: {exception.Message}");
            }
        }
    }
}
