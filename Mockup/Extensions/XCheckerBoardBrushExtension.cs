using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace Mockup.Extensions;

public class XCheckerBoardBrushExtension : MarkupExtension
{
    public double TileSize { get; set; } = 8.0;
    public Color CheckerForeground { get; set; }
    public Color CheckerBackground { get; set; }

    public XCheckerBoardBrushExtension() { }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var halfSize = TileSize / 2;

        var drawingGroup = new DrawingGroup();

        // Hintergrund (CheckerForeground)
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(CheckerForeground),
            null,
            new RectangleGeometry(new Rect(0, 0, TileSize, TileSize))));

        // Muster (CheckerBackground)
        var geometryGroup = new GeometryGroup();
        geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, halfSize, halfSize)));
        geometryGroup.Children.Add(new RectangleGeometry(new Rect(halfSize, halfSize, halfSize, halfSize)));

        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(CheckerBackground),
            null,
            geometryGroup));

        return new DrawingBrush(drawingGroup)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, TileSize, TileSize),
            ViewportUnits = BrushMappingMode.Absolute
        };
    }
}


//using System.Windows;
//using System.Windows.Markup;
//using System.Windows.Media;


////EXAMPLE USAGE: <Rectangle Fill="{local:CheckerBoardBrushExtension TileSize=8}" Width = "200" Height = "200" />

//namespace Mockup.Extensions;

//public class XCheckerBoardBrushExtension : MarkupExtension
//{
//    public double TileSize { get; set; } = 8.0;
//    public Brush CheckerForeground { get; set; } = Brushes.Red;
//    public Brush CheckerBackground { get; set; } = Brushes.DarkRed;

//    public XCheckerBoardBrushExtension() { }

//    public override object ProvideValue(IServiceProvider serviceProvider)
//    {
//        var halfSize = TileSize / 2;

//        var drawingGroup = new DrawingGroup();

//        // Hintergrund (CheckerForeground)
//        drawingGroup.Children.Add(new GeometryDrawing(
//            CheckerForeground,
//            null,
//            new RectangleGeometry(new Rect(0, 0, TileSize, TileSize))));

//        // Muster (CheckerBackground)
//        var geometryGroup = new GeometryGroup();
//        geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, halfSize, halfSize)));
//        geometryGroup.Children.Add(new RectangleGeometry(new Rect(halfSize, halfSize, halfSize, halfSize)));

//        drawingGroup.Children.Add(new GeometryDrawing(
//            CheckerBackground,
//            null,
//            geometryGroup));

//        return new DrawingBrush(drawingGroup)
//        {
//            TileMode = TileMode.Tile,
//            Viewport = new Rect(0, 0, TileSize, TileSize),
//            ViewportUnits = BrushMappingMode.Absolute
//        };
//    }
//}