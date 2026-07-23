using System.Linq;
using System.Windows;
using System.Windows.Threading;
using VIA.WPF.Controls;
using VIA.WPF.Windowing;

namespace Mockup.Services;

/// <summary>
/// Lightweight application notifications backed by the active VIA.WPF <see cref="XWindow"/>.
/// Central notification seam backed by the active VIA.WPF window.
/// </summary>
public static class XNotifications
{
    public static void Info(string message) => Show(message, XControlVariant.Info, "Information");

    public static void Success(string message) => Show(message, XControlVariant.Success, "Completed");

    public static void Warning(string message) => Show(message, XControlVariant.Warning, "Attention");

    public static void Error(string message) => Show(message, XControlVariant.Danger, "Error");

    private static void Show(string message, XControlVariant variant, string title)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        _ = dispatcher.InvokeAsync(() =>
        {
            var window = Application.Current.Windows
                .OfType<XWindow>()
                .FirstOrDefault(candidate => candidate.IsActive)
                ?? Application.Current.Windows.OfType<XWindow>().FirstOrDefault();

            if (window is null)
            {
                return;
            }

            var toast = new XInfoBar
            {
                Title = title,
                Message = message,
                Variant = variant,
                IsClosable = true
            };
            window.ToastContent = toast;
            window.IsToastOpen = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (ReferenceEquals(window.ToastContent, toast))
                {
                    window.IsToastOpen = false;
                }
            };
            timer.Start();
        });
    }
}
