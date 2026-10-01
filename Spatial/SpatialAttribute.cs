namespace OpenSpatial.Spatial;

[AttributeUsage(AttributeTargets.Method)]
public class SpatialAttribute : Attribute
{
    public string Ecosystem  { get; set; } = "";
    public string Context    { get; set; } = "";
    public string Container  { get; set; } = "";
    public string Component  { get; set; } = "";
    public string Workflow   { get; set; } = "";
    public string Action     { get; set; } = "";
    public string Task       { get; set; } = "";
    public string Capability { get; set; } = "";
    public int    TimeoutMs  { get; set; } = 5000;

    // The full 8-tier coordinate — function name is T8 (Leaf)
    public string Coordinate =>
        $"{Ecosystem}.{Context}.{Container}.{Component}" +
        $":{Workflow}.{Action}.{Task}";
}
