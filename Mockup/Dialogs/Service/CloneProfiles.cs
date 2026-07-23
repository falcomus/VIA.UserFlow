//======================================================================================
//FILE: Mockup.Editors / CloneProfiles.cs
//MO44 – Editor-sichere Clone-Profile (Domain → Editor Boundary)
//======================================================================================

using System.Collections.ObjectModel;

namespace Mockup.Dialogs;

public static class CloneProfiles
{
    //============================================================
    // Öffentlicher Einstiegspunkt
    //============================================================
    public static T CloneForEditor<T>(T source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return source switch
        {
            Project p => (T)(object)CloneProject(p),
            ScreenTemplate t => (T)(object)CloneTemplate(t),
            ScreenPopup p2 => (T)(object)ClonePopup(p2),

            // ScreenDialog soll Band-Struktur editieren können:
            // => FULL Clone inkl. Bands
            Screen s => (T)(object)CloneScreenFull(s),

            _ => throw new NotSupportedException(
                $"No clone profile defined for type {typeof(T).Name}."
            ),
        };
    }

    //============================================================
    // PROJECT
    // Nur Metadaten – KEINE Screens!
    //============================================================
    private static Project CloneProject(Project p)
    {
        return new Project
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            FilePath = p.FilePath,
            LastOpenedUtc = p.LastOpenedUtc,
            ShowGrid = p.ShowGrid,
            GridSize = p.GridSize,
            IsShared = p.IsShared,
            IsSharedReadonly = p.IsSharedReadonly,
            ColorSchemaKey = p.ColorSchemaKey,
            ActiveColorSchema = p.ActiveColorSchema,
            DeviceWidth = p.DeviceWidth,
            DeviceHeight = p.DeviceHeight,
            ScreenZoomPercent = p.ScreenZoomPercent,
            PreviewZoomPercent = p.PreviewZoomPercent,
            TemplateZoomPercent = p.TemplateZoomPercent,
            PopupZoomPercent = p.PopupZoomPercent,
            ProjectZoomPercent = p.ProjectZoomPercent,

            // WICHTIG:
            // Screens werden im Editor NIE mitgeklont.
            Screens = new ObservableCollection<Screen>(),
        };
    }

    //============================================================
    // SCREEN (FULL Clone) – inkl. Bands/Pages/Controls
    //============================================================

    private static Screen CloneScreenFull(Screen s)
    {
        var clone = new Screen(id: s.Id, name: s.Name, project: s.Project)
        {
            Background = s.Background,
            BackgroundImageFilename = s.BackgroundImageFilename,
            BackgroundImage = s.BackgroundImage,
            BackgroundImageBase64 = s.BackgroundImageBase64,
            GroupName = s.GroupName,
            Descr = s.Descr,
            ShowHeader = s.ShowHeader,
            ShowFooter = s.ShowFooter,
            ShowBackButton = s.ShowBackButton,
            ShowHamburgerButton = s.ShowHamburgerButton,
            IsHomeScreen = s.IsHomeScreen
        };

        clone.Bands.Clear();

        int i = 0;
        foreach (var b in s.Bands)
        {
            var cb = b.DeepClone();
            cb.ParentScreen = clone;
            clone.Bands.Add(cb);
            i++;
        }

        if (s.Project != null)
        {
            clone.Reconstruct(s.Project);
        }

        return clone;
    }

    //============================================================
    // TEMPLATE
    // Reines Template-Metadatenobjekt für Editor
    //============================================================
    private static ScreenTemplate CloneTemplate(ScreenTemplate t)
    {
        var clone = new ScreenTemplate
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            GroupName = t.GroupName,
            Width = t.Width,
            Height = t.Height,
        };

        // KEINE Bands, KEINE Controls
        clone.Bands.Clear();

        return clone;
    }

    //============================================================
    // POPUP
    // Nur Popup-spezifische Metadaten
    //============================================================
    private static ScreenPopup ClonePopup(ScreenPopup p)
    {
        var clone = new ScreenPopup
        {
            Id = p.Id,
            Name = p.Name,
            Title = p.Title,
            Position = p.Position,
            Width = p.Width,
            Height = p.Height,
            Description = p.Description,
            GroupName = p.GroupName,
            HasHeader = p.HasHeader,
            HeaderHeight = p.HeaderHeight,
        };

        // KEINE Controls übernehmen
        clone.Controls.Clear();

        return clone;
    }
}
