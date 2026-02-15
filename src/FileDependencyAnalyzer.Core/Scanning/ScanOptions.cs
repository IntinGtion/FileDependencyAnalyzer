namespace FileDependencyAnalyzer.Core.Scanning;

/// <summary>
/// Options that influence how the scanner traverses the directory tree.
/// </summary>
public sealed class ScanOptions
{
    /// <summary>
    /// Directory names to skip completely (e.g. ".git", "bin", "obj").
    /// The comparison is case-insensitive.
    /// </summary>
    public ISet<string> ExcludedDirectoryNames { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            "bin",
            "obj"
        };

    /// <summary>
    /// Optional file filter (true => include).
    /// </summary>
    public Func<string, bool>? FileFilter { get; set; }
}
