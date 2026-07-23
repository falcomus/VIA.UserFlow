// ======================================================================================
// DATEI: Mockup.ViewModel/MockupViewModel.Commands.cs
// ======================================================================================
// Diese Datei enthält alle RelayCommand-Methoden für Project, Screen, Template & Popup
// Sie ist als partielle Klasse organisiert.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mockup.ColorSystem;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MessageBox = Mockup.Services.XDialogs;
using VisualTreeHelper = Mockup.Helper.VisualTreeHelper;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    #region === DESIGNER HOOK ===

    private bool _designerRequestsHooked;

    /// <summary>
    /// Richtet die Nachrichtenempfänger für Designer-Anfragen ein (z. B. MoveBandMessage).
    /// Wird beim Start der Anwendung aufgerufen.
    /// </summary>
    public void HookDesignerRequests()
    {
        if (_designerRequestsHooked)
            return;

        _designerRequestsHooked = true;

        WeakReferenceMessenger.Default.Unregister<MoveBandMessage>(this);

        WeakReferenceMessenger.Default.Register<MoveBandMessage>(
            this,
            (_, msg) => HandleMoveBand(msg));
    }

    #endregion

    #region === UI AKTUALISIERUNG ===

    /// <summary>
    /// Aktualisiert die UI-Eigenschaften, die das aktuelle Projekt und den aktuellen Bildschirm betreffen.
    /// </summary>
    private void RefreshProjectUiInfo()
    {
        OnPropertyChanged(nameof(CurrentProject));
        OnPropertyChanged(nameof(CurrentScreen));
        OnPropertyChanged(nameof(ScreenCountInfo));
        OnPropertyChanged(nameof(TemplateCountInfo));
        OnPropertyChanged(nameof(PopupCountInfo));
        OnPropertyChanged(nameof(ControlCountInfo));
    }

    #endregion

    #region === COLOR SCHEME COMMANDS ===

    /// <summary>
    /// Erstellt ein neues Farbschema und weist es dem aktuellen Projekt zu.
    /// </summary>
    [RelayCommand]
    private void NewColorSchema()
    {
        if (CurrentProject == null)
            return;

        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter schema name:",
            "New Color Scheme",
            "New Scheme");

        if (string.IsNullOrWhiteSpace(name))
            return;

        // CreateSchema() legt bereits an + speichert
        var schema = ColorSchemaCatalog.CreateSchema(name);

        CurrentProject.ColorSchemaKey = schema.Key;

        ThemeService.SetSchema(schema.Key);
        CurrentProject.ActiveColorSchema = ThemeService.Current.Clone();

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    /// <summary>
    /// Kopiert das aktuelle Farbschema und weist es dem Projekt zu.
    /// </summary>
    [RelayCommand]
    private void CopyColorSchema()
    {
        if (CurrentProject == null)
            return;

        var source = ColorSchemaCatalog.GetSchema(CurrentProject.ColorSchemaKey);
        if (source == null)
            return;

        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter name for the copied color scheme:",
            "Copy Color Scheme",
            $"{source.DisplayName} Copy");

        if (string.IsNullOrWhiteSpace(name))
            return;

        var clone = source.Clone();
        clone.DisplayName = name;
        clone.Key = ColorSchemaCatalog.GenerateUniqueKey(name);

        ColorSchemaCatalog.AddSchema(clone);

        CurrentProject.ColorSchemaKey = clone.Key;

        ThemeService.SetSchema(clone.Key);
        CurrentProject.ActiveColorSchema = ThemeService.Current.Clone();

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    /// <summary>
    /// Öffnet den Editor zum Bearbeiten des aktuellen Farbschemas.
    /// </summary>
    [RelayCommand]
    private void EditColorSchema()
    {
        if (CurrentProject == null)
            return;

        var schema = ColorSchemaCatalog.GetSchema(CurrentProject.ColorSchemaKey);
        if (schema == null)
            return;

        MSG.UI.ShowOverlay(true);

        try
        {
            // Edit-Copy, Commit erst bei OK
            var editable = schema.Clone();
            var editor = new ColorSchemaEditor(editable);

            if (editor.ShowDialog() != true)
                return;

            // Persist
            ColorSchemaCatalog.UpdateSchema(editable);

            // Projekt zeigt auf dieses Schema
            CurrentProject.ColorSchemaKey = editable.Key;

            // Apply
            ThemeService.SetSchema(editable.Key);
            CurrentProject.ActiveColorSchema = ThemeService.Current.Clone();

            SaveCurrentProject();
            MSG.UI.InvalidateDesigner();
        }
        finally
        {
            MSG.UI.ShowOverlay(false);
        }
    }

    /// <summary>
    /// Löscht das aktuelle Farbschema (außer "Default").
    /// </summary>
    [RelayCommand]
    private void DeleteColorSchema()
    {
        if (CurrentProject == null)
            return;

        var key = CurrentProject.ColorSchemaKey;

        if (string.IsNullOrWhiteSpace(key) || key == "Default")
        {
            XNotifications.Info("Default Scheme cannot be deleted!");
            return;
        }

        MSG.UI.ShowOverlay(true);

        try
        {
            var schema = ColorSchemaCatalog.GetSchema(key);
            if (schema == null)
                return;

            var result = MessageBox.Show(
                $"Delete scheme '{schema.DisplayName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            if (!ColorSchemaCatalog.RemoveSchema(key))
                return;

            // Fallback auf Default
            CurrentProject.ColorSchemaKey = "Default";

            ThemeService.SetSchema("Default");
            CurrentProject.ActiveColorSchema = ThemeService.Current.Clone();

            SaveCurrentProject();
            MSG.UI.InvalidateDesigner();
        }
        finally
        {
            MSG.UI.ShowOverlay(false);
        }
    }

    #endregion

    #region === EXPANDER / LISTEN COMMANDS ===

    private bool skipEvent;

    /// <summary>
    /// Schließt alle Steuerelement-Gruppen und wählt das erste Element der aktuellen Gruppe aus.
    /// </summary>
    [RelayCommand]
    private void CollapseAllControlGroups(Expander expander)
    {
        if (skipEvent) return;
        CollapseAllGroupsAndSelectFirst<ControlDescriptor>(expander, first => CurrentControlDescriptor = first);
    }

    /// <summary>
    /// Schließt alle Bildschirm-Gruppen und wählt das erste Element der aktuellen Gruppe aus.
    /// </summary>
    [RelayCommand]
    private void CollapseAllScreenGroups(Expander expander)
    {
        if (skipEvent) return;
        CollapseAllGroupsAndSelectFirst<Screen>(expander, first => CurrentScreen = first);
    }

    /// <summary>
    /// Schließt alle Template-Gruppen und wählt das erste Element der aktuellen Gruppe aus.
    /// </summary>
    [RelayCommand]
    private void CollapseAllTemplateGroups(Expander expander)
    {
        if (skipEvent) return;
        CollapseAllGroupsAndSelectFirst<ScreenTemplate>(expander, first => CurrentTemplate = first);
    }

    /// <summary>
    /// Schließt alle Popup-Gruppen und wählt das erste Element der aktuellen Gruppe aus.
    /// </summary>
    [RelayCommand]
    private void CollapseAllPopupGroups(Expander expander)
    {
        if (skipEvent) return;
        CollapseAllGroupsAndSelectFirst<ScreenPopup>(expander, first => CurrentPopup = first);
    }

    /// <summary>
    /// Hilfsmethode: Schließt alle Gruppen in der ListBox, öffnet die Gruppe des übergebenen Expanders
    /// und wählt das erste darin enthaltene Element aus.
    /// </summary>
    /// <typeparam name="T">Typ der Elemente in der Gruppe.</typeparam>
    /// <param name="expander">Der Expander, dessen Gruppe geöffnet bleiben soll.</param>
    /// <param name="setCurrent">Aktion zum Setzen des aktuellen Elements im ViewModel.</param>
    private void CollapseAllGroupsAndSelectFirst<T>(Expander expander, Action<T> setCurrent)
    {
        if (skipEvent || expander == null)
            return;

        var listBox = VisualTreeHelper.FindAncestor<ListBox>(expander);
        if (listBox == null)
            return;

        // Alle Gruppen schließen
        var groupItems = VisualTreeHelper.FindVisualChildren<GroupItem>(listBox).ToList();
        foreach (var gi in groupItems)
        {
            var grpExpander = VisualTreeHelper.FindVisualChild<Expander>(gi);
            if (grpExpander != null)
                grpExpander.IsExpanded = false;
        }

        // Aktuelle Gruppe öffnen
        skipEvent = true;
        expander.IsExpanded = true;
        skipEvent = false;

        // Erstes Item der aktuellen Gruppe selektieren (nach Layout!)
        if (expander.DataContext is not CollectionViewGroup cvg || cvg.ItemCount == 0)
            return;

        if (cvg.Items[0] is not T first)
            return;

        setCurrent(first);

        listBox.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (listBox.ItemsSource is ICollectionView cv)
                cv.MoveCurrentTo(first);

            listBox.SelectedItem = first;
            listBox.ScrollIntoView(first);

            if (listBox.ItemContainerGenerator.ContainerFromItem(first) is ListBoxItem lbi)
                lbi.BringIntoView();
        }),
        System.Windows.Threading.DispatcherPriority.Loaded);
    }

    #endregion
}
