// ======================================================================================
// FILE: Mockup.Controls/SocialLoginButtons.cs
//
// PURPOSE:
// - Social login buttons for mockup login forms.
// - Includes Google, Facebook, Microsoft, Apple, X, GitHub, LinkedIn, Amazon and Discord.
// - Renders the complete button surface, border, interaction states, text and provider icon.
// - Uses packaged PNG resources when available and built-in vector fallbacks otherwise.
// - Uses fully-qualified attribute namespaces to avoid cleanup removing usings.
//
// PROJECT: Mockup.Controls
// GROUP: Social Login
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Domain.Registry;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === SOCIAL BUTTON SHAPE ===

public enum SocialButtonShape
{
    RoundedRectangle,
    Circle,
    Square
}

#endregion

#region === SOCIAL LOGIN BUTTON BASE ===

public partial class SocialLoginButtonBase : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "Continue";

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Icon")]
    private ImageRef? icon = new("login_google", ImageFormat.Png);

    #endregion

    #region === APPEARANCE ===

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
    private Color textColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Hover Background")]
    private Color hoverBackgroundColor = Theme.ControlBG.Darken(0.03f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Pressed Background")]
    private Color pressedBackgroundColor = Theme.ControlBG.Darken(0.06f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Shape")]
    private SocialButtonShape shape = SocialButtonShape.RoundedRectangle;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 8f;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Elevation")]
    private int elevation = 0;

    partial void OnElevationChanged(int value)
    {
        elevation = Math.Clamp(value, 0, 5);
    }

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private double fontSize = 13d;

    partial void OnFontSizeChanged(double value)
    {
        fontSize = Math.Clamp(value, 8d, 32d);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Medium;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Show Text")]
    private bool showText = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(0);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Size")]
    private float iconSize = 48f;

    partial void OnIconSizeChanged(float value)
    {
        iconSize = Math.Clamp(value, 10f, 256f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Spacing")]
    private float iconSpacing = 0f;

    partial void OnIconSpacingChanged(float value)
    {
        iconSpacing = Math.Clamp(value, 0f, 24f);
    }

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    private static readonly ConcurrentDictionary<string, byte[]> EmbeddedPngBytes =
        new(StringComparer.OrdinalIgnoreCase);

    #endregion

    #region === CTOR ===

    public SocialLoginButtonBase()
    {
        IsActionControl = true;

        Name = "Social Login Button";
        ResizeStyle = ResizeStyles.KeepRatio;

        Width = 48f;
        Height = 48f;

        MinWidth = 24f;
        MinHeight = 24f;

        MaxWidth = 256f;
        MaxHeight = 256f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (!VisualRect.Contains(ctx.WorldPoint))
            return;

        _isPressed = true;
        _isHovered = true;
        InvalidateVisuals();
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool isInside = VisualRect.Contains(ctx.WorldPoint);

        if (_isHovered != isInside)
        {
            _isHovered = isInside;
            InvalidateVisuals();
        }

        if (isInside)
            Mouse.OverrideCursor = Cursors.Hand;

        if (!isInside && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool isInside = VisualRect.Contains(ctx.WorldPoint);
        _isPressed = false;
        _isHovered = isInside;
        InvalidateVisuals();
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        if (!IsUsableRect(layout))
            return;

        var (fillColor, resolvedBorderColor, resolvedTextColor) = GetVisualColors(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: GetSafeCornerRadius(layout),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: GetVisualShadow(ctx),
            borderWidth: 0.9f
        );

        SKRect contentRect = CreateContentRect(layout, Padding);
        if (!IsUsableRect(contentRect))
            return;

        bool hasIcon = Icon is not null && !string.IsNullOrWhiteSpace(Icon.Id);
        bool hasText = ShowText && !string.IsNullOrWhiteSpace(Text);

        if (!hasIcon && !hasText)
            return;

        if (!hasText)
        {
            float iconSize = Math.Min(
                GetSafeIconSize(),
                Math.Min(contentRect.Width, contentRect.Height) * 0.66f
            );

            SKRect iconRect = CreateCenteredSquare(contentRect, iconSize);
            DrawImage(canvas, iconRect, resolvedTextColor);
            return;
        }

        if (!hasIcon)
        {
            DrawText(canvas, contentRect, resolvedTextColor, System.Windows.TextAlignment.Center);
            return;
        }

        float resolvedIconSize = Math.Min(
            GetSafeIconSize(),
            Math.Min(contentRect.Height * 0.62f, contentRect.Width * 0.32f)
        );
        float spacing = GetSafeIconSpacing();
        float textWidth = TextRenderer.MeasureTextWidth(
            Text,
            FontSize,
            fontWeight: FontWeight
        );

        float groupWidth = resolvedIconSize + spacing + textWidth;
        float left = contentRect.Left + Math.Max(0f, (contentRect.Width - groupWidth) / 2f);

        SKRect imageRect = new(
            left,
            contentRect.MidY - resolvedIconSize / 2f,
            left + resolvedIconSize,
            contentRect.MidY + resolvedIconSize / 2f
        );

        SKRect textRect = new(
            imageRect.Right + spacing,
            contentRect.Top,
            contentRect.Right,
            contentRect.Bottom
        );

        if (IsUsableRect(imageRect))
            DrawImage(canvas, imageRect, resolvedTextColor);

        if (IsUsableRect(textRect))
            DrawText(canvas, textRect, resolvedTextColor, System.Windows.TextAlignment.Left);
    }

    private bool DrawImage(SKCanvas canvas, SKRect rect, Color fallbackColor)
    {
        if (Icon is null || string.IsNullOrWhiteSpace(Icon.Id))
            return false;

        string assetId = Icon.Id.Trim();

        var asset = Mockup.AssetSystem.AssetCatalog.TryGet(assetId);
        if (asset is not null)
        {
            Mockup.AssetSystem.ImageRenderer.Draw(canvas, asset, rect, null);
            return true;
        }

        if (TryDrawEmbeddedPng(canvas, rect, assetId))
            return true;

        return TryDrawBuiltInBrandIcon(canvas, rect, assetId, fallbackColor);
    }

    private static bool TryDrawEmbeddedPng(SKCanvas canvas, SKRect rect, string assetId)
    {
        byte[] bytes = EmbeddedPngBytes.GetOrAdd(
            assetId,
            static id => LoadEmbeddedPngBytes(id) ?? Array.Empty<byte>()
        );

        if (bytes.Length == 0)
            return false;

        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            return false;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };

        canvas.DrawBitmap(
            bitmap,
            new SKRect(0f, 0f, bitmap.Width, bitmap.Height),
            rect,
            paint
        );

        return true;
    }

    private static byte[]? LoadEmbeddedPngBytes(string assetId)
    {
        foreach (string assemblyName in new[] { "VIA.Mockup", "Mockup" })
        {
            try
            {
                Uri uri = new(
                    $"pack://application:,,,/{assemblyName};component/Resources/PNG/{assetId}.png",
                    UriKind.Absolute
                );

                var resource = Application.GetResourceStream(uri);
                if (resource?.Stream is null)
                    continue;

                using Stream stream = resource.Stream;
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return memory.ToArray();
            }
            catch
            {
                // Try the next known assembly name, then use vector fallback.
            }
        }

        return null;
    }

    private static bool TryDrawBuiltInBrandIcon(
        SKCanvas canvas,
        SKRect rect,
        string assetId,
        Color fallbackColor
    )
    {
        string key = assetId.Trim().ToLowerInvariant();

        switch (key)
        {
            case "login_google":
                DrawGoogleIcon(canvas, rect);
                return true;

            case "login_facebook":
                DrawCenteredBrandText(canvas, rect, "f", SKColors.White, 0.92f);
                return true;

            case "login_microsoft":
                DrawMicrosoftIcon(canvas, rect);
                return true;

            case "login_apple":
                DrawAppleIcon(canvas, rect, SKColors.White);
                return true;

            case "login_x":
                DrawXIcon(canvas, rect, SKColors.White);
                return true;

            case "login_github":
                DrawGitHubIcon(canvas, rect, SKColors.Black);
                return true;

            case "login_linkedin":
                DrawCenteredBrandText(canvas, rect, "in", SKColors.White, 0.58f);
                return true;

            case "login_amazon":
                DrawAmazonIcon(canvas, rect);
                return true;

            case "login_discord":
                DrawDiscordIcon(canvas, rect, SKColors.White);
                return true;

            default:
                DrawCenteredBrandText(
                    canvas,
                    rect,
                    GetFallbackInitial(assetId),
                    fallbackColor.ToSKColor(),
                    0.56f
                );
                return true;
        }
    }

    private static void DrawGoogleIcon(SKCanvas canvas, SKRect rect)
    {
        float inset = rect.Width * 0.12f;
        SKRect arcRect = new(
            rect.Left + inset,
            rect.Top + inset,
            rect.Right - inset,
            rect.Bottom - inset
        );

        float strokeWidth = Math.Max(2f, arcRect.Width * 0.18f);

        DrawArcSegment(canvas, arcRect, -45f, 105f, new SKColor(66, 133, 244), strokeWidth);
        DrawArcSegment(canvas, arcRect, 60f, 92f, new SKColor(52, 168, 83), strokeWidth);
        DrawArcSegment(canvas, arcRect, 152f, 96f, new SKColor(251, 188, 5), strokeWidth);
        DrawArcSegment(canvas, arcRect, 248f, 67f, new SKColor(234, 67, 53), strokeWidth);

        using var bluePaint = new SKPaint
        {
            Color = new SKColor(66, 133, 244),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            StrokeCap = SKStrokeCap.Square
        };

        canvas.DrawLine(
            arcRect.MidX,
            arcRect.MidY,
            arcRect.Right + strokeWidth * 0.08f,
            arcRect.MidY,
            bluePaint
        );
    }

    private static void DrawArcSegment(
        SKCanvas canvas,
        SKRect rect,
        float startAngle,
        float sweepAngle,
        SKColor color,
        float strokeWidth
    )
    {
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            StrokeCap = SKStrokeCap.Butt
        };

        canvas.DrawArc(rect, startAngle, sweepAngle, false, paint);
    }

    private static void DrawMicrosoftIcon(SKCanvas canvas, SKRect rect)
    {
        float size = Math.Min(rect.Width, rect.Height) * 0.80f;
        float gap = Math.Max(1f, size * 0.06f);
        float tile = (size - gap) / 2f;
        float left = rect.MidX - size / 2f;
        float top = rect.MidY - size / 2f;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        paint.Color = new SKColor(242, 80, 34);
        canvas.DrawRect(left, top, tile, tile, paint);

        paint.Color = new SKColor(127, 186, 0);
        canvas.DrawRect(left + tile + gap, top, tile, tile, paint);

        paint.Color = new SKColor(0, 164, 239);
        canvas.DrawRect(left, top + tile + gap, tile, tile, paint);

        paint.Color = new SKColor(255, 185, 0);
        canvas.DrawRect(left + tile + gap, top + tile + gap, tile, tile, paint);
    }

    private static void DrawAppleIcon(SKCanvas canvas, SKRect rect, SKColor color)
    {
        float size = Math.Min(rect.Width, rect.Height);
        float cx = rect.MidX;
        float cy = rect.MidY + size * 0.05f;

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawOval(
            new SKRect(
                cx - size * 0.29f,
                cy - size * 0.22f,
                cx + size * 0.03f,
                cy + size * 0.28f
            ),
            paint
        );

        canvas.DrawOval(
            new SKRect(
                cx - size * 0.02f,
                cy - size * 0.22f,
                cx + size * 0.30f,
                cy + size * 0.28f
            ),
            paint
        );

        canvas.Save();
        canvas.RotateDegrees(-30f, cx + size * 0.08f, cy - size * 0.36f);
        canvas.DrawOval(
            new SKRect(
                cx - size * 0.01f,
                cy - size * 0.46f,
                cx + size * 0.18f,
                cy - size * 0.29f
            ),
            paint
        );
        canvas.Restore();
    }

    private static void DrawXIcon(SKCanvas canvas, SKRect rect, SKColor color)
    {
        float inset = Math.Min(rect.Width, rect.Height) * 0.20f;
        float stroke = Math.Max(2f, Math.Min(rect.Width, rect.Height) * 0.105f);

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = stroke,
            StrokeCap = SKStrokeCap.Square
        };

        canvas.DrawLine(
            rect.Left + inset,
            rect.Top + inset,
            rect.Right - inset,
            rect.Bottom - inset,
            paint
        );

        paint.StrokeWidth = stroke * 0.54f;

        canvas.DrawLine(
            rect.Right - inset,
            rect.Top + inset,
            rect.Left + inset,
            rect.Bottom - inset,
            paint
        );
    }

    private static void DrawGitHubIcon(SKCanvas canvas, SKRect rect, SKColor color)
    {
        float size = Math.Min(rect.Width, rect.Height);
        float cx = rect.MidX;
        float cy = rect.MidY;

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawCircle(cx, cy - size * 0.02f, size * 0.31f, paint);

        using var earPath = new SKPath();
        earPath.MoveTo(cx - size * 0.28f, cy - size * 0.19f);
        earPath.LineTo(cx - size * 0.24f, cy - size * 0.42f);
        earPath.LineTo(cx - size * 0.08f, cy - size * 0.30f);
        earPath.Close();

        earPath.MoveTo(cx + size * 0.28f, cy - size * 0.19f);
        earPath.LineTo(cx + size * 0.24f, cy - size * 0.42f);
        earPath.LineTo(cx + size * 0.08f, cy - size * 0.30f);
        earPath.Close();

        canvas.DrawPath(earPath, paint);

        canvas.DrawOval(
            new SKRect(
                cx - size * 0.19f,
                cy + size * 0.18f,
                cx + size * 0.19f,
                cy + size * 0.46f
            ),
            paint
        );

        using var tailPaint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(2f, size * 0.09f),
            StrokeCap = SKStrokeCap.Round
        };

        using var tailPath = new SKPath();
        tailPath.MoveTo(cx + size * 0.13f, cy + size * 0.28f);
        tailPath.CubicTo(
            cx + size * 0.39f,
            cy + size * 0.23f,
            cx + size * 0.38f,
            cy + size * 0.48f,
            cx + size * 0.48f,
            cy + size * 0.39f
        );

        canvas.DrawPath(tailPath, tailPaint);
    }

    private static void DrawAmazonIcon(SKCanvas canvas, SKRect rect)
    {
        DrawCenteredBrandText(
            canvas,
            new SKRect(rect.Left, rect.Top - rect.Height * 0.08f, rect.Right, rect.Bottom),
            "a",
            SKColors.Black,
            0.70f
        );

        using var smilePaint = new SKPaint
        {
            Color = new SKColor(255, 153, 0),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(2f, rect.Height * 0.065f),
            StrokeCap = SKStrokeCap.Round
        };

        SKRect smileRect = new(
            rect.Left + rect.Width * 0.20f,
            rect.Top + rect.Height * 0.47f,
            rect.Right - rect.Width * 0.15f,
            rect.Bottom - rect.Height * 0.08f
        );

        canvas.DrawArc(smileRect, 15f, 145f, false, smilePaint);
    }

    private static void DrawDiscordIcon(SKCanvas canvas, SKRect rect, SKColor color)
    {
        float size = Math.Min(rect.Width, rect.Height);
        float cx = rect.MidX;
        float cy = rect.MidY;

        using var fill = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var path = new SKPath();
        path.MoveTo(cx - size * 0.34f, cy - size * 0.16f);
        path.CubicTo(
            cx - size * 0.20f,
            cy - size * 0.30f,
            cx + size * 0.20f,
            cy - size * 0.30f,
            cx + size * 0.34f,
            cy - size * 0.16f
        );
        path.LineTo(cx + size * 0.25f, cy + size * 0.25f);
        path.CubicTo(
            cx + size * 0.08f,
            cy + size * 0.38f,
            cx - size * 0.08f,
            cy + size * 0.38f,
            cx - size * 0.25f,
            cy + size * 0.25f
        );
        path.Close();

        canvas.DrawPath(path, fill);

        using var eyePaint = new SKPaint
        {
            Color = new SKColor(88, 101, 242),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawCircle(cx - size * 0.11f, cy + size * 0.01f, size * 0.055f, eyePaint);
        canvas.DrawCircle(cx + size * 0.11f, cy + size * 0.01f, size * 0.055f, eyePaint);
    }

    private static void DrawCenteredBrandText(
        SKCanvas canvas,
        SKRect rect,
        string text,
        SKColor color,
        float sizeRatio
    )
    {
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = Math.Min(rect.Width, rect.Height) * sizeRatio,
            Typeface = SKTypeface.FromFamilyName(
                "Segoe UI",
                SKFontStyle.Bold
            )
        };

        SKFontMetrics metrics = paint.FontMetrics;
        float baseline = rect.MidY - (metrics.Ascent + metrics.Descent) / 2f;

        canvas.DrawText(text, rect.MidX, baseline, paint);
    }

    private static string GetFallbackInitial(string assetId)
    {
        string value = assetId
            .Replace("login_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return string.IsNullOrWhiteSpace(value)
            ? "?"
            : value[..1].ToUpperInvariant();
    }

    #endregion

    #region === HELPERS ===

    private void ResetInteractionState()
    {
        bool changed = false;

        if (_isHovered)
        {
            _isHovered = false;
            changed = true;
        }

        if (_isPressed)
        {
            _isPressed = false;
            changed = true;
        }

        if (changed)
            InvalidateVisuals();

        Mouse.OverrideCursor = null;
    }

    private void InvalidateVisuals()
    {
        MSG.UI.InvalidateDesigner();
    }

    private (Color FillColor, Color BorderColor, Color TextColor) GetVisualColors(
        RenderContext ctx
    )
    {
        Color fillColor = BackgroundColor;
        Color resolvedBorderColor = BorderColor;
        Color resolvedTextColor = TextColor;

        if (ctx.LiveMode && _isHovered)
        {
            fillColor = HoverBackgroundColor;
            resolvedBorderColor = resolvedBorderColor.Darken(0.04f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fillColor = PressedBackgroundColor;
            resolvedBorderColor = resolvedBorderColor.Darken(0.08f);
        }

        return (fillColor, resolvedBorderColor, resolvedTextColor);
    }

    private ShadowOptions GetVisualShadow(RenderContext ctx)
    {
        int safeElevation = Math.Clamp(Elevation, 0, 5);

        if (safeElevation <= 0)
            return ShadowOptions.Default;

        if (ctx.LiveMode && _isPressed)
            return GetElevation(Math.Max(0, safeElevation - 1));

        return GetElevation(safeElevation);
    }

    private float GetSafeCornerRadius(SKRect layout)
    {
        float maximumRadius = Math.Min(layout.Width, layout.Height) / 2f;

        return Shape switch
        {
            SocialButtonShape.Circle => maximumRadius,
            SocialButtonShape.Square => 0f,
            _ => Math.Clamp(CornerRadius, 0f, maximumRadius)
        };
    }

    private float GetSafeIconSize()
    {
        return Math.Clamp(IconSize, 10f, 256f);
    }

    private float GetSafeIconSpacing()
    {
        return Math.Clamp(IconSpacing, 0f, 24f);
    }

    private static SKRect CreateContentRect(SKRect layout, Thickness padding)
    {
        return new SKRect(
            layout.Left + (float)padding.Left,
            layout.Top + (float)padding.Top,
            layout.Right - (float)padding.Right,
            layout.Bottom - (float)padding.Bottom
        );
    }

    private static SKRect CreateCenteredSquare(SKRect bounds, float size)
    {
        float safeSize = Math.Max(1f, size);

        return new SKRect(
            bounds.MidX - safeSize / 2f,
            bounds.MidY - safeSize / 2f,
            bounds.MidX + safeSize / 2f,
            bounds.MidY + safeSize / 2f
        );
    }

    private static bool IsUsableRect(SKRect rect)
    {
        return rect.Width > 1f && rect.Height > 1f;
    }

    private void DrawText(
        SKCanvas canvas,
        SKRect bounds,
        Color color,
        System.Windows.TextAlignment alignment
    )
    {
        TextRenderer.Draw2(
            canvas: canvas,
            text: Text,
            bounds: bounds,
            fontSize: FontSize,
            color: color,
            padding: new Thickness(0),
            fontWeight: FontWeight,
            textAlignment: alignment
        );
    }

    #endregion
}

#endregion

#region === SOCIAL LOGIN BUTTONS ===

[ControlType(displayName: "Google Login Button", group: "Social Login")]
public sealed class GoogleLoginButton : SocialLoginButtonBase
{
    public GoogleLoginButton()
    {
        Name = "Google Login Button";
        Text = "Continue with Google";
        Icon = new ImageRef("login_google", ImageFormat.Png);
        BackgroundColor = Colors.White;
        BorderColor = Theme.ControlBorder;
        TextColor = Theme.Text;
        HoverBackgroundColor = Color.FromRgb(245, 245, 245);
        PressedBackgroundColor = Color.FromRgb(235, 235, 235);
    }
}

[ControlType(displayName: "Facebook Login Button", group: "Social Login")]
public sealed class FacebookLoginButton : SocialLoginButtonBase
{
    public FacebookLoginButton()
    {
        Name = "Facebook Login Button";
        Text = "Continue with Facebook";
        Icon = new ImageRef("login_facebook", ImageFormat.Png);
        BackgroundColor = Color.FromRgb(24, 119, 242);
        BorderColor = Color.FromRgb(24, 119, 242).Darken(0.10f);
        TextColor = Colors.White;
        HoverBackgroundColor = Color.FromRgb(30, 126, 250);
        PressedBackgroundColor = Color.FromRgb(18, 104, 220);
    }
}

[ControlType(displayName: "Microsoft Login Button", group: "Social Login")]
public sealed class MicrosoftLoginButton : SocialLoginButtonBase
{
    public MicrosoftLoginButton()
    {
        Name = "Microsoft Login Button";
        Text = "Continue with Microsoft";
        Icon = new ImageRef("login_microsoft", ImageFormat.Png);
        BackgroundColor = Colors.White;
        BorderColor = Theme.ControlBorder;
        TextColor = Theme.Text;
        HoverBackgroundColor = Color.FromRgb(245, 245, 245);
        PressedBackgroundColor = Color.FromRgb(235, 235, 235);
    }
}

[ControlType(displayName: "Apple Login Button", group: "Social Login")]
public sealed class AppleLoginButton : SocialLoginButtonBase
{
    public AppleLoginButton()
    {
        Name = "Apple Login Button";
        Text = "Continue with Apple";
        Icon = new ImageRef("login_apple", ImageFormat.Png);
        BackgroundColor = Colors.Black;
        BorderColor = Colors.Black;
        TextColor = Colors.White;
        HoverBackgroundColor = Color.FromRgb(30, 30, 30);
        PressedBackgroundColor = Color.FromRgb(15, 15, 15);
    }
}

[ControlType(displayName: "X Login Button", group: "Social Login")]
public sealed class XLoginButton : SocialLoginButtonBase
{
    public XLoginButton()
    {
        Name = "X Login Button";
        Text = "Continue with X";
        Icon = new ImageRef("login_x", ImageFormat.Png);
        BackgroundColor = Colors.Black;
        BorderColor = Colors.Black;
        TextColor = Colors.White;
        HoverBackgroundColor = Color.FromRgb(30, 30, 30);
        PressedBackgroundColor = Color.FromRgb(15, 15, 15);
    }
}

[ControlType(displayName: "GitHub Login Button", group: "Social Login")]
public sealed class GitHubLoginButton : SocialLoginButtonBase
{
    public GitHubLoginButton()
    {
        Name = "GitHub Login Button";
        Text = "Continue with GitHub";
        Icon = new ImageRef("login_github", ImageFormat.Png);
        BackgroundColor = Colors.White;
        BorderColor = Theme.ControlBorder;
        TextColor = Theme.Text;
        HoverBackgroundColor = Color.FromRgb(245, 245, 245);
        PressedBackgroundColor = Color.FromRgb(235, 235, 235);
    }
}

[ControlType(displayName: "LinkedIn Login Button", group: "Social Login")]
public sealed class LinkedInLoginButton : SocialLoginButtonBase
{
    public LinkedInLoginButton()
    {
        Name = "LinkedIn Login Button";
        Text = "Continue with LinkedIn";
        Icon = new ImageRef("login_linkedin", ImageFormat.Png);
        BackgroundColor = Color.FromRgb(10, 102, 194);
        BorderColor = Color.FromRgb(10, 102, 194).Darken(0.10f);
        TextColor = Colors.White;
        HoverBackgroundColor = Color.FromRgb(17, 112, 205);
        PressedBackgroundColor = Color.FromRgb(8, 87, 166);
    }
}

[ControlType(displayName: "Amazon Login Button", group: "Social Login")]
public sealed class AmazonLoginButton : SocialLoginButtonBase
{
    public AmazonLoginButton()
    {
        Name = "Amazon Login Button";
        Text = "Continue with Amazon";
        Icon = new ImageRef("login_amazon", ImageFormat.Png);
        BackgroundColor = Colors.White;
        BorderColor = Theme.ControlBorder;
        TextColor = Theme.Text;
        HoverBackgroundColor = Color.FromRgb(245, 245, 245);
        PressedBackgroundColor = Color.FromRgb(235, 235, 235);
    }
}

[ControlType(displayName: "Discord Login Button", group: "Social Login")]
public sealed class DiscordLoginButton : SocialLoginButtonBase
{
    public DiscordLoginButton()
    {
        Name = "Discord Login Button";
        Text = "Continue with Discord";
        Icon = new ImageRef("login_discord", ImageFormat.Png);
        BackgroundColor = Color.FromRgb(88, 101, 242);
        BorderColor = Color.FromRgb(88, 101, 242).Darken(0.10f);
        TextColor = Colors.White;
        HoverBackgroundColor = Color.FromRgb(101, 113, 246);
        PressedBackgroundColor = Color.FromRgb(73, 84, 213);
    }
}

#endregion
