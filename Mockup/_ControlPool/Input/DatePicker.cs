// ======================================================================================
// FILE: Mockup.Controls/DatePicker.cs
//
// PURPOSE:
// - Modern DatePicker control for the mockup designer.
// - Visual style aligned with TextBox / ComboBox / MonthCalendar controls.
// - Compact header with optional title, selected date / placeholder and calendar button.
// - Popup month calendar with explicit interactive rects for reliable hit testing.
//
// PROJECT: Mockup.Controls
// GROUP: Input
//
// NOTES:
// - This is a visual mockup control, not a real native DatePicker.
// - The popup calendar is rendered by this control itself.
// - Selection changes only in LiveMode on left mouse click.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === DATE PICKER ===

[ControlType(displayName: "Date Picker", group: "Pickers & Sliders")]
public partial class DatePicker : DesignControl
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
    [property: System.ComponentModel.DisplayName("Placeholder")]
    private string placeholder = "Select date...";

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Selected Date")]
    private DateTime? selectedDate = DateTime.Today;

    partial void OnSelectedDateChanged(DateTime? oldValue, DateTime? newValue)
    {
        _currentMonth = new DateTime((newValue ?? DateTime.Today).Year, (newValue ?? DateTime.Today).Month, 1);
        InvalidateVisuals();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Display Format")]
    private string displayFormat = "dd.MM.yyyy";

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
    [property: System.ComponentModel.DisplayName("Popup Background")]
    private Color popupBackgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Popup Border")]
    private Color popupBorderColor = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Weekday Color")]
    private Color weekdayColor = Theme.Text.Lighten(0.18f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Day Color")]
    private Color dayColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Other Month Day Color")]
    private Color otherMonthDayColor = Theme.Text.WithAlpha(110);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Day Background")]
    private Color selectedDayBackground = SkiaRenderer.SelectionColor;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Day Color")]
    private Color selectedDayColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Today Accent Color")]
    private Color todayAccentColor = Colors.Orange;

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
    private double fontSize = 13d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private double titleFontSize = 12d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

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
    private Thickness padding = new(10, 0, 10, 0);

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    [ObservableProperty]
    [property: Browsable(false)]
    private bool isCalendarOpen = false;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _hoverButton;

    [JsonIgnore, Browsable(false)]
    private CalendarNavTarget _hoverNavTarget = CalendarNavTarget.None;

    [JsonIgnore, Browsable(false)]
    private DateTime? _hoverDay;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    [JsonIgnore, Browsable(false)]
    private SKRect _headerRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _buttonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _popupRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _prevMonthRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _nextMonthRect;

    [JsonIgnore, Browsable(false)]
    private readonly List<DayHitTarget> _dayHitTargets = new();

    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private const float TitleGap = 2f;
    private const float PopupGap = 4f;

    #endregion

    #region === CTOR ===

    public DatePicker()
    {
        IsActionControl = true;

        Name = "DatePicker";
        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 150f;
        Height = 30f;

        MinWidth = 80f;
        MinHeight = 26f;

        MaxWidth = 600f;
        MaxHeight = 340f;

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
    }

    partial void OnShowTitleChanged(bool value)
    {
        RecalculateOverallHeight();
    }

    partial void OnTitleFontSizeChanged(double value)
    {
        RecalculateOverallHeight();
    }

    #endregion

    #region === HIT TEST ===

    public override bool HitTest(SKPoint point)
    {
        if (VisualRect.Contains(point))
            return true;

        if (IsCalendarOpen && _popupRect.Contains(point))
            return true;

        return false;
    }

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (IsCalendarOpen)
        {
            if (_popupRect.Contains(ctx.WorldPoint))
            {
                if (_prevMonthRect.Contains(ctx.WorldPoint))
                {
                    _currentMonth = _currentMonth.AddMonths(-1);
                    InvalidateVisuals();
                    return;
                }

                if (_nextMonthRect.Contains(ctx.WorldPoint))
                {
                    _currentMonth = _currentMonth.AddMonths(1);
                    InvalidateVisuals();
                    return;
                }

                if (TryHitTestDay(ctx.WorldPoint, out var hitDate))
                {
                    SelectedDate = hitDate;
                    IsCalendarOpen = false;
                    ResetInteractionState();
                    InvalidateVisuals();
                    return;
                }

                return;
            }

            if (_headerRect.Contains(ctx.WorldPoint))
            {
                _isPressed = true;
                _isHovered = true;
                _hoverButton = _buttonRect.Contains(ctx.WorldPoint);
                InvalidateVisuals();
                return;
            }

            IsCalendarOpen = false;
            ResetInteractionState();
            InvalidateVisuals();
            return;
        }

        if (_headerRect.Contains(ctx.WorldPoint))
        {
            _isPressed = true;
            _isHovered = true;
            _hoverButton = _buttonRect.Contains(ctx.WorldPoint);
            InvalidateVisuals();
        }
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool insideButton = _buttonRect.Contains(ctx.WorldPoint);

        if (_isHovered != insideHeader)
        {
            _isHovered = insideHeader;
            InvalidateVisuals();
        }

        if (_hoverButton != insideButton)
        {
            _hoverButton = insideButton;
            InvalidateVisuals();
        }

        if (insideHeader || (IsCalendarOpen && _popupRect.Contains(ctx.WorldPoint)))
            Mouse.OverrideCursor = Cursors.Hand;

        if (!insideHeader && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }

        if (IsCalendarOpen)
        {
            var nav = CalendarNavTarget.None;
            if (_prevMonthRect.Contains(ctx.WorldPoint))
                nav = CalendarNavTarget.PrevMonth;
            else if (_nextMonthRect.Contains(ctx.WorldPoint))
                nav = CalendarNavTarget.NextMonth;

            if (_hoverNavTarget != nav)
            {
                _hoverNavTarget = nav;
                InvalidateVisuals();
            }

            DateTime? hoverDay = null;
            if (TryHitTestDay(ctx.WorldPoint, out var hitDate))
                hoverDay = hitDate;

            if (_hoverDay != hoverDay)
            {
                _hoverDay = hoverDay;
                InvalidateVisuals();
            }
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool commitClick = _isPressed && insideHeader;

        _isPressed = false;
        _isHovered = insideHeader;
        _hoverButton = _buttonRect.Contains(ctx.WorldPoint);

        if (commitClick)
        {
            IsCalendarOpen = !IsCalendarOpen;
            _hoverNavTarget = CalendarNavTarget.None;
            _hoverDay = null;
            InvalidateVisuals();
        }
        else
        {
            InvalidateVisuals();
        }
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        _dayHitTargets.Clear();

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

        _buttonRect = new SKRect(
            _headerRect.Right - headerHeight,
            _headerRect.Top,
            _headerRect.Right,
            _headerRect.Bottom
        );

        DrawHeader(canvas, titleRect, _headerRect, ctx, hasTitle);

        if (IsCalendarOpen)
            DrawCalendarPopup(canvas, _headerRect, ctx);
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

        string displayText = SelectedDate.HasValue
            ? SelectedDate.Value.ToString(DisplayFormat, CultureInfo.CurrentCulture)
            : Placeholder;

        Color displayColor = SelectedDate.HasValue ? TextColor : PlaceholderColor;

        var contentRect = new SKRect(
            headerRect.Left + (float)Padding.Left,
            headerRect.Top,
            _buttonRect.Left - 4f,
            headerRect.Bottom
        );

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

        DrawCalendarButton(canvas, _buttonRect, ctx);
    }

    private void DrawCalendarButton(SKCanvas canvas, SKRect rect, RenderContext ctx)
    {
        Color fill = _hoverButton && ctx.LiveMode
            ? Theme.ControlBG.Darken(0.02f)
            : BackgroundColor;

        var inner = new SKRect(rect.Left + 1f, rect.Top + 1f, rect.Right - 1f, rect.Bottom - 1f);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: inner,
            cornerRadius: Math.Max(2f, GetSafeCornerRadius() - 1f),
            fillStyle: FillStyle.Solid,
            fillColor: fill,
            borderColor: Colors.Transparent,
            borderStyle: BorderStyle.None,
            shadowOptions: ShadowOptions.Default,
            borderWidth: 0f
        );

        using var stroke = new SKPaint
        {
            Color = TextColor.ToSKColor().WithAlpha(180),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.2f,
            IsAntialias = true
        };

        using var fillPaint = new SKPaint
        {
            Color = TextColor.ToSKColor().WithAlpha(180),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float w = inner.Width * 0.48f;
        float h = inner.Height * 0.44f;
        float x = inner.MidX - w / 2f;
        float y = inner.MidY - h / 2f;

        var calRect = new SKRect(x, y, x + w, y + h);
        canvas.DrawRect(calRect, stroke);
        canvas.DrawLine(calRect.Left, calRect.Top + h * 0.28f, calRect.Right, calRect.Top + h * 0.28f, stroke);

        canvas.DrawCircle(calRect.Left + w * 0.22f, calRect.Top, 1.2f, fillPaint);
        canvas.DrawCircle(calRect.Right - w * 0.22f, calRect.Top, 1.2f, fillPaint);
    }

    private void DrawCalendarPopup(SKCanvas canvas, SKRect headerRect, RenderContext ctx)
    {
        float popupTop = headerRect.Bottom + PopupGap;
        float popupWidth = Math.Max(220f, headerRect.Width);
        float popupHeight = 218f;

        _popupRect = new SKRect(
            headerRect.Left,
            popupTop,
            headerRect.Left + popupWidth,
            popupTop + popupHeight
        );

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: _popupRect,
            cornerRadius: Math.Max(4f, GetSafeCornerRadius()),
            fillStyle: FillStyle.Solid,
            fillColor: PopupBackgroundColor,
            borderColor: PopupBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: GetPopupShadow(),
            borderWidth: 0.9f
        );

        const float inset = 10f;
        const float headerHeight = 28f;
        const float headerGap = 6f;
        const float weekdayHeight = 18f;
        const float weekdayGap = 4f;
        const float navButtonSize = 20f;

        var content = new SKRect(
            _popupRect.Left + inset,
            _popupRect.Top + inset,
            _popupRect.Right - inset,
            _popupRect.Bottom - inset
        );

        _prevMonthRect = new SKRect(content.Left + 2f, content.Top + 3f, content.Left + 2f + navButtonSize, content.Top + 3f + navButtonSize);
        _nextMonthRect = new SKRect(content.Right - 2f - navButtonSize, content.Top + 3f, content.Right - 2f, content.Top + 3f + navButtonSize);

        DrawCalendarHeader(canvas, content, headerHeight, ctx);
        DrawCalendarWeekdays(canvas, content, headerHeight, headerGap, weekdayHeight);
        DrawCalendarDays(canvas, content, headerHeight, headerGap, weekdayHeight, weekdayGap, ctx);
    }

    private void DrawCalendarHeader(SKCanvas canvas, SKRect content, float headerHeight, RenderContext ctx)
    {
        var titleRect = new SKRect(
            _prevMonthRect.Right + 16f,
            content.Top,
            _nextMonthRect.Left - 16f,
            content.Top + headerHeight
        );

        TextRenderer.Draw2(
            canvas: canvas,
            text: _currentMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            bounds: titleRect,
            fontSize: 14d,
            color: TextColor,
            padding: new Thickness(0),
            fontWeight: FontWeights.SemiBold,
            textAlignment: System.Windows.TextAlignment.Center
        );

        DrawCalendarNavButton(canvas, _prevMonthRect, false, _hoverNavTarget == CalendarNavTarget.PrevMonth && ctx.LiveMode);
        DrawCalendarNavButton(canvas, _nextMonthRect, true, _hoverNavTarget == CalendarNavTarget.NextMonth && ctx.LiveMode);
    }

    private void DrawCalendarNavButton(SKCanvas canvas, SKRect rect, bool isRight, bool isHover)
    {
        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: rect,
            cornerRadius: 2f,
            fillStyle: FillStyle.Solid,
            fillColor: isHover ? Theme.ControlBG.Darken(0.02f) : PopupBackgroundColor,
            borderColor: PopupBorderColor.Lighten(0.04f),
            borderStyle: BorderStyle.Solid,
            shadowOptions: ShadowOptions.Default,
            borderWidth: 0.9f
        );

        using var paint = new SKPaint
        {
            Color = TextColor.ToSKColor().WithAlpha(170),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float s = Math.Min(rect.Width, rect.Height) * 0.17f;
        float cx = rect.MidX;
        float cy = rect.MidY;

        using var path = new SKPath();
        if (isRight)
        {
            path.MoveTo(cx - s, cy - s);
            path.LineTo(cx + s * 0.75f, cy);
            path.LineTo(cx - s, cy + s);
            path.LineTo(cx - s * 0.45f, cy);
            path.Close();
        }
        else
        {
            path.MoveTo(cx + s, cy - s);
            path.LineTo(cx - s * 0.75f, cy);
            path.LineTo(cx + s, cy + s);
            path.LineTo(cx + s * 0.45f, cy);
            path.Close();
        }

        canvas.DrawPath(path, paint);
    }

    private void DrawCalendarWeekdays(SKCanvas canvas, SKRect content, float headerHeight, float headerGap, float weekdayHeight)
    {
        string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        float gridLeft = content.Left;
        float weekdayTop = content.Top + headerHeight + headerGap;
        float dayCellWidth = content.Width / 7f;

        for (int i = 0; i < 7; i++)
        {
            var rect = new SKRect(
                gridLeft + i * dayCellWidth,
                weekdayTop,
                gridLeft + (i + 1) * dayCellWidth,
                weekdayTop + weekdayHeight
            );

            TextRenderer.Draw2(
                canvas: canvas,
                text: dayNames[i],
                bounds: rect,
                fontSize: 10d,
                color: WeekdayColor,
                padding: new Thickness(0),
                fontWeight: FontWeights.Normal,
                textAlignment: System.Windows.TextAlignment.Center
            );
        }
    }

    private void DrawCalendarDays(SKCanvas canvas, SKRect content, float headerHeight, float headerGap, float weekdayHeight, float weekdayGap, RenderContext ctx)
    {
        DateTime firstDay = _currentMonth;
        int offset = (int)firstDay.DayOfWeek;
        DateTime currentDay = firstDay.AddDays(-offset);
        DateTime today = DateTime.Today;

        float daysTop = content.Top + headerHeight + headerGap + weekdayHeight + weekdayGap;
        float dayCellWidth = content.Width / 7f;
        float dayCellHeight = (content.Bottom - daysTop) / 6f;

        for (int week = 0; week < 6; week++)
        {
            for (int day = 0; day < 7; day++)
            {
                float x = content.Left + day * dayCellWidth;
                float y = daysTop + week * dayCellHeight;

                var cellRect = new SKRect(x, y, x + dayCellWidth, y + dayCellHeight);
                _dayHitTargets.Add(new DayHitTarget(cellRect, currentDay));

                bool isCurrentMonth = currentDay.Month == _currentMonth.Month;
                bool isSelected = SelectedDate.HasValue && currentDay.Date == SelectedDate.Value.Date;
                bool isToday = currentDay.Date == today;
                bool isHoverDay = ctx.LiveMode && _hoverDay.HasValue && _hoverDay.Value.Date == currentDay.Date;

                var markerRect = SKRect.Create(
                    cellRect.Left + 1f,
                    cellRect.Top + 1f,
                    Math.Max(1f, cellRect.Width - 2f),
                    Math.Max(1f, cellRect.Height - 2f)
                );

                if (isSelected)
                {
                    SkiaRenderer.DrawRect(
                        canvas: canvas,
                        rect: markerRect,
                        cornerRadius: 2f,
                        fillStyle: FillStyle.Solid,
                        fillColor: SelectedDayBackground,
                        borderColor: Colors.Transparent,
                        borderStyle: BorderStyle.None,
                        shadowOptions: ShadowOptions.Default,
                        borderWidth: 0f
                    );
                }
                else if (isHoverDay)
                {
                    SkiaRenderer.DrawRect(
                        canvas: canvas,
                        rect: markerRect,
                        cornerRadius: 2f,
                        fillStyle: FillStyle.Solid,
                        fillColor: Theme.ControlBG.Darken(0.03f),
                        borderColor: Colors.Transparent,
                        borderStyle: BorderStyle.None,
                        shadowOptions: ShadowOptions.Default,
                        borderWidth: 0f
                    );
                }

                if (isToday && !isSelected)
                {
                    SkiaRenderer.DrawRect(
                        canvas: canvas,
                        rect: markerRect,
                        cornerRadius: 2f,
                        fillStyle: FillStyle.Solid,
                        fillColor: TodayAccentColor,
                        borderColor: Colors.Transparent,
                        borderStyle: BorderStyle.None,
                        shadowOptions: ShadowOptions.Default,
                        borderWidth: 0f
                    );
                }

                Color dayTextColor =
                    isSelected ? SelectedDayColor :
                    isToday ? Colors.White :
                    isCurrentMonth ? DayColor :
                    OtherMonthDayColor;

                TextRenderer.Draw2(
                    canvas: canvas,
                    text: currentDay.Day.ToString(),
                    bounds: markerRect,
                    fontSize: 11d,
                    color: dayTextColor,
                    padding: new Thickness(0),
                    fontWeight: isSelected || isToday ? FontWeights.Medium : FontWeights.Normal,
                    textAlignment: System.Windows.TextAlignment.Center
                );

                currentDay = currentDay.AddDays(1);
            }
        }
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
                    TitleFontSize = 11d;
                    Padding = new Thickness(8, 0, 8, 0);
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 36f;
                    FontSize = 14d;
                    TitleFontSize = 12d;
                    Padding = new Thickness(12, 0, 12, 0);
                    CornerRadius = 5f;
                    break;

                default:
                    Height = 30f;
                    MinHeight = 30f;
                    FontSize = 13d;
                    TitleFontSize = 12d;
                    Padding = new Thickness(10, 0, 10, 0);
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

    private bool HasVisibleTitle()
    {
        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
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

    private bool TryHitTestDay(SKPoint point, out DateTime date)
    {
        foreach (var item in _dayHitTargets)
        {
            if (item.Rect.Contains(point))
            {
                date = item.Date;
                return true;
            }
        }

        date = default;
        return false;
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

        if (_hoverButton)
        {
            _hoverButton = false;
            changed = true;
        }

        if (_hoverNavTarget != CalendarNavTarget.None)
        {
            _hoverNavTarget = CalendarNavTarget.None;
            changed = true;
        }

        if (_hoverDay.HasValue)
        {
            _hoverDay = null;
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

    private ShadowOptions GetPopupShadow()
    {
        return new ShadowOptions
        {
            Color = SKColors.Black.WithAlpha(40),
            Dx = 0f,
            Dy = 2f,
            Sigma = 3f
        };
    }

    private float GetSafeCornerRadius()
    {
        return Math.Clamp(CornerRadius, 0f, 12f);
    }

    #endregion

    #region === PRIVATE TYPES ===

    private readonly record struct DayHitTarget(SKRect Rect, DateTime Date);

    private enum CalendarNavTarget
    {
        None,
        PrevMonth,
        NextMonth
    }

    #endregion
}

#endregion
