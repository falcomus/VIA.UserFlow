// ============================================================================
// FILE: Mockup.AssetSystem/ImageRefEditor.cs
// PURPOSE:
//   Legacy compatibility helper for ImageRef selection.
// ============================================================================

using Mockup.Domain.Registry;
using System.Windows;

namespace Mockup.AssetSystem;

public static class ImageRefEditor
{
    public static ImageRef? PickImageRef(Window? owner, ImageRef? current)
    {
        var dlg = new ImageRefDialog(current)
        {
            Owner = owner ?? Application.Current?.MainWindow
        };

        return dlg.ShowDialog() == true
            ? dlg.SelectedImageRef
            : current;
    }
}
