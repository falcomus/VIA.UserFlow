using System.Resources;

namespace Mockup.Resources;

/// <summary>
/// Provides the application-owned strings used by the UserFlow user interface.
/// </summary>
public static class UserFlowResources
{
    private static readonly ResourceManager ResourceManagerInstance = new(
        "VIA.Mockup.Resources.UserFlowStrings",
        typeof(UserFlowResources).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerInstance;
}
