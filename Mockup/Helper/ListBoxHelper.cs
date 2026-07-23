using Mockup.Extensions;
using System.Windows.Controls;


namespace Mockup.Helper;

public static class ListBoxHelper
{
    public static void DeselectAll(ListBox? listBox)
    {
        if (listBox == null) return;

        // Durchsuche alle Expander und deselectiere andere ListBoxen
        var itemsControl = VisualTreeExtensions.FindParent<ItemsControl>(listBox);
        if (itemsControl != null)
        {
            foreach (var group in itemsControl.Items)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(group);
                if (container != null)
                {
                    var lb = VisualTreeExtensions.FindVisualChild<ListBox>(container);
                    if (lb != null && lb != listBox)
                    {
                        lb.SelectedItem = null;
                    }
                }
            }
        }
    }

}