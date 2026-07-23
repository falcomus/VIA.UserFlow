// ======================================================================================
// FILE: Mockup.Controls/CollapseButton.cs
//
// PURPOSE:
// - Small light-mode collapse / expand button for the mockup designer.
// - Visual style aligned with the modern button/input controls.
// - Supports hover / pressed feedback in LiveMode.
// - Renders a centered chevron icon.
//
// PROJECT: Mockup.Controls
// GROUP: Buttons [Misc]
//
// NOTES:
// - This is a small icon button, typically used for collapse / expand actions.
// - The control itself only renders state; action handling stays outside or via preview.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === COLLAPSE BUTTON ===

public enum CollapseButtonDirection
{
    Up,
    Down,
    Left,
    Right
}

[ControlType(displayName: "Collapse Button", group: "Icon Buttons")]
public partial class CollapseButton : DesignControl
{
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
    [property: System.ComponentModel.DisplayName("Icon Color")]
    private Color iconColor = Theme.Text;

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
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 4f;

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

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Size")]
    private float iconSize = 9f;

    partial void OnIconSizeChanged(float value)
    {
        iconSize = Math.Clamp(value, 5f, 18f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Inner Padding")]
    private float innerPadding = 6f;

    partial void OnInnerPaddingChanged(float value)
    {
        innerPadding = Math.Clamp(value, 1f, 12f);
    }

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Direction")]
    private CollapseButtonDirection direction = CollapseButtonDirection.Up;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    #endregion

    #region === CTOR ===

    public CollapseButton()
    {
        IsActionControl = true;

        Name = "CollapseButton";
        ResizeStyle = ResizeStyles.None;

        ExplicitePreviewHeight = 50f;
        ExplicitePreviewWidth = 50f;

        Width = 25f;
        Height = 30f;

        MinWidth = 25f;
        MinHeight = 30f;

        MaxWidth = 25f;
        MaxHeight = 30f;
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
        bool commitClick = _isPressed && isInside;

        _isPressed = false;
        _isHovered = isInside;

        if (commitClick)
        {
            Direction = Direction switch
            {
                CollapseButtonDirection.Up => CollapseButtonDirection.Down,
                CollapseButtonDirection.Down => CollapseButtonDirection.Up,
                CollapseButtonDirection.Left => CollapseButtonDirection.Right,
                CollapseButtonDirection.Right => CollapseButtonDirection.Left,
                _ => CollapseButtonDirection.Down
            };
        }

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
        DrawBackground(canvas, layout, ctx);
        DrawChevron(canvas, layout);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawBackground(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        Color fill = BackgroundColor;
        Color border = BorderColor;

        if (ctx.LiveMode && _isHovered)
        {
            fill = HoverBackgroundColor;
            border = border.Darken(0.04f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fill = PressedBackgroundColor;
            border = border.Darken(0.08f);
        }

        ShadowOptions shadow = Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 10f),
            fillStyle: FillStyle.Solid,
            fillColor: fill,
            borderColor: border,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadow,
            borderWidth: 0.85f
        );
    }

    private void DrawChevron(SKCanvas canvas, SKRect layout)
    {
        if (layout.Width < 10 || layout.Height < 10)
            return;

        float safePadding = Math.Clamp(InnerPadding, 1f, 12f);
        float safeSize = Math.Clamp(IconSize, 5f, Math.Min(layout.Width, layout.Height) - safePadding * 2f);

        float cx = layout.MidX;
        float cy = layout.MidY;

        using var paint = new SKPaint
        {
            Color = IconColor.ToSKColor().WithAlpha(190),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.6f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        using var path = new SKPath();

        switch (Direction)
        {
            case CollapseButtonDirection.Down:
                path.MoveTo(cx - safeSize * 0.5f, cy - safeSize * 0.2f);
                path.LineTo(cx, cy + safeSize * 0.35f);
                path.LineTo(cx + safeSize * 0.5f, cy - safeSize * 0.2f);
                break;

            case CollapseButtonDirection.Left:
                path.MoveTo(cx + safeSize * 0.2f, cy - safeSize * 0.5f);
                path.LineTo(cx - safeSize * 0.35f, cy);
                path.LineTo(cx + safeSize * 0.2f, cy + safeSize * 0.5f);
                break;

            case CollapseButtonDirection.Right:
                path.MoveTo(cx - safeSize * 0.2f, cy - safeSize * 0.5f);
                path.LineTo(cx + safeSize * 0.35f, cy);
                path.LineTo(cx - safeSize * 0.2f, cy + safeSize * 0.5f);
                break;

            default:
                path.MoveTo(cx - safeSize * 0.5f, cy + safeSize * 0.2f);
                path.LineTo(cx, cy - safeSize * 0.35f);
                path.LineTo(cx + safeSize * 0.5f, cy + safeSize * 0.2f);
                break;
        }

        canvas.DrawPath(path, paint);
    }

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

    #endregion
}

#endregion




//using Mockup.ColorSystem;
//using Mockup.Registry;
//using Mockup.Rendering;
//using SkiaSharp;
//using SkiaSharp.Views.WPF;

//namespace Mockup.Controls;

//[ControlType(displayName: "Collapse Button", group: "Buttons [Misc]")]
//public partial class CollapseButton : DesignControl
//{
//    private readonly float _arrowSize = 9f;
//    private readonly float _arrowPadding = 8f;

//    public CollapseButton()
//    {
//        Name = "CollapseButton";

//        ResizeStyle = ResizeStyles.None;

//        Width = 25;
//        Height = 30;

//        MinWidth = 25;
//        MinHeight = 30;

//        MaxWidth = 25;
//        MaxHeight = 30;
//    }

//    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
//    {
//        // Hintergrund zeichnen
//        //SkiaRenderer.DrawRaisedButton(
//        //    canvas: canvas,
//        //    rect: layout,
//        //    cornerRadius: Theme.CornerRadius,
//        //    baseColor: Theme.ControlBG,
//        //    borderColor: Theme.ControlBorder,
//        //    elevation: 1.0f);

//        // Pfeil zeichnen
//        DrawArrow(canvas, layout);
//    }

//    private void DrawArrow(SKCanvas canvas, SKRect layout)
//    {
//        float arrowX = layout.Right - _arrowPadding - _arrowSize / 2;
//        float arrowY = layout.MidY;

//        using var path = new SKPath();

//        // Aufwärtspfeil zeichnen
//        path.MoveTo(arrowX - _arrowSize / 2, arrowY + _arrowSize / 3);
//        path.LineTo(arrowX, arrowY - _arrowSize / 3);
//        path.LineTo(arrowX + _arrowSize / 2, arrowY + _arrowSize / 3);

//        using var paint = new SKPaint
//        {
//            Color = Theme.Text.ToSKColor(),
//            Style = SKPaintStyle.Fill,
//            IsAntialias = true
//        };

//        canvas.DrawPath(path, paint);
//    }

//}