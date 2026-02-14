using FileDependencyAnalyzer.Core.Rules;
using FileDependencyAnalyzer.Core.Scanning;
using Xunit;

namespace FileDependencyAnalyzer.Tests.Scanning;

public sealed class FileScannerTests
{
    [Fact]
    public void Scan_AddsNodes_ForFilesEvenIfNoDependencies()
    {
        using var fs = new TempFolder();

        // Datei ohne Links / ohne Dependencies
        var lonely = fs.WriteFile("lonely.md", "# Just text");

        // Scanner mit Markdown-Rule (ist egal, weil keine Links vorhanden)
        var scanner = new FileScanner(new[] { new MarkdownDependencyRule() });

        var graph = scanner.Scan(fs.RootPath);

        Assert.Contains(graph.Nodes, n =>
            string.Equals(n.FilePath, lonely, StringComparison.OrdinalIgnoreCase));

        // Optional: Sicherstellen, dass wirklich keine Edges entstanden sind
        Assert.Empty(graph.Edges);
    }

    private sealed class TempFolder : IDisposable
    {
        public string RootPath { get; } =
            Path.Combine(Path.GetTempPath(), "FileDependencyAnalyzer.Tests", Guid.NewGuid().ToString("N"));

        public TempFolder()
        {
            Directory.CreateDirectory(RootPath);
        }

        public string WriteFile(string relativePath, string content)
        {
            var fullPath = Path.Combine(RootPath, relativePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }
}
