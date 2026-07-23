// ============================================================================
// FILE: Mockup/MockupService.cs
// ============================================================================

using Mockup.AssetSystem;
using Mockup.ViewModel;

namespace Mockup;

public static class MockupService
{
    public static MockupViewModel Mockup { get; set; } = null!;

    public static Type Assets => typeof(AssetCatalog);

    public static void InitializeAssets()
    {
        AssetHost.Initialize();
    }

    public static void RefreshAssets()
    {
        AssetCatalog.Refresh();
    }

    public static IReadOnlyCollection<AssetCatalog.AssetInfo> GetAllAssets()
    {
        return AssetCatalog.AllAssets;
    }

    public static AssetCatalog.AssetInfo? ImportAsset(string sourcePath)
    {
        return AssetCatalog.ImportFile(sourcePath);
    }
}

public sealed class ControlCategoryAttribute : Attribute
{
    public string Category { get; }
    public ControlCategoryAttribute(string category) => Category = category;
}
