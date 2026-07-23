using System.Windows;

namespace Mockup.Dialogs;

/// <summary>
/// Editor für Template-Metadaten (arbeitet auf Clone).
/// </summary>
public partial class TemplateDialog : ModalDialogWindow
{
    public TemplateDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => InputName.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (MockupService.Mockup.CurrentTemplate is null)
            return;

        MockupService.Mockup.EnsureTemplateGroupExists(MockupService.Mockup.CurrentTemplate.GroupName);
    }
}
