using System.Windows;

namespace Mockup.Dialogs;

/// <summary>
/// Editor für Popup-Metadaten (arbeitet auf Clone).
/// </summary>
public partial class PopupDialog : ModalDialogWindow
{
    public PopupDialog()
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
}
