// ======================================================================================
// FILE: Mockup.Domain/Registry/ControlPropSchemaCache.cs
//
// ZWECK:
//  - Einmaliges Scannen aller [ControlProp]-Properties pro Control-Typ
//  - Caching eines "Schemas" (ControlPropSpec-Liste) pro Type
//  - Schnelle Getter/Setter-Delegates (Expressions) für JSON-Serializer
//  - Case-insensitive Property-Matching für JSON-Kompatibilität
//
// WICHTIG:
//  - Wird von ControlRegistry, BaseScreenSerializer, TemplateSerializer usw. genutzt.
//  - Keine Abhängigkeit zu DTOs – nur Reflection & JsonElement.
//  - Case-insensitive Property-Lookups für JSON-Flexibilität
//
// AUTOR: Claus + ChatGPT (MO36)
// ======================================================================================

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Mockup.Registry;

/// <summary>
/// Caches the "schema" of design-time serializable properties for each control type.
/// The schema is the list of properties marked with [ControlProp], along with fast
/// getter/setter delegates and the CLR type for JSON conversion.
///
/// Why a cache?
/// - Reflection ist relativ teuer → wir scannen jeden Control-Typ nur einmal.
/// - Expression-basierte Getter/Setter sind wesentlich schneller/allokationsfrei
///   als PropertyInfo.GetValue/SetValue in Hotpaths.
/// </summary>
public static class ControlPropSchemaCache
{
    /// <summary>
    /// Thread-safe cache: control type -> list of property specs.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, List<ControlPropSpec>> _cache = new();

    /// <summary>
    /// Thread-safe cache for case-insensitive property lookup.
    /// Key: Control Type, Value: Dictionary with case-insensitive keys -> ControlPropSpec
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Dictionary<string, ControlPropSpec>> _caseInsensitiveLookupCache = new();

    /// <summary>
    /// Get (or build) the list of [ControlProp]-marked properties for a control type.
    /// Returned list is stable and can be enumerated freely.
    /// </summary>
    public static List<ControlPropSpec> Get(Type controlType)
    {
        return _cache.GetOrAdd(controlType, BuildSchema);
    }

    /// <summary>
    /// Get a case-insensitive lookup dictionary for the control type.
    /// Supports: "text", "Text", "TEXT", etc. all mapping to the same property.
    /// </summary>
    public static Dictionary<string, ControlPropSpec> GetCaseInsensitiveLookup(Type controlType)
    {
        return _caseInsensitiveLookupCache.GetOrAdd(controlType, BuildCaseInsensitiveLookup);
    }

    /// <summary>
    /// Build the schema for a single type by scanning public instance properties,
    /// honoring [ControlProp], and precompiling accessors.
    /// </summary>
    private static List<ControlPropSpec> BuildSchema(Type controlType)
    {
        var result = new List<ControlPropSpec>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Case-insensitive duplicate check

        // Inklusive geerbter Properties; gefiltert auf public instance
        var props = controlType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var p in props)
        {
            // Indexer und nicht les/ schreibbare Properties überspringen
            if (p.GetIndexParameters().Length != 0) continue;
            if (!p.CanRead || !p.CanWrite) continue;

            // Nur Properties, die explizit mit [ControlProp] markiert sind
            var attr = p.GetCustomAttribute<ControlPropAttribute>(inherit: true);
            if (attr is null) continue;

            // JSON-Property-Name: Attribut-Key oder CLR-Propertyname
            var key = attr.Key ?? p.Name;

            // Doppelte Keys im gleichen Typenbaum hart ablehnen (case-insensitive)
            if (!seenKeys.Add(key))
                throw new InvalidOperationException(
                    $"Duplicate [ControlProp] key '{key}' on '{controlType.FullName}'. " +
                    "Use distinct keys via ControlPropAttribute.Key to disambiguate.");

            // Getter/Setter vorab kompilieren (Fastpath zur Laufzeit)
            var getter = CompileGetter(controlType, p);
            var setter = CompileSetter(controlType, p);

            result.Add(new ControlPropSpec
            {
                Key = key,
                PropType = p.PropertyType,
                Getter = getter,
                Setter = setter,
                OriginalPropertyName = p.Name // Für Debugging/Referenz
            });
        }

        // Deterministische Sortierung → schönere/stabilere JSON-Diffs
        result.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        return result;
    }

    /// <summary>
    /// Build a case-insensitive lookup dictionary from the schema.
    /// </summary>
    private static Dictionary<string, ControlPropSpec> BuildCaseInsensitiveLookup(Type controlType)
    {
        var specs = Get(controlType);
        var lookup = new Dictionary<string, ControlPropSpec>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            // Original-Key (wie im Attribut definiert)
            lookup[spec.Key] = spec;

            // Kleinbuchstaben-Version
            var lowerKey = spec.Key.ToLowerInvariant();
            if (!lookup.ContainsKey(lowerKey))
            {
                lookup[lowerKey] = spec;
            }

            // PascalCase-Version (erster Buchstabe groß, Rest klein)
            var pascalKey = char.ToUpperInvariant(spec.Key[0]) +
                           (spec.Key.Length > 1 ? spec.Key.Substring(1).ToLowerInvariant() : "");
            if (!lookup.ContainsKey(pascalKey))
            {
                lookup[pascalKey] = spec;
            }

            // CamelCase-Version (erster Buchstabe klein)
            var camelKey = char.ToLowerInvariant(spec.Key[0]) +
                          (spec.Key.Length > 1 ? spec.Key.Substring(1) : "");
            if (!lookup.ContainsKey(camelKey))
            {
                lookup[camelKey] = spec;
            }

            // Original Property Name (falls unterschiedlich von Key)
            if (!string.IsNullOrEmpty(spec.OriginalPropertyName) &&
                !lookup.ContainsKey(spec.OriginalPropertyName))
            {
                lookup[spec.OriginalPropertyName] = spec;
            }
        }

        return lookup;
    }

    /// <summary>
    /// Compile: obj => (object)((TDeclaring)obj).Property
    /// </summary>
    private static Func<object, object?> CompileGetter(Type declaringType, PropertyInfo prop)
    {
        var obj = Expression.Parameter(typeof(object), "obj");
        var typedObj = Expression.Convert(obj, declaringType);
        var access = Expression.Property(typedObj, prop);
        var box = Expression.Convert(access, typeof(object));

        return Expression.Lambda<Func<object, object?>>(box, obj).Compile();
    }

    /// <summary>
    /// Compile: (obj, val) => ((TDeclaring)obj).Property = (TProp)val;
    /// Handles null for value types by using default(TProp).
    /// </summary>
    private static Action<object, object?> CompileSetter(Type declaringType, PropertyInfo prop)
    {
        var obj = Expression.Parameter(typeof(object), "obj");
        var val = Expression.Parameter(typeof(object), "val");

        var typedObj = Expression.Convert(obj, declaringType);
        var targetType = prop.PropertyType;

        Expression typedValue;
        if (targetType.IsValueType)
        {
            // val != null ? (TProp)val : default(TProp)
            var tmp = Expression.Variable(typeof(object), "tmp");
            var assign = Expression.Assign(tmp, val);
            var whenNotNull = Expression.Convert(tmp, targetType);
            var defaultVal = Expression.Default(targetType);
            var cond = Expression.Condition(
                Expression.NotEqual(tmp, Expression.Constant(null, typeof(object))),
                whenNotNull,
                defaultVal);

            typedValue = Expression.Block(new[] { tmp }, assign, cond);
        }
        else
        {
            typedValue = Expression.Convert(val, targetType);
        }

        var call = Expression.Call(typedObj, prop.SetMethod!, typedValue);
        return Expression.Lambda<Action<object, object?>>(call, obj, val).Compile();
    }

    /// <summary>
    /// Deserialize a JSON element to the requested CLR type using the given options.
    /// Uses the efficient JsonElement.Deserialize overload (no string allocation).
    /// </summary>
    public static object? ReadJson(JsonElement elem, Type type, JsonSerializerOptions opts)
        => elem.Deserialize(type, opts);

    /// <summary>
    /// Serialize a value into a detached JsonElement.
    /// Uses SerializeToElement to avoid serialize→parse roundtrips.
    /// </summary>
    public static JsonElement WriteJson(object? value, Type type, JsonSerializerOptions opts)
    {
        if (value is null)
        {
            // Null korrekt serialisieren
            using var doc = JsonDocument.Parse("null");
            return doc.RootElement.Clone();
        }

        return JsonSerializer.SerializeToElement(value, type, opts);
    }

    // ───────────────────────────────────────────────────────────────
    //  OPTIONALE HELFER (für Serializer, Clone, etc.)
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Apply a Props dictionary to a target instance using the cached schema.
    /// Unknown keys are ignored. Per-item (de)serialization errors are swallowed.
    /// Case-insensitive property matching.
    /// </summary>
    public static void ApplyProps(
        object target,
        IReadOnlyDictionary<string, JsonElement>? props,
        JsonSerializerOptions opts)
    {
        if (props is null || props.Count == 0) return;

        var lookup = GetCaseInsensitiveLookup(target.GetType());
        if (lookup.Count == 0) return;

        foreach (var kvp in props)
        {
            // Case-insensitive lookup
            if (!lookup.TryGetValue(kvp.Key, out var spec))
                continue;

            object? obj;
            try
            {
                obj = ReadJson(kvp.Value, spec.PropType, opts);
            }
            catch
            {
                // Einzelner Wert kaputt? → Überspringen, Rest weiterladen
                continue;
            }

            try
            {
                spec.Setter(target, obj);
            }
            catch
            {
                // Setter wirft? → Für Robustheit bewusst ignorieren (optional loggen)
            }
        }
    }

    /// <summary>
    /// Extract all [ControlProp] values from a source instance into a Props dictionary.
    /// Null values are skipped (smaller JSON); callsites may store nulls if desired.
    /// Dictionary uses case-insensitive comparer.
    /// </summary>
    public static Dictionary<string, JsonElement> ExtractProps(object source, JsonSerializerOptions opts)
    {
        var specs = Get(source.GetType());
        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var val = spec.Getter(source);
            if (val is null) continue;

            dict[spec.Key] = WriteJson(val, spec.PropType, opts);
        }

        return dict;
    }

    /// <summary>
    /// Find a property spec by name with case-insensitive matching.
    /// Returns true if found, false otherwise.
    /// </summary>
    public static bool TryGetPropertySpec(Type controlType, string propertyName, out ControlPropSpec? spec)
    {
        var lookup = GetCaseInsensitiveLookup(controlType);
        return lookup.TryGetValue(propertyName, out spec);
    }

    /// <summary>
    /// Get all property names for a control type (for debugging/UI).
    /// </summary>
    public static List<string> GetPropertyNames(Type controlType)
    {
        var specs = Get(controlType);
        return specs.Select(s => s.Key).ToList();
    }

    /// <summary>
    /// Get all property names with case-insensitive variants (for debugging).
    /// </summary>
    public static Dictionary<string, string> GetPropertyNameVariants(Type controlType)
    {
        var lookup = GetCaseInsensitiveLookup(controlType);
        var result = new Dictionary<string, string>();

        foreach (var kvp in lookup)
        {
            result[kvp.Key] = kvp.Value.Key;
        }

        return result;
    }

    /// <summary>
    /// Kopiert alle [ControlProp]-markierten Werte vom Quellobjekt zum Zielobjekt.
    /// Nutzt das Schema-Caching für maximale Performance.
    /// </summary>
    public static void CopyProps(object source, object target)
    {
        if (source.GetType() != target.GetType())
            throw new ArgumentException("Source and target must be of the same type");

        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        // 1️⃣ Alle Properties des Quellobjekts auslesen
        var props = ExtractProps(source, opts);

        // 2️⃣ Diese Properties auf das Zielobjekt anwenden
        ApplyProps(target, props, opts);
    }

    /// <summary>
    /// Clears the cache (e.g., after hot-loading a plugin assembly). Safe to call anytime.
    /// </summary>
    public static void Reset()
    {
        _cache.Clear();
        _caseInsensitiveLookupCache.Clear();
    }
}
