using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Mockup.Extensions;

public static class GridEx
{
    // RowDef Property
    public static readonly DependencyProperty RowDefProperty =
        DependencyProperty.RegisterAttached(
            "RowDef",
            typeof(string),
            typeof(GridEx),
            new PropertyMetadata(null, OnRowDefChanged)
        );

    public static string GetRowDef(Grid grid)
        => (string)grid.GetValue(RowDefProperty);

    public static void SetRowDef(Grid grid, string value)
        => grid.SetValue(RowDefProperty, value);

    // ColDef Property
    public static readonly DependencyProperty ColDefProperty =
        DependencyProperty.RegisterAttached(
            "ColDef",
            typeof(string),
            typeof(GridEx),
            new PropertyMetadata(null, OnColDefChanged)
        );

    public static string GetColDef(Grid grid)
        => (string)grid.GetValue(ColDefProperty);

    public static void SetColDef(Grid grid, string value)
        => grid.SetValue(ColDefProperty, value);

    private static void OnRowDefChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Grid grid && e.NewValue is string definitions)
        {
            grid.RowDefinitions.Clear();
            ParseDefinitions(definitions,
                token => grid.RowDefinitions.Add(new RowDefinition { Height = ParseGridLength(token) }));
        }
    }

    private static void OnColDefChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Grid grid && e.NewValue is string definitions)
        {
            grid.ColumnDefinitions.Clear();
            ParseDefinitions(definitions,
                token => grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ParseGridLength(token) }));
        }
    }

    private static void ParseDefinitions(string definitions, Action<string> addDefinitionAction)
    {
        var parts = definitions.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            addDefinitionAction(part.Trim());
        }
    }

    private static GridLength ParseGridLength(string token)
    {
        string trimmed = token.Trim();

        if (trimmed == "*")
        {
            return new GridLength(1, GridUnitType.Star);
        }
        else if (trimmed.EndsWith("*") && double.TryParse(
            trimmed.TrimEnd('*'),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out double stars))
        {
            return new GridLength(stars, GridUnitType.Star);
        }
        else if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return GridLength.Auto;
        }
        else if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out double pixels))
        {
            return new GridLength(pixels);
        }
        else
        {
            throw new FormatException($"Invalid grid definition: '{trimmed}'");
        }
    }
}