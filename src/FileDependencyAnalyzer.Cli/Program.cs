using FileDependencyAnalyzer.Core.Rules;
using FileDependencyAnalyzer.Core.Scanning;
using System.Text;
using System.Text.Json;

namespace FileDependencyAnalyzer.Cli;

/// <summary>
/// 
/// </summary>
internal static class Program
{
    /// <summary>
    /// Output format used by the CLI.
    /// </summary>
    private enum OutputFormat
    {
        /// <summary>
        /// Human-readable text summary for console output.
        /// </summary>
        Summary,

        /// <summary>
        /// JSON export of nodes, edges, orphans and cycles.
        /// </summary>
        Json,

        /// <summary>
        /// Markdown report suitable for GitHub rendering.
        /// </summary>
        Markdown
    }

    /// <summary>
    /// Parsed command line options for a scan run.
    /// </summary>
    private sealed class CliOptions
    {
        /// <summary>
        /// Root directory that will be scanned.
        /// </summary>
        public string RootPath { get; set; } = "";

        /// <summary>
        /// Selected output format (summary/json/markdown).
        /// </summary>
        public OutputFormat Format { get; set; } = OutputFormat.Summary;

        /// <summary>
        /// Optional output file path. If not set, output is written to stdout.
        /// </summary>
        public string? OutPath { get; set; }

        /// <summary>
        /// Number of entries shown for "top inbound/outbound" lists.
        /// </summary>
        public int Top { get; set; } = 5;

        /// <summary>
        /// Maximum number of orphan nodes to print.
        /// </summary>
        public int MaxOrphans { get; set; } = 20;

        /// <summary>
        /// Maximum number of cycles to print.
        /// </summary>
        public int MaxCycles { get; set; } = 20;

        /// <summary>
        /// If true, prints full paths instead of relative paths.
        /// </summary>
        public bool FullPaths { get; set; }

        /// <summary>
        /// Directory names to exclude in addition to the defaults.
        /// </summary>
        public List<string> ExcludeDirs { get; } = new();

        /// <summary>
        /// If true, usage/help should be printed and the program should exit.
        /// </summary>
        public bool ShowHelp { get; set; }
    }

    /// <summary>
    /// Application entry point.
    /// Parses CLI arguments, scans the directory tree and writes the selected report format.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>
    /// Process exit code:
    /// 0 = success,
    /// 1 = unhandled/fatal error,
    /// 2 = invalid arguments / missing path.
    /// </returns>
    public static int Main(string[] args)
    {
        try
        {
            var opts = ParseArgs(args);
            if (opts.ShowHelp)
            {
                PrintUsage();
                return 0;
            }

            if (string.IsNullOrWhiteSpace(opts.RootPath))
            {
                Console.Error.WriteLine("Missing <path>.");
                PrintUsage();
                return 2;
            }

            if (!Directory.Exists(opts.RootPath))
            {
                Console.Error.WriteLine("Path does not exist: " + opts.RootPath);
                return 2;
            }

            // Status output should not pollute report files / stdout.
            Console.Error.WriteLine("Scanning...");

            var rules = new List<IDependencyRule>
            {
                new MarkdownDependencyRule(),
                new PowerShellDependencyRule()
            };

            var scanner = new FileScanner(rules);

            var scanOptions = new ScanOptions();
            foreach (var x in opts.ExcludeDirs)
                scanOptions.ExcludedDirectoryNames.Add(x);

            var graph = scanner.Scan(opts.RootPath, scanOptions);

            Console.Error.WriteLine($"Done. Nodes={graph.Nodes.Count} Edges={graph.Edges.Count}");

            using var writer = CreateWriter(opts.OutPath);

            switch (opts.Format)
            {
                case OutputFormat.Summary:
                    WriteSummary(writer, graph, opts);
                    break;

                case OutputFormat.Markdown:
                    WriteMarkdown(writer, graph, opts, scanOptions);
                    break;

                case OutputFormat.Json:
                    WriteJson(writer, graph, opts, scanOptions);
                    break;

                default:
                    throw new InvalidOperationException("Unknown format: " + opts.Format);
            }

            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            PrintUsage();
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal error: " + ex);
            return 1;
        }
    }

    /// <summary>
    /// Creates a <see cref="TextWriter"/> for stdout or for a target file path.
    /// Ensures the output directory exists and writes UTF-8 without BOM.
    /// </summary>
    /// <param name="outPath">Optional file path. If null/empty, <see cref="Console.Out"/> is returned.</param>
    /// <returns>A writer that must be disposed by the caller.</returns>
    private static TextWriter CreateWriter(string? outPath)
    {
        if (string.IsNullOrWhiteSpace(outPath))
            return Console.Out;

        var dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        return new StreamWriter(File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Parses command line arguments into a strongly typed options object.
    /// Supports <c>scan &lt;path&gt;</c> and the shorthand <c>&lt;path&gt;</c>.
    /// </summary>
    /// <param name="args">Raw CLI arguments.</param>
    /// <returns>Parsed CLI options.</returns>
    /// <exception cref="ArgumentException">Thrown for unknown options, missing values, or unexpected arguments.</exception>
    private static CliOptions ParseArgs(string[] args)
    {
        // Supported:
        //   FileDependencyAnalyzer <path>
        //   FileDependencyAnalyzer scan <path> [options]
        //   FileDependencyAnalyzer scan [options] <path>
        //
        // Options:
        //   --format summary|json|md
        //   --out <file>
        //   --top <n>
        //   --max-orphans <n>
        //   --max-cycles <n>
        //   --exclude-dir <name>   (repeatable)
        //   --full-paths
        //   -h|--help

        var o = new CliOptions();

        if (args.Length == 0)
        {
            o.ShowHelp = true;
            return o;
        }

        var idx = 0;
        if (IsHelp(args[0]))
        {
            o.ShowHelp = true;
            return o;
        }

        if (string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
        {
            idx = 1;
        }
        else
        {
            // Shorthand: <path>
            o.RootPath = args[0];
            idx = 1;
        }

        while (idx < args.Length)
        {
            var a = args[idx];

            if (IsHelp(a))
            {
                o.ShowHelp = true;
                return o;
            }

            if (a.StartsWith("-", StringComparison.Ordinal))
            {
                switch (a)
                {
                    case "--format":
                        o.Format = ParseFormat(RequireValue(args, ref idx));
                        break;

                    case "--out":
                        o.OutPath = RequireValue(args, ref idx);
                        break;

                    case "--top":
                        o.Top = ParseInt(RequireValue(args, ref idx), "--top");
                        break;

                    case "--max-orphans":
                        o.MaxOrphans = ParseInt(RequireValue(args, ref idx), "--max-orphans");
                        break;

                    case "--max-cycles":
                        o.MaxCycles = ParseInt(RequireValue(args, ref idx), "--max-cycles");
                        break;

                    case "--exclude-dir":
                        o.ExcludeDirs.Add(RequireValue(args, ref idx));
                        break;

                    case "--full-paths":
                        o.FullPaths = true;
                        idx++;
                        break;

                    default:
                        throw new ArgumentException("Unknown option: " + a);
                }

                continue;
            }

            // Positional path (only once)
            if (string.IsNullOrWhiteSpace(o.RootPath))
            {
                o.RootPath = a;
                idx++;
                continue;
            }

            throw new ArgumentException("Unexpected argument: " + a);
        }

        return o;

        static bool IsHelp(string x)
            => x is "-h" or "--help" or "/?";
    }

    /// <summary>
    /// Converts the CLI string value of <c>--format</c> to an <see cref="OutputFormat"/>.
    /// </summary>
    /// <param name="s">Format value (summary/json/md/markdown).</param>
    /// <returns>The parsed output format.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="s"/> is not a supported value.</exception>
    private static OutputFormat ParseFormat(string s)
    {
        if (string.Equals(s, "summary", StringComparison.OrdinalIgnoreCase))
            return OutputFormat.Summary;

        if (string.Equals(s, "json", StringComparison.OrdinalIgnoreCase))
            return OutputFormat.Json;

        if (string.Equals(s, "md", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "markdown", StringComparison.OrdinalIgnoreCase))
            return OutputFormat.Markdown;

        throw new ArgumentException("Invalid --format. Allowed: summary | json | md");
    }

    /// <summary>
    /// Parses a non-negative integer for a given CLI option name.
    /// </summary>
    /// <param name="s">String value to parse.</param>
    /// <param name="optName">The option name (used for an error message).</param>
    /// <returns>The parsed integer value.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is not a valid non-negative integer.</exception>
    private static int ParseInt(string s, string optName)
    {
        if (!int.TryParse(s, out var v) || v < 0)
            throw new ArgumentException($"Invalid value for {optName}: {s}");
        return v;
    }

    /// <summary>
    /// Reads the next argument value for an option and advances the index accordingly.
    /// </summary>
    /// <param name="args">Full argument array.</param>
    /// <param name="idx">
    /// Current index pointing to the option token. On success, this is advanced to the next token after the value.
    /// </param>
    /// <returns>The option value following the current argument index.</returns>
    /// <exception cref="ArgumentException">Thrown if the option has no following value.</exception>
    private static string RequireValue(string[] args, ref int idx)
    {
        if (idx + 1 >= args.Length)
            throw new ArgumentException($"Missing value for option {args[idx]}");

        var val = args[idx + 1];
        idx += 2;
        return val;
    }

    /// <summary>
    /// Prints the CLI usage/help text to standard output.
    /// </summary>
    private static void PrintUsage()
    {
        Console.WriteLine(
@"FileDependencyAnalyzer - rule-based file dependency scanner

Usage:
  FileDependencyAnalyzer <path>
  FileDependencyAnalyzer scan <path> [options]
  FileDependencyAnalyzer scan [options] <path>

Options:
  --format summary|json|md   Output format (default: summary)
  --out <file>              Write output to file (default: stdout)
  --top <n>                 Top inbound/outbound list size (default: 5)
  --max-orphans <n>         Max orphan files listed (default: 20)
  --max-cycles <n>          Max cycles listed (default: 20)
  --exclude-dir <name>      Exclude directory name (repeatable)
  --full-paths              Print full paths instead of relative paths
  -h|--help                 Show help

Examples:
  FileDependencyAnalyzer scan . --format md --out dependency-report.md
  FileDependencyAnalyzer scan D:\Repo --format json --out graph.json --exclude-dir node_modules
");
    }

    /// <summary>
    /// Writes a human-readable summary containing basic stats, top inbound/outbound,
    /// orphan nodes and detected cycles.
    /// </summary>
    /// <param name="w">Target writer (stdout or file).</param>
    /// <param name="graph">Dependency graph resulting from the scan.</param>
    /// <param name="opts">CLI options controlling formatting and limits.</param>
    private static void WriteSummary(TextWriter w, FileDependencyAnalyzer.Core.Graph.DependencyGraph graph, CliOptions opts)
    {
        var root = opts.RootPath;

        w.WriteLine($"Nodes: {graph.Nodes.Count} | Edges: {graph.Edges.Count}");

        w.WriteLine();
        w.WriteLine($"Top {opts.Top} Inbound (most referenced):");
        var topInbound = graph.GetTopInbound(opts.Top).Where(x => x.Inbound > 0).ToList();
        if (topInbound.Count == 0) w.WriteLine("  (none)");
        else foreach (var x in topInbound) w.WriteLine($"  {x.Inbound,3}  {DisplayPath(root, x.Node.FilePath, opts.FullPaths)}");

        w.WriteLine();
        w.WriteLine($"Top {opts.Top} Outbound (most dependencies):");
        var topOutbound = graph.GetTopOutbound(opts.Top).Where(x => x.Outbound > 0).ToList();
        if (topOutbound.Count == 0) w.WriteLine("  (none)");
        else foreach (var x in topOutbound) w.WriteLine($"  {x.Outbound,3}  {DisplayPath(root, x.Node.FilePath, opts.FullPaths)}");

        var orphans = graph.GetOrphanNodes()
            .Select(n => n.FilePath)
            .OrderBy(p => DisplayPath(root, p, opts.FullPaths), StringComparer.OrdinalIgnoreCase)
            .ToList();

        w.WriteLine();
        w.WriteLine($"Orphans (no inbound & no outbound): {orphans.Count}");
        foreach (var p in orphans.Take(opts.MaxOrphans))
            w.WriteLine($"  - {DisplayPath(root, p, opts.FullPaths)}");
        if (orphans.Count > opts.MaxOrphans)
            w.WriteLine($"  ... (+{orphans.Count - opts.MaxOrphans} more)");

        var cycles = graph.GetCycles();

        w.WriteLine();
        w.WriteLine($"Cycles: {cycles.Count}");
        foreach (var (cycle, i) in cycles.Take(opts.MaxCycles).Select((c, i) => (c, i)))
        {
            w.WriteLine();
            w.WriteLine($"Cycle {i + 1} (size={cycle.Count}):");
            foreach (var node in cycle)
                w.WriteLine($"  - {DisplayPath(root, node.FilePath, opts.FullPaths)}");
        }

        if (cycles.Count > opts.MaxCycles)
        {
            w.WriteLine();
            w.WriteLine($"... (+{cycles.Count - opts.MaxCycles} more cycles)");
        }
    }

    /// <summary>
    /// DTO for exporting edges to JSON.
    /// </summary>
    private sealed record EdgeExport(string From, string To);

    /// <summary>
    /// DTO for exporting the scan result to JSON.
    /// </summary>
    private sealed record GraphExport(
        string RootPath,
        string CreatedAt,
        IReadOnlyList<string> ExcludedDirectoryNames,
        int NodeCount,
        int EdgeCount,
        IReadOnlyList<string> Nodes,
        IReadOnlyList<EdgeExport> Edges,
        IReadOnlyList<string> Orphans,
        IReadOnlyList<IReadOnlyList<string>> Cycles);

    /// <summary>
    /// Writes a JSON export of the dependency graph including nodes, edges, orphans and cycles.
    /// </summary>
    /// <param name="w">Target writer (stdout or file).</param>
    /// <param name="graph">Dependency graph resulting from the scan.</param>
    /// <param name="opts">CLI options controlling formatting.</param>
    /// <param name="scanOptions">Scan options used (included for export metadata).</param>
    private static void WriteJson(TextWriter w, FileDependencyAnalyzer.Core.Graph.DependencyGraph graph, CliOptions opts, ScanOptions scanOptions)
    {
        var root = opts.RootPath;

        string P(string p) => DisplayPath(root, p, opts.FullPaths);

        var nodes = graph.Nodes
            .Select(n => P(n.FilePath))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var edges = graph.Edges
            .Select(e => new EdgeExport(P(e.From.FilePath), P(e.To.FilePath)))
            .ToList();

        var orphans = graph.GetOrphanNodes()
            .Select(n => P(n.FilePath))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cycles = graph.GetCycles()
            .Select(c => (IReadOnlyList<string>)c.Select(n => P(n.FilePath)).ToList())
            .ToList();

        var payload = new GraphExport(
            RootPath: root,
            CreatedAt: DateTimeOffset.Now.ToString("O"),
            ExcludedDirectoryNames: scanOptions.ExcludedDirectoryNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            NodeCount: graph.Nodes.Count,
            EdgeCount: graph.Edges.Count,
            Nodes: nodes,
            Edges: edges,
            Orphans: orphans,
            Cycles: cycles
        );

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        w.WriteLine(json);
    }

    /// <summary>
    /// Writes a Markdown report for the scan result suitable for rendering on GitHub.
    /// Includes basic stats, top inbound/outbound lists, orphans and cycles.
    /// </summary>
    /// <param name="w">Target writer (stdout or file).</param>
    /// <param name="graph">Dependency graph resulting from the scan.</param>
    /// <param name="opts">CLI options controlling formatting and limits.</param>
    /// <param name="scanOptions">Scan options used (included for report metadata).</param>
    private static void WriteMarkdown(TextWriter w, FileDependencyAnalyzer.Core.Graph.DependencyGraph graph, CliOptions opts, ScanOptions scanOptions)
    {
        var root = opts.RootPath;
        string P(string p) => DisplayPath(root, p, opts.FullPaths);

        var sb = new StringBuilder();

        sb.AppendLine("# File Dependency Report");
        sb.AppendLine();
        sb.AppendLine($"- **Root:** `{root}`");
        sb.AppendLine($"- **Created:** `{DateTimeOffset.Now:O}`");
        sb.AppendLine($"- **Excluded dirs:** `{string.Join(", ", scanOptions.ExcludedDirectoryNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}`");
        sb.AppendLine();
        sb.AppendLine("## Stats");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|---|---:|");
        sb.AppendLine($"| Nodes | {graph.Nodes.Count} |");
        sb.AppendLine($"| Edges | {graph.Edges.Count} |");
        sb.AppendLine();

        sb.AppendLine($"## Top {opts.Top} inbound (most referenced)");
        sb.AppendLine();
        var topInbound = graph.GetTopInbound(opts.Top).Where(x => x.Inbound > 0).ToList();
        if (topInbound.Count == 0) sb.AppendLine("_none_");
        else
        {
            foreach (var x in topInbound)
                sb.AppendLine($"- **{x.Inbound}** — `{P(x.Node.FilePath)}`");
        }
        sb.AppendLine();

        sb.AppendLine($"## Top {opts.Top} outbound (most dependencies)");
        sb.AppendLine();
        var topOutbound = graph.GetTopOutbound(opts.Top).Where(x => x.Outbound > 0).ToList();
        if (topOutbound.Count == 0) sb.AppendLine("_none_");
        else
        {
            foreach (var x in topOutbound)
                sb.AppendLine($"- **{x.Outbound}** — `{P(x.Node.FilePath)}`");
        }
        sb.AppendLine();

        var orphans = graph.GetOrphanNodes()
            .Select(n => P(n.FilePath))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        sb.AppendLine($"## Orphans ({orphans.Count})");
        sb.AppendLine();
        if (orphans.Count == 0) sb.AppendLine("_none_");
        else
        {
            foreach (var p in orphans.Take(opts.MaxOrphans))
                sb.AppendLine($"- `{p}`");
            if (orphans.Count > opts.MaxOrphans)
                sb.AppendLine($"- … _( +{orphans.Count - opts.MaxOrphans} more )_");
        }
        sb.AppendLine();

        var cycles = graph.GetCycles();
        sb.AppendLine($"## Cycles ({cycles.Count})");
        sb.AppendLine();

        if (cycles.Count == 0)
        {
            sb.AppendLine("_none_");
        }
        else
        {
            foreach (var (cycle, i) in cycles.Take(opts.MaxCycles).Select((c, i) => (c, i)))
            {
                sb.AppendLine($"### Cycle {i + 1} (size={cycle.Count})");
                sb.AppendLine();
                foreach (var node in cycle)
                    sb.AppendLine($"- `{P(node.FilePath)}`");
                sb.AppendLine();
            }

            if (cycles.Count > opts.MaxCycles)
                sb.AppendLine($"_… (+{cycles.Count - opts.MaxCycles} more cycles)_");
        }

        w.Write(sb.ToString());
    }

    /// <summary>
    /// Returns either a relative path (relative to <paramref name="root"/>) or the original full path,
    /// depending on <paramref name="fullPaths"/>.
    /// </summary>
    /// <param name="root">Root directory for relative path calculation.</param>
    /// <param name="fullPath">Full (or original) file system path.</param>
    /// <param name="fullPaths">If true, returns <paramref name="fullPath"/> unchanged.</param>
    /// <returns>The relative path if possible; otherwise the original path.</returns>
    private static string DisplayPath(string root, string fullPath, bool fullPaths)
    {
        if (fullPaths)
            return fullPath;

        try
        {
            return Path.GetRelativePath(root, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}
