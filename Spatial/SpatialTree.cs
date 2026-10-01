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
    // level: 1=C1 only, 2=C1+C2, 3=C1+C2+C3, 4=full (default)
    public static void Print(string manifestPath = ".spatial/manifest.json",
                             string? filter      = null,
                             string? capability  = null,
                             int     level       = 4)
    {
        if (!File.Exists(manifestPath))
        {
            Console.WriteLine($"No manifest found at {manifestPath}. Run dotnet run first.");
            return;
        }

        var json  = File.ReadAllText(manifestPath);
        var doc   = JsonDocument.Parse(json);
        var nodes = doc.RootElement
                       .GetProperty("nodes")
                       .Deserialize<List<ManifestNode>>()!;

        if (filter is not null)
            nodes = nodes.Where(n => n.Coordinate.StartsWith(filter)).ToList();

        if (capability is not null)
            nodes = nodes.Where(n => n.Capability == capability).ToList();

        if (nodes.Count == 0) { Console.WriteLine("No nodes match the filter."); return; }

        PrintHeader(level);

        var byContext = nodes.GroupBy(n => $"{n.Ecosystem}.{n.Context}").OrderBy(g => g.Key);

        foreach (var ctxGroup in byContext)
        {
            // ── C1: System Context ────────────────────────────────────────
            Console.ForegroundColor = ConsoleColor.Cyan;
            var ctxCount = $"  ({ctxGroup.Count()} node{(ctxGroup.Count() > 1 ? "s" : "")})";
            Console.Write($"\n  [C1] {ctxGroup.Key}");
            if (level == 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(ctxCount);
            }
            Console.WriteLine();
            Console.ResetColor();

            if (level < 2) continue;

            var byContainer = ctxGroup.GroupBy(n => n.Container).OrderBy(g => g.Key).ToList();

            foreach (var ctnGroup in byContainer)
            {
                var isLastCtn  = ctnGroup == byContainer.Last();
                var ctnBranch  = isLastCtn ? "└─" : "├─";

                // ── C2: Container ─────────────────────────────────────────
                Console.ForegroundColor = ConsoleColor.Green;
                var ctnCount = $"  ({ctnGroup.Count()} node{(ctnGroup.Count() > 1 ? "s" : "")})";
                Console.Write($"    {ctnBranch} [C2] {ctnGroup.Key}");
                if (level == 2)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(ctnCount);
                }
                Console.WriteLine();
                Console.ResetColor();

                if (level < 3) continue;

                var byComponent = ctnGroup.GroupBy(n => n.Component).OrderBy(g => g.Key).ToList();
                var indent      = isLastCtn ? "     " : "  │  ";

                foreach (var cmpGroup in byComponent)
                {
                    var isLastCmp = cmpGroup == byComponent.Last();
                    var cmpBranch = isLastCmp ? "└─" : "├─";

                    // ── C3: Component ─────────────────────────────────────
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    var cmpCount = $"  ({cmpGroup.Count()} node{(cmpGroup.Count() > 1 ? "s" : "")})";
                    Console.Write($"  {indent}  {cmpBranch} [C3] {cmpGroup.Key}");
                    if (level == 3)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(cmpCount);
                    }
                    Console.WriteLine();
                    Console.ResetColor();

                    if (level < 4) continue;

                    var codeNodes  = cmpGroup.ToList();
                    var codeIndent = isLastCtn
                        ? (isLastCmp ? "           " : "      │    ")
                        : (isLastCmp ? "  │        " : "  │   │    ");

                    for (int i = 0; i < codeNodes.Count; i++)
                    {
                        var node      = codeNodes[i];
                        var isLastCode = i == codeNodes.Count - 1;
                        var codeBranch = isLastCode ? "└──" : "├──";
                        var cap        = CapTag(node.Capability);
                        var path       = $"{node.Workflow} › {node.Action} › {node.Task} › {node.Leaf}";

                        // ── C4: Code ──────────────────────────────────────
                        Console.Write($"  {codeIndent}{codeBranch} [C4] ");
                        Console.ForegroundColor = CapColor(node.Capability);
                        Console.Write($"{cap} ");
                        Console.ResetColor();
                        Console.WriteLine(path);
                    }
                }
            }
        }

        Console.WriteLine();
        PrintFooter(nodes, level);
    }

    private static void PrintHeader(int level)
    {
        var title = level switch
        {
            1 => "C1 — SYSTEM CONTEXT DIAGRAM",
            2 => "C2 — CONTAINER DIAGRAM",
            3 => "C3 — COMPONENT DIAGRAM",
            _ => "C4 — CODE DIAGRAM (Full)"
        };

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\n  ╔══════════════════════════════════════════════════════╗");
        Console.WriteLine($"  ║  {title,-52}║");
        Console.WriteLine($"  ╚══════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var legend = level switch
        {
            1 => "  Scope: Ecosystem · Context\n",
            2 => "  Scope: Ecosystem · Context  →  Container\n",
            3 => "  Scope: Ecosystem · Context  →  Container  →  Component\n",
            _ => "  Scope: C1 Context  →  C2 Container  →  C3 Component  →  C4 Code\n"
        };
        Console.WriteLine(legend);
    }

    private static void PrintFooter(List<ManifestNode> nodes, int level)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (level == 4)
            Console.WriteLine($"  {nodes.Count} node(s)   " +
                $"STATE_MUTATE: {nodes.Count(n => n.Capability == "STATE_MUTATE")}   " +
                $"DATA_ACCESS: {nodes.Count(n => n.Capability == "DATA_ACCESS")}   " +
                $"CRITICAL_DESTROY: {nodes.Count(n => n.Capability == "CRITICAL_DESTROY")}");
        else
            Console.WriteLine($"  {nodes.Count} node(s) across " +
                $"{nodes.Select(n => $"{n.Ecosystem}.{n.Context}").Distinct().Count()} context(s)");
        Console.ResetColor();
    }

    // ── Mermaid output ────────────────────────────────────────────────────────
    public static void PrintMermaid(string manifestPath = ".spatial/manifest.json",
                                    string? filter      = null,
                                    string? capability  = null,
                                    int     level       = 4)
    {
        if (!File.Exists(manifestPath))
        {
            Console.WriteLine($"No manifest found at {manifestPath}. Run dotnet run first.");
            return;
        }

        var json  = File.ReadAllText(manifestPath);
        var doc   = JsonDocument.Parse(json);
        var nodes = doc.RootElement
                       .GetProperty("nodes")
                       .Deserialize<List<ManifestNode>>()!;

        if (filter is not null)    nodes = nodes.Where(n => n.Coordinate.StartsWith(filter)).ToList();
        if (capability is not null) nodes = nodes.Where(n => n.Capability == capability).ToList();
        if (nodes.Count == 0) { Console.WriteLine("No nodes match the filter."); return; }

        var title = level switch
        {
            1 => "C1 — System Context",
            2 => "C2 — Container Diagram",
            3 => "C3 — Component Diagram",
            _ => "C4 — Code Diagram"
        };

        Console.WriteLine($"# {title}");
        Console.WriteLine();
        Console.WriteLine("```mermaid");
        Console.WriteLine("graph TD");

        var byContext = nodes.GroupBy(n => $"{n.Ecosystem}.{n.Context}").OrderBy(g => g.Key).ToList();
        int nodeIdx   = 0;

        foreach (var ctxGroup in byContext)
        {
            var ctxId = Safe(ctxGroup.Key);
            Console.WriteLine($"  subgraph {ctxId}[\"🌐 {ctxGroup.Key}\"]");

            if (level >= 2)
            {
                foreach (var ctnGroup in ctxGroup.GroupBy(n => n.Container).OrderBy(g => g.Key))
                {
                    var ctnId = Safe($"{ctxGroup.Key}.{ctnGroup.Key}");
                    Console.WriteLine($"    subgraph {ctnId}[\"📦 {ctnGroup.Key}\"]");

                    if (level >= 3)
                    {
                        foreach (var cmpGroup in ctnGroup.GroupBy(n => n.Component).OrderBy(g => g.Key))
                        {
                            var cmpId = Safe($"{ctxGroup.Key}.{ctnGroup.Key}.{cmpGroup.Key}");
                            Console.WriteLine($"      subgraph {cmpId}[\"⚙️ {cmpGroup.Key}\"]");

                            if (level >= 4)
                            {
                                foreach (var node in cmpGroup)
                                {
                                    var nid   = $"N{nodeIdx++}";
                                    var emoji = CapEmoji(node.Capability);
                                    var label = $"{emoji} {node.Capability}\\n{node.Leaf}\\n{node.Workflow} › {node.Action} › {node.Task}";
                                    Console.WriteLine($"        {nid}[\"{label}\"]");
                                }
                            }

                            Console.WriteLine("      end");
                        }
                    }

                    Console.WriteLine("    end");
                }
            }

            Console.WriteLine("  end");
        }

        Console.WriteLine("```");
        Console.WriteLine();
        Console.WriteLine($"_Generated by `sptree --md` · {nodes.Count} node(s) · {DateTime.UtcNow:yyyy-MM-dd}_");
    }

    private static string Safe(string s) =>
        s.Replace(".", "_").Replace("-", "_");

    private static string CapEmoji(string cap) => cap switch
    {
        "STATE_MUTATE"     => "🔴",
        "DATA_ACCESS"      => "🔵",
        "CRITICAL_DESTROY" => "🟣",
        _                  => "⚪"
    };

    private static string CapTag(string cap) => cap switch
    {
        "STATE_MUTATE"     => "[STATE_MUTATE    ]",
        "DATA_ACCESS"      => "[DATA_ACCESS     ]",
        "CRITICAL_DESTROY" => "[CRITICAL_DESTROY]",
        _                  => $"[{cap,-17}]"
    };

    private static ConsoleColor CapColor(string cap) => cap switch
    {
        "STATE_MUTATE"     => ConsoleColor.Red,
        "DATA_ACCESS"      => ConsoleColor.Blue,
        "CRITICAL_DESTROY" => ConsoleColor.Magenta,
        _                  => ConsoleColor.Gray
    };
}
