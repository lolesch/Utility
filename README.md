# Utility
frequently used tools and extensions

## Bindings

Named, rebindable input over the Input System package (`Utility.Bindings`). It compiles only where
`com.unity.inputsystem` is installed, so a project without the package is unaffected.

To use it in a project:

1. Define the catalog as an enum whose default member means "no action":
   `enum GameAction { None = 0, Jump = 1, Interact = 2 }`.
2. Author an `.inputactions` asset with one action per member, named after it, and load it.
3. `new InputService<GameAction>(asset, store)`, with a `PlayerPrefsBindingStore` for the player's
   rebindings (or your own `IBindingOverrideStore`). It throws when the asset lacks a member. Pass a
   third argument, `minRepeatSeconds`, to drop a hammered key (a press within it of the action's last
   is ignored). Register it under `IInputService<GameAction>`: `ServiceRegistry.Dispose` (which
   `ServiceLocator.Reset` calls) disposes it, or `Dispose` it yourself.
4. Subscribe to `Performed` for a press, ask `IsHeld` for a modifier and `PointerPosition` for the
   pointer, and call `Rebind` / `ResetBindings` from a settings screen. A rebind takes keys only;
   Escape cancels it, and a key another action uses is refused (`RebindResult.Conflict`) unless both
   actions are authored onto it.

Tests press keys through the package's `InputTestFixture`; see `Tests/EditMode/Bindings`.
