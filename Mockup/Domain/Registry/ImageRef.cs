// ======================================================================================
// FILE: Mockup/ImageRef.cs
// 
// ZWECK: 
// Repräsentiert eine Referenz auf ein Bild-Asset (SVG oder PNG) im Asset-System.
// Ermöglicht die lose Kopplung zwischen Controls und tatsächlichen Bild-Dateien.
//
// FUNKTIONALITÄTEN:
// - Identifikation von Bild-Assets über eindeutige IDs
// - Unterstützung verschiedener Bildformate (SVG, PNG)
// - Zentrale Verwaltung von Bildreferenzen im Asset-System
// - Serialisierbare Referenzen für JSON-Speicherung
//
// AUTOR: Claus Falkenstein
// VERSION: 1.0
// ======================================================================================

namespace Mockup.Domain.Registry;

/// <summary>
/// Repräsentiert eine Referenz auf ein Bild-Asset (SVG oder PNG) im Asset-System
/// </summary>
public class ImageRef
{
    /// <summary>
    /// Initialisiert eine neue Instanz der ImageRef-Klasse
    /// </summary>
    /// <param name="id">Eindeutige Identifikation des Bild-Assets</param>
    /// <param name="format">Format des Bildes (Standard: SVG)</param>
    public ImageRef(string id, ImageFormat format = ImageFormat.Svg)
    {
        Id = id;
        Format = format;
    }

    /// <summary>
    /// Eindeutige Identifikation des Bild-Assets im Asset-System
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Format des referenzierten Bildes
    /// </summary>
    public ImageFormat Format { get; set; } = ImageFormat.Svg;

    /// <summary>
    /// Gibt eine String-Repräsentation der Bildreferenz zurück
    /// </summary>
    /// <returns>String mit ID und Format der Bildreferenz</returns>
    public override string ToString() => $"{Id} ({Format})";
}
