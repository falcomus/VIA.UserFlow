using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Mockup.UIControls;

[ContentProperty(nameof(Content))]
public class XPanel : ContentControl
{
    static XPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(XPanel), new FrameworkPropertyMetadata(typeof(XPanel)));
    }
}