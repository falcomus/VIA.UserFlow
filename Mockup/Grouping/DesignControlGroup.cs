// ======================================================================================
// FILE: Mockup/DesignControlGroup.cs
// 
// ZWECK: 
// Repräsentiert eine Gruppe von Controls in der Toolbox für organisierte 
// Darstellung und Gruppierung verwandter Steuerelemente.
//
// FUNKTIONALITÄTEN:
// - Gruppierung von Control-Descriptors in kategorisierbaren Gruppen
// - Expand/Collapse-Funktionalität für bessere Übersicht
// - Integration in die Toolbox-Struktur des Mockup-Designers
//
// AUTOR: [Ihr Name]
// VERSION: 1.0
// ERSTELLT: [Datum]
// LETZTE ÄNDERUNG: [Datum]
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Registry;
using System.Collections.ObjectModel;

namespace Mockup.Grouping;

/// <summary>
/// Gruppe von Controls in der Toolbox.
/// </summary>
public sealed partial class DesignControlGroup : ObservableObject
{
    public string GroupName { get; }

    public ObservableCollection<ControlDescriptor> Controls { get; set; } = new();

    [ObservableProperty]
    private bool isExpanded = true;


    /// <summary>
    /// Gibt eine String-Repräsentation der Gruppe zurück
    /// </summary>
    /// <returns>Den Namen der Gruppe</returns>
    public override string ToString() => GroupName.Trim();


    public DesignControlGroup(string groupName)
    {
        GroupName = groupName;
    }
}
