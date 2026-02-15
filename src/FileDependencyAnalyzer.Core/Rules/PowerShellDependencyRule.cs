using System.Text.RegularExpressions;
using FileDependencyAnalyzer.Core.Graph;

namespace FileDependencyAnalyzer.Core.Rules;

public sealed class PowerShellDependencyRule : IDependencyRule
{
    // Dot-sourcing:  . .\helper.ps1   oder   . "$PSScriptRoot\utils.ps1"
    // Import-Module: Import-Module .\MyModule.psm1  oder Import-Module "$PSScriptRoot\MyModule.psd1"
    private static readonly Regex DotSourcingRegex =
        new(@"(?m)^\s*\.\s+(?<path>(""[^""]+""|'[^']+'|[^\s#;]+))",
            RegexOptions.Compiled);

    private static readonly Regex ImportModuleRegex =
        new(@"(?m)^\s*Import-Module\s+(?<path>(""[^""]+""|'[^']+'|[^\s#;]+))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanHandle(string filePath)
        => filePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)
           || filePath.EndsWith(".psm1", StringComparison.OrdinalIgnoreCase)
           || filePath.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase);

    public void Analyze(string filePath, string content, DependencyGraph graph)
    {
        var baseDir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(baseDir))
            return;

        AnalyzeRegex(filePath, content, baseDir, DotSourcingRegex, graph);
        AnalyzeRegex(filePath, content, baseDir, ImportModuleRegex, graph);
    }

    private static void AnalyzeRegex(
        string filePath,
        string content,
        string baseDir,
        Regex regex,
        DependencyGraph graph)
    {
        foreach (Match m in regex.Matches(content))
        {
            var raw = m.Groups["path"].Value;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (!TryResolvePath(raw, baseDir, out var resolvedFullPath))
                continue;

            if (File.Exists(resolvedFullPath))
            {
                graph.AddDependency(filePath, resolvedFullPath);
            }
        }
    }

    private static bool TryResolvePath(string raw, string baseDir, out string fullPath)
    {
        fullPath = string.Empty;

        var token = raw.Trim();

        // Quotes entfernen
        if ((token.StartsWith("\"") && token.EndsWith("\"")) ||
            (token.StartsWith("'") && token.EndsWith("'")))
        {
            token = token.Substring(1, token.Length - 2);
        }

        token = token.Trim();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        // "Import-Module Pester" o.ä. ignorieren (kein Pfad)
        // Wir nehmen nur Dinge, die wie ein Pfad aussehen:
        // - beginnt mit .\ oder ..\ oder / oder \ oder Laufwerk oder enthält Pfadtrenner
        var looksLikePath =
            token.StartsWith(".\\") ||
            token.StartsWith("..\\") ||
            token.StartsWith("./") ||
            token.StartsWith("../") ||
            token.StartsWith("\\") ||
            token.StartsWith("/") ||
            token.Contains('\\') ||
            token.Contains('/') ||
            Path.IsPathRooted(token);

        if (!looksLikePath)
            return false;

        // Map $PSScriptRoot to the directory of the current script (simple static case)
        token = token.Replace("$PSScriptRoot", baseDir, StringComparison.OrdinalIgnoreCase)
                     .Replace("${PSScriptRoot}", baseDir, StringComparison.OrdinalIgnoreCase);

        // If other variables remain, skip (not safely resolvable statically)
        if (token.Contains('$'))
            return false;

        // Normalize Windows/Unix separators to the current OS separator.
        // This allows resolving PowerShell paths like ".\helper.ps1" on Linux runners as well.
        token = NormalizeSeparators(token);

        // Resolve relative paths
        var combined = Path.IsPathRooted(token)
            ? token
            : Path.Combine(baseDir, token);

        fullPath = Path.GetFullPath(combined);
        return true;
    }

    private static string NormalizeSeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar)
               .Replace('/', Path.DirectorySeparatorChar);
}
