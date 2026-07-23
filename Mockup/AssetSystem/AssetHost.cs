// ============================================================================
// FILE: Mockup.AssetSystem/AssetHost.cs
// PURPOSE:
//   Zentrale Initialisierung des Hybrid-Asset-Systems.
//   Wird einmal beim App-Start aufgerufen (z. B. aus App.xaml.cs).
//
//   Aufruf: AssetHost.Initialize();
//
// AUTHOR: Claus Falkenstein / ChatGPT (XMOCKUP2 / MO27)
// VERSION: 1.1
// ============================================================================
namespace Mockup.AssetSystem;

public static class AssetHost
{
    private static bool _initialized;

    /// <summary>
    /// Initialisiert das Hybrid-Asset-System einmalig.
    /// Führt AssetCatalog.Refresh() aus (scannt Embedded + Custom).
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        AssetCatalog.Refresh();
    }
}



//namespace Mockup.AssetSystem;

//public static class AssetHost
//{
//    /// <summary>
//    /// Globale, einmalig erstellte Instanz des AssetCatalog.
//    /// In App-Start setzen: AssetHost.Catalog = new AssetCatalog();
//    /// </summary>
//    public static AssetCatalog Catalog { get; set; } = new AssetCatalog();
//}