using System.Windows;
using System.Windows.Media;

namespace Mockup.Dialogs;

public partial class XColorPickerDialog : ModalDialogWindow
{
    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register(
            nameof(SelectedColor),
            typeof(Color),
            typeof(XColorPickerDialog),
            new FrameworkPropertyMetadata(
                Colors.Transparent,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public XColorPickerDialog()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += XColorPickerDialog_Loaded;
    }

    private void XColorPickerDialog_Loaded(object sender, RoutedEventArgs e)
    {
        PART_ColorPicker.SelectedColor = SelectedColor;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        SelectedColor = PART_ColorPicker.SelectedColor;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
