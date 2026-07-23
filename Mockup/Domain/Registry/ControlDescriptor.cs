/// <summary>
/// Registry-facing descriptor for a single control type.
/// Built once at startup (or on demand) and used across the app.
/// 
/// Key design goals:
/// - Keep <see cref="TypeKey"/> stable: it is the JSON "Type".
/// - Provide a single, consistent <see cref="Factory"/> for instance creation.
/// - Cache default/min/max sizes from a preview instance (if available).
/// - Keep UI hints (DisplayName/Group/HiddenInToolbox) close to the type.
/// </summary>

namespace Mockup.Registry;

public sealed class ControlDescriptor
{
    /// <summary>
    /// Stable, unique type key (case-insensitive lookup). This becomes the "Type" in JSON.
    /// </summary>
    public required string TypeKey { get; init; }

    /// <summary>
    /// Factory to create a fresh instance of the control. Single source of truth for creation.
    /// WARNING: Must set TypeKey on the created control!
    /// </summary>
    public required Func<DesignControl> Factory { get; init; }

    /// <summary>Human-readable display name for UIs.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Logical UI group.</summary>
    public string Group { get; init; } = "Basic";

    /// <summary>If true, the control should be hidden in toolbox/pickers by default.</summary>
    public bool HiddenInToolbox { get; init; } = false;

    /// <summary>Optional relative path to the curated toolbox preview image.</summary>
    public string PreviewImage { get; init; } = "";

    /// <summary>Optional default size hints captured from a preview instance.</summary>
    public float? DefaultWidth { get; init; }
    public float? DefaultHeight { get; init; }

    /// <summary>Optional min/max size hints captured from a preview instance.</summary>
    public float? MinWidth { get; init; }
    public float? MinHeight { get; init; }
    public float? MaxWidth { get; init; }
    public float? MaxHeight { get; init; }

    /// <summary>The CLR type implementing this control.</summary>
    public required Type Type { get; init; }

    /// <summary>List of serializable design properties discovered via [ControlProp].</summary>
    public required List<ControlPropSpec> Props { get; init; }

    /// <summary>Create a new instance of the control via the configured <see cref="Factory"/>.</summary>
    public DesignControl CreateInstance() => Factory();

    /// <summary>For debugging.</summary>
    public override string ToString() => $"{TypeKey} ({Type.Name})";
}
