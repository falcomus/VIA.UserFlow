// ======================================================================================
// FILE: Mockup/Grouping/PopupToolboxGroup.cs
//
// PURPOSE:
// - UI-only grouping model for the Popups tab in the Toolbox.
// - Keeps popup grouping separate from persisted ScreenPopup data.
// ======================================================================================

using System.Collections.ObjectModel;

namespace Mockup.Grouping;

/// <summary>
/// Represents one Popup group in the Toolbox navigation list.
/// </summary>
public sealed class PopupToolboxGroup
{
    public string GroupName { get; }

    public ObservableCollection<ScreenPopup> Popups { get; }

    public PopupToolboxGroup(
        string groupName,
        IEnumerable<ScreenPopup> popups)
    {
        GroupName = groupName;
        Popups = new ObservableCollection<ScreenPopup>(popups);
    }

    public override string ToString() => GroupName.Trim();
}
