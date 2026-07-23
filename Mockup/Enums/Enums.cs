// ======================================================================================
// FILE: Mockup/DesignEnums.cs
//
// ZWECK:
// Sammlung aller Enumerationen für den Mockup-Designer. Definiert Betriebsmodi,
// Ausrichtungsoptionen, Steuerelement-Varianten und visuelle Einstellungen für
// die Konfiguration des Designers und seiner Komponenten.
//
// FUNKTIONALITÄTEN:
// - Designer-Betriebsmodi und Snap-Einstellungen
// - Steuerelement-Varianten und Interaktionsaktionen
// - Typografische Einstellungen und Größenangaben
// - Größenanpassungs- und Skalierungsmodi
// - Bildausrichtung und Magnet-Ausrichtungsoptionen
//
// AUTOR: Claus Falkenstein
// VERSION: 1.0
// ======================================================================================

namespace Mockup;

#region === DESIGNER MODES ===

// ======================================================================================
// ENUM: DesignerMode
// ZWECK:
// Definiert die Betriebsmodi des Designers für unterschiedliche Bearbeitungskontexte.
// ======================================================================================
public enum DesignerMode
{
    // Screen-Modus: Bearbeitung von Bildschirm-Layouts und Screens
    Screen,

    // Template-Modus: Erstellung und Bearbeitung von Wiederverwendbaren Vorlagen
    Template,
}

public enum DesignerMouseMode
{
    None,
    ResizingBand,
    TogglingBand,
    SelectingBand,

    // === Control interaction ===
    ControlPressed,
    DragControls,
}

// ======================================================================================
// ENUM: SnapMode
// ZWECK:
// Steuert das magnetische Einrastverhalten von Elementen während der Bewegung und Größenänderung.
// ======================================================================================
public enum SnapMode
{
    // Kein Einrasten - freie Positionierung
    NoSnap = 0,

    // Einrasten an Rasterpunkten
    GridSnap = 1,

    // Intelligentes Einrasten an anderen Elementen und Hilfslinien
    IntelliSnap = 2,
}

#endregion

#region === CONTROL VARIANTS ===

// ======================================================================================
// ENUM: ControlVariant
// ZWECK:
// Definiert visuelle Varianten für Steuerelemente zur Darstellung unterschiedlicher
// Zustände und Prioritäten im Design-System.
// ======================================================================================
public enum ControlVariant
{
    // Standard-Variante ohne besondere Hervorhebung
    CUSTOM,

    // Primäre Aktion oder Haupt-Element
    Primary,

    // Akzent-Farbe für sekundäre Elemente
    Accent,

    // Informations-Zustand
    Info,

    // Warnungs-Zustand
    Warning,

    // Fehler-Zustand
    Error,
}

// ======================================================================================
// ENUM: ControlClickAction
// ZWECK:
// Definiert mögliche Aktionen die bei Klick auf ein Steuerelement ausgelöst werden können.
// ======================================================================================
public enum ControlClickAction
{
    // Keine Aktion bei Klick
    None = 0,

    // Umschalten des Expandier-Zustands
    ToggleExpand,

    // Navigation zu einem anderen Screen
    Navigate,

    // Navigation zurück zur vorherigen Ansicht
    NavigateBack,

    // Navigation zur Startansicht
    NavigateHome,

    // Bearbeitungsmodus aktivieren
    Edit,

    // Benutzerdefinierte Aktion
    Custom,
}

#endregion

#region === SIZE PRESET ===
public enum ButtonSizePreset
{
    Small,
    Normal,
    Large
}

#endregion

#region === LAYOUT CONTAINERS ===

// ======================================================================================
// ENUM: BandType
// ZWECK:
// Definiert die Arten von Layout-Containern (Bands) für die strukturierte Anordnung
// von Steuerelementen auf einem Screen.
// ======================================================================================
public enum BandType
{
    // Kopfbereich des Screens (typischerweise für Navigation)
    Header,

    // Fußbereich des Screens (typischerweise für Aktionen)
    Footer,

    // Benutzerdefinierter Containerbereich (Teil des scrollbaren Main-Stacks)
    Custom,

    Popup,
}

#endregion

#region === TYPOGRAPHY ===

// ======================================================================================
// ENUM: FontSize
// ZWECK:
// Definiert standardisierte Schriftgrößen für ein konsistentes Text-Design-System.
// Werte entsprechen Pixel-Größen.
// ======================================================================================
public enum FontSize
{
    // Sehr große Schrift für Hauptüberschriften (32px)
    Display = 32,

    // Große Schrift für Bereichsüberschriften (24px)
    Heading = 24,

    // Standard-Titelgröße (20px)
    Title = 20,

    // Untertitel-Größe (18px)
    Subtitle = 18,

    // Standard-Textkörper (16px)
    Body = 16,

    // Sehr kleine Beschriftung (11px)
    SmallLabel = 11,

    // Standard-Beschriftung (13px)
    Label = 13,

    // Große Beschriftung (16px)
    LargeLabel = 16,

    // Hilfstext und Hinweise (12px)
    Hint = 12,
}

#endregion

#region === RESIZE & SCALING ===

// ======================================================================================
// ENUM: ResizeStyles
// ZWECK:
// Definiert Verhaltensweisen für die Größenänderung von Steuerelementen.
// ======================================================================================
public enum ResizeStyles
{
    // Keine Größenänderung möglich
    None,

    // Freie Größenänderung in beide Richtungen
    ResizeAll,

    // Nur Breitenänderung möglich
    WidthOnly,

    // Nur Höhenänderung möglich
    HeightOnly,

    // Seitenverhältnis beibehalten bei Größenänderung
    KeepRatio,
}

// ======================================================================================
// ENUM: ControlResizeHandle
// ZWECK:
// Definiert die verfügbaren Ziehpunkte für die manuelle Größenänderung von Steuerelementen.
// ======================================================================================
public enum ControlResizeHandle
{
    // Kein Ziehpunkt
    None,

    // Oben links
    TopLeft,

    // Oben mittig
    Top,

    // Oben rechts
    TopRight,

    // Links mittig
    Left,

    // Rechts mittig
    Right,

    // Unten links
    BottomLeft,

    // Unten mittig
    Bottom,

    // Unten rechts
    BottomRight,
}

// ======================================================================================
// ENUM: TemplateScaleMode
// ZWECK:
// Definiert Skalierungsverhalten für Templates beim Einfügen in unterschiedlich große Container.
// ======================================================================================
public enum TemplateScaleMode
{
    // Keine Skalierung - Originalgröße beibehalten
    None,

    // Komplett in Container einpassen
    Fit,

    // Auf Container-Breite skalieren
    FillWidth,

    // Auf Container-Höhe skalieren
    Stretch,
}

// ======================================================================================
// ENUM: ImageScaleMode
// ZWECK:
// Definiert Skalierungsmodi für Bilddarstellung innerhalb von Steuerelementen.
// ======================================================================================
public enum ImageScaleMode
{
    // Keine Skalierung - Originalgröße
    None,

    // Komplett sichtbar in Container einpassen
    ScaleToFit,

    // Auf Container-Breite skalieren, Höhe proportional
    ScaleByWidth,

    // Auf Container-Höhe skalieren, Breite proportional
    ScaleByHeight,

    // Verzerrte Skalierung auf Container-Maße
    Stretch,

    // Gleichmäßige Skalierung ohne Beschnitt
    Uniform,

    // Gleichmäßige Skalierung mit Beschnitt
    Fill,
}

#endregion

#region === ALIGNMENT & POSITIONING ===

// ======================================================================================
// ENUM: HorizontalImageAlignment
// ZWECK:
// Definiert horizontale Ausrichtungsoptionen für Bilder innerhalb ihrer Container.
// ======================================================================================
public enum HorizontalImageAlignment
{
    // Links ausrichten
    Left,

    // Rechts ausrichten
    Right,
}

// ======================================================================================
// ENUM: VerticalImageAlignment
// ZWECK:
// Definiert vertikale Ausrichtungsoptionen für Bilder innerhalb ihrer Container.
// ======================================================================================
public enum VerticalImageAlignment
{
    // Oben ausrichten
    Top,

    // Unten ausrichten
    Bottom,
}

// ======================================================================================
// ENUM: MagnetEdge
// ZWECK:
// Definiert magnetische Kanten für die intelligente Ausrichtung von Elementen.
// ======================================================================================
public enum MagnetEdge
{
    // Linke Kante
    Left,

    // Horizontale Mitte
    CenterX,

    // Rechte Kante
    Right,
}

#endregion

#region === MEDIA FORMATS ===

// ======================================================================================
// ENUM: ImageFormat
// ZWECK:
// Definiert unterstützte Bildformate für die Verwendung im Designer.
// ======================================================================================
public enum ImageFormat
{
    // Vektorbasiertes SVG-Format
    Svg,

    // Rasterbasiertes PNG-Format
    Png,
}

#endregion
