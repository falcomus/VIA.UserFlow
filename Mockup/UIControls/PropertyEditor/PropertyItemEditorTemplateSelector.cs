using System.Windows;
using System.Windows.Controls;

namespace Mockup.UIControls;

public sealed class PropertyItemEditorTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not PropertyItemTemp propertyItem)
            return base.SelectTemplate(item, container);

        if (container is not FrameworkElement fe)
            return base.SelectTemplate(item, container);

        return propertyItem.EditorKind switch
        {
            PropertyEditorKind.Text => fe.TryFindResource("TextEditorTemplate") as DataTemplate,
            PropertyEditorKind.Numeric => fe.TryFindResource("NumericEditorTemplate") as DataTemplate,
            PropertyEditorKind.Bool => fe.TryFindResource("BoolEditorTemplate") as DataTemplate,
            PropertyEditorKind.Enum => fe.TryFindResource("EnumEditorTemplate") as DataTemplate,
            PropertyEditorKind.ImageRef => fe.TryFindResource("ImageRefEditorTemplate") as DataTemplate,
            PropertyEditorKind.Color => fe.TryFindResource("ColorEditorTemplate") as DataTemplate,
            _ => fe.TryFindResource("ReadOnlyEditorTemplate") as DataTemplate,
        };
    }
}
