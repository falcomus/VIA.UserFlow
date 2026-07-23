using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.UIControls;

public class DropDown : Control
{
    private ToggleButton? _toggleButton;
    private Popup? _popup;
    private Border? _clickArea;

    public DropDown()
    {
        Width = 42;
    }

    static DropDown()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DropDown),
            new FrameworkPropertyMetadata(typeof(DropDown)));
    }

    #region DropDownContent
    public object DropDownContent
    {
        get => GetValue(DropDownContentProperty);
        set => SetValue(DropDownContentProperty, value);
    }

    public static readonly DependencyProperty DropDownContentProperty =
        DependencyProperty.Register(
            nameof(DropDownContent),
            typeof(object),
            typeof(DropDown),
            new PropertyMetadata(null));
    #endregion

    #region LeftContent
    public object LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public static readonly DependencyProperty LeftContentProperty =
        DependencyProperty.Register(
            nameof(LeftContent),
            typeof(object),
            typeof(DropDown),
            new PropertyMetadata(null));
    #endregion

    #region ArrowColor
    public Brush ArrowColor
    {
        get => (Brush)GetValue(ArrowColorProperty);
        set => SetValue(ArrowColorProperty, value);
    }

    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(
            nameof(ArrowColor),
            typeof(Brush),
            typeof(DropDown),
            new PropertyMetadata(Brushes.White));
    #endregion

    #region IsDropDownOpen
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(DropDown),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    #endregion

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _toggleButton = GetTemplateChild("PART_Toggle") as ToggleButton;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _clickArea = GetTemplateChild("PART_ClickArea") as Border;

        if (_toggleButton != null)
        {
            _toggleButton.Checked += OnToggleButtonChecked;
            _toggleButton.Unchecked += OnToggleButtonUnchecked;
        }

        if (_clickArea != null)
        {
            _clickArea.MouseLeftButtonUp += OnClickAreaMouseLeftButtonUp;
        }

        if (_popup != null)
        {
            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
        }
    }

    private void OnToggleButtonChecked(object sender, RoutedEventArgs e)
    {
        IsDropDownOpen = true;
        OpenPopup();
    }

    private void OnToggleButtonUnchecked(object sender, RoutedEventArgs e)
    {
        IsDropDownOpen = false;
        ClosePopup();
    }

    private void OnClickAreaMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_toggleButton != null && !_toggleButton.IsMouseOver)
        {
            TogglePopup();
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (_toggleButton != null)
        {
            _toggleButton.IsChecked = true;
        }
        IsDropDownOpen = true;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        if (_toggleButton != null)
        {
            _toggleButton.IsChecked = false;
        }
        IsDropDownOpen = false;
    }

    private void OpenPopup()
    {
        if (_popup != null)
        {
            _popup.IsOpen = true;
        }
    }

    private void ClosePopup()
    {
        if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }

    private void TogglePopup()
    {
        if (_popup != null)
        {
            _popup.IsOpen = !_popup.IsOpen;
        }
    }

    public void CloseDropDown()
    {
        ClosePopup();
    }
}
