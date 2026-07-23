using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mockup.Behaviour;

public static class MouseWheelBehavior
{
    public static readonly DependencyProperty ForwardToScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "ForwardToScrollViewer",
            typeof(ScrollViewer),
            typeof(MouseWheelBehavior),
            new PropertyMetadata(null, OnForwardToScrollViewerChanged));

    public static void SetForwardToScrollViewer(UIElement element, ScrollViewer value)
        => element.SetValue(ForwardToScrollViewerProperty, value);

    public static ScrollViewer GetForwardToScrollViewer(UIElement element)
        => (ScrollViewer)element.GetValue(ForwardToScrollViewerProperty);

    private static void OnForwardToScrollViewerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.PreviewMouseWheel -= OnPreviewMouseWheel;
            if (e.NewValue is ScrollViewer scrollViewer)
            {
                element.PreviewMouseWheel += OnPreviewMouseWheel;
            }
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is UIElement element)
        {
            var scrollViewer = GetForwardToScrollViewer(element);
            if (scrollViewer != null)
            {
                // Mausrad-Bewegung an den äußeren ScrollViewer weitergeben
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}