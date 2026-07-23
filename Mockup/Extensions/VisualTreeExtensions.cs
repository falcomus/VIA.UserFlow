using System.Windows;
using System.Windows.Media;

namespace Mockup.Extensions;

public static class VisualTreeExtensions
{
    public static T FindAncestor<T>(this DependencyObject child) where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);
        while (parent != null && !(parent is T))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as T ?? null!;
    }

    public static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null && !(parent is T))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as T;
    }

    public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

}