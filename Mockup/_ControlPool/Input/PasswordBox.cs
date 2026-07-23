// ======================================================================================
// FILE: Mockup.Controls/PasswordBox.cs
//
// PURPOSE:
// - Modern PasswordBox control for the mockup designer.
// - Visual style aligned with TextBox / ComboBox / SearchBox controls.
// - Supports optional title, placeholder, left lock icon and right reveal button.
// - Compact light-mode style with hover feedback in LiveMode.
//
// PROJECT: Mockup.Controls
// GROUP: Input
//
// NOTES:
// - This is a visual mockup control, not a real editable input.
// - The control itself does not edit text in preview; it only renders state.
// - The reveal button toggles masked / plain password display in LiveMode.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === PASSWORD BOX ===

[ControlType(displayName: "Password Box", group: "Input Fields")]
public partial class PasswordBox : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Title")]
    private string title = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Password")]
    private string password = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Placeholder")]
    private string placeholder = "Enter password";

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
    [property: System.ComponentModel.DisplayName("Placeholder Color")]
    private Color placeholderColor = Theme.Text.Lighten(0.45f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Title Color")]
    private Color titleColor = Theme.Text.Lighten(0.20f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Icon Color")]
    private Color iconColor = Theme.Text.Lighten(0.10f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Reveal Text Color")]
    private Color revealTextColor = Theme.Text.Lighten(0.05f);

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

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private double fontSize = 13.5d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private double titleFontSize = 13d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Reveal Font Size")]
    private double revealFontSize = 13d;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Size")]
    private ButtonSizePreset sizePreset = ButtonSizePreset.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(10, 0, 6, 0);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Spacing")]
    private float iconSpacing = 8f;

    partial void OnIconSpacingChanged(float value)
    {
        iconSpacing = Math.Clamp(value, 2f, 20f);
    }

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Left Icon")]
    private bool showLeftIcon = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Reveal Button")]
    private bool showRevealButton = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Reveal Password")]
    private bool revealPassword;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _hoverRevealButton;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    [JsonIgnore, Browsable(false)]
    private SKRect _headerRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _revealButtonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _iconRect;

    [JsonIgnore, Browsable(false)]
    private SKBitmap? _lockBitmap;

    [JsonIgnore, Browsable(false)]
    private bool _lockBitmapLoadAttempted;

    private const float TitleGap = 2f;

    private const string LockPngAbsolutePath =
        @"C:\VIA_DEVELOPMENT\#PROJECTS 2026\UserFlow\UserFlow\Mockup\Resources\PNG\lock.png";

    #endregion

    #region === CTOR ===

    public PasswordBox()
    {
        IsActionControl = true;

        Name = "PasswordBox";
        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 180f;
        Height = 30f;

        MinWidth = 110f;
        MinHeight = 26f;

        MaxWidth = 600f;
        MaxHeight = 120f;

        ApplySizePreset(SizePreset);
        RecalculateOverallHeight();
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === PROPERTY REACTIONS ===

    partial void OnSizePresetChanged(ButtonSizePreset value)
    {
        ApplySizePreset(value);
        RecalculateOverallHeight();
    }

    partial void OnTitleChanged(string value)
    {
        RecalculateOverallHeight();
        InvalidateVisuals();
    }

    partial void OnShowTitleChanged(bool value)
    {
        RecalculateOverallHeight();
        InvalidateVisuals();
    }

    partial void OnTitleFontSizeChanged(double value)
    {
        RecalculateOverallHeight();
        InvalidateVisuals();
    }

    partial void OnPasswordChanged(string value)
    {
        InvalidateVisuals();
    }

    partial void OnPlaceholderChanged(string value)
    {
        InvalidateVisuals();
    }

    partial void OnRevealPasswordChanged(bool value)
    {
        InvalidateVisuals();
    }

    partial void OnShowRevealButtonChanged(bool value)
    {
        InvalidateVisuals();
    }

    partial void OnShowLeftIconChanged(bool value)
    {
        InvalidateVisuals();
    }

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (!_headerRect.Contains(ctx.WorldPoint))
            return;

        _isPressed = true;
        _isHovered = true;
        _hoverRevealButton = CanShowRevealButton() && _revealButtonRect.Contains(ctx.WorldPoint);
        InvalidateVisuals();
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool hoverReveal = CanShowRevealButton() && _revealButtonRect.Contains(ctx.WorldPoint);

        if (_isHovered != insideHeader)
        {
            _isHovered = insideHeader;
            InvalidateVisuals();
        }

        if (_hoverRevealButton != hoverReveal)
        {
            _hoverRevealButton = hoverReveal;
            InvalidateVisuals();
        }

        if (hoverReveal)
            Mouse.OverrideCursor = Cursors.Hand;
        else if (insideHeader)
            Mouse.OverrideCursor = Cursors.IBeam;
        else
            Mouse.OverrideCursor = null;

        if (!insideHeader && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool commitClick = _isPressed && insideHeader;
        bool hitReveal = CanShowRevealButton() && _revealButtonRect.Contains(ctx.WorldPoint);

        _isPressed = false;
        _isHovered = insideHeader;
        _hoverRevealButton = hitReveal;

        if (commitClick && hitReveal)
            RevealPassword = !RevealPassword;

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
        bool hasTitle = HasVisibleTitle();
        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
        float titleGap = hasTitle ? TitleGap : 0f;
        float headerHeight = GetHeaderRowHeight();

        var titleRect = hasTitle
            ? new SKRect(layout.Left, layout.Top, layout.Right, layout.Top + titleHeight)
            : SKRect.Empty;

        _headerRect = new SKRect(
            layout.Left,
            layout.Top + titleHeight + titleGap,
            layout.Right,
            layout.Top + titleHeight + titleGap + headerHeight
        );

        BuildInteractiveRects(_headerRect);

        DrawHeader(canvas, titleRect, _headerRect, ctx, hasTitle);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawHeader(SKCanvas canvas, SKRect titleRect, SKRect headerRect, RenderContext ctx, bool hasTitle)
    {
        var (fillColor, resolvedBorderColor) = GetHeaderVisualColors(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: headerRect,
            cornerRadius: GetSafeCornerRadius(),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: GetVisualShadow(ctx),
            borderWidth: 0.85f
        );

        if (hasTitle)
        {
            TextRenderer.Draw2(
                canvas: canvas,
                text: Title,
                bounds: titleRect,
                fontSize: TitleFontSize,
                color: TitleColor,
                padding: new Thickness(0),
                fontWeight: FontWeights.Normal,
                textAlignment: System.Windows.TextAlignment.Left
            );
        }

        DrawLockIcon(canvas);

        if (CanShowRevealButton())
            DrawRevealButton(canvas, ctx);

        float contentLeft = ShowLeftIcon
            ? _iconRect.Right + IconSpacing
            : headerRect.Left + (float)Padding.Left;

        float contentRight = CanShowRevealButton()
            ? _revealButtonRect.Left - IconSpacing
            : headerRect.Right - (float)Padding.Right;

        if (contentRight < contentLeft + 8f)
            contentRight = contentLeft + 8f;

        var contentRect = new SKRect(
            contentLeft,
            headerRect.Top,
            contentRight,
            headerRect.Bottom
        );

        contentRect.Offset(-10, 0);

        bool hasPassword = !string.IsNullOrWhiteSpace(Password);
        string displayText = hasPassword
            ? GetDisplayedPassword()
            : Placeholder;

        Color displayColor = hasPassword
            ? TextColor
            : PlaceholderColor;

        TextRenderer.Draw2(
            canvas: canvas,
            text: displayText,
            bounds: contentRect,
            fontSize: FontSize,
            color: displayColor,
            padding: new Thickness(0),
            fontWeight: FontWeight,
            textAlignment: System.Windows.TextAlignment.Left
        );
    }

    private void DrawLockIcon(SKCanvas canvas)
    {
        if (!ShowLeftIcon)
            return;

        if (TryEnsureLockBitmap())
        {
            DrawLockBitmap(canvas);
            return;
        }

        DrawLockVectorFallback(canvas);
    }

    private void DrawLockBitmap(SKCanvas canvas)
    {
        if (_lockBitmap == null)
            return;


        var targetRect = GetAspectFitRect(
            sourceWidth: _lockBitmap.Width,
            sourceHeight: _lockBitmap.Height,
            bounds: _iconRect,
            inset: 1f
        );

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };

        targetRect.Offset(-8, -1);

        targetRect.Inflate(-2, -4);

        canvas.DrawBitmap(_lockBitmap, targetRect, paint);
    }

    private void DrawLockVectorFallback(SKCanvas canvas)
    {
        var strokeColor = IconColor.ToSKColor().WithAlpha(185);
        var fillColor = IconColor.ToSKColor().WithAlpha(28);

        float cx = _iconRect.MidX;
        float bodyWidth = _iconRect.Width * 0.62f;
        float bodyHeight = _iconRect.Height * 0.42f;
        float bodyLeft = cx - bodyWidth * 0.5f;
        float bodyTop = _iconRect.Bottom - bodyHeight - _iconRect.Height * 0.10f;

        var bodyRect = new SKRect(
            bodyLeft,
            bodyTop,
            bodyLeft + bodyWidth,
            bodyTop + bodyHeight
        );

        using var fillPaint = new SKPaint
        {
            Color = fillColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var strokePaint = new SKPaint
        {
            Color = strokeColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.35f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        canvas.DrawRoundRect(bodyRect, 2.5f, 2.5f, fillPaint);
        canvas.DrawRoundRect(bodyRect, 2.5f, 2.5f, strokePaint);

        float shackleWidth = bodyWidth * 0.72f;
        float shackleHeight = _iconRect.Height * 0.42f;
        float shackleLeft = cx - shackleWidth * 0.5f;
        float shackleTop = _iconRect.Top + _iconRect.Height * 0.10f;

        var shackleRect = new SKRect(
            shackleLeft,
            shackleTop,
            shackleLeft + shackleWidth,
            shackleTop + shackleHeight
        );

        canvas.DrawArc(shackleRect, 180f, 180f, false, strokePaint);

        float legTop = shackleTop + shackleHeight * 0.5f;
        canvas.DrawLine(
            shackleRect.Left,
            legTop,
            shackleRect.Left,
            bodyRect.Top + 0.8f,
            strokePaint
        );

        canvas.DrawLine(
            shackleRect.Right,
            legTop,
            shackleRect.Right,
            bodyRect.Top + 0.8f,
            strokePaint
        );

        using var keyholePaint = new SKPaint
        {
            Color = strokeColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float keyholeRadius = Math.Max(1.2f, bodyRect.Width * 0.08f);
        float keyholeCx = bodyRect.MidX;
        float keyholeCy = bodyRect.Top + bodyRect.Height * 0.40f;

        canvas.DrawCircle(keyholeCx, keyholeCy, keyholeRadius, keyholePaint);
        canvas.DrawRoundRect(
            new SKRect(
                keyholeCx - keyholeRadius * 0.45f,
                keyholeCy,
                keyholeCx + keyholeRadius * 0.45f,
                keyholeCy + keyholeRadius * 2.1f
            ),
            0.8f,
            0.8f,
            keyholePaint
        );
    }

    private void DrawRevealButton(SKCanvas canvas, RenderContext ctx)
    {
        if (!CanShowRevealButton())
            return;

        Color fill = Color.FromRgb(225, 226, 228);
        Color border = Colors.Transparent;

        if (RevealPassword)
        {
            fill = Color.FromRgb(120, 200, 240);
            border = BorderColor.Darken(0.04f);
        }

        if (_hoverRevealButton && ctx.LiveMode)
        {
            //fill = Color.FromArgb(42, BorderColor.R, BorderColor.G, BorderColor.B);
            fill = Color.FromRgb(245, 246, 248);
            border = BorderColor.Darken(0.08f);
        }

        _revealButtonRect.Offset(2, 0);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: _revealButtonRect,
            cornerRadius: Math.Max(3f, GetSafeCornerRadius() - 1f),
            fillStyle: FillStyle.Solid,
            fillColor: fill,
            borderColor: border,
            borderStyle: border == Colors.Transparent ? BorderStyle.None : BorderStyle.Solid,
            shadowOptions: ShadowOptions.Default,
            borderWidth: border == Colors.Transparent ? 0f : 0.8f
        );

        TextRenderer.Draw2(
            canvas: canvas,
            text: RevealPassword ? "Hide" : "Show",
            bounds: _revealButtonRect,
            fontSize: RevealFontSize - 1,
            color: RevealTextColor,
            padding: new Thickness(0),
            fontWeight: FontWeights.SemiBold,
            textAlignment: System.Windows.TextAlignment.Center
        );
    }

    private void BuildInteractiveRects(SKRect headerRect)
    {
        float insetY = 3f;
        float iconSide = Math.Max(10f, headerRect.Height - insetY * 2f);
        float left = headerRect.Left + (float)Padding.Left;

        _iconRect = new SKRect(
            left,
            headerRect.Top + insetY,
            left + iconSide,
            headerRect.Bottom - insetY
        );

        float buttonHeight = Math.Max(14f, headerRect.Height - 8f);
        float buttonWidth = GetRevealButtonWidth();
        float buttonRight = headerRect.Right - (float)Padding.Right;
        float buttonTop = headerRect.MidY - buttonHeight * 0.5f;

        _revealButtonRect = new SKRect(
            buttonRight - buttonWidth,
            buttonTop,
            buttonRight,
            buttonTop + buttonHeight
        );
    }

    #endregion

    #region === HELPERS ===

    private void ApplySizePreset(ButtonSizePreset preset)
    {
        if (_applyingSizePreset)
            return;

        _applyingSizePreset = true;

        try
        {
            switch (preset)
            {
                case ButtonSizePreset.Small:
                    Height = 26f;
                    MinHeight = 26f;
                    FontSize = 12d;
                    TitleFontSize = 10d;
                    RevealFontSize = 10d;
                    Padding = new Thickness(8, 0, 5, 0);
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 36f;
                    FontSize = 14d;
                    TitleFontSize = 12d;
                    RevealFontSize = 12d;
                    Padding = new Thickness(12, 0, 7, 0);
                    CornerRadius = 5f;
                    break;

                default:
                    Height = 30f;
                    MinHeight = 30f;
                    FontSize = 13d;
                    TitleFontSize = 11d;
                    RevealFontSize = 12d;
                    Padding = new Thickness(10, 0, 6, 0);
                    CornerRadius = 4f;
                    break;
            }

            if (Width < MinWidth)
                Width = MinWidth;
        }
        finally
        {
            _applyingSizePreset = false;
        }
    }

    private void RecalculateOverallHeight()
    {
        float headerHeight = GetHeaderRowHeight();
        float titleExtra = HasVisibleTitle() ? GetMeasuredTitleHeight() + TitleGap : 0f;
        float desiredHeight = Math.Clamp(headerHeight + titleExtra, MinHeight, MaxHeight);

        if (Math.Abs(Height - desiredHeight) > 0.5f)
            Height = desiredHeight;
    }

    private float GetHeaderRowHeight()
    {
        return SizePreset switch
        {
            ButtonSizePreset.Small => 26f,
            ButtonSizePreset.Large => 36f,
            _ => 30f
        };
    }

    private float GetRevealButtonWidth()
    {
        return SizePreset switch
        {
            ButtonSizePreset.Small => 38f,
            ButtonSizePreset.Large => 52f,
            _ => 46f
        };
    }

    private bool HasVisibleTitle()
    {
        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
    }

    private bool CanShowRevealButton()
    {
        return ShowRevealButton;
    }

    private string GetDisplayedPassword()
    {
        if (string.IsNullOrEmpty(Password))
            return string.Empty;

        if (RevealPassword)
            return Password;

        return new string('•', Password.Length);
    }

    private float GetMeasuredTitleHeight()
    {
        var style = new Topten.RichTextKit.Style
        {
            FontFamily = Theme.FontFamily,
            FontSize = (float)TitleFontSize,
            FontWeight = FontWeights.Normal.ToFontWeightValue(),
            TextColor = TitleColor.ToSKColor()
        };

        var tb = new Topten.RichTextKit.TextBlock
        {
            MaxWidth = Math.Max(1f, Width),
            Alignment = Topten.RichTextKit.TextAlignment.Left,
            EllipsisEnabled = true
        };

        tb.AddText(string.IsNullOrWhiteSpace(Title) ? " " : Title, style);
        tb.Layout();

        return Math.Max(12f, tb.MeasuredHeight + 2f);
    }

    private bool TryEnsureLockBitmap()
    {
        if (_lockBitmapLoadAttempted)
            return _lockBitmap != null;

        _lockBitmapLoadAttempted = true;
        _lockBitmap = LoadLockBitmap();

        return _lockBitmap != null;
    }

    private SKBitmap? LoadLockBitmap()
    {
        Stream? stream = null;

        try
        {
            stream = TryOpenLockBitmapStream();

            if (stream == null)
                return null;

            return SKBitmap.Decode(stream);
        }
        catch
        {
            return null;
        }
        finally
        {
            stream?.Dispose();
        }
    }

    private Stream? TryOpenLockBitmapStream()
    {
        Stream? stream = TryOpenPackResourceStream("pack://application:,,,/Mockup;component/Resources/PNG/lock.png");
        if (stream != null)
            return stream;

        stream = TryOpenPackResourceStream("pack://application:,,,/Mockup.Controls;component/Resources/PNG/lock.png");
        if (stream != null)
            return stream;

        stream = TryOpenPackResourceStream("pack://application:,,,/Resources/PNG/lock.png");
        if (stream != null)
            return stream;

        if (File.Exists(LockPngAbsolutePath))
            return File.OpenRead(LockPngAbsolutePath);

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        string[] fallbackPaths =
        [
            Path.Combine(baseDir, "Resources", "PNG", "lock.png"),
            Path.Combine(baseDir, "Mockup", "Resources", "PNG", "lock.png"),
            Path.Combine(baseDir, "Assets", "Default", "PNG", "lock.png")
        ];

        foreach (string path in fallbackPaths)
        {
            if (File.Exists(path))
                return File.OpenRead(path);
        }

        return null;
    }

    private Stream? TryOpenPackResourceStream(string uriText)
    {
        try
        {
            var info = Application.GetResourceStream(new Uri(uriText, UriKind.Absolute));
            return info?.Stream;
        }
        catch
        {
            return null;
        }
    }

    private SKRect GetAspectFitRect(float sourceWidth, float sourceHeight, SKRect bounds, float inset)
    {
        var inner = new SKRect(
            bounds.Left + inset,
            bounds.Top + inset,
            bounds.Right - inset,
            bounds.Bottom - inset
        );

        if (sourceWidth <= 0f || sourceHeight <= 0f || inner.Width <= 0f || inner.Height <= 0f)
            return inner;

        float scale = Math.Min(inner.Width / sourceWidth, inner.Height / sourceHeight);
        float width = sourceWidth * scale;
        float height = sourceHeight * scale;
        float left = inner.MidX - width * 0.5f;
        float top = inner.MidY - height * 0.5f;

        return new SKRect(left, top, left + width, top + height);
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

        if (_hoverRevealButton)
        {
            _hoverRevealButton = false;
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

    private (Color FillColor, Color BorderColor) GetHeaderVisualColors(RenderContext ctx)
    {
        Color fillColor = BackgroundColor;
        Color resolvedBorderColor = BorderColor;

        if (ctx.LiveMode && _isHovered)
        {
            fillColor = fillColor.Darken(0.015f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.04f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fillColor = fillColor.Darken(0.03f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.08f);
        }

        return (fillColor, resolvedBorderColor);
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

    private float GetSafeCornerRadius()
    {
        return Math.Clamp(CornerRadius, 0f, 12f);
    }

    #endregion
}

#endregion