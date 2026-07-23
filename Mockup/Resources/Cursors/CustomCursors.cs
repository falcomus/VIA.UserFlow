using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;

namespace Mockup.Resources.Cursors;

public static class CustomCursors
{
    public static readonly Cursor ResizeWE = Load("Resources/Cursors/horizontalResize.ani");
    public static readonly Cursor ResizeNS = Load("Resources/Cursors/verticalResize.ani");
    public static readonly Cursor ResizeNWSE = Load("Resources/Cursors/diagonalResize1.ani");
    public static readonly Cursor ResizeNESW = Load("Resources/Cursors/diagonalResize2.ani");

    private static Cursor Load(string relativePath)
    {
        // pack://application:,,,/YourUiProject;component/Assets/Cursors/resize-we.ico
        var asm = Assembly.GetExecutingAssembly().GetName().Name;
        var uri = new Uri(
            $"pack://application:,,,/{asm};component/{relativePath}",
            UriKind.Absolute);

        StreamResourceInfo sri = Application.GetResourceStream(uri)
            ?? throw new FileNotFoundException($"Cursor not found: {relativePath}");

        return new Cursor(sri.Stream);
    }
}