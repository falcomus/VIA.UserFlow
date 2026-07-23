using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace Mockup.ColorSystem;

public sealed class ColorSchemaCatalog
{
    // 🔥 Globale Singleton-Instanz
    public static ColorSchemaCatalog Current { get; private set; } = null!;

    public static void Initialize(string filePath)
    {
        Current = new ColorSchemaCatalog(filePath);
        Current.Load();
    }


    // ============================================================
    // STATIC Fassade (abwärtskompatibel, KEINE CS0111-Duplikate)
    // ============================================================

    public static ObservableCollection<ColorSchema> All
        => Current == null ? [] : Current.Schemas;

    public static string GenerateUniqueKey(string name) => Current.GenerateUniqueKeyInternal(name);

    public static ColorSchema? GetSchema(string key) => Current.Get(key);

    public static ColorSchema CreateSchema(string name) => Current.CreateNew(name);


    public static void AddSchema(ColorSchema schema) => Current.Add(schema);

    public static void UpdateSchema(ColorSchema schema) => Current.Update(schema);

    public static bool RemoveSchema(string key) => Current.Remove(key);


    // ============================================================
    // FELDER
    // ============================================================

    private readonly string _filePath;

    public ObservableCollection<ColorSchema> Schemas { get; private set; } = [];


    public ColorSchemaCatalog(string filePath)
    {
        _filePath = filePath;
    }


    // ============================================================
    // LOAD / SAVE
    // ============================================================

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            // Erststart → Default + Presets erzeugen
            Schemas = new ObservableCollection<ColorSchema>(CreateDefaultPresets());
            Save();
            return;
        }

        var json = File.ReadAllText(_filePath);
        var list = JsonSerializer.Deserialize<List<ColorSchema>>(json, JsonSerializerOptions.Default)
                   ?? new List<ColorSchema>();

        Schemas = new ObservableCollection<ColorSchema>(list);
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
        };


        var json = JsonSerializer.Serialize(Schemas.ToList(), options);
        File.WriteAllText(_filePath, json);
    }


    // ============================================================
    // INSTANZ-CRUD  (bleibt unverändert!)
    // ============================================================

    //public ColorSchema? Get(string key)
    //    => Schemas.FirstOrDefault(s => s.Key == key);

    public ColorSchema? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = "Default";

        var schema = Schemas.FirstOrDefault(s => s.Key == key);

        // Fallback: Wenn Schema nicht gefunden, Default zurückgeben
        if (schema == null)
        {
            schema = Schemas.FirstOrDefault(s => s.Key == "Default");

            // Wenn auch Default nicht existiert, erstelle einen
            if (schema == null)
            {
                schema = CreateDefaultSchema();
                Schemas.Insert(0, schema);
            }
        }

        return schema;
    }


    private ColorSchema CreateDefaultSchema()
    {
        return new ColorSchema
        {
            Key = "Default",
            DisplayName = "Default (Sky Day)",
            PrimaryColor = (Color)ColorConverter.ConvertFromString("#3A7DFF"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#60D7FF"),
            InfoColor = (Color)ColorConverter.ConvertFromString("#4FA9F7"),
            SuccessColor = (Color)ColorConverter.ConvertFromString("#59C28E"),
            WarningColor = (Color)ColorConverter.ConvertFromString("#FFC147"),
            ErrorColor = (Color)ColorConverter.ConvertFromString("#E66A6A"),
            NeutralColor = (Color)ColorConverter.ConvertFromString("#EDEFF2"),
            TextColor = (Color)ColorConverter.ConvertFromString("#222222"),
            ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
            ControlBorderColor = (Color)ColorConverter.ConvertFromString("#C9CCD2"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#D0D3DA")
        };
    }


    public void Add(ColorSchema schema)
    {
        if (Schemas.Any(s => s.Key == schema.Key))
        {
            Update(schema);
            return;
        }

        Schemas.Add(schema);
        Save();
    }

    public void Update(ColorSchema schema)
    {
        var existing = Get(schema.Key);
        if (existing != null)
        {
            int idx = Schemas.IndexOf(existing);
            Schemas[idx] = schema;
            Save();
        }
    }

    public bool Remove(string key)
    {
        if (key == "Default") return false;

        var schema = Get(key);
        if (schema != null)
        {
            Schemas.Remove(schema);
            Save();
            return true;
        }
        return false;
    }

    public ColorSchema CreateNew(string displayName)
    {
        var schema = new ColorSchema
        {
            Key = GenerateUniqueKeyInternal(displayName),
            DisplayName = displayName
        };

        Schemas.Add(schema);
        Save();

        return schema;
    }

    private string GenerateUniqueKeyInternal(string baseName)
    {
        string key = baseName.Replace(" ", "");
        string candidate = key;
        int index = 1;

        while (Schemas.Any(s => s.Key == candidate))
        {
            candidate = $"{key}{index}";
            index++;
        }

        return candidate;
    }


    // ============================================================
    // PRESETS (Default + 6 Themes)
    // ============================================================

    private static IEnumerable<ColorSchema> CreateDefaultPresets()
    {
        return new List<ColorSchema>
        {
            // (ALLE 7 SCHEMAS HIER BELASSEN – UNVERÄNDERT)
            // --- Default
            new ColorSchema
            {
                Key = "Default",
                DisplayName = "Default (Sky Day)",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#3A7DFF"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#60D7FF"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#4FA9F7"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#59C28E"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#FFC147"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#E66A6A"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#EDEFF2"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#C9CCD2"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#D0D3DA")
            },

            // --- SkyDay
            new ColorSchema
            {
                Key = "SkyDay",
                DisplayName = "Sky Day",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#3A7DFF"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#60D7FF"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#4FA9F7"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#59C28E"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#FFC147"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#E66A6A"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#EDEFF2"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#C9CCD2"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#D0D3DA")
            },

            // --- Fresh Mint
            new ColorSchema
            {
                Key = "FreshMint",
                DisplayName = "Fresh Mint",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#3FBF9F"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#6EE7C8"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#6ABCE8"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#4CAF84"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#FFCC48"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#E76E6E"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#EEF6F3"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#C3D7D2"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#CEDDD8")
            },

            // --- Blossom Rose
            new ColorSchema
            {
                Key = "BlossomRose",
                DisplayName = "Blossom Rose",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#E38CB7"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#FFB7D9"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#9FA8FF"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#7CCFA4"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#FFCA66"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#E86A8C"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#F7EEF3"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#D8C8D4"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#E0D1DD")
            },

            // --- Olive Light
            new ColorSchema
            {
                Key = "OliveLight",
                DisplayName = "Olive Light",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#7D9A6B"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#A9C788"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#86A9DA"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#73B87B"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#F2C35D"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#D96A6A"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#F0F3EC"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#C9CEC4"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#D6DCD3")
            },

            // --- Solar Wave
            new ColorSchema
            {
                Key = "SolarWave",
                DisplayName = "Solar Wave",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF8E3C"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#FFC68D"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#6FAAF7"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#5CBF8A"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#FFDA59"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#E06B6B"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#F7F2ED"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#D9CFC8"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#E5DCD6")
            },

            // --- Aero Slate
            new ColorSchema
            {
                Key = "AeroSlate",
                DisplayName = "Aero Slate",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#4B6EAF"),
                AccentColor = (Color)ColorConverter.ConvertFromString("#7DB3F0"),
                InfoColor = (Color)ColorConverter.ConvertFromString("#5FA8D3"),
                SuccessColor = (Color)ColorConverter.ConvertFromString("#58B28C"),
                WarningColor = (Color)ColorConverter.ConvertFromString("#F4C963"),
                ErrorColor = (Color)ColorConverter.ConvertFromString("#D46A6A"),
                NeutralColor = (Color)ColorConverter.ConvertFromString("#E7ECF3"),
                TextColor = (Color)ColorConverter.ConvertFromString("#000000"),
                ControlBGColor = (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                ControlBorderColor = (Color)ColorConverter.ConvertFromString("#C7CED9"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#D4D9E2")
            }
        };
    }
}
