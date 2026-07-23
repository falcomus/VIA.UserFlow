using System.Linq;
using System.Windows;
using VIA.WPF.Services;

namespace Mockup.Services;

/// <summary>
/// Central VIA.WPF-backed message box facade for modal application decisions.
/// </summary>
public static class XDialogs
{
    public static MessageBoxResult Show(
        string message,
        string caption,
        MessageBoxButton button,
        MessageBoxImage image = MessageBoxImage.None)
    {
        var service = new XMessageBoxService
        {
            Owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsActive)
        };

        return service.Show(message, caption, button, image);
    }
}
