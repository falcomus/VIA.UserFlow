namespace Mockup;


public sealed class ControlDto
{
    public string TypeKey { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Dictionary<string, object>? Props { get; set; }
}
