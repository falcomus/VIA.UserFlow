// ======================================================================================
// FILE: Mockup.Controls/TextBlock.cs
//
// PURPOSE:
// - Multi-line text label control for the mockup control library.
// - Wraps text to the available width and auto-sizes its height.
// - Uses RichTextKit for measurement and rendering.
// - Intended for descriptive text, captions and longer copy blocks.
//
// NOTES:
// - Width is user-controlled; Height is derived from wrapped content.
// - ResizeStyle is WidthOnly so the user can control wrap width directly.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Media;
using RichTextAlignment = Topten.RichTextKit.TextAlignment;
using Style = Topten.RichTextKit.Style;

namespace Mockup.Controls;

#region === MULTI LINE TEXT BLOCK =========================================================

[ControlType(displayName: "Text Block", group: "Content")]
public partial class TextBlock : DesignControl
{
    #region === CONTENT ===================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "Multi-line text";

    #endregion

    #region === APPEARANCE ================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Background Color")]
    private Color backgroundColor = Colors.Transparent;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Colors.Transparent;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(0, 0, 0, 0);

    #endregion

    #region === TYPOGRAPHY ================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private float fontSize = 13f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Text Alignment")]
    private System.Windows.TextAlignment textAlignment = System.Windows.TextAlignment.Left;

    #endregion

    #region === CTOR ======================================================================

    public TextBlock()
    {
        Name = "TextBlock";
        ResizeStyle = ResizeStyles.WidthOnly;

        ExplicitePreviewWidth = 120f;
        ExplicitePreviewHeight = 60f;

        Width = 180f;
        Height = 60f;

        MinWidth = 40f;
        MinHeight = 20f;

        MaxWidth = 1200f;
        MaxHeight = 2000f;

        Text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr.";
    }

    #endregion

    #region === RENDER ====================================================================

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        var contentRect = CreateContentRect(layout, Padding);
        if (contentRect.Width <= 1f)
            return;

        var measured = CreateMeasuredTextBlock(
            text: Text,
            maxWidth: contentRect.Width,
            alignment: ConvertAlignment(TextAlignment));

        float desiredHeight = measured.MeasuredHeight + (float)Padding.Top + (float)Padding.Bottom;
        desiredHeight = Math.Clamp(desiredHeight, MinHeight, MaxHeight);

        if (Math.Abs(Height - desiredHeight) > 0.5f)
            Height = desiredHeight;

        DrawBackground(canvas, layout);

        contentRect = CreateContentRect(layout, Padding);
        if (contentRect.Width <= 1f || contentRect.Height <= 1f)
            return;

        canvas.Save();
        canvas.Translate(contentRect.Left, contentRect.Top - 1.5f);
        measured.Paint(canvas);
        canvas.Restore();
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === HELPERS ===================================================================

    private Topten.RichTextKit.TextBlock CreateMeasuredTextBlock(
        string text,
        float maxWidth,
        RichTextAlignment alignment)
    {
        var style = new Style
        {
            FontFamily = Theme.FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight.ToFontWeightValue(),
            TextColor = TextColor.ToSKColor()
        };

        var textBlock = new Topten.RichTextKit.TextBlock
        {
            MaxWidth = Math.Max(1f, maxWidth),
            Alignment = alignment,
            EllipsisEnabled = true
        };

        textBlock.AddText(string.IsNullOrEmpty(text) ? " " : text, style);
        textBlock.Layout();

        return textBlock;
    }

    private void DrawBackground(SKCanvas canvas, SKRect layout)
    {
        if (BackgroundColor != Colors.Transparent || BorderColor != Colors.Transparent)
        {
            SkiaRenderer.DrawRect(
                canvas: canvas,
                rect: layout,
                cornerRadius: 0,
                fillStyle: FillStyle.Solid,
                fillColor: BackgroundColor,
                borderColor: BorderColor,
                borderStyle: BorderStyle.Solid,
                shadowOptions: ShadowOptions.Default,
                borderWidth: BorderColor == Colors.Transparent ? 0f : 1f);
        }
    }

    private static SKRect CreateContentRect(SKRect layout, Thickness padding)
    {
        return new SKRect(
            layout.Left + (float)padding.Left,
            layout.Top + (float)padding.Top,
            layout.Right - (float)padding.Right,
            layout.Bottom - (float)padding.Bottom);
    }

    private static RichTextAlignment ConvertAlignment(System.Windows.TextAlignment alignment)
    {
        return alignment switch
        {
            System.Windows.TextAlignment.Left => RichTextAlignment.Left,
            System.Windows.TextAlignment.Right => RichTextAlignment.Right,
            System.Windows.TextAlignment.Center => RichTextAlignment.Center,
            _ => RichTextAlignment.Left
        };
    }

    #endregion
}

#endregion
