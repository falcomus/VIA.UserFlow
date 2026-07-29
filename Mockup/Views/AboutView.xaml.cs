using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace Mockup.Views;

/// <summary>
/// Interaction logic for ProjectView.xaml
/// </summary>
public partial class AboutView : UserControl
{
    public string ProductVersion { get; } = ResolveProductVersion();

    public string RuntimeDescription { get; } = $".NET {Environment.Version}";

    public string OperatingSystemDescription { get; } = RuntimeInformation.OSDescription;

    public AboutView()
    {
        InitializeComponent();
    }

    private static string ResolveProductVersion()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(AboutView).Assembly;
        string? informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
            return informationalVersion.Split('+')[0];

        return assembly.GetName().Version?.ToString(3) ?? "—";
    }
}

