// ======================================================================================
// FILE: Mockup.Controls/MonthCalendarControl.cs
//
// PURPOSE:
// - Modern month calendar control with a WinUI-inspired visual style.
// - Renders header, weekday row, optional week numbers and 6 weeks of day cells.
// - Builds explicit hit rects for year/month navigation buttons and day cells.
// - Updates SelectedDate directly on navigation and day selection.
//
// NOTES:
// - Keeps the existing control type and core behavior.
// - Uses exactly 6 calendar weeks for stable layout.
// - Interactivity depends on the preview mouse routing calling the control hooks.
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

#region === MONTH CALENDAR CONTROL ========================================================

[ControlType(displayName: "Month Calendar", group: "Pickers & Sliders")]
public partial class MonthCalendarControl : DesignControl
{
    #region === APPEARANCE ================================================================

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
    [property: System.ComponentModel.DisplayName("Header Text Color")]
    private Color headerTextColor = Theme.Text;

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
    private Color selectedDayBackground = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Day Color")]
    private Color selectedDayColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Today Accent Color")]
    private Color todayAccentColor = Color.FromRgb(82, 210, 242);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Week Number Color")]
    private Color weekNumberColor = Theme.Text.Lighten(0.22f);

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

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Grid")]
    private bool showGrid = true;

    #endregion

    #region === CONTENT / BEHAVIOR ========================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Selected Date")]
    private DateTime selectedDate = DateTime.Today;

    partial void OnSelectedDateChanged(DateTime oldValue, DateTime newValue)
    {
        _currentMonth = new DateTime(newValue.Year, newValue.Month, 1);
        InvalidateVisuals();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Week Numbers")]
    private bool showWeekNumbers = true;

    #endregion

    #region === PRIVATE FIELDS ============================================================

    private DateTime _currentMonth;

    [JsonIgnore, Browsable(false)]
    private SKRect _prevYearButtonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _prevMonthButtonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _nextMonthButtonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _nextYearButtonRect;

    [JsonIgnore, Browsable(false)]
    private readonly List<DayHitTarget> _dayHitTargets = new();

    [JsonIgnore, Browsable(false)]
    private CalendarNavTarget _hoverNavTarget = CalendarNavTarget.None;

    [JsonIgnore, Browsable(false)]
    private DateTime? _hoverDay;

    #endregion

    #region === CTOR ======================================================================

    public MonthCalendarControl()
    {
        Name = "MonthCalendar";
        ResizeStyle = ResizeStyles.KeepRatio;

        ExplicitePreviewHeight = 200f;
        ExplicitePreviewWidth = 220f;

        Width = 260f;
        Height = 230f;

        MinWidth = 220f;
        MinHeight = 180f;

        MaxWidth = 480f;
        MaxHeight = 420f;

        _currentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
    }

    #endregion

    #region === POINTER EVENTS ============================================================

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetHoverState();
            return;
        }

        var hoverNav = HitTestNavTarget(ctx.WorldPoint);
        DateTime? hoverDay = null;

        foreach (var item in _dayHitTargets)
        {
            if (item.Rect.Contains(ctx.WorldPoint))
            {
                hoverDay = item.Date;
                break;
            }
        }

        if (_hoverNavTarget != hoverNav || _hoverDay != hoverDay)
        {
            _hoverNavTarget = hoverNav;
            _hoverDay = hoverDay;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        switch (HitTestNavTarget(ctx.WorldPoint))
        {
            case CalendarNavTarget.PrevYear:
                SetSelectedDateSafe(SelectedDate.AddYears(-1));
                return;

            case CalendarNavTarget.PrevMonth:
                SetSelectedDateSafe(SelectedDate.AddMonths(-1));
                return;

            case CalendarNavTarget.NextMonth:
                SetSelectedDateSafe(SelectedDate.AddMonths(+1));
                return;

            case CalendarNavTarget.NextYear:
                SetSelectedDateSafe(SelectedDate.AddYears(+1));
                return;
        }

        foreach (var item in _dayHitTargets)
        {
            if (item.Rect.Contains(ctx.WorldPoint))
            {
                SelectedDate = item.Date;
                InvalidateVisuals();
                return;
            }
        }
    }

    public override void OnPointerLeave()
    {
        ResetHoverState();
    }

    #endregion

    #region === RENDER ====================================================================

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        _dayHitTargets.Clear();
        _prevYearButtonRect = SKRect.Empty;
        _prevMonthButtonRect = SKRect.Empty;
        _nextMonthButtonRect = SKRect.Empty;
        _nextYearButtonRect = SKRect.Empty;

        DrawCard(canvas, layout, ctx);

        var chrome = GetChrome(layout);
        BuildHeaderHitRects(chrome);

        DrawHeader(canvas, chrome, ctx);
        DrawWeekdays(canvas, chrome);
        DrawDays(canvas, chrome, ctx);
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === DRAW HELPERS ==============================================================

    private void DrawCard(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 24f),
            fillStyle: FillStyle.Solid,
            fillColor: BackgroundColor,
            borderColor: BorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: GetVisualShadow(ctx),
            borderWidth: 0.9f
        );
    }

    private void DrawHeader(SKCanvas canvas, CalendarChrome chrome, RenderContext ctx)
    {
        var titleRect = new SKRect(
            _prevMonthButtonRect.Right + 14f,
            chrome.Content.Top + 1f,
            _nextMonthButtonRect.Left - 14f,
            chrome.Content.Top + chrome.HeaderHeight
        );

        TextRenderer.Draw2(
            canvas: canvas,
            text: _currentMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            bounds: titleRect,
            fontSize: 14,
            color: HeaderTextColor,
            padding: new Thickness(0),
            fontWeight: FontWeights.SemiBold,
            textAlignment: System.Windows.TextAlignment.Center
        );

        DrawNavButton(
            canvas,
            _prevYearButtonRect,
            CalendarNavTarget.PrevYear,
            _hoverNavTarget == CalendarNavTarget.PrevYear && ctx.LiveMode);

        DrawNavButton(
            canvas,
            _prevMonthButtonRect,
            CalendarNavTarget.PrevMonth,
            _hoverNavTarget == CalendarNavTarget.PrevMonth && ctx.LiveMode);

        DrawNavButton(
            canvas,
            _nextMonthButtonRect,
            CalendarNavTarget.NextMonth,
            _hoverNavTarget == CalendarNavTarget.NextMonth && ctx.LiveMode);

        DrawNavButton(
            canvas,
            _nextYearButtonRect,
            CalendarNavTarget.NextYear,
            _hoverNavTarget == CalendarNavTarget.NextYear && ctx.LiveMode);

        using var divider = new SKPaint
        {
            Color = BorderColor.Lighten(0.08f).ToSKColor(),
            StrokeWidth = 0.8f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        float y = chrome.Content.Top + chrome.HeaderHeight + chrome.HeaderGap * 0.5f;
        canvas.DrawLine(chrome.Content.Left, y, chrome.Content.Right, y, divider);
    }

    private void DrawNavButton(
    SKCanvas canvas,
    SKRect rect,
    CalendarNavTarget target,
    bool isHover)
    {
        bool isYearButton =
            target == CalendarNavTarget.PrevYear || target == CalendarNavTarget.NextYear;
        bool isRight =
            target == CalendarNavTarget.NextMonth || target == CalendarNavTarget.NextYear;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: rect,
            cornerRadius: 2f,
            fillStyle: FillStyle.Solid,
            fillColor: isHover ? Theme.Primary.WithAlpha(18) : BackgroundColor,
            borderColor: BorderColor.Lighten(0.04f),
            borderStyle: BorderStyle.Solid,
            shadowOptions: ShadowOptions.Default,
            borderWidth: 0.9f
        );

        using var paint = new SKPaint
        {
            Color = HeaderTextColor.ToSKColor().WithAlpha(170),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float s = Math.Min(rect.Width, rect.Height) * 0.15f;
        float cx = rect.MidX;
        float cy = rect.MidY;

        if (isYearButton)
        {
            float centerGap = s * 1.0f;

            float cx1 = cx - centerGap * 0.5f;
            float cx2 = cx + centerGap * 0.5f;

            DrawFilledChevron(canvas, paint, cx1 - 1, cy, s, isRight);
            DrawFilledChevron(canvas, paint, cx2 + 1.5f, cy, s, isRight);
        }
        else
        {
            DrawFilledChevron(canvas, paint, cx, cy, s, isRight);
        }
    }

    private static void DrawFilledChevron(
    SKCanvas canvas,
    SKPaint paint,
    float cx,
    float cy,
    float s,
    bool isRight)
    {
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

    private void DrawWeekdays(SKCanvas canvas, CalendarChrome chrome)
    {
        string[] dayNames = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };

        float gridLeft = chrome.GridLeft;
        float weekdayTop = chrome.Content.Top + chrome.HeaderHeight + chrome.HeaderGap;

        if (ShowWeekNumbers)
        {
            var weekHdrRect = new SKRect(
                chrome.Content.Left,
                weekdayTop,
                chrome.Content.Left + chrome.WeekNumberWidth,
                weekdayTop + chrome.WeekdayHeight
            );

            TextRenderer.Draw2(
                canvas,
                "KW",
                weekHdrRect,
                10,
                WeekdayColor,
                new Thickness(0),
                fontWeight: FontWeights.Normal,
                textAlignment: System.Windows.TextAlignment.Center
            );
        }

        for (int i = 0; i < 7; i++)
        {
            var rect = new SKRect(
                gridLeft + i * chrome.DayCellWidth,
                weekdayTop,
                gridLeft + (i + 1) * chrome.DayCellWidth,
                weekdayTop + chrome.WeekdayHeight
            );

            TextRenderer.Draw2(
                canvas,
                dayNames[i],
                rect,
                10,
                WeekdayColor,
                new Thickness(0),
                fontWeight: FontWeights.Normal,
                textAlignment: System.Windows.TextAlignment.Center
            );
        }
    }

    private void DrawDays(SKCanvas canvas, CalendarChrome chrome, RenderContext ctx)
    {
        DateTime firstDay = _currentMonth;
        int offset = (int)firstDay.DayOfWeek;
        DateTime currentDay = firstDay.AddDays(-offset);
        DateTime today = DateTime.Today;

        float daysTop = chrome.Content.Top + chrome.HeaderHeight + chrome.HeaderGap
            + chrome.WeekdayHeight + chrome.WeekdayGap;

        for (int week = 0; week < 6; week++)
        {
            if (ShowWeekNumbers)
            {
                var weekRect = new SKRect(
                        chrome.Content.Left,
                        daysTop + week * chrome.DayCellHeight,
                        chrome.Content.Left + chrome.WeekNumberWidth,
                        daysTop + (week + 1) * chrome.DayCellHeight
                    );

                SkiaRenderer.DrawRect(
                    canvas: canvas,
                    rect: weekRect,
                    cornerRadius: 0f,
                    fillStyle: FillStyle.Solid,
                    fillColor: Theme.ControlBG.Darken(0.035f),
                    borderColor: Colors.Transparent,
                    borderStyle: BorderStyle.None,
                    shadowOptions: ShadowOptions.Default,
                    borderWidth: 0f
                );

                int weekNum = ISOWeek.GetWeekOfYear(currentDay);

                if (ShowGrid)
                    DrawCellBorder(canvas, weekRect);

                TextRenderer.Draw2(
                    canvas,
                    weekNum.ToString(),
                    weekRect,
                    10,
                    WeekNumberColor,
                    new Thickness(0),
                    fontWeight: FontWeights.Medium,
                    textAlignment: System.Windows.TextAlignment.Center
                );
            }

            for (int day = 0; day < 7; day++)
            {
                float x = chrome.GridLeft + day * chrome.DayCellWidth;
                float y = daysTop + week * chrome.DayCellHeight;

                var cellRect = new SKRect(x, y, x + chrome.DayCellWidth, y + chrome.DayCellHeight);
                _dayHitTargets.Add(new DayHitTarget(cellRect, currentDay));

                bool isCurrentMonth = currentDay.Month == _currentMonth.Month;
                bool isSelected = currentDay.Date == SelectedDate.Date;
                bool isToday = currentDay.Date == today;
                bool isHoverDay = ctx.LiveMode
                    && _hoverDay.HasValue
                    && _hoverDay.Value.Date == currentDay.Date;

                if (ShowGrid)
                    DrawCellBorder(canvas, cellRect);

                var markerRect = SKRect.Create(
                    cellRect.Left,
                    cellRect.Top,
                    Math.Max(1f, cellRect.Width),
                    Math.Max(1f, cellRect.Height)
                );

                if (isSelected)
                {
                    SkiaRenderer.DrawRect(
                        canvas: canvas,
                        rect: markerRect,
                        cornerRadius: 0,
                        fillStyle: FillStyle.Solid,
                        fillColor: SelectedDayBackground,
                        borderColor: Colors.Transparent,
                        borderStyle: BorderStyle.None,
                        shadowOptions: ShadowOptions.Default,
                        borderWidth: 0f
                    );
                }
                if (isHoverDay)
                {
                    SkiaRenderer.DrawRect(
                        canvas: canvas,
                        rect: markerRect,
                        cornerRadius: 2f,
                        fillStyle: FillStyle.Solid,
                        fillColor: Theme.Primary.Lighten(0.35f),
                        borderColor: Colors.Transparent,
                        borderStyle: BorderStyle.None,
                        shadowOptions: ShadowOptions.Default,
                        borderWidth: 0f
                    );
                }

                if (isToday && !isSelected)
                {
                    using var todayPaint = new SKPaint
                    {
                        Color = TodayAccentColor.ToSKColor(),
                        Style = SKPaintStyle.Fill,
                        IsAntialias = false
                    };

                    canvas.DrawRect(markerRect, todayPaint);
                }

                Color dayTextColor =
                    isSelected ? SelectedDayColor :
                    isCurrentMonth ? DayColor :
                    OtherMonthDayColor;

                var textRect = SKRect.Create(
                    cellRect.Left + 1f,
                    cellRect.Top + 1.5f,
                    Math.Max(1f, cellRect.Width - 2f),
                    Math.Max(1f, cellRect.Height - 2f)
                );

                TextRenderer.Draw2(
                    canvas,
                    currentDay.Day.ToString(),
                    textRect,
                    11,
                    dayTextColor,
                    new Thickness(0),
                    fontWeight: isSelected || isToday ? FontWeights.Medium : FontWeights.Normal,
                    textAlignment: System.Windows.TextAlignment.Center
                );

                currentDay = currentDay.AddDays(1);
            }
        }
    }

    private void DrawCellBorder(SKCanvas canvas, SKRect rect)
    {
        using var p = new SKPaint
        {
            Color = BorderColor.Lighten(0.10f).ToSKColor(),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.8f,
            IsAntialias = true
        };

        canvas.DrawRect(rect, p);
    }

    #endregion

    #region === LAYOUT HELPERS ============================================================

    private CalendarChrome GetChrome(SKRect layout)
    {
        float inset = 10f;
        float headerHeight = 30f;
        float headerGap = 6f;
        float weekdayHeight = 18f;
        float weekdayGap = 4f;
        float navButtonSize = 20f;
        float navButtonGap = 4f;

        var content = new SKRect(
            layout.Left + inset,
            layout.Top + inset,
            layout.Right - inset,
            layout.Bottom - inset
        );

        float weekNumberWidth = ShowWeekNumbers ? 28f : 0f;
        float gridLeft = ShowWeekNumbers ? content.Left + weekNumberWidth : content.Left;
        float gridWidth = content.Width - weekNumberWidth;
        float dayCellWidth = gridWidth / 7f;

        float daysTop = content.Top + headerHeight + headerGap + weekdayHeight + weekdayGap;
        float availableDaysHeight = Math.Max(6f, content.Bottom - daysTop);
        float dayCellHeight = availableDaysHeight / 6f;

        return new CalendarChrome(
            Content: content,
            HeaderHeight: headerHeight,
            HeaderGap: headerGap,
            WeekdayHeight: weekdayHeight,
            WeekdayGap: weekdayGap,
            WeekNumberWidth: weekNumberWidth,
            GridLeft: gridLeft,
            DayCellWidth: dayCellWidth,
            DayCellHeight: dayCellHeight,
            NavButtonSize: navButtonSize,
            NavButtonGap: navButtonGap
        );
    }

    private void BuildHeaderHitRects(CalendarChrome chrome)
    {
        float top = chrome.Content.Top + 3f;
        float left = chrome.Content.Left + 2f;
        float right = chrome.Content.Right - 2f;
        float size = chrome.NavButtonSize;
        float gap = chrome.NavButtonGap;

        _prevYearButtonRect = new SKRect(left, top, left + size, top + size);

        _prevMonthButtonRect = new SKRect(
            _prevYearButtonRect.Right + gap,
            top,
            _prevYearButtonRect.Right + gap + size,
            top + size
        );

        _nextYearButtonRect = new SKRect(
            right - size,
            top,
            right,
            top + size
        );

        _nextMonthButtonRect = new SKRect(
            _nextYearButtonRect.Left - gap - size,
            top,
            _nextYearButtonRect.Left - gap,
            top + size
        );
    }

    private CalendarNavTarget HitTestNavTarget(SKPoint point)
    {
        if (_prevYearButtonRect.Contains(point))
            return CalendarNavTarget.PrevYear;

        if (_prevMonthButtonRect.Contains(point))
            return CalendarNavTarget.PrevMonth;

        if (_nextMonthButtonRect.Contains(point))
            return CalendarNavTarget.NextMonth;

        if (_nextYearButtonRect.Contains(point))
            return CalendarNavTarget.NextYear;

        return CalendarNavTarget.None;
    }

    private void SetSelectedDateSafe(DateTime newDate)
    {
        int day = Math.Min(SelectedDate.Day, DateTime.DaysInMonth(newDate.Year, newDate.Month));
        SelectedDate = new DateTime(newDate.Year, newDate.Month, day);
        _currentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
        InvalidateVisuals();
    }

    #endregion

    #region === VISUAL STATE ===============================================================

    private void ResetHoverState()
    {
        if (_hoverNavTarget == CalendarNavTarget.None && !_hoverDay.HasValue)
            return;

        _hoverNavTarget = CalendarNavTarget.None;
        _hoverDay = null;
        InvalidateVisuals();
    }

    private void InvalidateVisuals()
    {
        MSG.UI.InvalidateDesigner();
    }

    private ShadowOptions GetVisualShadow(RenderContext ctx)
    {
        int safeElevation = Math.Clamp(Elevation, 0, 5);
        return safeElevation <= 0 ? ShadowOptions.Default : GetElevation(safeElevation);
    }

    #endregion

    #region === PRIVATE TYPES ==============================================================

    private readonly record struct CalendarChrome(
        SKRect Content,
        float HeaderHeight,
        float HeaderGap,
        float WeekdayHeight,
        float WeekdayGap,
        float WeekNumberWidth,
        float GridLeft,
        float DayCellWidth,
        float DayCellHeight,
        float NavButtonSize,
        float NavButtonGap
    );

    private readonly record struct DayHitTarget(
        SKRect Rect,
        DateTime Date
    );

    private enum CalendarNavTarget
    {
        None,
        PrevYear,
        PrevMonth,
        NextMonth,
        NextYear
    }

    #endregion
}

#endregion
