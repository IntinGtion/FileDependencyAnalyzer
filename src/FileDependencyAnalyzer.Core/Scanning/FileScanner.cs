using FileDependencyAnalyzer.Core.Graph;
using FileDependencyAnalyzer.Core.Rules;

namespace FileDependencyAnalyzer.Core.Scanning;

/// <summary>
/// Provides functionality to scan files within a directory structure and analyze their dependencies using configurable
/// analysis rules.
/// </summary>
public sealed class FileScanner
{
    /// <summary>
    /// The dependency analysis rules applied to each scanned file.
    /// </summary>
    private readonly IEnumerable<IDependencyRule> _rules;

    /// <summary>
    /// Creates a new <see cref="FileScanner"/> that applies the provided dependency rules during scanning.
    /// </summary>
    /// <param name="rules">
    /// The set of rules used to detect dependencies (e.g. Markdown links, PowerShell dot-sourcing).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rules"/> is <c>null</c>.</exception>
    public FileScanner(IEnumerable<IDependencyRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Scans a directory with default traversal options (excludes common build/system folders such as ".git", ".vs", "bin", "obj").
    /// </summary>
    /// <param name="rootPath">Root directory to scan.</param>
    /// <returns>
    /// A <see cref="DependencyGraph"/> containing nodes for scanned files and directed edges representing dependencies.
    /// </returns>
    public DependencyGraph Scan(string rootPath)
        => Scan(rootPath, new ScanOptions());

    /// <summary>
    /// Scans all files under <paramref name="rootPath"/> and builds a dependency graph by applying the configured rules.
    /// Every successfully read file becomes a node, even if no dependencies are found.
    /// </summary>
    /// <param name="rootPath">Root directory to scan.</param>
    /// <param name="options">Traversal options (e.g. excluded directories, optional file filter).</param>
    /// <returns>
    /// A <see cref="DependencyGraph"/> containing nodes for scanned files and directed edges representing dependencies.
    /// </returns>
    /// <remarks>
    /// The scan is best-effort: files or directories that cannot be accessed are skipped.
    /// IO errors while reading a file (e.g. locked file, access denied) will not abort the scan.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <c>null</c>.</exception>
    public DependencyGraph Scan(string rootPath, ScanOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        var graph = new DependencyGraph();

        foreach (var file in EnumerateFiles(rootPath, options))
        {
            string content;

            try
            {
                content = File.ReadAllText(file);
            }
            catch (IOException)
            {
                // file locked, skip
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                // no access, skip
                continue;
            }

            // Every successfully read file becomes a node (even if it has no dependencies)
            graph.GetOrAddNode(file);

            foreach (var rule in _rules)
            {
                if (rule.CanHandle(file))
                    rule.Analyze(file, content, graph);
            }
        }

        return graph;
    }

    /// <summary>
    /// Enumerates all files under <paramref name="rootPath"/> using an iterative depth-first traversal.
    /// Excluded directories are skipped early (by name), and an optional file filter can be applied.
    /// </summary>
    /// <param name="rootPath">Root directory to start traversal from.</param>
    /// <param name="options">
    /// Scanner options controlling traversal, such as excluded directory names and an optional file filter.
    /// </param>
    /// <returns>
    /// A lazy sequence of full file paths found under <paramref name="rootPath"/> that match the configured options.
    /// </returns>
    /// <remarks>
    /// Uses an explicit stack instead of recursion to avoid stack overflows on deep directory trees.
    /// Directory enumeration errors (e.g. access denied) are handled by skipping the affected branch.
    /// </remarks>
    private static IEnumerable<string> EnumerateFiles(string rootPath, ScanOptions options)
    {
        // Iterative DFS to allow skipping excluded directories early.
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            IEnumerable<string> subDirs;
            try
            {
                subDirs = Directory.EnumerateDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (var subDir in subDirs)
            {
                var name = GetDirectoryName(subDir);
                if (options.ExcludedDirectoryNames.Contains(name))
                    continue;

                pending.Push(subDir);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (options.FileFilter is not null && !options.FileFilter(file))
                    continue;

                yield return file;
            }
        }
    }

    /// <summary>
    /// Returns the last directory name segment of a path in a robust way.
    /// Trailing directory separators are removed first so that <see cref="Path.GetFileName(string)"/> works reliably.
    /// </summary>
    /// <param name="dirPath">A directory path (absolute or relative).</param>
    /// <returns>
    /// The directory name (last segment) of <paramref name="dirPath"/>.
    /// If the name cannot be determined (e.g. root path), the original <paramref name="dirPath"/> is returned.
    /// </returns>
    private static string GetDirectoryName(string dirPath)
    {
        // Normalize trailing separators to make Path.GetFileName reliable.
        dirPath = dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(dirPath);
        return string.IsNullOrWhiteSpace(name) ? dirPath : name;
    }
}
