using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mockup.Navigation;

public partial class BreadcrumbBar : UserControl
{
    public BreadcrumbBar() => InitializeComponent();

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(BreadcrumbBar));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty NavigateToIndexCommandProperty =
        DependencyProperty.Register(nameof(NavigateToIndexCommand), typeof(ICommand), typeof(BreadcrumbBar));

    public ICommand? NavigateToIndexCommand
    {
        get => (ICommand?)GetValue(NavigateToIndexCommandProperty);
        set => SetValue(NavigateToIndexCommandProperty, value);
    }

    private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        // 🔹 Wenn das Event bereits von einem Parent behandelt wurde → zurücksetzen
        if (e.Handled)
            e.Handled = false;

        // 🔹 Jetzt Fokus aktiv auf BreadcrumbBar setzen
        if (!IsKeyboardFocusWithin)
        {
            Focus();
            Keyboard.Focus(this);
        }
    }
}
