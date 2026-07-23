using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Mockup.Registry;

/// <summary>
/// Central registry for all designable controls (subclasses of <see cref="DesignControl"/>).
/// 
/// Responsibilities:
/// 1) Scan one or more assemblies for concrete DesignControl types
/// 2) Build a descriptor map (by string key and by Type)
/// 3) Provide creation (factory) and metadata lookup
/// 
/// Design principles:
/// - Keys are case-insensitive (robust JSON interop)
/// - We never exclude concrete controls here (e.g., TemplateRef). UI decides visibility.
/// - Safe and lazy initialization: if you forget to call Initialize(), the first use triggers it.
/// 
/// Typical usage:
/// - App start: ControlRegistry.Initialize(typeof(SomeControl).Assembly, ...);
///   (optional; otherwise lazy init will scan currently loaded assemblies)
/// - Lookup: ControlRegistry.GetDescriptor("button"), ControlRegistry.Create("templateRef")
/// </summary>
public static class ControlRegistry
{
    /// <summary>
    /// Lookup by TypeKey (e.g., "button", "templateRef"). Case-insensitive.
    /// </summary>
    private static readonly Dictionary<string, ControlDescriptor> _byKey =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Lookup by CLR <see cref="Type"/>.
    /// </summary>
    private static readonly Dictionary<Type, ControlDescriptor> _byType = new();

    /// <summary>
    /// Initialization guard for lazy init.
    /// </summary>
    private static volatile bool _isInitialized;

    /// <summary>
    /// Lock for Init/Register to be threadsafe.
    /// </summary>
    private static readonly object _initLock = new();

    /// <summary>
    /// All known descriptors (forces lazy initialization on first access).
    /// </summary>
    public static IReadOnlyCollection<ControlDescriptor> AllDescriptors
    {
        get { EnsureInitialized(); return _byKey.Values; }
    }

    /// <summary>
    /// Scans given assemblies (or all currently loaded ones) and builds the registry.
    /// Safe to call multiple times; it resets the registry each time.
    /// </summary>
    public static void Initialize(params Assembly[] assemblies)
    {
        lock (_initLock)
        {
            var sourceAssemblies = (assemblies is { Length: > 0 })
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies();

            _byKey.Clear();
            _byType.Clear();

            foreach (var asm in sourceAssemblies)
            {
                foreach (var type in SafeGetTypes(asm))
                {
                    // Only non-abstract, non-generic, public DesignControl subclasses
                    if (!typeof(DesignControl).IsAssignableFrom(type)) continue;
                    if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                    if (!type.IsPublic && !type.IsNestedPublic) continue;

                    RegisterType(type);
                }
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Registers all eligible DesignControls from a single assembly.
    /// Use this when you dynamically load a plugin assembly at runtime.
    /// </summary>
    public static void RegisterAssembly(Assembly asm)
    {
        EnsureInitialized(); // make sure dictionaries exist
        lock (_initLock)
        {
            foreach (var type in SafeGetTypes(asm))
            {
                if (!typeof(DesignControl).IsAssignableFrom(type)) continue;
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                if (!type.IsPublic && !type.IsNestedPublic) continue;

                RegisterType(type);
            }
        }
    }

    /// <summary>
    /// Core: turn one CLR Type into a <see cref="ControlDescriptor"/> and store it.
    /// </summary>
    private static void RegisterType(Type type)
    {
        // 1) Optional attribute with metadata (key, display name, group, ...)
        var attr = type.GetCustomAttribute<ControlTypeAttribute>();

        // 2) Decide TypeKey, DisplayName, Group
        //    ⚠️ Keep TypeKey stable; it is persisted in JSON.
        var typeKey = !string.IsNullOrWhiteSpace(attr?.Key) ? attr!.Key! : type.Name;
        var displayName = !string.IsNullOrWhiteSpace(attr?.DisplayName) ? attr!.DisplayName! : type.Name;
        var group = attr?.Group ?? string.Empty;

        // 3) Hard fail on duplicate keys (fail fast rather than silently overriding)
        if (_byKey.ContainsKey(typeKey))
            throw new InvalidOperationException(
                $"Duplicate control key '{typeKey}' for types '{_byKey[typeKey].Type.FullName}' and '{type.FullName}'.");

        // 4) Pre-resolve all [ControlProp] properties via schema cache
        var props = ControlPropSchemaCache.Get(type);

        // 5) Optional: create a preview instance to capture defaults (width/height/min/max)
        DesignControl? preview = null;
        try
        {
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                preview = (DesignControl)Activator.CreateInstance(type)!;
                // Setze TypeKey auch auf der Preview-Instanz
                preview.TypeKey = typeKey;
            }
        }
        catch (Exception ex)
        {
            // Loggen, nicht ignorieren
            Debug.WriteLine($"Failed to create preview instance for {type.FullName}: {ex.Message}");
        }

        var descriptor = new ControlDescriptor
        {
            TypeKey = typeKey,
            DisplayName = displayName,
            Group = group,
            PreviewImage = ResolvePreviewImage(attr, typeKey),
            Type = type,
            Props = props,

            // Use a factory so Create(...) doesn't have to reinvent instantiation logic.
            Factory = () =>
            {
                var control = (DesignControl)Activator.CreateInstance(type)!;
                control.TypeKey = typeKey;
                return control;
            },

            DefaultWidth = preview?.Width ?? 0,
            DefaultHeight = preview?.Height ?? 0,
            MinWidth = preview?.MinWidth ?? 0,
            MinHeight = preview?.MinHeight ?? 0,
            MaxWidth = preview?.MaxWidth ?? float.PositiveInfinity,
            MaxHeight = preview?.MaxHeight ?? float.PositiveInfinity
        };

        _byKey[typeKey] = descriptor;
        _byType[type] = descriptor;

        // ❗Important:
        // Do NOT filter out TemplateRef or other core controls here.
        // If you don't want a control in the Toolbox, filter at the UI layer (e.g., HiddenInToolbox flag),
        // but keep it registered so JSON load/save can always resolve it.
    }

    /// <summary>
    /// Resolves the static toolbox preview image. Explicitly curated paths win;
    /// all remaining controls use the centralized Generated/&lt;TypeKey&gt;.png convention.
    /// </summary>
    private static string ResolvePreviewImage(ControlTypeAttribute? attr, string typeKey)
    {
        if (!string.IsNullOrWhiteSpace(attr?.PreviewImage))
            return attr!.PreviewImage.Trim().TrimStart('/', '\\').Replace('\\', '/');

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string fileName = new(typeKey
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray());

        return $"Generated/{fileName}.png";
    }

    /// <summary>
    /// Get all loadable types from an assembly, even if some fail to load.
    /// </summary>
    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
        catch { return Array.Empty<Type>(); }
    }

    /// <summary>
    /// Try get descriptor by TypeKey (case-insensitive).
    /// </summary>
    public static bool TryGet(string typeKey, out ControlDescriptor desc)
    {
        EnsureInitialized();
        return _byKey.TryGetValue(typeKey, out desc!);
    }

    /// <summary>
    /// Try get descriptor by CLR type.
    /// </summary>
    public static bool TryGet(Type type, out ControlDescriptor desc)
    {
        EnsureInitialized();
        return _byType.TryGetValue(type, out desc!);
    }

    /// <summary>
    /// Create a fresh instance of the control behind the given TypeKey.
    /// Returns null if the key is unknown.
    /// </summary>
    public static DesignControl? Create(string key)
    {
        EnsureInitialized();
        return _byKey.TryGetValue(key, out var desc)
            ? desc.Factory()  // ← Factory setzt TypeKey (wenn korrigiert)
            : null;
    }

    /// <summary>
    /// Get descriptor by TypeKey (or null if unknown).
    /// </summary>
    public static ControlDescriptor? GetDescriptor(string key)
    {
        EnsureInitialized();
        return _byKey.TryGetValue(key, out var desc) ? desc : null;
    }

    /// <summary>
    /// Get descriptor by CLR type (or null if unknown).
    /// </summary>
    public static ControlDescriptor? GetDescriptor(Type type)
    {
        EnsureInitialized();
        return _byType.TryGetValue(type, out var desc) ? desc : null;
    }

    /// <summary>
    /// Convenience for diagnostics/logging.
    /// </summary>
    public static IEnumerable<string> KnownTypeKeys()
    {
        EnsureInitialized();
        return _byKey.Keys.OrderBy(k => k).ToArray();
    }

    /// <summary>
    /// Force a full rebuild (e.g., if you load a different set of assemblies).
    /// </summary>
    public static void Reset(params Assembly[] assemblies) => Initialize(assemblies);

    /// <summary>
    /// Lazy init if needed. Keeps the rest of the code clean and safe.
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_isInitialized) return;
        lock (_initLock)
        {
            if (_isInitialized) return;
            Initialize();
        }
    }
}
