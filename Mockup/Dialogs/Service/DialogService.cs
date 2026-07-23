// ======================================================================================
// FILE: Mockup/Editors/DialogService.cs
// MO44 – Editor-Dialog Service
// ======================================================================================

using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using VIA.WPF.Windowing;

namespace Mockup.Dialogs;

public static class DialogService
{
    // ============================================================
    //  Generischer Editor-Dialog
    // ============================================================
    public static bool EditEntity<T>(
        T source,
        Func<T, Window> createDialog,
        string title,
        Action? beforeApply = null)
        where T : class
    {
        return EditEntityCore(
            source,
            createDialog,
            title,
            static dialog => ShowLegacyDialog(dialog),
            beforeApply);
    }

    // ============================================================
    //  Schrittweise VIA.WPF-Migration
    // ============================================================
    internal static bool EditEntity<T>(
        T source,
        Func<T, Window> createDialog,
        string title,
        IXDialogService dialogService,
        Action? beforeApply = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(dialogService);

        return EditEntityCore(
            source,
            createDialog,
            title,
            dialog => dialogService.ShowModal(dialog).IsAccepted,
            beforeApply);
    }

    private static bool EditEntityCore<T>(
        T source,
        Func<T, Window> createDialog,
        string title,
        Func<Window, bool> showDialog,
        Action? beforeApply)
        where T : class
    {
        if (source == null)
            return false;

        // 1) Clone erzeugen (Profile)
        T clone = CloneProfiles.CloneForEditor(source);

        // 2) Dialog erzeugen
        var dlg = createDialog(clone);
        dlg.Title = title;

        if (dlg.DataContext == null)
            dlg.DataContext = clone;

        // 3) Dialog über den für diese Migrationsphase gewählten Weg anzeigen
        if (!showDialog(dlg))
            return false;

        beforeApply?.Invoke();

        // 4) Rückkopieren
        switch (source)
        {
            case Screen target when clone is Screen edited:
                DuplicateScreenFull(edited, target);
                break;

            default:
                CopyPropsSelective(clone, source);
                break;
        }

        return true;
    }

    private static bool ShowLegacyDialog(Window dialog)
    {
        var owner = GetBestOwnerWindow(dialog);
        if (owner != null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }

    //============================================================
    // SCREEN – Für ScreenDialog inkl. Bands
    //
    // WICHTIGER FIX:
    // Bands NICHT "Clear + Add" in die bestehende Collection, weil das
    // pro Add UI-Invalidations/Render triggern kann (LayoutPrepass/Fill),
    // wodurch Band-Höhen während des Kopierens überschrieben werden.
    //
    // Stattdessen: Neue ObservableCollection aufbauen und am Ende 1x zuweisen.
    //============================================================
    private static void DuplicateScreenFull(Screen source, Screen target)
    {
        // 1) Scalars / Meta
        target.Name = source.Name;
        target.Background = source.Background;
        target.BackgroundImageFilename = source.BackgroundImageFilename;
        target.BackgroundImageBase64 = source.BackgroundImageBase64;
        target.BackgroundImage = source.BackgroundImage;

        target.GroupName = source.GroupName;
        target.Descr = source.Descr;

        target.ShowHeader = source.ShowHeader;
        target.ShowFooter = source.ShowFooter;
        target.ShowBackButton = source.ShowBackButton;
        target.ShowHamburgerButton = source.ShowHamburgerButton;
        target.IsHomeScreen = source.IsHomeScreen;

        // 2) Bands: erst komplett in eine neue Collection kopieren (keine Zwischen-Renderzustände!)
        var newBands = new ObservableCollection<Band>();

        int i = 0;
        foreach (var b in source.Bands)
        {
            var cb = b.DeepClone();
            cb.ParentScreen = target;
            newBands.Add(cb);
            i++;
        }

        // 3) Atomar ersetzen (triggert OnBandsChanged nur 1x)
        target.Bands = newBands;

        // 4) Parents / Layout konsistent machen (Reconstruct ruft intern RecalculateBandLayout auf)
        if (target.Project != null)
            target.Reconstruct(target.Project);
        else if (source.Project != null)
            target.Reconstruct(source.Project);
    }

    // ============================================================
    //  Owner-Ermittlung
    // ============================================================
    private static Window? GetBestOwnerWindow(Window dialog)
    {
        if (Application.Current == null)
            return null;

        // 1) Aktives sichtbares Fenster bevorzugen
        var activeWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w =>
                !ReferenceEquals(w, dialog)
                && w.IsVisible
                && w.WindowState != WindowState.Minimized
                && w.IsActive);

        if (activeWindow != null)
            return activeWindow;

        // 2) Sichtbares Hauptfenster als Fallback
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null
            && !ReferenceEquals(mainWindow, dialog)
            && mainWindow.IsVisible
            && mainWindow.WindowState != WindowState.Minimized)
        {
            return mainWindow;
        }

        // 3) Irgendein sichtbares normales Fenster als letzter Fallback
        return Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w =>
                !ReferenceEquals(w, dialog)
                && w.IsVisible
                && w.WindowState != WindowState.Minimized);
    }

    // ============================================================
    //  Generische selektive Property-Kopie
    // ============================================================
    private static void CopyPropsSelective<T>(T source, T target)
    {
        var type = typeof(T);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            if (ShouldSkipProperty(prop))
                continue;

            var value = prop.GetValue(source);
            prop.SetValue(target, value);
        }
    }

    private static bool ShouldSkipProperty(PropertyInfo prop)
    {
        var type = prop.PropertyType;

        // Collections überspringen
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
            return true;

        // Arrays / Listen
        if (type.IsArray || typeof(System.Collections.IList).IsAssignableFrom(type))
            return true;

        // Parent-Referenzen
        if (
            prop.Name.Contains("Parent")
            || prop.Name.Contains("Owner")
            || prop.Name.Contains("Project")
        )
            return true;

        return false;
    }
}







//// ======================================================================================
//// FILE: Mockup/Editors/DialogService.cs
//// MO44 – Editor-Dialog Service
//// ======================================================================================

//using System.Collections.ObjectModel;
//using System.Reflection;
//using System.Windows;

//namespace Mockup.Dialogs;

//public static class DialogService
//{
//    // ============================================================
//    //  Generischer Editor-Dialog
//    // ============================================================
//    public static bool EditEntity<T>(T source, Func<T, Window> createDialog, string title)
//        where T : class
//    {
//        if (source == null)
//            return false;

//        // 1) Clone erzeugen (Profile)
//        T clone = CloneProfiles.CloneForEditor(source);

//        // 2) Dialog erzeugen
//        var dlg = createDialog(clone);
//        dlg.Title = title;

//        if (dlg.DataContext == null)
//            dlg.DataContext = clone;

//        // 3) Dialog anzeigen
//        bool? result = dlg.ShowDialog();
//        if (result != true)
//        {
//            return false;
//        }

//        // 4) Rückkopieren
//        switch (source)
//        {
//            case Screen target when clone is Screen edited:
//                DuplicateScreenFull(edited, target);
//                break;

//            default:
//                CopyPropsSelective(clone, source);
//                break;
//        }

//        return true;
//    }

//    //============================================================
//    // SCREEN – Für ScreenDialog inkl. Bands
//    //
//    // WICHTIGER FIX:
//    // Bands NICHT "Clear + Add" in die bestehende Collection, weil das
//    // pro Add UI-Invalidations/Render triggern kann (LayoutPrepass/Fill),
//    // wodurch Band-Höhen während des Kopierens überschrieben werden.
//    //
//    // Stattdessen: Neue ObservableCollection aufbauen und am Ende 1x zuweisen.
//    //============================================================
//    private static void DuplicateScreenFull(Screen source, Screen target)
//    {
//        // 1) Scalars / Meta
//        target.Name = source.Name;
//        target.Background = source.Background;
//        target.BackgroundImageFilename = source.BackgroundImageFilename;
//        target.BackgroundImageBase64 = source.BackgroundImageBase64;
//        target.GroupName = source.GroupName;
//        target.Descr = source.Descr;

//        target.ShowHeader = source.ShowHeader;
//        target.ShowFooter = source.ShowFooter;
//        target.ShowBackButton = source.ShowBackButton;
//        target.ShowHamburgerButton = source.ShowHamburgerButton;
//        target.IsHomeScreen = source.IsHomeScreen;
//        //target.IsSticky = source.IsSticky;

//        // Height
//        target.UserHeight = source.UserHeight;

//        // 2) Bands: erst komplett in eine neue Collection kopieren (keine Zwischen-Renderzustände!)
//        var newBands = new ObservableCollection<Band>();

//        int i = 0;
//        foreach (var b in source.Bands)
//        {
//            var cb = b.DeepClone();
//            cb.ParentScreen = target;
//            newBands.Add(cb);
//            i++;
//        }

//        // 3) Atomar ersetzen (triggert OnBandsChanged nur 1x)
//        target.Bands = newBands;

//        // 4) Parents / Layout konsistent machen (Reconstruct ruft intern RecalculateBandLayout auf)
//        if (target.Project != null)
//            target.Reconstruct(target.Project);
//        else if (source.Project != null)
//            target.Reconstruct(source.Project);
//    }

//    // ============================================================
//    //  Generische selektive Property-Kopie
//    // ============================================================
//    private static void CopyPropsSelective<T>(T source, T target)
//    {
//        var type = typeof(T);

//        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
//        {
//            if (!prop.CanRead || !prop.CanWrite)
//                continue;

//            if (ShouldSkipProperty(prop))
//                continue;

//            var value = prop.GetValue(source);
//            prop.SetValue(target, value);
//        }
//    }

//    private static bool ShouldSkipProperty(PropertyInfo prop)
//    {
//        var type = prop.PropertyType;

//        // Collections überspringen
//        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
//            return true;

//        // Arrays / Listen
//        if (type.IsArray || typeof(System.Collections.IList).IsAssignableFrom(type))
//            return true;

//        // Parent-Referenzen
//        if (
//            prop.Name.Contains("Parent")
//            || prop.Name.Contains("Owner")
//            || prop.Name.Contains("Project")
//        )
//            return true;

//        return false;
//    }
//}
