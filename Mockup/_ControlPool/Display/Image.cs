// ======================================================================================
// FILE: Mockup.Controls/Image.cs
//
// PURPOSE:
// - Unified image control for the mockup designer.
// - Supports SVG and PNG assets through ImageRef.
// - SVG can be tinted via Image Color; PNG is rendered as-is.
// - Optional circular framing for avatar / icon-style presentation.
//
// PROJECT: Mockup.Controls
// GROUP: Media
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Domain.Registry;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.Windows.Media;

namespace Mockup.Controls;

#region === IMAGE SHAPE ===

public enum ImageShape
{
    None,
    Circle
}

#endregion

#region === IMAGE ===

[ControlType("image", "Image", "Content")]
public partial class Image : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Image")]
    private ImageRef? imageRef = new("smiley", ImageFormat.Svg);

    partial void OnImageRefChanged(ImageRef? value)
    {
        var oldWidth = Width;
        var oldHeight = Height;

        switch (value?.Format)
        {
            case ImageFormat.Svg:
                Width = oldWidth;
                Height = oldWidth;
                ResizeStyle = ResizeStyles.KeepRatio;
                break;

            case ImageFormat.Png:
                Width = oldWidth;
                Height = oldHeight;
                ResizeStyle = ResizeStyles.ResizeAll;
                break;
        }
    }

    #endregion

    #region === APPEARANCE ===

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
    [property: System.ComponentModel.DisplayName("Border Width")]
    private float borderWidth = 2;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Icon Color")]
    private Color imageColor = Colors.Black;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Shape")]
    private ImageShape shape = ImageShape.None;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Frame Margin")]
    private float frameMargin = 0f;

    partial void OnFrameMarginChanged(float value)
    {
        frameMargin = Math.Clamp(value, 0f, 200f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Content Margin")]
    private float contentMargin = 0f;

    partial void OnContentMarginChanged(float value)
    {
        contentMargin = Math.Clamp(value, 0f, 200f);
    }

    #endregion

    #region === CTOR ===

    public Image()
    {
        Name = "Image";

        ResizeStyle = ResizeStyles.KeepRatio;

        Width = 60f;
        Height = 60f;

        MinWidth = 30f;
        MinHeight = 30f;

        MaxWidth = 5000f;
        MaxHeight = 5000f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        bool inCircle = Shape == ImageShape.Circle;

        SkiaRenderer.RenderSVGIcon(
            canvas,
            layout,
            ImageRef,
            BackgroundColor,
            BorderColor,
            ImageColor,
            BorderWidth,
            inCircle,
            FrameMargin,
            ContentMargin
        );
    }

    #endregion

    #region === HIT TEST ===

    public override bool HitTest(SKPoint p) => VisualRect.Contains(p);

    #endregion
}

#endregion
