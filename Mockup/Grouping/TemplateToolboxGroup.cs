// ======================================================================================
// FILE: Mockup/Grouping/TemplateToolboxGroup.cs
//
// PURPOSE:
// - UI-only grouping model for the Templates tab in the Toolbox.
// - Keeps template grouping separate from persisted ScreenTemplate data.
// ======================================================================================

using System.Collections.ObjectModel;

namespace Mockup.Grouping;

/// <summary>
/// Represents one Template group in the Toolbox navigation list.
/// </summary>
public sealed class TemplateToolboxGroup
{
    public string GroupName { get; }

    public ObservableCollection<ScreenTemplate> Templates { get; }

    public TemplateToolboxGroup(
        string groupName,
        IEnumerable<ScreenTemplate> templates)
    {
        GroupName = groupName;
        Templates = new ObservableCollection<ScreenTemplate>(templates);
    }

    public override string ToString() => GroupName.Trim();
}
