//using System.Text.Json.Serialization;
//using System.Windows;
//using System.Windows.Media;

//namespace Mockup.ColorSystem;

//public sealed class ColorSchema
//{
//    // Identity
//    public string Key { get; set; } = "Default";
//    public string DisplayName { get; set; } = "Default";

//    // Brand Colors
//    public Color PrimaryColor { get; set; } = (Color)ColorConverter.ConvertFromString("#90CAF9");
//    public Color AccentColor { get; set; } = (Color)ColorConverter.ConvertFromString("#4DD0E1");
//    public Color InfoColor { get; set; } = (Color)ColorConverter.ConvertFromString("#64B5F6");
//    public Color WarningColor { get; set; } = (Color)ColorConverter.ConvertFromString("#FFD54F");
//    public Color ErrorColor { get; set; } = (Color)ColorConverter.ConvertFromString("#EF9A9A");
//    public Color SuccessColor { get; set; } = (Color)ColorConverter.ConvertFromString("#A5D6A7");
//    public Color NeutralColor { get; set; } = (Color)ColorConverter.ConvertFromString("#EEEEEE");

//    // System Colors / Controls
//    public Color TextColor { get; set; } = Colors.Black;
//    public Color ControlBGColor { get; set; } = (Color)ColorConverter.ConvertFromString("#FFFFFF");
//    public Color ControlBorderColor { get; set; } = (Color)ColorConverter.ConvertFromString("#B0B1B1");
//    public Color BorderColor { get; set; } = (Color)ColorConverter.ConvertFromString("#B0B1B1");

//    // Layout
//    public float CornerRadius { get; set; } = 3f;
//    public float BorderThickness { get; set; } = 1f;

//    // Typography
//    public string FontFamily { get; set; } = "Segoe UI";
//    public FontWeight FontWeightNormal { get; set; } = FontWeights.Regular;
//    public FontWeight FontWeightBold { get; set; } = FontWeights.SemiBold;
//    public FontWeight FontWeightLight { get; set; } = FontWeights.Light;

//    // Brush Properties für XAML Binding
//    [JsonIgnore] public SolidColorBrush PrimaryBrush => PrimaryColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush AccentBrush => AccentColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush InfoBrush => InfoColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush WarningBrush => WarningColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush ErrorBrush => ErrorColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush SuccessBrush => SuccessColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush NeutralBrush => NeutralColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush TextBrush => TextColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush ControlBGBrush => ControlBGColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush ControlBorderBrush => ControlBorderColor.ToBrush();
//    [JsonIgnore] public SolidColorBrush BorderBrush => BorderColor.ToBrush();

//    public ColorSchema Clone()
//    {
//        return (ColorSchema)this.MemberwiseClone();
//    }

//    public static ColorSchema CreateDefault()
//    {
//        return new ColorSchema
//        {
//            Key = "Default",
//            DisplayName = "Default"
//        };
//    }

//    public override string ToString() => DisplayName;
//}


using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace Mockup.ColorSystem;

public sealed partial class ColorSchema : ObservableObject
{
    public string Key { get; set; } = "Default";

    [ObservableProperty] private string displayName = "Default";

    [ObservableProperty] private Color primaryColor = (Color)ColorConverter.ConvertFromString("#90CAF9");
    [ObservableProperty] private Color accentColor = (Color)ColorConverter.ConvertFromString("#4DD0E1");
    [ObservableProperty] private Color infoColor = (Color)ColorConverter.ConvertFromString("#64B5F6");
    [ObservableProperty] private Color warningColor = (Color)ColorConverter.ConvertFromString("#FFD54F");
    [ObservableProperty] private Color errorColor = (Color)ColorConverter.ConvertFromString("#EF9A9A");
    [ObservableProperty] private Color successColor = (Color)ColorConverter.ConvertFromString("#A5D6A7");
    [ObservableProperty] private Color neutralColor = (Color)ColorConverter.ConvertFromString("#EEEEEE");

    [ObservableProperty] private Color textColor = Colors.Black;
    [ObservableProperty] private Color controlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
    [ObservableProperty] private Color controlBorderColor = (Color)ColorConverter.ConvertFromString("#B0B1B1");
    [ObservableProperty] private Color borderColor = (Color)ColorConverter.ConvertFromString("#B0B1B1");

    public float CornerRadius { get; set; } = 3f;
    public float BorderThickness { get; set; } = 1f;

    public string FontFamily { get; set; } = "Segoe UI";
    public FontWeight FontWeightNormal { get; set; } = FontWeights.Regular;
    public FontWeight FontWeightBold { get; set; } = FontWeights.SemiBold;
    public FontWeight FontWeightLight { get; set; } = FontWeights.Light;

    // --- cached brushes (stabile Instanzen) ---
    [JsonIgnore] public SolidColorBrush PrimaryBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush AccentBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush InfoBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush WarningBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush ErrorBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush SuccessBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush NeutralBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush TextBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush ControlBGBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush ControlBorderBrush { get; } = new();
    [JsonIgnore] public SolidColorBrush BorderBrush { get; } = new();

    public ColorSchema()
    {
        SyncAllBrushes();
    }

    public static ColorSchema CreateDefault()
    {
        return new ColorSchema
        {
            Key = "Default",
            DisplayName = "Default"
        };
    }

    private void SyncAllBrushes()
    {
        PrimaryBrush.Color = PrimaryColor;
        AccentBrush.Color = AccentColor;
        InfoBrush.Color = InfoColor;
        WarningBrush.Color = WarningColor;
        ErrorBrush.Color = ErrorColor;
        SuccessBrush.Color = SuccessColor;
        NeutralBrush.Color = NeutralColor;

        TextBrush.Color = TextColor;
        ControlBGBrush.Color = ControlBGColor;
        ControlBorderBrush.Color = ControlBorderColor;
        BorderBrush.Color = BorderColor;
    }

    partial void OnPrimaryColorChanged(Color value) => PrimaryBrush.Color = value;
    partial void OnAccentColorChanged(Color value) => AccentBrush.Color = value;
    partial void OnInfoColorChanged(Color value) => InfoBrush.Color = value;
    partial void OnWarningColorChanged(Color value) => WarningBrush.Color = value;
    partial void OnErrorColorChanged(Color value) => ErrorBrush.Color = value;
    partial void OnSuccessColorChanged(Color value) => SuccessBrush.Color = value;
    partial void OnNeutralColorChanged(Color value) => NeutralBrush.Color = value;

    partial void OnTextColorChanged(Color value) => TextBrush.Color = value;
    partial void OnControlBGColorChanged(Color value) => ControlBGBrush.Color = value;
    partial void OnControlBorderColorChanged(Color value) => ControlBorderBrush.Color = value;
    partial void OnBorderColorChanged(Color value) => BorderBrush.Color = value;

    public ColorSchema Clone()
    {
        // MemberwiseClone kopiert Brush-Referenzen -> nicht gut.
        // Daher: neue Instanz und Werte kopieren.
        var c = new ColorSchema
        {
            Key = Key,
            DisplayName = DisplayName,

            PrimaryColor = PrimaryColor,
            AccentColor = AccentColor,
            InfoColor = InfoColor,
            WarningColor = WarningColor,
            ErrorColor = ErrorColor,
            SuccessColor = SuccessColor,
            NeutralColor = NeutralColor,

            TextColor = TextColor,
            ControlBGColor = ControlBGColor,
            ControlBorderColor = ControlBorderColor,
            BorderColor = BorderColor,

            CornerRadius = CornerRadius,
            BorderThickness = BorderThickness,

            FontFamily = FontFamily,
            FontWeightNormal = FontWeightNormal,
            FontWeightBold = FontWeightBold,
            FontWeightLight = FontWeightLight,
        };

        return c;
    }

    public override string ToString() => DisplayName;
}
