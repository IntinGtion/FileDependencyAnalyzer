using FileDependencyAnalyzer.Core.Graph;
using FileDependencyAnalyzer.Core.Rules;
using Xunit;

namespace FileDependencyAnalyzer.Tests.Rules;

/// <summary>
/// Provides unit tests for the behavior of the MarkdownDependencyRule class, ensuring it correctly handles Markdown
/// files and their dependencies.
/// </summary>
/// <remarks>This class contains tests that verify the functionality of the MarkdownDependencyRule, including its
/// ability to recognize Markdown files, analyze dependencies, and ignore non-relative links. Each test method is
/// annotated with the [Fact] attribute, indicating that they are unit tests to be executed by a test runner.</remarks>
public sealed class MarkdownDependencyRuleTests
{
    /// <summary>
    /// Verifies that the CanHandle method of MarkdownDependencyRule returns true for file paths with '.md' or '.MD'
    /// extensions and false for other extensions.
    /// </summary>
    /// <remarks>This test ensures that MarkdownDependencyRule correctly identifies Markdown files based on
    /// their file extension, regardless of case sensitivity.</remarks>
    [Fact]
    public void CanHandle_ReturnsTrue_ForMdFiles()
    {
        var rule = new MarkdownDependencyRule();

        Assert.True(rule.CanHandle(@"C:\x\readme.md"));
        Assert.True(rule.CanHandle(@"C:\x\README.MD"));
        Assert.False(rule.CanHandle(@"C:\x\readme.txt"));
    }

    /// <summary>
    /// Verifies that the dependency graph correctly includes both the source and target files and adds an edge when a
    /// Markdown file contains a relative link to an existing file.
    /// </summary>
    /// <remarks>This test ensures that when a Markdown file references another file using a relative link,
    /// the analyzer resolves the link relative to the source file's location and updates the dependency graph
    /// accordingly. Both the source and linked files should be present as nodes, and an edge should exist between them
    /// in the graph.</remarks>
    [Fact]
    public void Analyze_AddsEdge_WhenRelativeLinkedFileExists()
    {
        using var fs = new TempFolder();

        var from = fs.WriteFile("README.md", """
            # Test
            [Intro](intro.md)
            """);

        var to = fs.WriteFile("intro.md", "hi");

        var graph = new DependencyGraph();
        var rule = new MarkdownDependencyRule();

        var content = File.ReadAllText(from);
        rule.Analyze(from, content, graph);

        Assert.Contains(graph.Nodes, n => string.Equals(n.FilePath, from, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(graph.Nodes, n => string.Equals(n.FilePath, to, StringComparison.OrdinalIgnoreCase));

        Assert.Contains(graph.Edges, e =>
            string.Equals(e.From.FilePath, from, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.To.FilePath, to, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that the dependency analysis does not add an edge to the graph when the referenced target file does not
    /// exist in the analyzed Markdown file.
    /// </summary>
    /// <remarks>This test ensures that missing references in Markdown files are handled gracefully by the
    /// dependency analysis, preventing the creation of invalid or non-existent dependencies in the resulting
    /// graph.</remarks>
    [Fact]
    public void Analyze_DoesNotAddEdge_WhenTargetDoesNotExist()
    {
        using var fs = new TempFolder();

        var from = fs.WriteFile("README.md", """
            # Test
            [Missing](missing.md)
            """);

        var graph = new DependencyGraph();
        var rule = new MarkdownDependencyRule();

        rule.Analyze(from, File.ReadAllText(from), graph);

        Assert.Empty(graph.Edges);
    }

    /// <summary>
    /// Verifies that HTTP links are ignored during the analysis of Markdown files.
    /// </summary>
    /// <remarks>This test ensures that links to external HTTP resources do not create edges in the dependency
    /// graph, confirming that only relevant dependencies are captured.</remarks>
    [Fact]
    public void Analyze_IgnoresHttpLinks()
    {
        using var fs = new TempFolder();

        var from = fs.WriteFile("README.md", """
            [Google](https://google.com)
            """);

        var graph = new DependencyGraph();
        var rule = new MarkdownDependencyRule();

        rule.Analyze(from, File.ReadAllText(from), graph);

        Assert.Empty(graph.Edges);
    }

    /// <summary>
    /// Provides a temporary folder for file operations during tests, ensuring that files are created in a unique
    /// directory that is automatically cleaned up after use.
    /// </summary>
    /// <remarks>The temporary folder is created in the system's temporary path and is uniquely identified by
    /// a GUID. It is important to dispose of the TempFolder instance to ensure that the temporary directory is deleted
    /// and resources are released.</remarks>
    private sealed class TempFolder : IDisposable
    {
        /// <summary>
        /// Gets the root directory path used for storing temporary files during test execution.
        /// </summary>
        /// <remarks>The root path is unique for each test run and is located within the system's
        /// temporary directory. This ensures isolation between test runs and prevents conflicts between temporary
        /// files.</remarks>
        public string RootPath { get; } =
            Path.Combine(Path.GetTempPath(), "FileDependencyAnalyzer.Tests", Guid.NewGuid().ToString("N"));

        /// <summary>
        /// Initializes a new instance of the TempFolder class and creates the root directory specified by RootPath.
        /// </summary>
        /// <remarks>This constructor ensures that the directory is created upon instantiation, which is
        /// necessary for any subsequent operations that depend on the existence of the root directory.</remarks>
        public TempFolder()
        {
            Directory.CreateDirectory(RootPath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string WriteFile(string relativePath, string content)
        {
            var fullPath = Path.Combine(RootPath, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        /// <summary>
        /// Releases all resources used by the object and deletes the directory specified by the RootPath property, if
        /// it exists.
        /// </summary>
        /// <remarks>This method attempts to delete the directory and its contents recursively. If the
        /// directory cannot be deleted, such as when it is locked by another process, the method suppresses any
        /// exceptions and continues, ensuring a best-effort cleanup. Call this method when the object is no longer
        /// needed to free associated resources promptly.</remarks>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
            }
            catch
            {
                // Best effort cleanup (z.B. Virenscanner/Index kann kurz sperren)
            }
        }
    }
}
