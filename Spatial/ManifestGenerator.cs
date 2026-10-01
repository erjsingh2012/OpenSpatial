using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSpatial.Spatial;

public record SpatialNode(
    string Coordinate,
    string Ecosystem,
    string Context,
    string Container,
    string Component,
    string Workflow,
    string Action,
    string Task,
    string Leaf,
    string Capability,
    int    TimeoutMs
);

public static class ManifestGenerator
{
    public static void Generate(Assembly assembly, string outputPath = ".spatial/manifest.json")
    {
        var nodes = new List<SpatialNode>();
        var seen  = new HashSet<string>();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                var attr = method.GetCustomAttribute<SpatialAttribute>();
                if (attr is null) continue;

                var coordinate = $"{attr.Coordinate}.{method.Name}";

                // Validate: no duplicate coordinates
                if (!seen.Add(coordinate))
                    throw new InvalidOperationException(
                        $"Duplicate coordinate detected: {coordinate}");

                nodes.Add(new SpatialNode(
                    Coordinate: coordinate,
                    Ecosystem:  attr.Ecosystem,
                    Context:    attr.Context,
                    Container:  attr.Container,
                    Component:  attr.Component,
                    Workflow:   attr.Workflow,
                    Action:     attr.Action,
                    Task:       attr.Task,
                    Leaf:       method.Name,
                    Capability: attr.Capability,
                    TimeoutMs:  attr.TimeoutMs
                ));
            }
        }

        // Write manifest
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var json = JsonSerializer.Serialize(
            new { generated = DateTime.UtcNow, nodes },
            new JsonSerializerOptions { WriteIndented = true }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine($"\n=== Spatial Manifest ({outputPath}) ===");
        foreach (var node in nodes)
            Console.WriteLine($"  {node.Coordinate}  [{node.Capability}]");
        Console.WriteLine($"✓ {nodes.Count} nodes written to {outputPath}");
    }
}
