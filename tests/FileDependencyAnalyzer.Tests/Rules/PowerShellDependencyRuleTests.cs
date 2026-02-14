using FileDependencyAnalyzer.Core.Graph;
using FileDependencyAnalyzer.Core.Rules;
using Xunit;

namespace FileDependencyAnalyzer.Tests.Rules;

public sealed class PowerShellDependencyRuleTests
{
    [Fact]
    public void CanHandle_ReturnsTrue_ForPsFiles()
    {
        var rule = new PowerShellDependencyRule();

        Assert.True(rule.CanHandle(@"C:\x\a.ps1"));
        Assert.True(rule.CanHandle(@"C:\x\a.PSM1"));
        Assert.True(rule.CanHandle(@"C:\x\a.psd1"));
        Assert.False(rule.CanHandle(@"C:\x\a.md"));
    }

    [Fact]
    public void Analyze_AddsEdge_ForDotSourcing_RelativePath()
    {
        using var fs = new TempFolder();

        var helper = fs.WriteFile("helper.ps1", "# helper");
        var main = fs.WriteFile("main.ps1", ". .\\helper.ps1");

        var graph = new DependencyGraph();
        var rule = new PowerShellDependencyRule();

        rule.Analyze(main, File.ReadAllText(main), graph);

        Assert.Contains(graph.Edges, e =>
            string.Equals(e.From.FilePath, main, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.To.FilePath, helper, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_AddsEdge_ForImportModule_WithPSScriptRoot()
    {
        using var fs = new TempFolder();

        var module = fs.WriteFile("MyModule.psm1", "# module");
        var main = fs.WriteFile("main.ps1", "Import-Module \"$PSScriptRoot\\MyModule.psm1\"");

        var graph = new DependencyGraph();
        var rule = new PowerShellDependencyRule();

        rule.Analyze(main, File.ReadAllText(main), graph);

        Assert.Contains(graph.Edges, e =>
            string.Equals(e.From.FilePath, main, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.To.FilePath, module, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_Ignores_NonPath_ImportModule()
    {
        using var fs = new TempFolder();

        var main = fs.WriteFile("main.ps1", "Import-Module Pester");

        var graph = new DependencyGraph();
        var rule = new PowerShellDependencyRule();

        rule.Analyze(main, File.ReadAllText(main), graph);

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
                // best effort
            }
        }
    }
}
