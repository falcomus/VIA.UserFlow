// ======================================================================================
// FILE: Mockup.Controls/Breadcrumb.cs
//
// PURPOSE:
// - Breadcrumb control for the mockup designer.
// - Displays a horizontal navigation path like "Home > Products > Detail".
// - Supports active item styling, separators, spacing and optional background.
//
// PROJECT: Mockup.Controls
// GROUP: Display
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Mockup.Controls;

#region === BREADCRUMB ===

[ControlType(displayName: "Breadcrumb", group: "Navigation")]
public partial class Breadcrumb : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Items")]
    private ObservableCollection<string> items = ["Home", "Products", "Detail"];

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Separator")]
    private string separator = ">";

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Current Item Index")]
    private int currentItemIndex = 2;

    partial void OnCurrentItemIndexChanged(int value)
    {
        if (Items == null || Items.Count == 0)
        {
            currentItemIndex = -1;
            return;
        }

        currentItemIndex = Math.Clamp(value, 0, Items.Count - 1);
    }

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Background")]
    private bool showBackground = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Background Color")]
    private Color backgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Theme.Text.Lighten(0.05f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Current Item Color")]
    private Color currentItemColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Separator Color")]
    private Color separatorColor = Theme.Text.WithAlpha(140);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 0;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Width")]
    private float borderWidth = 0;

    partial void OnBorderWidthChanged(float value)
    {
        borderWidth = Math.Clamp(value, 0f, 8f);
    }

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private float fontSize = 13f;

    partial void OnFontSizeChanged(float value)
    {
        fontSize = Math.Clamp(value, 6f, 40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Current Item Weight")]
    private FontWeight currentItemWeight = FontWeights.SemiBold;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(10, 4, 10, 4);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Spacing")]
    private float itemSpacing = 6f;

    partial void OnItemSpacingChanged(float value)
    {
        itemSpacing = Math.Clamp(value, 0f, 40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Separator Spacing")]
    private float separatorSpacing = 6f;

    partial void OnSeparatorSpacingChanged(float value)
    {
        separatorSpacing = Math.Clamp(value, 0f, 40f);
    }

    #endregion

    #region === CTOR ===

    public Breadcrumb()
    {
        Name = "Breadcrumb";
        ResizeStyle = ResizeStyles.ResizeAll;

        ExplicitePreviewWidth = 200f;
        ExplicitePreviewHeight = 30f;

        Width = 180f;
        Height = 28f;

        MinWidth = 80f;
        MinHeight = 20f;

        MaxWidth = 1200f;
        MaxHeight = 120f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        if (Items == null || Items.Count == 0)
            return;

        DrawBackground(canvas, layout);
        DrawItems(canvas, layout);
    }

    private void DrawBackground(SKCanvas canvas, SKRect layout)
    {
        if (!ShowBackground && BorderWidth <= 0f)
            return;

        float radius = Math.Clamp(CornerRadius, 0f, Math.Min(layout.Width, layout.Height) / 2f);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: radius,
            fillStyle: FillStyle.Solid,
            fillColor: ShowBackground ? BackgroundColor : Colors.Transparent,
            borderStyle: BorderStyle.Solid,
            borderColor: BorderColor,
            borderWidth: BorderWidth);
    }

    private void DrawItems(SKCanvas canvas, SKRect layout)
    {
        float left = layout.Left + (float)Padding.Left;
        float right = layout.Right - (float)Padding.Right;
        float x = left;
        float top = layout.Top + (float)Padding.Top;
        float bottom = layout.Bottom - (float)Padding.Bottom;
        float availableHeight = Math.Max(0f, bottom - top);

        if (availableHeight <= 1f || right <= left)
            return;

        for (int i = 0; i < Items.Count; i++)
        {
            string text = Items[i] ?? string.Empty;
            bool isCurrent = i == CurrentItemIndex;

            float textWidth = MeasureTextWidth(
                text,
                FontSize,
                isCurrent ? CurrentItemWeight : FontWeight);

            if (x >= right)
                break;

            var textRect = new SKRect(x, top, right, bottom);

            TextRenderer.Draw2(
                canvas: canvas,
                text: text,
                bounds: textRect,
                fontSize: FontSize,
                color: isCurrent ? CurrentItemColor : TextColor,
                fontWeight: isCurrent ? CurrentItemWeight : FontWeight,
                padding: new Thickness(0),
                textAlignment: TextAlignment.Left);

            x += textWidth;

            if (i >= Items.Count - 1)
                continue;

            x += ItemSpacing;

            float sepWidth = MeasureTextWidth(Separator, FontSize, FontWeights.Normal);
            if (x + sepWidth > right)
                break;

            var sepRect = new SKRect(x, top, x + sepWidth, bottom);

            TextRenderer.Draw2(
                canvas: canvas,
                text: Separator,
                bounds: sepRect,
                fontSize: FontSize,
                color: SeparatorColor,
                fontWeight: FontWeights.Normal,
                padding: new Thickness(0),
                textAlignment: TextAlignment.Left);

            x += sepWidth;
            x += SeparatorSpacing;
        }
    }

    #endregion

    #region === HELPERS ===

    private static float MeasureTextWidth(string text, float fontSize, FontWeight fontWeight)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0f;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = fontWeight >= FontWeights.SemiBold
                ? SKTypeface.FromFamilyName(
                    Theme.FontFamily,
                    SKFontStyleWeight.SemiBold,
                    SKFontStyleWidth.Normal,
                    SKFontStyleSlant.Upright)
                : SKTypeface.FromFamilyName(
                    Theme.FontFamily,
                    SKFontStyleWeight.Normal,
                    SKFontStyleWidth.Normal,
                    SKFontStyleSlant.Upright)
        };

        return paint.MeasureText(text);
    }

    #endregion
}

#endregion