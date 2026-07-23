// ======================================================================================
// FILE: Mockup.Controls/Avatar.cs
//
// PURPOSE:
// - Avatar control for the mockup designer.
// - Supports image, initials and fallback icon.
// - Supports circle / rounded square shape, status dot and size presets.
//
// PROJECT: Mockup.Controls
// GROUP: Display
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Domain.Registry;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Media;

namespace Mockup.Controls;

#region === ENUMS ===

public enum AvatarShape
{
    Circle,
    RoundedSquare
}

public enum AvatarSizePreset
{
    Small,
    Normal,
    Large
}

#endregion

#region === AVATAR ===

[ControlType(displayName: "Avatar", group: "Content")]
public partial class Avatar : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Icon")]
    [property: System.ComponentModel.DisplayName("Image")]
    private ImageRef? image;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Initials")]
    private string initials = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Icon")]
    [property: System.ComponentModel.DisplayName("Fallback Icon")]
    private ImageRef? fallbackIcon = new("user", ImageFormat.Svg);

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Shape")]
    private AvatarShape shape = AvatarShape.Circle;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Size")]
    private AvatarSizePreset sizePreset = AvatarSizePreset.Normal;

    partial void OnSizePresetChanged(AvatarSizePreset value)
    {
        ApplySizePreset(value);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Background Color")]
    private Color backgroundColor = Theme.ControlBG;

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
    [property: System.ComponentModel.DisplayName("Icon Color")]
    private Color iconColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Width")]
    private float borderWidth = 1f;

    partial void OnBorderWidthChanged(float value)
    {
        borderWidth = Math.Clamp(value, 0f, 8f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 8f;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 60f);
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

    #region === STATUS ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Status")]
    private bool showStatus = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Status")]
    [property: System.ComponentModel.DisplayName("Status Color")]
    private Color statusColor = Theme.Success;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Status")]
    [property: System.ComponentModel.DisplayName("Status Size")]
    private float statusSize = 10f;

    partial void OnStatusSizeChanged(float value)
    {
        statusSize = Math.Clamp(value, 4f, 24f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Status")]
    [property: System.ComponentModel.DisplayName("Status Border Color")]
    private Color statusBorderColor = Colors.White;

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private float fontSize = 14f;

    partial void OnFontSizeChanged(float value)
    {
        fontSize = Math.Clamp(value, 6f, 48f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.SemiBold;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Inner Padding")]
    private float innerPadding = 4f;

    partial void OnInnerPaddingChanged(float value)
    {
        innerPadding = Math.Clamp(value, 0f, 20f);
    }

    #endregion

    #region === CTOR ===

    public Avatar()
    {
        Name = "Avatar";
        ResizeStyle = ResizeStyles.ResizeAll;

        ApplySizePreset(SizePreset);

        ExplicitePreviewWidth = 50f;
        ExplicitePreviewHeight = 50f;

        MinWidth = 20f;
        MinHeight = 20f;

        MaxWidth = 240f;
        MaxHeight = 240f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        DrawBackground(canvas, layout);
        DrawContent(canvas, layout);
        DrawStatus(canvas, layout);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawBackground(SKCanvas canvas, SKRect layout)
    {
        ShadowOptions shadow = Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default;

        float radius = Shape == AvatarShape.Circle
            ? MathF.Min(layout.Width, layout.Height) / 2f
            : Math.Clamp(CornerRadius, 0f, Math.Min(layout.Width, layout.Height) / 2f);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: radius,
            fillStyle: FillStyle.Solid,
            fillColor: BackgroundColor,
            borderStyle: BorderStyle.Solid,
            borderColor: BorderColor,
            borderWidth: BorderWidth,
            shadowOptions: shadow
        );
    }

    private void DrawContent(SKCanvas canvas, SKRect layout)
    {
        float pad = Math.Clamp(InnerPadding, 0f, Math.Min(layout.Width, layout.Height) / 4f);

        var contentRect = new SKRect(
            layout.Left + pad,
            layout.Top + pad,
            layout.Right - pad,
            layout.Bottom - pad
        );

        if (contentRect.Width <= 2f || contentRect.Height <= 2f)
            return;

        if (Image != null && !string.IsNullOrWhiteSpace(Image.Id))
        {
            DrawImage(canvas, contentRect, Image);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Initials))
        {
            DrawInitials(canvas, contentRect);
            return;
        }

        if (FallbackIcon != null && !string.IsNullOrWhiteSpace(FallbackIcon.Id))
        {
            DrawIcon(canvas, contentRect, FallbackIcon, IconColor);
        }
    }

    private void DrawImage(SKCanvas canvas, SKRect rect, ImageRef imageRef)
    {
        SkiaRenderer.RenderSVGIcon(
            canvas,
            rect,
            imageRef,
            Colors.Transparent,
            Colors.Transparent,
            Colors.Transparent,
            1,
            false,
            0,
            0
        );
    }

    private void DrawInitials(SKCanvas canvas, SKRect rect)
    {
        TextRenderer.Draw2(
            canvas: canvas,
            text: Initials,
            bounds: rect,
            fontSize: FontSize,
            color: TextColor,
            fontWeight: FontWeight,
            padding: new System.Windows.Thickness(0),
            textAlignment: System.Windows.TextAlignment.Center
        );
    }

    private void DrawIcon(SKCanvas canvas, SKRect rect, ImageRef imageRef, Color color)
    {
        float size = Math.Min(rect.Width, rect.Height) * 0.62f;
        float left = rect.MidX - size / 2f;
        float top = rect.MidY - size / 2f;

        var iconRect = new SKRect(left, top, left + size, top + size);

        SkiaRenderer.RenderSVGIcon(
            canvas,
            iconRect,
            imageRef,
            Colors.Transparent,
            Colors.Transparent,
            color,
            1,
            false,
            0,
            0
        );
    }

    private void DrawStatus(SKCanvas canvas, SKRect layout)
    {
        if (!ShowStatus)
            return;

        float dot = Math.Clamp(StatusSize, 4f, Math.Min(layout.Width, layout.Height) * 0.45f);
        float cx = layout.Right - dot * 0.7f;
        float cy = layout.Bottom - dot * 0.7f;
        float r = dot / 2f;

        using var fill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = StatusColor.ToSKColor()
        };

        using var stroke = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = StatusBorderColor.ToSKColor()
        };

        canvas.DrawCircle(cx, cy, r, fill);
        canvas.DrawCircle(cx, cy, r, stroke);
    }

    private void ApplySizePreset(AvatarSizePreset preset)
    {
        switch (preset)
        {
            case AvatarSizePreset.Small:
                Width = 28f;
                Height = 28f;
                FontSize = 11f;
                StatusSize = 8f;
                InnerPadding = 3f;
                break;

            case AvatarSizePreset.Large:
                Width = 56f;
                Height = 56f;
                FontSize = 20f;
                StatusSize = 12f;
                InnerPadding = 5f;
                break;

            default:
                Width = 40f;
                Height = 40f;
                FontSize = 14f;
                StatusSize = 10f;
                InnerPadding = 4f;
                break;
        }
    }

    #endregion
}

#endregion