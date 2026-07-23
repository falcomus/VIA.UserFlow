// ============================================================================
// FILE: Mockup/BandPage.cs
// NEW HEIGHT MODEL (Phase 1)
// - Height: persisted "content height" (user-resized)
// - MinHeight: derived from bottom-most control (+ padding)
// - EnsureMinHeight(): clamps Height >= MinHeight
// ============================================================================

using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mockup;

public sealed class BandPage
{
    [property: Browsable(false)]
    public long Id { get; set; } = IdGenerator.NewID;

    public string Name { get; set; } = "Page";
    public string? Title { get; set; }

    /// <summary>
    /// Persisted content-height (user resizes this).
    /// </summary>
    public float Height { get; set; } = Screen.DefaultBandHeight;

    [JsonIgnore]
    [property: Browsable(false)]
    public Band ParentBand { get; set; } = null!;

    [JsonIgnore]
    public SKRect WorldBounds { get; internal set; }

    [property: Browsable(false)]
    public ObservableCollection<DesignControl> Controls { get; set; } = [];

    // -------------------------------------------
    // MinHeight (derived) + caching
    // -------------------------------------------

    private const float MIN_PADDING_BOTTOM = 10f;

    [JsonIgnore]
    private bool _minHeightDirty = true;

    [JsonIgnore]
    private float _cachedMinHeight;

    [JsonIgnore]
    [Browsable(false)]
    public float MinHeight
    {
        get
        {
            if (_minHeightDirty)
                RecomputeMinHeight();
            return _cachedMinHeight;
        }
    }

    public BandPage()
    {
        Controls.CollectionChanged += Controls_CollectionChanged;
    }

    private void Controls_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DesignControl c in e.NewItems)
            {
                c.PropertyChanged += Control_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (DesignControl c in e.OldItems)
            {
                c.PropertyChanged -= Control_PropertyChanged;
            }
        }

        InvalidateMinHeight();
    }

    private void Control_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // MinHeight hängt nur von Y + Height ab
        if (e.PropertyName == nameof(DesignControl.Y) ||
            e.PropertyName == nameof(DesignControl.Height))
        {
            InvalidateMinHeight();
        }
    }

    public void InvalidateMinHeight()
    {
        _minHeightDirty = true;
    }

    private void RecomputeMinHeight()
    {
        float bottomMost = 0f;

        foreach (var c in Controls)
        {
            float b = c.Y + c.Height;
            if (b > bottomMost)
                bottomMost = b;
        }

        float min = bottomMost + MIN_PADDING_BOTTOM;

        if (min < 0)
            min = 0;

        _cachedMinHeight = MathF.Round(min);
        _minHeightDirty = false;
    }

    public void EnsureMinHeight()
    {
        var min = MinHeight;
        if (Height < min)
            Height = min;
    }

    // ============================================================
    // WORLD BOUNDS
    // Wird vom BaseDesigner.UpdateActivePageWorldBounds gesetzt
    // ============================================================
    public void UpdateWorldBounds(float x, float y, float width)
    {
        WorldBounds = new SKRect(
            x,
            y,
            x + width,
            y + Height);

        foreach (var c in Controls)
        {
            c.UpdateWorldBounds(
                WorldBounds.Left,
                WorldBounds.Top);
        }
    }

    // ============================================================
    // CLONE
    // ============================================================

    public BandPage DeepClone()
    {
        var copy = new BandPage
        {
            Id = IdGenerator.NewID,
            Name = Name,
            Title = Title,
            Height = Height
        };

        foreach (var c in Controls)
        {
            var ctrl = c.DeepClone();
            ctrl.ParentBandPage = copy;
            copy.Controls.Add(ctrl);
        }

        copy.InvalidateMinHeight();
        copy.EnsureMinHeight();

        return copy;
    }

    public override string ToString() => Name ?? "Unnamed Page";
}
