using FileDependencyAnalyzer.Core.Graph;
using FileDependencyAnalyzer.Core.Rules;

namespace FileDependencyAnalyzer.Core.Scanning;

/// <summary>
/// Provides functionality to scan files within a directory structure and analyze their dependencies using configurable
/// analysis rules.
/// </summary>
/// <remarks>The FileScanner class enables recursive scanning of files in a specified root directory and applies a
/// set of dependency rules to construct a dependency graph. The rules must implement the IDependencyRule interface and
/// are used to determine how dependencies are identified and validated. This class is designed to support extensible
/// and customizable dependency analysis scenarios, allowing users to tailor the scanning process to their specific
/// requirements.</remarks>
public sealed class FileScanner
{
    /// <summary>
    /// Contains the collection of dependency rules used to validate dependencies.
    /// </summary>
    /// <remarks>Each rule in the collection implements the IDependencyRule interface and defines a specific
    /// validation criterion. The rules are applied during dependency analysis to enforce constraints or detect issues
    /// according to the application's requirements.</remarks>
    private readonly IEnumerable<IDependencyRule> _rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileScanner"/> class using the specified collection of dependency
    /// rules.
    /// </summary>
    /// <remarks>The <see cref="FileScanner"/> uses the provided dependency rules to determine how file
    /// dependencies are identified during scanning. Ensure that each rule in the collection is properly configured to
    /// achieve the desired analysis results.</remarks>
    /// <param name="rules">The collection of dependency rules to apply when scanning files. This parameter must not be null and should
    /// contain at least one rule.</param>
    public FileScanner(IEnumerable<IDependencyRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Analyzes all files within the specified root directory and its subdirectories to construct a dependency graph
    /// based on defined analysis rules.
    /// </summary>
    /// <remarks>The method recursively scans all subdirectories for files and applies each configured
    /// analysis rule to determine dependencies. Ensure that the analysis rules are compatible with the file types
    /// present in the directory structure.</remarks>
    /// <param name="rootPath">The full path to the root directory to scan. This path must exist and be accessible.</param>
    /// <returns>A DependencyGraph object representing the relationships and dependencies identified among the scanned files.</returns>
    public DependencyGraph Scan(string rootPath)
    {
        var graph = new DependencyGraph();

        var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".vs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
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

            // Jede erfolgreich gelesene Datei als Node aufnehmen
            graph.GetOrAddNode(file);

            foreach (var rule in _rules)
            {
                if (rule.CanHandle(file))
                {
                    rule.Analyze(file, content, graph);
                }
            }
        }

        return graph;
    }
}
