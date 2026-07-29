namespace Mockup.Grouping;

/// <summary>
/// Represents a screen category in the master column of the Screen view.
/// The persisted group key is deliberately kept separate from its UI display name.
/// </summary>
public sealed class ScreenNavigationGroup
{
    public ScreenNavigationGroup(string key, string displayName, int count, bool isAll = false)
    {
        Key = key;
        DisplayName = displayName;
        Count = count;
        IsAll = isAll;
    }

    /// <summary>Stable group identifier used for filtering.</summary>
    public string Key { get; }

    /// <summary>Human-readable, localized display text used by the UI.</summary>
    public string DisplayName { get; }

    public int Count { get; }

    public bool IsAll { get; }
}
