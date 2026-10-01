using System.Text.Json;

namespace OpenSpatial.Spatial;

public record ManifestNode(
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

public static class SpatialTree
{
    public static void Print(string manifestPath = ".spatial/manifest.json",
                             string? filter      = null,
                             string? capability  = null)
    {
        if (!File.Exists(manifestPath))
        {
            Console.WriteLine($"No manifest found at {manifestPath}. Run the app first.");
            return;
        }

        var json    = File.ReadAllText(manifestPath);
        var doc     = JsonDocument.Parse(json);
        var nodes   = doc.RootElement
                         .GetProperty("nodes")
                         .Deserialize<List<ManifestNode>>()!;

        // Apply filters
        if (filter is not null)
            nodes = nodes.Where(n => n.Coordinate.StartsWith(filter)).ToList();

        if (capability is not null)
            nodes = nodes.Where(n => n.Capability == capability).ToList();

        if (nodes.Count == 0)
        {
            Console.WriteLine("No nodes match the filter.");
            return;
        }

        PrintHeader();

        // Group into C4 levels
        var byContext   = nodes.GroupBy(n => $"{n.Ecosystem}.{n.Context}").OrderBy(g => g.Key);

        foreach (var ctxGroup in byContext)
        {
            // C1 — System Context
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n  [C1] {ctxGroup.Key}");
            Console.ResetColor();

            var byContainer = ctxGroup.GroupBy(n => n.Container).OrderBy(g => g.Key);

            foreach (var ctnGroup in byContainer)
            {
                // C2 — Container
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    └─ [C2] {ctnGroup.Key}");
                Console.ResetColor();

                var byComponent = ctnGroup.GroupBy(n => n.Component).OrderBy(g => g.Key);

                foreach (var cmpGroup in byComponent)
                {
                    // C3 — Component
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"         └─ [C3] {cmpGroup.Key}");
                    Console.ResetColor();

                    var codeNodes = cmpGroup.ToList();

                    for (int i = 0; i < codeNodes.Count; i++)
                    {
                        var node   = codeNodes[i];
                        var isLast = i == codeNodes.Count - 1;
                        var branch = isLast ? "└──" : "├──";
                        var cap    = CapTag(node.Capability);
                        var path   = $"{node.Workflow} › {node.Action} › {node.Task} › {node.Leaf}";

                        // C4 — Code
                        Console.Write($"              {branch} [C4] ");
                        Console.ForegroundColor = CapColor(node.Capability);
                        Console.Write($"{cap} ");
                        Console.ResetColor();
                        Console.WriteLine(path);
                    }
                }
            }
        }

        Console.WriteLine();
        PrintFooter(nodes);
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n  ╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║          OPENSPATIAL TREE — C4 Architecture          ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine("  C1 System Context  →  C2 Container  →  C3 Component  →  C4 Code\n");
    }

    private static void PrintFooter(List<ManifestNode> nodes)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {nodes.Count} node(s)   " +
            $"STATE_MUTATE: {nodes.Count(n => n.Capability == "STATE_MUTATE")}   " +
            $"DATA_ACCESS: {nodes.Count(n => n.Capability == "DATA_ACCESS")}   " +
            $"CRITICAL_DESTROY: {nodes.Count(n => n.Capability == "CRITICAL_DESTROY")}");
        Console.ResetColor();
    }

    private static string CapTag(string cap) => cap switch
    {
        "STATE_MUTATE"      => "[STATE_MUTATE    ]",
        "DATA_ACCESS"       => "[DATA_ACCESS     ]",
        "CRITICAL_DESTROY"  => "[CRITICAL_DESTROY]",
        _                   => $"[{cap,-17}]"
    };

    private static ConsoleColor CapColor(string cap) => cap switch
    {
        "STATE_MUTATE"     => ConsoleColor.Red,
        "DATA_ACCESS"      => ConsoleColor.Blue,
        "CRITICAL_DESTROY" => ConsoleColor.Magenta,
        _                  => ConsoleColor.Gray
    };
}
