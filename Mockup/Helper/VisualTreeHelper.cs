using System.Windows;

namespace Mockup.Helper;

public static class VisualTreeHelper
{
    public static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t)
                return t;

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            yield break;

        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is T t)
                yield return t;

            foreach (var sub in FindVisualChildren<T>(child))
                yield return sub;
        }
    }

    public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is T t)
                return t;

            var found = FindVisualChild<T>(child);
            if (found != null)
                return found;
        }
        return null;
    }

}
