// ============================================================================
// FILE: Mockup.JsonConverters/DesignControlConverter.cs
// ============================================================================

using Mockup.Registry;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mockup.JsonConverters;

public sealed class DesignControlConverter : JsonConverter<DesignControl>
{
    [ThreadStatic]
    private static int _writeDepth;

    private static readonly HashSet<string> _basicProps = new(StringComparer.Ordinal)
    {
        "TypeKey", "X", "Y", "Width", "Height", "ZIndex", "Name", "Id",
        "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
        "IsSelected", "IsActive", "ResizeStyle",
        "ParentBand", "ParentPage", "ParentBandPage",
        "VisualRect", "VisualContentRect", "ExplicitePreviewHeight","ExplicitePreviewWidth"
    };

    private static readonly HashSet<string> _ignoredProps = new(StringComparer.Ordinal)
    {
        "FlowContribution", "Bounds"
    };

    public override bool CanConvert(Type typeToConvert)
        => typeof(DesignControl).IsAssignableFrom(typeToConvert);

    public override DesignControl Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        string typeKey = FindTypeKey(root);
        if (string.IsNullOrWhiteSpace(typeKey))
            throw new JsonException("[DesignControlConverter] Missing typeKey in JSON");

        try
        {
            var ctrl = ControlRegistry.Create(typeKey)
                ?? throw new JsonException($"Unknown control typeKey: {typeKey}");

            LoadBasicProperties(root, ctrl);
            LoadDynamicProperties(root, ctrl, options);

            return ctrl;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, DesignControl value, JsonSerializerOptions options)
    {
        // Re-Entrancy Guard: wenn wir durch Prop-Serialisierung erneut hier landen,
        // schreiben wir nur die Basisdaten und KEINE props.
        if (_writeDepth > 0)
        {
            WriteBaseOnly(writer, value);
            return;
        }

        _writeDepth++;
        try
        {
            writer.WriteStartObject();

            writer.WriteString("typeKey", value.TypeKey);
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("width", value.Width);
            writer.WriteNumber("height", value.Height);
            writer.WriteNumber("zIndex", value.ZIndex);
            writer.WriteString("name", value.Name);

            var props = ExtractAllProperties(value, options);
            if (props.Count > 0)
            {
                writer.WritePropertyName("props");
                writer.WriteStartObject();
                foreach (var kv in props)
                {
                    writer.WritePropertyName(kv.Key);
                    kv.Value.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
        finally
        {
            _writeDepth--;
        }
    }

    private static void WriteBaseOnly(Utf8JsonWriter writer, DesignControl value)
    {
        writer.WriteStartObject();
        writer.WriteString("typeKey", value.TypeKey);
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("width", value.Width);
        writer.WriteNumber("height", value.Height);
        writer.WriteNumber("zIndex", value.ZIndex);
        writer.WriteString("name", value.Name);
        writer.WriteEndObject();
    }

    private static string FindTypeKey(JsonElement root)
    {
        if (root.TryGetProperty("typeKey", out var p) && p.ValueKind == JsonValueKind.String)
            return p.GetString() ?? "";

        if (root.TryGetProperty("TypeKey", out p) && p.ValueKind == JsonValueKind.String)
            return p.GetString() ?? "";

        if (root.TryGetProperty("typekey", out p) && p.ValueKind == JsonValueKind.String)
            return p.GetString() ?? "";

        if (root.TryGetProperty("TYPEKEY", out p) && p.ValueKind == JsonValueKind.String)
            return p.GetString() ?? "";

        return "";
    }

    private static void LoadBasicProperties(JsonElement root, DesignControl ctrl)
    {
        string[] propertyNames = { "x", "y", "width", "height", "zIndex", "name" };
        string[] pascalNames = { "X", "Y", "Width", "Height", "ZIndex", "Name" };

        for (int i = 0; i < propertyNames.Length; i++)
        {
            if (root.TryGetProperty(propertyNames[i], out var prop))
                SetBasicProperty(ctrl, pascalNames[i], prop);
            else if (root.TryGetProperty(pascalNames[i], out prop))
                SetBasicProperty(ctrl, pascalNames[i], prop);
        }
    }

    private static void SetBasicProperty(DesignControl ctrl, string propertyName, JsonElement value)
    {
        var property = ctrl.GetType().GetProperty(propertyName);
        if (property == null)
            return;

        try
        {
            if (property.PropertyType == typeof(float) && value.TryGetSingle(out float floatValue))
                property.SetValue(ctrl, floatValue);
            else if (property.PropertyType == typeof(int) && value.TryGetInt32(out int intValue))
                property.SetValue(ctrl, intValue);
            else if (property.PropertyType == typeof(long) && value.TryGetInt64(out long longValue))
                property.SetValue(ctrl, longValue);
            else if (property.PropertyType == typeof(string) && value.ValueKind == JsonValueKind.String)
                property.SetValue(ctrl, value.GetString());
            else if (property.PropertyType == typeof(bool) && value.ValueKind == JsonValueKind.True)
                property.SetValue(ctrl, true);
            else if (property.PropertyType == typeof(bool) && value.ValueKind == JsonValueKind.False)
                property.SetValue(ctrl, false);
        }
        catch
        {
            // bewusst robust: einzelne Felder dürfen fehlschlagen
        }
    }

    private static void LoadDynamicProperties(JsonElement root, DesignControl ctrl, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("props", out var propsProp) && propsProp.ValueKind == JsonValueKind.Object)
        {
            LoadPropertiesFromPropsObject(ctrl, propsProp, options);
            return;
        }

        if (root.TryGetProperty("Props", out propsProp) && propsProp.ValueKind == JsonValueKind.Object)
        {
            LoadPropertiesFromPropsObject(ctrl, propsProp, options);
            return;
        }

        LoadPropertiesFromRoot(ctrl, root, options);
    }

    private static void LoadPropertiesFromPropsObject(DesignControl ctrl, JsonElement propsElement, JsonSerializerOptions options)
    {
        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in propsElement.EnumerateObject())
            dict[kv.Name] = kv.Value.Clone();

        if (dict.Count > 0)
            ControlPropSchemaCache.ApplyProps(ctrl, dict, options);
    }

    private static void LoadPropertiesFromRoot(DesignControl ctrl, JsonElement root, JsonSerializerOptions options)
    {
        var excludedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "typeKey",
            "x", "y", "width", "height", "zIndex", "name",
            "props"
        };

        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in root.EnumerateObject())
        {
            if (!excludedProps.Contains(prop.Name))
                dict[prop.Name] = prop.Value.Clone();
        }

        if (dict.Count > 0)
            ControlPropSchemaCache.ApplyProps(ctrl, dict, options);
    }

    private static Dictionary<string, JsonElement> ExtractAllProperties(DesignControl control, JsonSerializerOptions options)
    {
        var safeOptions = CreateSafePropOptions(options);

        try
        {
            var result = ControlPropSchemaCache.ExtractProps(control, safeOptions);
            if (result.Count > 0)
                return result;

            return ExtractPropertiesManually(control, safeOptions);
        }
        catch
        {
            return ExtractPropertiesManually(control, safeOptions);
        }
    }

    private static Dictionary<string, JsonElement> ExtractPropertiesManually(DesignControl control, JsonSerializerOptions safeOptions)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var properties = control.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => !_basicProps.Contains(p.Name))
            .Where(p => !_ignoredProps.Contains(p.Name))
            .Where(HasControlPropAttribute)
            .Where(p => !HasJsonIgnore(p))
            .Where(p => !IsDesignControlRelatedType(p.PropertyType))
            .ToList();

        foreach (var prop in properties)
        {
            object? propValue;
            try
            {
                propValue = prop.GetValue(control);
            }
            catch
            {
                continue;
            }

            if (propValue == null)
                continue;

            try
            {
                // ohne Serialize→Parse Roundtrip
                result[prop.Name] = JsonSerializer.SerializeToElement(propValue, prop.PropertyType, safeOptions);
            }
            catch
            {
                // ignore
            }
        }

        return result;
    }

    private static bool IsDesignControlRelatedType(Type t)
    {
        if (typeof(DesignControl).IsAssignableFrom(t))
            return true;

        if (t.IsGenericType)
        {
            foreach (var ga in t.GetGenericArguments())
            {
                if (typeof(DesignControl).IsAssignableFrom(ga))
                    return true;
            }
        }

        return false;
    }

    private static bool HasJsonIgnore(PropertyInfo p)
        => p.GetCustomAttributes(inherit: true).Any(a => a is JsonIgnoreAttribute);

    private static bool HasControlPropAttribute(PropertyInfo p)
    {
        foreach (var a in p.GetCustomAttributes(inherit: true))
        {
            var n = a.GetType().Name;
            if (n.Equals("ControlPropAttribute", StringComparison.Ordinal) ||
                n.Equals("ControlProp", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static JsonSerializerOptions CreateSafePropOptions(JsonSerializerOptions options)
    {
        var clone = new JsonSerializerOptions(options);

        for (int i = clone.Converters.Count - 1; i >= 0; i--)
        {
            var c = clone.Converters[i];
            var t = c.GetType();

            if (t == typeof(DesignControlConverter))
            {
                clone.Converters.RemoveAt(i);
                continue;
            }

            try
            {
                if (c.CanConvert(typeof(DesignControl)))
                    clone.Converters.RemoveAt(i);
            }
            catch
            {
                // ignore
            }
        }

        return clone;
    }
}
