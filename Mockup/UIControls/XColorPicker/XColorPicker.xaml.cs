using Mockup.Helper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.UIControls
{
    public partial class XColorPicker : UserControl
    {
        // Einzige öffentliche DependencyProperty – kann von aussen gebunden werden
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color),
                typeof(XColorPicker),
                new FrameworkPropertyMetadata(
                    Colors.Transparent,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedColorChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (XColorPicker)d;
            var vm = picker.DataContext as XColorPickerViewModel;
            if (vm != null && (Color)e.NewValue != vm.SelectedColor)
            {
                vm.SelectedColor = (Color)e.NewValue;
            }
        }

        public XColorPicker()
        {
            InitializeComponent();

            if (DesignModeHelper.IsInDesignMode)
                return;

            DataContext = new XColorPickerViewModel();

            // ViewModel-Änderungen zurück in die DP propagieren
            ((XColorPickerViewModel)DataContext).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(XColorPickerViewModel.SelectedColor))
                {
                    var vm = (XColorPickerViewModel)s;
                    if (vm.SelectedColor != SelectedColor)
                        // Keep a host's TwoWay binding intact. SetValue replaces that
                        // binding, which meant dialogs could no longer receive the
                        // selected colour after the first picker interaction.
                        SetCurrentValue(SelectedColorProperty, vm.SelectedColor);
                }
            };
        }

        // Größenänderungen an das ViewModel weitergeben
        private void SvField_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            if (DataContext is XColorPickerViewModel vm)
            {
                vm.SvFieldWidth = fe.ActualWidth;
                vm.SvFieldHeight = fe.ActualHeight;
            }
        }

        private void HueBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            if (DataContext is XColorPickerViewModel vm)
                vm.HueBarHeight = fe.ActualHeight;
        }

        // Maus-Ereignisse für SV-Feld
        private void SvField_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            fe.CaptureMouse();
            var vm = DataContext as XColorPickerViewModel;
            if (vm != null)
            {
                var pos = e.GetPosition(fe);
                vm.SetSvFromPoint(pos, fe.ActualWidth, fe.ActualHeight);
            }
        }

        private void SvField_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var fe = (FrameworkElement)sender;
                var vm = DataContext as XColorPickerViewModel;
                if (vm != null)
                {
                    var pos = e.GetPosition(fe);
                    vm.SetSvFromPoint(pos, fe.ActualWidth, fe.ActualHeight);
                }
            }
        }

        private void SvField_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).ReleaseMouseCapture();
        }

        // Maus-Ereignisse für Hue-Bar
        private void HueBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            fe.CaptureMouse();
            var vm = DataContext as XColorPickerViewModel;
            if (vm != null)
            {
                var pos = e.GetPosition(fe);
                vm.SetHueFromPoint(pos, fe.ActualHeight);
            }
        }

        private void HueBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var fe = (FrameworkElement)sender;
                var vm = DataContext as XColorPickerViewModel;
                if (vm != null)
                {
                    var pos = e.GetPosition(fe);
                    vm.SetHueFromPoint(pos, fe.ActualHeight);
                }
            }
        }

        private void HueBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).ReleaseMouseCapture();
        }

    }
}
