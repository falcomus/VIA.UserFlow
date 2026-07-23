// ======================================================================================
// FILE: Mockup.ColorSystem/ThemeService.cs
//
// PURPOSE:
//   Globale Verwaltung des aktiven ColorSchema.
//   Project lädt Schema → ThemeService.SetSchema(key) 
//        → ThemeService.Apply(schema)
//        → Designer invalidieren.
//
//   Arbeitet mit ColorSchemaCatalog.Current (MO32 persistenter Catalog).
//
// AUTHOR: Claus Falkenstein / ChatGPT
// VERSION: 3.0 (MO32 – ColorSchemaCatalog integration)
// ======================================================================================

using Mockup.Messages;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Mockup.ColorSystem;

public static class ThemeService
{
    // Derzeit aktives ColorSchema (Runtime-Copy)
    private static ColorSchema _current = ColorSchema.CreateDefault();
    private static bool _isCatalogInitialized = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Zugriff auf aktuelles Schema.
    /// </summary>
    public static ColorSchema Current => _current;

    // ========================================================================
    // INITIALIZATION (NEU)
    // ========================================================================

    /// <summary>
    /// Initialisiert den ColorSchemaCatalog mit einem Dateipfad.
    /// Muss aufgerufen werden, bevor SetSchema verwendet wird.
    /// </summary>
    public static void InitializeCatalog(string filePath)
    {
        lock (_lock)
        {
            if (!_isCatalogInitialized)
            {
                try
                {
                    ColorSchemaCatalog.Initialize(filePath);
                    _isCatalogInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize ColorSchemaCatalog: {ex.Message}");
                    // Fallback: Default-Schema verwenden
                    _current = ColorSchema.CreateDefault();
                }
            }
        }
    }

    /// <summary>
    /// Initialisiert den ColorSchemaCatalog mit Standardpfad.
    /// </summary>
    public static void InitializeCatalog()
    {
        string defaultPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "colorSchemas.json"
        );
        InitializeCatalog(defaultPath);
    }

    /// <summary>
    /// Überprüft und initialisiert den Catalog falls nötig.
    /// </summary>
    private static void EnsureCatalogInitialized()
    {
        if (!_isCatalogInitialized)
        {
            InitializeCatalog();
        }
    }

    // ========================================================================
    // APPLY DIRECT INSTANCE
    // ========================================================================
    public static void Apply(ColorSchema schema)
    {
        if (schema == null)
            return;

        // Sehr wichtig: Klonen, damit Designer-Schreiboperationen NICHT
        // das Katalog-Objekt ändern.
        _current = schema.Clone();

        // UI invalideren
        MSG.UI.InvalidateDesigner();
    }

    // ========================================================================
    // SET BY KEY  (für Projekt-Laden)
    // ========================================================================
    //public static void SetSchema(string key)
    //{
    //    EnsureCatalogInitialized();

    //    // Wichtig: über ColorSchemaCatalog.Current
    //    var schema = ColorSchemaCatalog.Current?.Get(key);

    //    if (schema == null)
    //    {
    //        // Fallback: Versuche Default-Schema
    //        schema = ColorSchemaCatalog.Current?.Get("Default");

    //        if (schema == null)
    //        {
    //            // Doppelter Fallback: Erstelle Default
    //            _current = ColorSchema.CreateDefault();
    //        }
    //        else
    //        {
    //            _current = schema.Clone();
    //        }
    //    }
    //    else
    //    {
    //        _current = schema.Clone();
    //    }

    //    MSG.UI.InvalidateDesigner();
    //}

    public static void SetSchema(string key)
    {
        EnsureCatalogInitialized();

        var schema = ColorSchemaCatalog.Current?.Get(key) ?? ColorSchemaCatalog.Current?.Get("Default");
        _current = schema ?? ColorSchema.CreateDefault();

        MSG.UI.InvalidateDesigner();
    }

    // ========================================================================
    // SET BY KEY (safe version mit Fallback)
    // ========================================================================
    public static bool TrySetSchema(string key, out ColorSchema? appliedSchema)
    {
        EnsureCatalogInitialized();
        appliedSchema = null;

        try
        {
            if (ColorSchemaCatalog.Current == null)
            {
                _current = ColorSchema.CreateDefault();
                appliedSchema = _current;
                return false;
            }

            var schema = ColorSchemaCatalog.Current.Get(key);
            if (schema == null)
            {
                // Versuche Default
                schema = ColorSchemaCatalog.Current.Get("Default");
                if (schema == null)
                {
                    _current = ColorSchema.CreateDefault();
                    appliedSchema = _current;
                    return false;
                }
            }

            _current = schema.Clone();
            appliedSchema = _current;

            MSG.UI.InvalidateDesigner();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting schema '{key}': {ex.Message}");
            _current = ColorSchema.CreateDefault();
            appliedSchema = _current;
            return false;
        }
    }

    // ========================================================================
    // Convenience for Controls
    // ========================================================================

    public static Color Primary => _current.PrimaryColor;
    public static Color Accent => _current.AccentColor;
    public static Color Info => _current.InfoColor;
    public static Color Warning => _current.WarningColor;
    public static Color Error => _current.ErrorColor;
    public static Color Success => _current.SuccessColor;
    public static Color Neutral => _current.NeutralColor;

    public static Color Text => _current.TextColor;
    public static Color ControlBG => _current.ControlBGColor;
    public static Color ControlBorder => _current.ControlBorderColor;

    public static float CornerRadius => _current.CornerRadius;
    public static string FontFamily => _current.FontFamily;

    public static FontWeight FontWeightNormal => _current.FontWeightNormal;
    public static FontWeight FontWeightBold => _current.FontWeightBold;
    public static FontWeight FontWeightLight => _current.FontWeightLight;

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Gibt alle verfügbaren Schemas zurück.
    /// </summary>
    public static IEnumerable<ColorSchema> GetAllSchemas()
    {
        EnsureCatalogInitialized();
        return ColorSchemaCatalog.Current?.Schemas ?? Enumerable.Empty<ColorSchema>();
    }

    /// <summary>
    /// Speichert das aktuelle Schema zurück in den Katalog.
    /// </summary>
    public static void SaveCurrentSchema()
    {
        try
        {
            EnsureCatalogInitialized();
            if (ColorSchemaCatalog.Current != null && _current != null)
            {
                ColorSchemaCatalog.Current.Update(_current);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save current schema: {ex.Message}");
        }
    }

    /// <summary>
    /// Überprüft, ob ein Schema mit dem angegebenen Key existiert.
    /// </summary>
    public static bool SchemaExists(string key)
    {
        EnsureCatalogInitialized();
        return ColorSchemaCatalog.Current?.Get(key) != null;
    }
}
