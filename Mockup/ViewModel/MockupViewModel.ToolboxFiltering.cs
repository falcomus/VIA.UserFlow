// ======================================================================================
// FILE: Mockup/ViewModel/MockupViewModel.ToolboxFiltering.cs
//
// PURPOSE:
// - Search/filter support for Controls, Templates and Popups in the Toolbox.
// - Provides UI-only groups with "All" always first.
// - Sorts every remaining group alphabetically.
// - Does not change persistence, public file formats or registry keys.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Grouping;
using Mockup.Registry;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Mockup.ViewModel;

public partial class MockupViewModel
{
    #region === CONTROLS TOOLBOX SEARCH ===

    [ObservableProperty]
    private string controlToolboxSearchText = string.Empty;

    public ObservableCollection<ControlDescriptor> FilteredControlDescriptors { get; } = [];

    partial void OnControlToolboxSearchTextChanged(string value)
    {
        RefreshControlToolboxItems();
    }

    partial void OnCurrentControlGroupChanged(DesignControlGroup? value)
    {
        RefreshControlToolboxItems();
    }

    private void RefreshControlToolboxItems()
    {
        IEnumerable<ControlDescriptor> source =
            CurrentControlGroup?.Controls
            ?? Enumerable.Empty<ControlDescriptor>();

        string searchText = ControlToolboxSearchText?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            source = source.Where(descriptor =>
                ContainsSearch(descriptor.DisplayName, searchText) ||
                ContainsSearch(descriptor.Group, searchText) ||
                ContainsSearch(descriptor.TypeKey, searchText));
        }

        ReplaceCollection(
            FilteredControlDescriptors,
            source.OrderBy(
                descriptor => descriptor.DisplayName,
                StringComparer.OrdinalIgnoreCase));
    }

    #endregion

    #region === TEMPLATES TOOLBOX GROUPING AND SEARCH ===

    public ObservableCollection<TemplateToolboxGroup> TemplateToolboxGroups { get; } = [];

    [ObservableProperty]
    private TemplateToolboxGroup? currentTemplateToolboxGroup;

    [ObservableProperty]
    private string templateToolboxSearchText = string.Empty;

    public ObservableCollection<ScreenTemplate> FilteredTemplates { get; } = [];

    partial void OnCurrentTemplateToolboxGroupChanged(TemplateToolboxGroup? value)
    {
        RefreshTemplateToolboxItems();
    }

    partial void OnTemplateToolboxSearchTextChanged(string value)
    {
        RefreshTemplateToolboxItems();
    }

    #endregion

    #region === POPUPS TOOLBOX GROUPING AND SEARCH ===

    public ObservableCollection<PopupToolboxGroup> PopupToolboxGroups { get; } = [];

    [ObservableProperty]
    private PopupToolboxGroup? currentPopupToolboxGroup;

    [ObservableProperty]
    private string popupToolboxSearchText = string.Empty;

    public ObservableCollection<ScreenPopup> FilteredPopups { get; } = [];

    partial void OnCurrentPopupToolboxGroupChanged(PopupToolboxGroup? value)
    {
        RefreshPopupToolboxItems();
    }

    partial void OnPopupToolboxSearchTextChanged(string value)
    {
        RefreshPopupToolboxItems();
    }

    #endregion

    #region === TOOLBOX FILTERING INITIALIZATION ===

    private Project? _toolboxSubscribedProject;
    private ObservableCollection<ScreenTemplate>? _toolboxSubscribedTemplates;

    private void InitToolboxFiltering()
    {
        PropertyChanged += OnToolboxViewModelPropertyChanged;

        AttachTemplateToolboxCollection(Templates);
        AttachPopupToolboxProject(CurrentProject);

        RebuildTemplateToolboxGroups();
        RebuildPopupToolboxGroups();
        RefreshControlToolboxItems();
    }

    private void OnToolboxViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Templates))
        {
            AttachTemplateToolboxCollection(Templates);
            RebuildTemplateToolboxGroups();
            return;
        }

        if (e.PropertyName == nameof(CurrentProject))
        {
            AttachPopupToolboxProject(CurrentProject);
            RebuildPopupToolboxGroups();
        }
    }

    #endregion

    #region === TEMPLATE TOOLBOX EVENTS ===

    private void AttachTemplateToolboxCollection(
        ObservableCollection<ScreenTemplate>? templates)
    {
        if (ReferenceEquals(_toolboxSubscribedTemplates, templates))
            return;

        if (_toolboxSubscribedTemplates != null)
        {
            _toolboxSubscribedTemplates.CollectionChanged -=
                OnTemplateToolboxCollectionChanged;

            foreach (ScreenTemplate template in _toolboxSubscribedTemplates)
            {
                template.PropertyChanged -=
                    OnTemplateToolboxPropertyChanged;
            }
        }

        _toolboxSubscribedTemplates = templates;

        if (_toolboxSubscribedTemplates == null)
            return;

        _toolboxSubscribedTemplates.CollectionChanged +=
            OnTemplateToolboxCollectionChanged;

        foreach (ScreenTemplate template in _toolboxSubscribedTemplates)
        {
            template.PropertyChanged +=
                OnTemplateToolboxPropertyChanged;
        }
    }

    private void OnTemplateToolboxCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ScreenTemplate template in e.OldItems)
            {
                template.PropertyChanged -=
                    OnTemplateToolboxPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (ScreenTemplate template in e.NewItems)
            {
                template.PropertyChanged +=
                    OnTemplateToolboxPropertyChanged;
            }
        }

        RebuildTemplateToolboxGroups();
    }

    private void OnTemplateToolboxPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScreenTemplate.Name) ||
            e.PropertyName == nameof(ScreenTemplate.Description) ||
            e.PropertyName == nameof(ScreenTemplate.GroupName))
        {
            RebuildTemplateToolboxGroups();
        }
    }

    #endregion

    #region === POPUP TOOLBOX EVENTS ===

    private void AttachPopupToolboxProject(Project? project)
    {
        if (ReferenceEquals(_toolboxSubscribedProject, project))
            return;

        if (_toolboxSubscribedProject != null)
        {
            _toolboxSubscribedProject.Popups.CollectionChanged -=
                OnPopupToolboxCollectionChanged;

            foreach (ScreenPopup popup in _toolboxSubscribedProject.Popups)
            {
                popup.PropertyChanged -=
                    OnPopupToolboxPropertyChanged;
            }
        }

        _toolboxSubscribedProject = project;

        if (_toolboxSubscribedProject == null)
            return;

        _toolboxSubscribedProject.Popups.CollectionChanged +=
            OnPopupToolboxCollectionChanged;

        foreach (ScreenPopup popup in _toolboxSubscribedProject.Popups)
        {
            popup.PropertyChanged +=
                OnPopupToolboxPropertyChanged;
        }
    }

    private void OnPopupToolboxCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ScreenPopup popup in e.OldItems)
            {
                popup.PropertyChanged -=
                    OnPopupToolboxPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (ScreenPopup popup in e.NewItems)
            {
                popup.PropertyChanged +=
                    OnPopupToolboxPropertyChanged;
            }
        }

        RebuildPopupToolboxGroups();
    }

    private void OnPopupToolboxPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScreenPopup.Name) ||
            e.PropertyName == nameof(ScreenPopup.Description) ||
            e.PropertyName == nameof(ScreenPopup.GroupName))
        {
            RebuildPopupToolboxGroups();
        }
    }

    #endregion

    #region === TEMPLATE TOOLBOX BUILD / FILTER ===

    private void RebuildTemplateToolboxGroups()
    {
        string? previousGroupName =
            CurrentTemplateToolboxGroup?.GroupName;

        var allTemplates = Templates
            .OrderBy(
                template => template.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var alphabeticalGroups = allTemplates
            .GroupBy(
                template =>
                    NormalizeTemplateGroupName(template.GroupName),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                group => group.Key,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new TemplateToolboxGroup(
                group.Key,
                group.OrderBy(
                    template => template.Name,
                    StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var allGroup =
            new TemplateToolboxGroup("All", allTemplates);

        ReplaceCollection(
            TemplateToolboxGroups,
            new[] { allGroup }.Concat(alphabeticalGroups));

        CurrentTemplateToolboxGroup =
            !string.IsNullOrWhiteSpace(previousGroupName)
                ? TemplateToolboxGroups.FirstOrDefault(group =>
                    string.Equals(
                        group.GroupName,
                        previousGroupName,
                        StringComparison.OrdinalIgnoreCase))
                : null;

        CurrentTemplateToolboxGroup ??= allGroup;

        RefreshTemplateToolboxItems();
    }

    private void RefreshTemplateToolboxItems()
    {
        IEnumerable<ScreenTemplate> source =
            CurrentTemplateToolboxGroup?.Templates
            ?? Enumerable.Empty<ScreenTemplate>();

        string searchText =
            TemplateToolboxSearchText?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            source = source.Where(template =>
                ContainsSearch(template.Name, searchText) ||
                ContainsSearch(template.Description, searchText) ||
                ContainsSearch(template.GroupName, searchText));
        }

        ReplaceCollection(
            FilteredTemplates,
            source.OrderBy(
                template => template.Name,
                StringComparer.OrdinalIgnoreCase));
    }

    #endregion

    #region === POPUP TOOLBOX BUILD / FILTER ===

    private void RebuildPopupToolboxGroups()
    {
        string? previousGroupName =
            CurrentPopupToolboxGroup?.GroupName;

        var allPopups = CurrentProject?.Popups
            .OrderBy(
                popup => popup.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];

        var alphabeticalGroups = allPopups
            .GroupBy(
                popup =>
                    NormalizePopupGroupName(popup.GroupName),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                group => group.Key,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new PopupToolboxGroup(
                group.Key,
                group.OrderBy(
                    popup => popup.Name,
                    StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var allGroup =
            new PopupToolboxGroup("All", allPopups);

        ReplaceCollection(
            PopupToolboxGroups,
            new[] { allGroup }.Concat(alphabeticalGroups));

        CurrentPopupToolboxGroup =
            !string.IsNullOrWhiteSpace(previousGroupName)
                ? PopupToolboxGroups.FirstOrDefault(group =>
                    string.Equals(
                        group.GroupName,
                        previousGroupName,
                        StringComparison.OrdinalIgnoreCase))
                : null;

        CurrentPopupToolboxGroup ??= allGroup;

        RefreshPopupToolboxItems();
    }

    private void RefreshPopupToolboxItems()
    {
        IEnumerable<ScreenPopup> source =
            CurrentPopupToolboxGroup?.Popups
            ?? Enumerable.Empty<ScreenPopup>();

        string searchText =
            PopupToolboxSearchText?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            source = source.Where(popup =>
                ContainsSearch(popup.Name, searchText) ||
                ContainsSearch(popup.Description, searchText) ||
                ContainsSearch(popup.GroupName, searchText) ||
                ContainsSearch(popup.Title, searchText));
        }

        ReplaceCollection(
            FilteredPopups,
            source.OrderBy(
                popup => popup.Name,
                StringComparer.OrdinalIgnoreCase));
    }

    #endregion

    #region === SHARED HELPERS ===

    private static string NormalizeTemplateGroupName(
        string? groupName)
    {
        return string.IsNullOrWhiteSpace(groupName)
            ? DEFAULT_TEMPLATE_GROUPNAME
            : groupName.Trim();
    }

    private static string NormalizePopupGroupName(
        string? groupName)
    {
        return string.IsNullOrWhiteSpace(groupName)
            ? DEFAULT_POPUP_GROUPNAME
            : groupName.Trim();
    }

    private static bool ContainsSearch(
        string? value,
        string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(
                   searchText,
                   StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
