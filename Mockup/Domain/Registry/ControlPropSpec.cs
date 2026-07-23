namespace Mockup.Registry;


/// <summary>
/// Describes one design property discovered via [ControlProp].
/// It carries minimal info required by the serializer to read/write JSON efficiently.
/// The actual getter/setter delegates are precompiled in the schema cache.
/// </summary>
public sealed class ControlPropSpec
{
    /// <summary>JSON key used to persist this property.</summary>
    public required string Key { get; init; }

    /// <summary>CLR type of the property (used to pick the correct JSON conversion).</summary>
    public required Type PropType { get; init; }

    /// <summary>Original CLR property name (for debugging/reference).</summary>
    public string? OriginalPropertyName { get; init; }

    /// <summary>Fast getter (compiled once), called during save.</summary>
    public required Func<object, object?> Getter { get; init; }

    /// <summary>Fast setter (compiled once), called during load.</summary>
    public required Action<object, object?> Setter { get; init; }

    /// <summary>For debugging.</summary>
    public override string ToString() => $"{Key} ({PropType.Name})";
}
