using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using VIA.WPF.Controls;
using VIA.WPF.Windowing;

namespace Mockup.ColorSystem;

[ObservableObject]
public partial class ColorSchemaEditor : XWindow
{
    private readonly ColorSchema _schema;

    [ObservableProperty]
    private ColorItem? selectedColorItem;

    // === NAME ===
    [ObservableProperty] private string displayName = "";

    // === COLOR MODEL FOR THE UI ===
    public sealed class ColorItem : XColorToken
    {
        public ColorItem(string label, string key, Color color)
            : base(label, key, color)
        {
        }
    }

    // COLOR LIST
    public ObservableCollection<ColorItem> ColorItems { get; } = new();

    // PREVIEW ITEMS
    public ObservableCollection<ColorItem> PreviewItems { get; } = new();


    // === C O N S T R U C T O R ====================================================
    public ColorSchemaEditor(ColorSchema schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));

        InitializeComponent();

        DataContext = this;

        LoadSchemaIntoUi();
    }


    // === LOAD DATA INTO UI ========================================================
    private void LoadSchemaIntoUi()
    {
        DisplayName = _schema.DisplayName;

        AddColor("Primary", nameof(_schema.PrimaryColor), _schema.PrimaryColor);
        AddColor("Neutral", nameof(_schema.NeutralColor), _schema.NeutralColor);
        AddColor("Accent", nameof(_schema.AccentColor), _schema.AccentColor);
        AddColor("Text", nameof(_schema.TextColor), _schema.TextColor);
        AddColor("Info", nameof(_schema.InfoColor), _schema.InfoColor);
        AddColor("Control Background", nameof(_schema.ControlBGColor), _schema.ControlBGColor);
        AddColor("Warning", nameof(_schema.WarningColor), _schema.WarningColor);
        AddColor("Control Border", nameof(_schema.ControlBorderColor), _schema.ControlBorderColor);
        AddColor("Error", nameof(_schema.ErrorColor), _schema.ErrorColor);
        AddColor("Card Border", nameof(_schema.BorderColor), _schema.BorderColor);
        AddColor("Success", nameof(_schema.SuccessColor), _schema.SuccessColor);

        foreach (var c in ColorItems)
            PreviewItems.Add(c);

        SelectedColorItem = ColorItems.Count > 0 ? ColorItems[0] : null;
    }

    private void AddColor(string label, string key, Color value)
    {
        var ci = new ColorItem(label, key, value);

        // HEX -> COLOR sync
        ci.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ColorItem.Hex))
            {
                if (TryParseHex(ci.Hex, out var newColor))
                {
                    ci.Color = newColor;
                }
            }
            else if (e.PropertyName == nameof(ColorItem.Color))
            {
                ci.Hex = ColorToHex(ci.Color);
            }
        };

        ColorItems.Add(ci);
    }


    // === OK / CANCEL ==============================================================
    [RelayCommand]
    private void Ok()
    {
        _schema.DisplayName = DisplayName;

        foreach (var c in ColorItems)
        {
            var value = c.Color;

            switch (c.Key)
            {
                case nameof(_schema.PrimaryColor): _schema.PrimaryColor = value; break;
                case nameof(_schema.AccentColor): _schema.AccentColor = value; break;
                case nameof(_schema.InfoColor): _schema.InfoColor = value; break;
                case nameof(_schema.WarningColor): _schema.WarningColor = value; break;
                case nameof(_schema.ErrorColor): _schema.ErrorColor = value; break;

                case nameof(_schema.SuccessColor): _schema.SuccessColor = value; break;
                case nameof(_schema.NeutralColor): _schema.NeutralColor = value; break;
                case nameof(_schema.ControlBorderColor): _schema.ControlBorderColor = value; break;

                case nameof(_schema.TextColor): _schema.TextColor = value; break;
                case nameof(_schema.ControlBGColor): _schema.ControlBGColor = value; break;
                case nameof(_schema.BorderColor): _schema.BorderColor = value; break;
            }
        }

        // WICHTIG: Key niemals verändern!
        if (string.IsNullOrWhiteSpace(_schema.Key))
            _schema.Key = Guid.NewGuid().ToString();

        DialogResult = true;
        Close();
    }


    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        Close();
    }


    // === HEX UTILS ================================================================
    public static string ColorToHex(Color c)
        => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static bool TryParseHex(string? hex, out Color c)
    {
        c = Colors.Transparent;
        if (hex == null) return false;

        hex = hex.Trim().TrimStart('#');

        if (hex.Length == 6 &&
            byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            c = Color.FromRgb(r, g, b);
            return true;
        }

        return false;
    }
}


