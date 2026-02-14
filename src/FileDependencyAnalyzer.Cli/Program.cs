using FileDependencyAnalyzer.Core.Rules;
using FileDependencyAnalyzer.Core.Scanning;
using System.Text.Json;
using System.Linq;

if (args.Length == 0)
{
    Console.WriteLine("Usage: FileDependencyAnalyzer <path>");
    return;
}

var rootPath = args[0];

if (!Directory.Exists(rootPath))
{
    Console.WriteLine("Path does not exist.");
    return;
}

static string Rel(string root, string fullPath)
{
    try
    {
        return Path.GetRelativePath(root, fullPath);
    }
    catch
    {
        return fullPath;
    }
}


var rules = new List<IDependencyRule>
{
    new MarkdownDependencyRule(),
    new PowerShellDependencyRule()
};

var scanner = new FileScanner(rules);

Console.WriteLine("Scanning...");
var graph = scanner.Scan(rootPath);



Console.WriteLine();
Console.WriteLine($"Nodes: {graph.Nodes.Count} | Edges: {graph.Edges.Count}");

// --- Top Inbound / Outbound / Orphans ---
const int topN = 3;

var topInbound = graph.GetTopInbound(topN)
    .Where(x => x.Inbound > 0)
    .ToList();

Console.WriteLine();
Console.WriteLine($"Top {topN} Inbound (most referenced):");
if (topInbound.Count == 0)
{
    Console.WriteLine("  (none)");
}
else
{
    foreach (var x in topInbound)
        Console.WriteLine($"  {x.Inbound,3}  {Rel(rootPath, x.Node.FilePath)}");
}

var topOutbound = graph.GetTopOutbound(topN)
    .Where(x => x.Outbound > 0)
    .ToList();

Console.WriteLine();
Console.WriteLine($"Top {topN} Outbound (most dependencies):");
if (topOutbound.Count == 0)
{
    Console.WriteLine("  (none)");
}
else
{
    foreach (var x in topOutbound)
        Console.WriteLine($"  {x.Outbound,3}  {Rel(rootPath, x.Node.FilePath)}");
}

var orphans = graph.GetOrphanNodes()
    .Select(n => n.FilePath)
    .OrderBy(p => Rel(rootPath, p), StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine();
Console.WriteLine($"Orphans (no inbound & no outbound): {orphans.Count}");
foreach (var p in orphans.Take(20))
{
    Console.WriteLine($"  - {Rel(rootPath, p)}");
}
if (orphans.Count > 20)
{
    Console.WriteLine($"  ... (+{orphans.Count - 20} more)");
}

Console.WriteLine();

// --- Cycles ---
var cycles = graph.GetCycles();

Console.WriteLine($"Cycles: {cycles.Count}");
if (cycles.Count > 0)
{
    for (int i = 0; i < cycles.Count; i++)
    {
        var cycle = cycles[i];
        Console.WriteLine();
        Console.WriteLine($"Cycle {i + 1} (size={cycle.Count}):");

        foreach (var node in cycle)
        {
            Console.WriteLine($"  - {Rel(rootPath, node.FilePath)}");
        }
    }

    Console.WriteLine();
}





var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions
{
    WriteIndented = true
});

Console.WriteLine(json);

Console.ReadLine();
