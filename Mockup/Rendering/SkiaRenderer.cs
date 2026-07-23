// ======================================================================================
// FILE: Mockup/Renderer/SkiaRenderer.cs
//
// ZWECK:
//  Skia-Renderer, der WPF-Color vollständig unterstützt.
//  Alle öffentlichen Methoden akzeptieren WPF Colors –
//  interne Konvertierung erfolgt automatisch.
//
// AUTOR: Claus Falkenstein / ChatGPT (MO30 – ColorSystem Migration)
// VERSION: 2.0
// ======================================================================================

using Mockup.AssetSystem;
using Mockup.ColorSystem;
using Mockup.Controls;
using Mockup.Domain.Registry;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Media;

namespace Mockup.Rendering;

public static class SkiaRenderer
{
    public static readonly Color SelectionColor = Color.FromRgb(80, 190, 255);

    // ==================================================================================
    //  RECT + BORDER + SHADOW
    // ==================================================================================
    public static void DrawRect(
        SKCanvas canvas,
        SKRect rect,
        float cornerRadius,
        FillStyle fillStyle = FillStyle.Solid,
        Color? fillColor = null,
        GradientOptions? gradient = null,
        BorderStyle borderStyle = BorderStyle.None,
        Color? borderColor = null,
        float borderWidth = 1f,
        ShadowOptions? shadowOptions = null,
        bool innerBorder = false)
    {
        // --- Schatten --------------------------------------------------------------
        if (shadowOptions != null)
        {
            DrawShadow(canvas, rect, cornerRadius, shadowOptions.Value);
        }

        // --- Füllung ---------------------------------------------------------------
        if (fillStyle != FillStyle.None)
        {
            using var fillPaint = CreateFillPaint(fillStyle, fillColor?.ToSKColor(), gradient, rect);

            SKRect fillRect = rect;

            // InnerBorder = weißer Innenrahmen → light style
            if (innerBorder)
            {
                using var innerBorderPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

                DrawRoundedShape(canvas, rect, cornerRadius, innerBorderPaint);

                fillRect.Inflate(-0.8f, -0.8f);
            }

            DrawRoundedShape(canvas, fillRect, cornerRadius, fillPaint);
        }

        // --- Rahmen ---------------------------------------------------------------
        if (borderStyle != BorderStyle.None && borderColor.HasValue && borderWidth > 0)
        {
            var skColor = borderColor.Value.ToSKColor();
            using var borderPaint = CreateBorderPaint(borderStyle, skColor, borderWidth);

            DrawRoundedShape(canvas, rect, cornerRadius, borderPaint);
        }
    }




    // ######################################################################################
    // ######################################################################################
    // USED
    // ######################################################################################
    // ######################################################################################

    public static void RenderFlatButton(
        Button button,
        SKCanvas canvas,
        SKRect layout,
        RenderContext ctx,
        ShadowOptions shadowOptions)
    {
        // ==================================================================================
        // 1) Fill Box
        // ==================================================================================

        Color fillColor = button.GetFillColor(button.Variant, button.BackgroundColor);
        Color borderColor = button.GetBorderColor(button.Variant, button.BorderColor).Darken(0.4f);
        Color textColor = button.GetTextColor(button.Variant, button.TextColor);

        DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: button.CornerRadius,
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: borderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadowOptions,
            borderWidth: 0.5f
        );

        // ==================================================================================
        // 2) Content Layout (Padding + Icon + Text)
        // ==================================================================================

        var contentRect = CreateContentRect(layout, button.Padding);
        if (!IsUsableRect(contentRect))
            return;

        ComputeInlineContentLayout(
            contentRect,
            button.Icon,
            button.ImageAlignment,
            button.IconSize,
            4f,
            out var imageRect,
            out var textBounds);

        // ==================================================================================
        // 3) Icon Rendering (optional)
        // ==================================================================================

        if (HasRenderableIcon(button.Icon) && IsUsableRect(imageRect))
        {
            RenderSVGIcon(
                canvas,
                imageRect,
                button.Icon,
                fillColor,
                Colors.Transparent,
                textColor,
                1,
                false,
                0,
                0);
        }

        // ==================================================================================
        // 4) Text Rendering
        // ==================================================================================

        if (!IsUsableRect(textBounds))
            return;

        DrawInlineText(
            canvas,
            button.Text,
            textBounds,
            button.FontSize,
            textColor,
            button.FontWeight,
            HasRenderableIcon(button.Icon));
    }




    // ==================================================================================
    //  RAISED BUTTON (3D)
    // ==================================================================================
    public static void RenderRaisedButton(
        SKCanvas canvas,
        SKRect rect,
        float cornerRadius,
        Color baseColor,
        Color borderColor,
        float elevation = 0f,
        float scale = 1.0f)
    {
        // WPF → SK
        SKColor baseSk = baseColor.ToSKColor();
        SKColor borderSk = borderColor.ToSKColor();

        var lightColor = baseSk.WithBrightness(1.35f);
        var darkColor = baseSk.WithBrightness(0.55f);
        var shadowSize = elevation * scale;

        // Schatten
        ShadowOptions shadow = new()
        {
            Color = SKColors.Black.WithAlpha(130),
            Dx = shadowSize,
            Dy = shadowSize,
            Sigma = shadowSize
        };
        DrawShadow(canvas, rect, cornerRadius, shadow);

        // Gradient Layer
        DrawRect(
            canvas,
            rect,
            cornerRadius,
            fillStyle: FillStyle.Gradient,
            gradient: new GradientOptions
            {
                Colors = new[] { lightColor, baseSk, darkColor },
                StartPoint = new SKPoint(rect.Left, rect.Top),
                EndPoint = new SKPoint(rect.Left, rect.Bottom + 8)
            }
        );

        // Border
        using var borderPaint = new SKPaint
        {
            Color = borderSk,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.5f
        };

        DrawRoundedShape(canvas, rect, cornerRadius, borderPaint);
    }

    public static void RenderOutlineButton(
            OutlineButton button,
            SKCanvas canvas,
            SKRect layout,
            RenderContext ctx,
            ShadowOptions shadowOptions)
    {
        // ==================================================================================
        // 1) Fill Box
        // ==================================================================================

        Color fillColor = button.GetFillColor(button.Variant, button.BackgroundColor);
        Color textColor = button.Variant == ControlVariant.CUSTOM
            ? button.TextColor
            : fillColor.Darken(0.3f);
        Color borderColor = button.Variant == ControlVariant.CUSTOM
            ? textColor.Darken(0.1f)
            : button.GetBorderColor(button.Variant, button.BorderColor).Darken(0.1f);
        Color iconColor = textColor;

        if (shadowOptions.Equals(ShadowOptions.Default))
        {
            DrawRect(
                canvas: canvas,
                rect: layout,
                cornerRadius: button.CornerRadius,
                fillStyle: FillStyle.None,
                fillColor: Colors.Transparent,
                borderColor: borderColor,
                borderStyle: BorderStyle.Solid,
                borderWidth: 1.0f
            );
        }
        else
        {
            // SKColors.Black.WithAlpha(130)
            var oldShadowColor = shadowOptions.Color;
            if (shadowOptions.Equals(DesignControl.Elevation1))
            {
                shadowOptions.Color = SKColors.Black.WithAlpha(100);
            }

            DrawRect(
                canvas: canvas,
                rect: layout,
                cornerRadius: button.CornerRadius,
                fillStyle: FillStyle.None,
                fillColor: Colors.Transparent,
                borderColor: borderColor,
                borderStyle: BorderStyle.Solid,
                shadowOptions: shadowOptions,
                borderWidth: 1.0f
            );

            // Nochmal mit weiß darüber, um Schatten innen zu überdecken
            DrawRect(
                canvas: canvas,
                rect: layout,
                cornerRadius: button.CornerRadius,
                fillStyle: FillStyle.Solid,
                fillColor: Colors.White,
                borderColor: borderColor,
                borderStyle: BorderStyle.Solid,
                borderWidth: 1.0f
            );

            if (shadowOptions.Equals(DesignControl.Elevation1))
            {
                shadowOptions.Color = oldShadowColor;
            }
        }

        // ==================================================================================
        // 2) Content Layout (Padding + Icon + Text)
        // ==================================================================================

        var contentRect = CreateContentRect(layout, button.Padding);
        if (!IsUsableRect(contentRect))
            return;

        ComputeInlineContentLayout(
            contentRect,
            button.Icon,
            button.ImageAlignment,
            button.IconSize,
            4f,
            out var imageRect,
            out var textBounds);

        // ==================================================================================
        // 3) Icon Rendering (optional)
        // ==================================================================================

        if (HasRenderableIcon(button.Icon) && IsUsableRect(imageRect))
        {
            RenderSVGIcon(
                canvas,
                imageRect,
                button.Icon,
                Colors.Transparent,
                Colors.Transparent,
                iconColor,
                0,
                false,
                0,
                0);
        }

        // ==================================================================================
        // 4) Text Rendering
        // ==================================================================================

        if (!IsUsableRect(textBounds))
            return;

        DrawInlineText(
            canvas,
            button.Text,
            textBounds,
            button.FontSize,
            textColor,
            button.FontWeight,
            HasRenderableIcon(button.Icon));
    }


    public static void RenderChip(
            ChipBase chip,
            SKCanvas canvas,
            SKRect layout,
            RenderContext ctx,
            ShadowOptions shadowOptions)
    {
        // ==================================================================================
        // 1) Fill Box
        // ==================================================================================

        Color fillColor = chip.GetFillColor(chip.Variant, chip.BackgroundColor);
        Color borderColor = chip.GetBorderColor(chip.Variant, chip.BorderColor).Darken(0.2f);
        Color textColor = chip.GetTextColor(chip.Variant, chip.TextColor);

        DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: chip.Height,
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: borderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadowOptions,
            borderWidth: 0.5f
        );

        // ==================================================================================
        // 2) Content Layout (Padding + Icon + Text)
        // ==================================================================================

        var contentRect = CreateContentRect(layout, chip.Padding);
        if (!IsUsableRect(contentRect))
            return;

        ComputeInlineContentLayout(
            contentRect,
            chip.Icon,
            chip.ImageAlignment,
            chip.IconSize,
            4f,
            out var imageRect,
            out var textBounds);

        // ==================================================================================
        // 3) Icon Rendering (optional)
        // ==================================================================================

        if (HasRenderableIcon(chip.Icon) && IsUsableRect(imageRect))
        {
            Color iconColor = chip.Variant == ControlVariant.CUSTOM
                ? textColor
                : fillColor;

            RenderSVGIcon(
                canvas,
                imageRect,
                chip.Icon,
                fillColor,
                Colors.Transparent,
                iconColor,
                1,
                true,
                0,
                2);
        }

        // ==================================================================================
        // 4) Text Rendering
        // ==================================================================================

        if (!IsUsableRect(textBounds))
            return;

        DrawInlineText(
            canvas,
            chip.Text,
            textBounds,
            chip.FontSize,
            textColor,
            chip.FontWeight,
            HasRenderableIcon(chip.Icon));
    }


    private static SKRect CreateContentRect(SKRect layout, Thickness padding)
    {
        var rect = new SKRect(
            layout.Left + (float)padding.Left,
            layout.Top + (float)padding.Top,
            layout.Right - (float)padding.Right,
            layout.Bottom - (float)padding.Bottom);

        return rect;
    }

    private static bool HasRenderableIcon(ImageRef? imageRef)
    {
        return imageRef != null && !string.IsNullOrWhiteSpace(imageRef.Id);
    }

    private static bool IsUsableRect(SKRect rect)
    {
        return rect.Width >= 1f && rect.Height >= 1f;
    }

    private static void ComputeInlineContentLayout(
        SKRect contentRect,
        ImageRef? icon,
        HorizontalImageAlignment imageAlignment,
        float iconSize,
        float iconTextGap,
        out SKRect imageRect,
        out SKRect textRect)
    {
        imageRect = SKRect.Empty;
        textRect = contentRect;

        if (!IsUsableRect(contentRect))
            return;

        if (!HasRenderableIcon(icon))
            return;

        float effectiveIconSize = MathF.Min(iconSize, contentRect.Height);
        if (effectiveIconSize < 1f)
            return;

        float iconTop = contentRect.MidY - effectiveIconSize / 2f;
        float iconBottom = contentRect.MidY + effectiveIconSize / 2f;

        if (imageAlignment == HorizontalImageAlignment.Left)
        {
            imageRect = new SKRect(
                contentRect.Left,
                iconTop,
                contentRect.Left + effectiveIconSize,
                iconBottom);

            textRect = new SKRect(
                imageRect.Right + iconTextGap,
                contentRect.Top,
                contentRect.Right,
                contentRect.Bottom);
        }
        else
        {
            imageRect = new SKRect(
                contentRect.Right - effectiveIconSize,
                iconTop,
                contentRect.Right,
                iconBottom);

            textRect = new SKRect(
                contentRect.Left,
                contentRect.Top,
                imageRect.Left - iconTextGap,
                contentRect.Bottom);
        }

        if (!IsUsableRect(textRect))
        {
            textRect = SKRect.Empty;
        }
    }

    private static void DrawInlineText(
        SKCanvas canvas,
        string text,
        SKRect textBounds,
        double fontSize,
        Color textColor,
        FontWeight fontWeight,
        bool hasIcon)
    {
        TextRenderer.Draw2(
            canvas: canvas,
            text: text,
            bounds: textBounds,
            fontSize: fontSize,
            color: textColor,
            padding: new Thickness(0),
            fontWeight: fontWeight,
            textAlignment: hasIcon ? TextAlignment.Left : TextAlignment.Center
        );
    }


    public static void RenderSVGIcon(
        SKCanvas canvas,
        SKRect rect,
        ImageRef? imageRef,
        Color backgroundColor,
        Color borderColor,
        Color iconColor,
        float borderWidth,
        bool inCircle = false,
        float circleMargin = 5,
        float iconMargin = 5
        )
    {
        if (imageRef == null || string.IsNullOrWhiteSpace(imageRef.Id)) return;

        var icon = AssetCatalog.TryGet(imageRef.Id);

        if (icon == null) return;

        // Background
        using SKPaint paint = new SKPaint { Color = backgroundColor.ToSKColor() };
        if (backgroundColor != Colors.Transparent)
        {
            canvas.DrawRect(rect, paint);
        }

        // Border
        if (borderColor != Colors.Transparent)
        {
            paint.IsStroke = true;
            paint.Color = borderColor.ToSKColor();
            paint.StrokeWidth = borderWidth;
            canvas.DrawRect(rect, paint);
        }

        // Circle
        rect.Inflate(-circleMargin, -circleMargin);
        if (inCircle)
        {
            using SKPaint circlePaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            var center = new SKPoint(rect.MidX, rect.MidY);
            var radius = rect.Width / 2f;
            canvas.DrawCircle(center, radius, circlePaint);
        }

        // Icon
        rect.Inflate(-iconMargin, -iconMargin);
        ImageRenderer.Draw(canvas, icon, rect, iconColor.ToSKColor());
    }










    // ==================================================================================
    //  SHADOW
    // ==================================================================================
    public static void DrawShadow(SKCanvas canvas, SKRect rect, float cornerRadius, ShadowOptions shadow)
    {
        using var shadowPaint = new SKPaint
        {
            IsAntialias = true,
            Color = shadow.Color,
            Style = SKPaintStyle.Fill,
            ImageFilter = SKImageFilter.CreateDropShadow(
                shadow.Dx, shadow.Dy, shadow.Sigma, shadow.Sigma, shadow.Color)
        };

        DrawRoundedShape(canvas, rect, cornerRadius, shadowPaint);
    }


    // ==================================================================================
    //  HELPER: FILL PAINT
    // ==================================================================================
    private static SKPaint CreateFillPaint(
        FillStyle style,
        SKColor? color,
        GradientOptions? gradient,
        SKRect bounds)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        switch (style)
        {
            case FillStyle.Solid when color.HasValue:
                paint.Color = color.Value;
                break;

            case FillStyle.Gradient when gradient.HasValue:
                var g = gradient.Value;
                paint.Shader = SKShader.CreateLinearGradient(
                    g.StartPoint,
                    g.EndPoint,
                    g.Colors,
                    g.ColorPositions,
                    SKShaderTileMode.Clamp);
                break;

            case FillStyle.RadialGradient when gradient.HasValue:
                var rg = gradient.Value;
                float radius = Math.Max(bounds.Width, bounds.Height) / 2;
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(bounds.MidX, bounds.MidY),
                    radius,
                    rg.Colors,
                    rg.ColorPositions,
                    SKShaderTileMode.Clamp);
                break;
        }

        return paint;
    }


    // ==================================================================================
    //  HELPER: BORDER PAINT
    // ==================================================================================
    private static SKPaint CreateBorderPaint(BorderStyle style, SKColor color, float width)
    {
        return new SKPaint
        {
            Color = style == BorderStyle.InnerGlow ? color.WithAlpha(100) : color,
            StrokeWidth = width,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            PathEffect = style == BorderStyle.Dashed
                ? SKPathEffect.CreateDash(new[] { width * 2, width * 1.5f }, 0)
                : null
        };
    }


    // ==================================================================================
    //  HELPER: DRAW SHAPE
    // ==================================================================================
    private static void DrawRoundedShape(SKCanvas canvas, SKRect rect, float cornerRadius, SKPaint paint)
    {
        if (cornerRadius > 0)
            canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);
        else
            canvas.DrawRect(rect, paint);
    }


    // ==================================================================================
    //  IMAGES
    // ==================================================================================
    public static SKImage LoadPngFromEmbedded(string resourceName)
    {
        try
        {
            var uri = new Uri(resourceName, UriKind.Relative);
            var info = Application.GetResourceStream(uri);

            if (info != null)
            {
                using var s = info.Stream;
                return SKImage.FromEncodedData(s);
            }
        }
        catch { }

        return null!;
    }


    // ==================================================================================
    //  DRAW IMAGE
    // ==================================================================================
    public static void DrawImage(
        SKCanvas canvas,
        SKImage? image,
        SKRect destRect,
        float rotation = 0f,
        SKPoint? pivot = null,
        SKFilterQuality quality = SKFilterQuality.High)
    {
        if (image == null) return;

        using var paint = new SKPaint { IsAntialias = true, FilterQuality = quality };
        var p = pivot ?? new SKPoint(destRect.MidX, destRect.MidY);

        canvas.Save();
        canvas.Translate(p.X, p.Y);
        canvas.RotateDegrees(rotation);
        canvas.Translate(-p.X, -p.Y);

        canvas.DrawImage(image, destRect, paint);
        canvas.Restore();
    }


    // ==================================================================================
    //  ARROW (WPF Color)
    // ==================================================================================
    public static void DrawArrow(SKCanvas canvas, SKRect layout, Color arrowColor, bool arrowUp)
    {
        var skColor = arrowColor.ToSKColor();

        float size = 8f;
        float pad = 8f;

        float x = layout.Right - pad - size / 2;
        float y = layout.MidY;

        using var path = new SKPath();

        if (arrowUp)
        {
            path.MoveTo(x - size / 2, y + size / 3);
            path.LineTo(x, y - size / 3);
            path.LineTo(x + size / 2, y + size / 3);
        }
        else
        {
            path.MoveTo(x - size / 2, y - size / 3);
            path.LineTo(x, y + size / 3);
            path.LineTo(x + size / 2, y - size / 3);
        }

        using var paint = new SKPaint
        {
            Color = skColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawPath(path, paint);
    }
}


// ======================================================================================
//  ENUMS
// ======================================================================================
public enum FillStyle { None, Solid, Gradient, RadialGradient }
public enum BorderStyle { None, Solid, Dashed, InnerGlow }


// ======================================================================================
//  SHADOW
// ======================================================================================
public struct ShadowOptions
{
    public float Dx { get; set; }
    public float Dy { get; set; }
    public float Sigma { get; set; }
    public SKColor Color { get; set; }

    public static ShadowOptions Default => new()
    {
        Dx = 1,
        Dy = 1,
        Sigma = 1.5f,
        Color = SKColors.Black.WithAlpha(80)
    };
}


// ======================================================================================
//  GRADIENT OPTIONS
// ======================================================================================
public struct GradientOptions
{
    public SKColor[] Colors { get; set; }
    public float[]? ColorPositions { get; set; }
    public SKPoint StartPoint { get; set; }
    public SKPoint EndPoint { get; set; }
}


// ======================================================================================
//  SKIA HELPER
// ======================================================================================
public static class SkiaHelper
{
    public static SKColor WithBrightness(this SKColor color, float factor)
    {
        color.ToHsl(out float h, out float s, out float l);
        l = Math.Clamp(l * factor, 0, 1);
        return SKColor.FromHsl(h, s * 100, l * 100);
    }

    public static float[] ToHsl(this SKColor color)
    {
        color.ToHsl(out float h, out float s, out float l);
        return new[] { h, s, l };
    }

    public static void ToHsl(this SKColor color, out float h, out float s, out float l)
    {
        float r = color.Red / 255f;
        float g = color.Green / 255f;
        float b = color.Blue / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));

        h = 0f;
        l = (max + min) / 2f;

        if (max != min)
        {
            float d = max - min;
            s = l > 0.5f ? d / (2 - max - min) : d / (max + min);

            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;

            h /= 6;
        }
        else
        {
            s = 0;
        }

        h *= 360f;
    }
}
