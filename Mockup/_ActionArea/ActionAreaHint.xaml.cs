// ======================================================================================
// FILE: Mockup/_ActionArea/ActionAreaHint.xaml.cs
// ======================================================================================

using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Mockup._ActionArea;

public partial class ActionAreaHint : UserControl
{
    public ActionAreaHint()
    {
        InitializeComponent();
    }

    #region === DP: ItemsSource ===

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(ActionAreaHint),
            new PropertyMetadata(null));

    #endregion

    #region === DP: Title (optional) ===

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ActionAreaHint),
            new PropertyMetadata("ACTIONS"));

    public Visibility TitleVisibility
        => string.IsNullOrWhiteSpace(Title) ? Visibility.Collapsed : Visibility.Visible;

    #endregion
}