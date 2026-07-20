using Silk.NET.Input;

namespace SharpCraft.Client.Input;

/// <summary>
/// A single place mapping keys to named actions, so keybindings live in one registry rather than
/// scattered across ad-hoc switch statements. Rebinding a key replaces its action.
/// </summary>
public sealed class Keymap
{
    private readonly Dictionary<Key, Action> _binds = new();

    /// <summary>Binds (or rebinds) a key to an action.</summary>
    public void Bind(Key key, Action action)
    {
        _binds[key] = action;
    }

    /// <summary>Invokes the action bound to the key, if any. Returns whether a binding handled it.</summary>
    public bool Handle(Key key)
    {
        if (_binds.TryGetValue(key, out var action))
        {
            action();
            return true;
        }

        return false;
    }
}
