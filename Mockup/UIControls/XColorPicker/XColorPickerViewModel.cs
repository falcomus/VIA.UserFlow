using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.UIControls
{
    public partial class XColorPickerViewModel : ObservableObject
    {
        // ---------------------------
        // Public bindables
        // ---------------------------

        [ObservableProperty]
        private Color _selectedColor = Colors.White;

        [ObservableProperty]
        private Brush _previewForegroundBrush = Brushes.White;

        [ObservableProperty] private byte _r;
        [ObservableProperty] private byte _g;
        [ObservableProperty] private byte _b;
        [ObservableProperty] private byte _a = 255;

        // Always #AARRGGBB
        [ObservableProperty]
        private string _hex = "#FFFFFFFF";

        // HSV (0..1)
        [ObservableProperty] private double _h;
        [ObservableProperty] private double _s;
        [ObservableProperty] private double _v;

        // sizes (set by view)
        [ObservableProperty] private double _svFieldWidth = 100;
        [ObservableProperty] private double _svFieldHeight = 100;
        [ObservableProperty] private double _hueBarHeight = 200;

        // selector positions
        [ObservableProperty] private double _svSelectorX;
        [ObservableProperty] private double _svSelectorY;
        [ObservableProperty] private double _hueSelectorX;
        [ObservableProperty] private double _hueSelectorY;

        // brushes
        [ObservableProperty] private Brush _pickerSvBrush = Brushes.Red;
        [ObservableProperty] private Brush _selectedBrush = Brushes.White;

        // palettes
        [ObservableProperty]
        private ObservableCollection<SolidColorBrush> _defaultColors = new();

        // RECENTS ARE HEX STRINGS (persistable)
        [ObservableProperty]
        private ObservableCollection<string> _recentColors = new();

        public string RgbText => $"RGB {R}, {G}, {B}";
        public string OpacityText => $"Opacity {A / 255d:P0}";

        public ICommand PickSwatchCommand { get; }
        public ICommand PickRecentCommand { get; }

        // ---------------------------
        // Internal flags
        // ---------------------------

        private bool _isUpdatingFromHsv;
        private bool _isUpdatingFromRgb;
        private bool _isUpdatingFromHex;

        private const int RECENT_COUNT = 20;
        private const string RECENT_EMPTY = "#00000000";


        public XColorPickerViewModel()
        {
            InitDefaultColors();

            // bind to the *viewmodel* collection that gets saved/loaded
            var appVm = MockupService.Mockup;

            if (appVm != null)
                RecentColors = appVm.RecentColors;   // VM-Property
            else
                RecentColors = new ObservableCollection<string>();

            PickSwatchCommand = new RelayCommand<SolidColorBrush>(brush =>
            {
                if (brush == null) return;
                SelectedColor = brush.Color;
            });

            PickRecentCommand = new RelayCommand<string>(hex =>
            {
                if (TryParseHex(hex, out var c))
                {
                    SelectedColor = c;
                }
            });

            // start color
            SelectedColor = Color.FromRgb(0x0B, 0x6E, 0xA8);
        }




        private void InitDefaultColors()
        {
            DefaultColors.Clear();

            // ============================================================
            // Row 1 (20): Grays / neutrals
            // ============================================================
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)));

            // ============================================================
            // Row 2 (20): Warm colors (heller / kräftig)
            // ============================================================
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA6, 0x1E, 0x2D)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC0, 0x24, 0x34)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD9, 0x32, 0x43)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xEB, 0x4D, 0x5E)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF5, 0x72, 0x7F)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB2, 0x4A, 0x12)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xCC, 0x58, 0x15)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE5, 0x6B, 0x1B)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF7, 0x85, 0x2E)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xFF, 0xA0, 0x4D)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB3, 0x67, 0x00)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xCC, 0x79, 0x00)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE2, 0x8F, 0x05)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF5, 0xA8, 0x1A)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0x3D)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x9A, 0x86, 0x00)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB5, 0x9E, 0x00)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xCF, 0xB7, 0x08)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE6, 0xD0, 0x22)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF4, 0xE3, 0x55)));

            // ============================================================
            // Row 3 (20): Cool colors (green / teal / cyan / blue)
            // ============================================================
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x16, 0x7A, 0x3F)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0x93, 0x4D)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x20, 0xAD, 0x5C)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x34, 0xC7, 0x72)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x58, 0xD9, 0x8D)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x10, 0x78, 0x72)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x14, 0x92, 0x8A)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0xAD, 0xA3)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2A, 0xC7, 0xBA)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4C, 0xDA, 0xCD)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0F, 0x74, 0x99)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x14, 0x8B, 0xB3)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1C, 0xA5, 0xCC)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2B, 0xBE, 0xE5)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x52, 0xD2, 0xF2)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1C, 0x57, 0xB8)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x24, 0x6C, 0xD1)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2F, 0x82, 0xE8)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x49, 0x99, 0xF5)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x72, 0xB2, 0xFA)));

            // ============================================================
            // Row 4 (20): Accent colors (indigo / purple / pink / brown)
            // ============================================================
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x3A, 0x31, 0xA8)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4A, 0x40, 0xC0)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x5E, 0x55, 0xD9)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x77, 0x70, 0xEB)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x98, 0x92, 0xF5)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x6F, 0x2D, 0xA8)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x84, 0x36, 0xC0)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x9A, 0x45, 0xD9)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB0, 0x61, 0xEB)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC6, 0x86, 0xF5)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA1, 0x23, 0x67)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xBB, 0x2C, 0x7B)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD4, 0x39, 0x90)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0xA8)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF4, 0x81, 0xBF)));

            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x73, 0x52, 0x32)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x8A, 0x63, 0x3D)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA3, 0x76, 0x4B)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xBD, 0x8D, 0x60)));
            DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD6, 0xA8, 0x7E)));
        }



        //private void InitDefaultColors()
        //{
        //    DefaultColors.Clear();

        //    // ============================================================
        //    // Row 1 (20): Grays / neutrals
        //    // ============================================================
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x36, 0x36, 0x36)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x52)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x7C, 0x7C, 0x7C)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0x98)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA6, 0xA6, 0xA6)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB4, 0xB4, 0xB4)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC2, 0xC2, 0xC2)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xDE, 0xDE, 0xDE)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xEA, 0xEA, 0xEA)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)));

        //    // ============================================================
        //    // Row 2 (20): Warm colors (red / orange / yellow)
        //    // ============================================================
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x7A, 0x1E, 0x1E))); // deep red
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x9B, 0x22, 0x1F)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB8, 0x2E, 0x24)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD1, 0x3C, 0x2F)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x43)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x8A, 0x3D, 0x14))); // burnt orange
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA8, 0x4B, 0x16)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC9, 0x5F, 0x1A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE6, 0x78, 0x1F)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF2, 0x93, 0x3A)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x8A, 0x67, 0x14))); // amber
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA8, 0x7C, 0x16)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC4, 0x92, 0x1B)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xDB, 0xA8, 0x24)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF0, 0xBE, 0x38)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x7A, 0x72, 0x1A))); // yellow/olive
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x99, 0x92, 0x1F)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB8, 0xB1, 0x25)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD4, 0xCE, 0x3A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xEA, 0xE6, 0x62)));

        //    // ============================================================
        //    // Row 3 (20): Cool colors (green / teal / cyan / blue)
        //    // ============================================================
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1E, 0x6A, 0x3A))); // green
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x22, 0x82, 0x45)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x28, 0x9B, 0x52)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x34, 0xB5, 0x63)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x55, 0xC8, 0x7E)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0E, 0x63, 0x58))); // teal
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x11, 0x7B, 0x6D)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x16, 0x95, 0x84)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x21, 0xAF, 0x9B)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x46, 0xC4, 0xB1)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0E, 0x5F, 0x7A))); // cyan
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x12, 0x78, 0x99)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x17, 0x92, 0xB8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x20, 0xAB, 0xD4)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4A, 0xC2, 0xE8)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x13, 0x4E, 0x9B))); // blue
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0x63, 0xBD)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x24, 0x78, 0xDA)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x3A, 0x91, 0xEB)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x66, 0xAE, 0xF3)));

        //    // ============================================================
        //    // Row 4 (20): Accent colors (indigo / purple / pink / brown)
        //    // ============================================================
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2E, 0x3A, 0x8C))); // indigo
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x3A, 0x49, 0xA8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4A, 0x5E, 0xC7)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x62, 0x79, 0xDE)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x84, 0x97, 0xEC)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x5A, 0x2D, 0x91))); // purple
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x6F, 0x36, 0xAF)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x85, 0x43, 0xC9)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x9C, 0x59, 0xDC)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB5, 0x7A, 0xE8)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x8E, 0x2C, 0x5E))); // pink / rose
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA8, 0x34, 0x6F)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC1, 0x3F, 0x83)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD8, 0x56, 0x9B)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE8, 0x7C, 0xB5)));

        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x5A, 0x43, 0x2A))); // brown / sand
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x73, 0x54, 0x33)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x8C, 0x67, 0x3D)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA5, 0x7A, 0x4A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xC0, 0x96, 0x68)));
        //}


        //private void InitDefaultColors()
        //{
        //    // 10 grays
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)));

        //    // 10 base
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xD9, 0x3A, 0x2F)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF0, 0x8A, 0x24)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xF2, 0xC3, 0x2D)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x3B, 0xB2, 0x73)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1F, 0xB6, 0xC9)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1D, 0x74, 0xD8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x3F, 0x51, 0xB5)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x7B, 0x3F, 0xB6)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xB3, 0x3B, 0x8A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xE0, 0x4C, 0x8B)));

        //    // 10 accent
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0B, 0x6E, 0xA8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x12, 0x7C, 0xB5)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0x8F, 0xC8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2A, 0xA4, 0xD6)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4E, 0xB8, 0xE3)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x76, 0xCC, 0xEF)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA0, 0xDE, 0xF7)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x5A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x17, 0x4E, 0x71)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1F, 0x5F, 0x88)));


        //    // 10 accent
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0B, 0x6E, 0xA8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x12, 0x7C, 0xB5)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0x8F, 0xC8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2A, 0xA4, 0xD6)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4E, 0xB8, 0xE3)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x76, 0xCC, 0xEF)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA0, 0xDE, 0xF7)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x5A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x17, 0x4E, 0x71)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1F, 0x5F, 0x88)));

        //    // 10 accent
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0B, 0x6E, 0xA8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x12, 0x7C, 0xB5)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0x8F, 0xC8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2A, 0xA4, 0xD6)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4E, 0xB8, 0xE3)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x76, 0xCC, 0xEF)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA0, 0xDE, 0xF7)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x5A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x17, 0x4E, 0x71)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1F, 0x5F, 0x88)));

        //    // 10 accent
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0B, 0x6E, 0xA8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x12, 0x7C, 0xB5)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1A, 0x8F, 0xC8)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x2A, 0xA4, 0xD6)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x4E, 0xB8, 0xE3)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x76, 0xCC, 0xEF)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0xA0, 0xDE, 0xF7)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x5A)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x17, 0x4E, 0x71)));
        //    DefaultColors.Add(new SolidColorBrush(Color.FromRgb(0x1F, 0x5F, 0x88)));
        //}

        // ---------------------------
        // SelectedColor pipeline
        // ---------------------------

        partial void OnSelectedColorChanged(Color value)
        {
            _isUpdatingFromRgb = true;
            R = value.R;
            G = value.G;
            B = value.B;
            A = value.A;
            _isUpdatingFromRgb = false;

            _isUpdatingFromHex = true;
            Hex = ToHex(value); // #AARRGGBB
            _isUpdatingFromHex = false;

            ToHsv(value, out var h, out var s, out var v);
            _isUpdatingFromHsv = true;
            H = h;
            S = s;
            V = v;
            _isUpdatingFromHsv = false;

            var luminance = 0.2126 * value.R + 0.7152 * value.G + 0.0722 * value.B;
            PreviewForegroundBrush = luminance > 128 ? Brushes.Black : Brushes.White;

            SelectedBrush = new SolidColorBrush(value);
            UpdatePickerSvBrush();
            UpdateSelectorPositions();
        }

        partial void OnRChanged(byte value) { if (!_isUpdatingFromRgb) UpdateColorFromRgb(); }
        partial void OnGChanged(byte value) { if (!_isUpdatingFromRgb) UpdateColorFromRgb(); }
        partial void OnBChanged(byte value) { if (!_isUpdatingFromRgb) UpdateColorFromRgb(); }
        partial void OnAChanged(byte value)
        {
            OnPropertyChanged(nameof(OpacityText));
            if (!_isUpdatingFromRgb) UpdateColorFromRgb();
        }

        private void UpdateColorFromRgb()
        {
            if (_isUpdatingFromRgb) return;
            _isUpdatingFromRgb = true;
            SelectedColor = Color.FromArgb(A, R, G, B);
            _isUpdatingFromRgb = false;
        }

        partial void OnHexChanged(string value)
        {
            if (_isUpdatingFromHex) return;

            if (TryParseHex(value, out var c) && c != SelectedColor)
            {
                _isUpdatingFromHex = true;
                SelectedColor = c;
                _isUpdatingFromHex = false;
            }
        }

        partial void OnHChanged(double value) { if (!_isUpdatingFromHsv) UpdateColorFromHsv(); }
        partial void OnSChanged(double value) { if (!_isUpdatingFromHsv) UpdateColorFromHsv(); }
        partial void OnVChanged(double value) { if (!_isUpdatingFromHsv) UpdateColorFromHsv(); }

        private void UpdateColorFromHsv()
        {
            if (_isUpdatingFromHsv) return;
            _isUpdatingFromHsv = true;
            var rgb = ColorFromHSV(H * 360.0, S, V);
            SelectedColor = Color.FromArgb(A, rgb.R, rgb.G, rgb.B);
            _isUpdatingFromHsv = false;
        }

        // sizes -> selector positions
        partial void OnSvFieldWidthChanged(double value) => UpdateSelectorPositions();
        partial void OnSvFieldHeightChanged(double value) => UpdateSelectorPositions();
        partial void OnHueBarHeightChanged(double value) => UpdateSelectorPositions();

        private void UpdateSelectorPositions()
        {
            SvSelectorX = Math.Clamp(S * SvFieldWidth - 7, 0, Math.Max(0, SvFieldWidth - 14));
            SvSelectorY = Math.Clamp((1 - V) * SvFieldHeight - 7, 0, Math.Max(0, SvFieldHeight - 14));

            // if you use horizontal hue bar: use HueSelectorX instead
            HueSelectorY = Math.Clamp(H * HueBarHeight - 1.5, 0, Math.Max(0, HueBarHeight - 3));
        }

        private void UpdatePickerSvBrush()
        {
            PickerSvBrush = new SolidColorBrush(ColorFromHSV(H * 360.0, 1.0, 1.0));
        }

        // called by view events
        public void SetSvFromPoint(System.Windows.Point p, double width, double height)
        {
            SvFieldWidth = width;
            SvFieldHeight = height;

            if (width <= 0 || height <= 0) return;

            S = Math.Clamp(p.X / width, 0, 1);
            V = Math.Clamp(1.0 - (p.Y / height), 0, 1);
        }

        public void SetHueFromPoint(Point p, double height)
        {
            HueBarHeight = height;
            if (height <= 0) return;

            H = Math.Clamp(p.Y / height, 0, 1);

            UpdatePickerSvBrush();
            UpdateSelectorPositions();
        }

        // ---------------------------
        // Recents as HEX strings
        // ---------------------------

        private static string ToHex(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        private static bool TryParseHex(string? hex, out Color c)
        {
            c = Colors.Transparent;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            hex = hex.Trim();
            if (!hex.StartsWith("#", StringComparison.Ordinal))
                hex = "#" + hex;

            // support #RRGGBB
            if (hex.Length == 7)
                hex = "#FF" + hex.Substring(1);

            if (hex.Length != 9)
                return false;

            if (byte.TryParse(hex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a) &&
                byte.TryParse(hex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
                byte.TryParse(hex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
                byte.TryParse(hex.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                c = Color.FromArgb(a, r, g, b);
                return true;
            }

            return false;
        }

        // ---------------------------
        // HSV helpers (unchanged)
        // ---------------------------

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60.0 % 2) - 1));
            double m = value - c;

            double r1, g1, b1;
            if (hue < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (hue < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (hue < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (hue < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (hue < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);

            return Color.FromRgb(r, g, b);
        }

        private static void ToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue;
            if (delta == 0) hue = 0;
            else if (max == r) hue = 60 * (((g - b) / delta) % 6);
            else if (max == g) hue = 60 * (((b - r) / delta) + 2);
            else hue = 60 * (((r - g) / delta) + 4);

            if (hue < 0) hue += 360;

            h = hue / 360.0;
            s = max == 0 ? 0 : delta / max;
            v = max;
        }

        // small relay (if you don’t want Toolkit commands here, you can remove this)
        private sealed class RelayCommand<T> : ICommand
        {
            private readonly Action<T?> _execute;
            public RelayCommand(Action<T?> execute) => _execute = execute;
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute((T?)parameter);
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
