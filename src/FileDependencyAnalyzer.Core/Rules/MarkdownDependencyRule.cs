using System.Text.RegularExpressions;
using FileDependencyAnalyzer.Core.Graph;

namespace FileDependencyAnalyzer.Core.Rules;

/// <summary>
/// Analyzes Markdown files to identify and manage dependencies based on links within the content.
/// </summary>
/// <remarks>This class implements the IDependencyRule interface and is specifically designed to handle Markdown
/// files. It processes only relative links and ignores external links that start with 'http'. Use this rule to ensure
/// that dependencies between Markdown files are correctly identified and represented in the dependency graph.</remarks>
public sealed class MarkdownDependencyRule : IDependencyRule
{
    /// <summary>
    /// Represents a compiled regular expression used to match Markdown link syntax and capture the URL within
    /// parentheses.
    /// </summary>
    /// <remarks>This regular expression is optimized for performance by using the compiled option. It is
    /// designed to extract URLs from Markdown-formatted links, which use the [text](url) pattern. The captured group
    /// contains the URL specified in the link.</remarks>
    private static readonly Regex LinkRegex =
        new(@"\]\(([^)]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// Determines whether the specified file path refers to a Markdown file based on its extension.
    /// </summary>
    /// <remarks>The comparison is performed in a case-insensitive manner. Only files with the ".md" extension
    /// are considered Markdown files by this method.</remarks>
    /// <param name="filePath">The file path to evaluate. This parameter must not be null or empty.</param>
    /// <returns>true if the file path ends with the ".md" extension; otherwise, false.</returns>
    public bool CanHandle(string filePath)
        => filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Analyzes the specified file content to identify and add file dependencies to the provided dependency graph.
    /// </summary>
    /// <remarks>External links that begin with 'http' are ignored. Only links that are relative to the
    /// directory of the specified file are processed.</remarks>
    /// <param name="filePath">The path of the file being analyzed. Used to resolve relative links found within the content.</param>
    /// <param name="content">The content of the file to scan for links to other files.</param>
    /// <param name="graph">The dependency graph to which discovered file dependencies will be added.</param>
    public void Analyze(
        string filePath,
        string content,
        DependencyGraph graph)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory is null)
            return;

        foreach (Match match in LinkRegex.Matches(content))
        {
            var link = match.Groups[1].Value;

            // Externe Links ignorieren
            if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                continue;

            var targetPath = Path.GetFullPath(
                Path.Combine(directory, link));

            if (File.Exists(targetPath))
            {
                graph.AddDependency(filePath, targetPath);
            }
        }
    }
}
