using Mockup.ColorSystem;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Media;
using Topten.RichTextKit;
using Style = Topten.RichTextKit.Style;
using TextAlignment = Topten.RichTextKit.TextAlignment;

namespace Mockup.Rendering;

public static class TextRenderer
{
    private const float VerticalOpticalOffset = -1.5f;

    /// <summary>
    /// Legacy-Kompatibilität.
    /// Diese Instanz wird intern nicht mehr verwendet.
    /// </summary>
    public static TextBlock Textblock = new()
    {
        EllipsisEnabled = true,
    };

    /// <summary>
    /// Legacy-Kompatibilität.
    /// Diese Instanz wird intern nicht mehr verwendet.
    /// </summary>
    public static Style Style = new();

    /// <summary>
    /// Misst die Textbreite über RichTextKit.
    /// </summary>
    public static float MeasureTextWidth(
        string text,
        double fontSize,
        string fontFamily = "",
        FontWeight fontWeight = default,
        bool italic = false)
    {
        var style = CreateStyle(
            fontSize: (float)fontSize,
            textColor: SKColors.Black,
            fontFamily: fontFamily,
            fontWeight: fontWeight,
            italic: italic);

        var textBlock = CreateTextBlock(
            text: text,
            style: style,
            maxWidth: float.MaxValue,
            maxHeight: float.MaxValue,
            alignment: TextAlignment.Left);

        return textBlock.MeasuredWidth;
    }

    public static TextBlock Draw2(
        SKCanvas canvas,
        string text,
        SKRect bounds,
        double fontSize,
        Color color,
        Thickness padding,
        string fontFamily = "",
        System.Windows.TextAlignment textAlignment = System.Windows.TextAlignment.Center,
        FontWeight fontWeight = default,
        bool italic = false)
    {
        return DrawInternal(
            canvas: canvas,
            text: text,
            bounds: bounds,
            fontSize: (float)fontSize,
            color: color.ToSKColor(),
            paddingLeft: (float)padding.Left,
            paddingTop: (float)padding.Top,
            paddingRight: (float)padding.Right,
            paddingBottom: (float)padding.Bottom,
            fontFamily: fontFamily,
            textAlignment: ConvertAlignment(textAlignment),
            fontWeight: fontWeight,
            italic: italic);
    }

    public static TextBlock Draw(
        SKCanvas canvas,
        string text,
        SKRect bounds,
        double fontSize,
        SKColor color,
        string fontFamily = "",
        TextAlignment textAlignment = TextAlignment.Center,
        float padding = 2f,
        FontWeight fontWeight = default,
        bool italic = false)
    {
        return DrawInternal(
            canvas: canvas,
            text: text,
            bounds: bounds,
            fontSize: (float)fontSize,
            color: color,
            paddingLeft: padding,
            paddingTop: padding,
            paddingRight: padding,
            paddingBottom: padding,
            fontFamily: fontFamily,
            textAlignment: textAlignment,
            fontWeight: fontWeight,
            italic: italic);
    }

    /// <summary>
    /// Zeichnet einen Tooltip/Hover-Hint mit Hintergrund.
    /// </summary>
    public static void DrawHoverHint(
        SKCanvas canvas,
        string text,
        SKPoint position,
        int canvasWidth,
        int canvasHeight,
        float fontSize = 11f,
        string fontFamily = "",
        FontWeight fontWeight = default,
        bool italic = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        const float padX = 6f;
        const float padY = 3f;
        const float cursorGap = 6f;

        var style = CreateStyle(
            fontSize: fontSize,
            textColor: SKColors.Black,
            fontFamily: fontFamily,
            fontWeight: fontWeight,
            italic: italic);

        var textBlock = CreateTextBlock(
            text: text,
            style: style,
            maxWidth: canvasWidth * 0.3f,
            maxHeight: float.MaxValue,
            alignment: TextAlignment.Left);

        float textWidth = textBlock.MeasuredWidth;
        float textHeight = textBlock.MeasuredHeight;

        float bgLeft = position.X + cursorGap;
        float bgTop = position.Y + cursorGap;
        float bgWidth = textWidth + 2 * padX;
        float bgHeight = textHeight + 2 * padY;

        if (bgLeft + bgWidth > canvasWidth)
            bgLeft = position.X - bgWidth - cursorGap;

        if (bgTop + bgHeight > canvasHeight)
            bgTop = position.Y - bgHeight - cursorGap;

        bgLeft = Math.Max(0, Math.Min(bgLeft, canvasWidth - bgWidth));
        bgTop = Math.Max(0, Math.Min(bgTop, canvasHeight - bgHeight));

        var bgRect = SKRect.Create(bgLeft, bgTop, bgWidth, bgHeight);

        using var bgPaint = new SKPaint
        {
            Color = SKColor.Parse("#FFF3B0").WithAlpha(235),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRoundRect(bgRect, 3, 3, bgPaint);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(150),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        canvas.DrawRoundRect(bgRect, 3, 3, borderPaint);

        canvas.Save();
        canvas.Translate(bgLeft + padX, bgTop + padY);
        textBlock.Paint(canvas);
        canvas.Restore();
    }

    /// <summary>
    /// Alternative: Zeichnet mit vorhandenem Stil.
    /// </summary>
    public static void DrawHoverHintWithStyle(
        SKCanvas canvas,
        string text,
        SKPoint position,
        int canvasWidth,
        int canvasHeight,
        Style style,
        SKColor backgroundColor)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        const float pad = 16f;
        const float cursorGap = 6f;

        var textBlock = CreateTextBlock(
            text: text,
            style: style,
            maxWidth: canvasWidth * 0.3f,
            maxHeight: float.MaxValue,
            alignment: TextAlignment.Left);

        float textWidth = textBlock.MeasuredWidth;
        float textHeight = textBlock.MeasuredHeight;

        float bgLeft;
        float bgTop;

        if (position.X + textWidth + 2 * pad + cursorGap < canvasWidth)
        {
            bgLeft = position.X + cursorGap;
        }
        else
        {
            bgLeft = position.X - textWidth - 2 * pad - cursorGap;
        }

        if (position.Y + textHeight + 2 * pad + cursorGap < canvasHeight)
        {
            bgTop = position.Y + cursorGap;
        }
        else
        {
            bgTop = position.Y - textHeight - 2 * pad - cursorGap;
        }

        bgLeft = Math.Max(0, Math.Min(bgLeft, canvasWidth - (textWidth + 2 * pad)));
        bgTop = Math.Max(0, Math.Min(bgTop, canvasHeight - (textHeight + 2 * pad)));

        var bgRect = SKRect.Create(
            bgLeft,
            bgTop,
            textWidth + 2 * pad,
            textHeight + 2 * pad
        );

        using var bgPaint = new SKPaint
        {
            Color = backgroundColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRoundRect(bgRect, 4, 4, bgPaint);

        canvas.Save();
        canvas.Translate(bgLeft + pad, bgTop + pad);
        textBlock.Paint(canvas);
        canvas.Restore();
    }

    private static TextBlock DrawInternal(
        SKCanvas canvas,
        string text,
        SKRect bounds,
        float fontSize,
        SKColor color,
        float paddingLeft,
        float paddingTop,
        float paddingRight,
        float paddingBottom,
        string fontFamily,
        TextAlignment textAlignment,
        FontWeight fontWeight,
        bool italic)
    {
        float availableWidth = Math.Max(1f, bounds.Width - paddingLeft - paddingRight);
        float availableHeight = Math.Max(1f, bounds.Height - paddingTop - paddingBottom);

        var style = CreateStyle(
            fontSize: fontSize,
            textColor: color,
            fontFamily: fontFamily,
            fontWeight: fontWeight,
            italic: italic);

        var textBlock = CreateTextBlock(
            text: text,
            style: style,
            maxWidth: availableWidth,
            maxHeight: availableHeight,
            alignment: textAlignment);

        float textHeight = textBlock.MeasuredHeight;
        float offsetX = bounds.Left + paddingLeft;
        float offsetY =
            bounds.Top
            + paddingTop
            + (availableHeight - textHeight) / 2f
            + VerticalOpticalOffset;

        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        textBlock.Paint(canvas);
        canvas.Restore();

        return textBlock;
    }

    private static Style CreateStyle(
        float fontSize,
        SKColor textColor,
        string fontFamily,
        FontWeight fontWeight,
        bool italic)
    {
        return new Style
        {
            FontSize = fontSize,
            TextColor = textColor,
            FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? Theme.FontFamily : fontFamily,
            FontWeight = fontWeight.ToFontWeightValue(),
            FontItalic = italic
        };
    }

    private static TextBlock CreateTextBlock(
        string text,
        Style style,
        float maxWidth,
        float maxHeight,
        TextAlignment alignment)
    {
        var textBlock = new TextBlock
        {
            EllipsisEnabled = true,
            Alignment = alignment,
            MaxWidth = Math.Max(1f, maxWidth),
            MaxHeight = Math.Max(1f, maxHeight)
        };

        textBlock.Clear();
        textBlock.AddText(text ?? string.Empty, style);
        textBlock.Layout();

        return textBlock;
    }

    private static TextAlignment ConvertAlignment(System.Windows.TextAlignment textAlignment)
    {
        return textAlignment switch
        {
            System.Windows.TextAlignment.Left => TextAlignment.Left,
            System.Windows.TextAlignment.Right => TextAlignment.Right,
            System.Windows.TextAlignment.Center => TextAlignment.Center,
            System.Windows.TextAlignment.Justify => TextAlignment.Center,
            _ => TextAlignment.Center
        };
    }
}
