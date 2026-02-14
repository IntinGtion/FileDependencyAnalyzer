namespace FileDependencyAnalyzer.Core.Models;

/// <summary>
/// Represents a node in a dependency graph that is associated with a specific file path.
/// </summary>
/// <remarks>Instances of this class are compared based on their file paths using case-insensitive logic, making
/// it suitable for use in environments where file system case sensitivity is not required or not supported. This class
/// is typically used to model dependencies between files in scenarios such as build systems or static analysis
/// tools.</remarks>
public sealed class DependencyNode
{
    /// <summary>
    /// Gets the file path associated with the current instance.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the DependencyNode class for the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the file associated with this dependency node. This parameter cannot be null or empty.</param>
    public DependencyNode(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance based on the file path.
    /// </summary>
    /// <remarks>This method performs a case-insensitive comparison of the file paths.</remarks>
    /// <param name="obj">The object to compare with the current instance. It can be null.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
        => obj is DependencyNode other &&
           string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a hash code for the current instance that is based on the file path, using a case-insensitive
    /// comparison.
    /// </summary>
    /// <remarks>This override ensures that file paths differing only by case produce the same hash code,
    /// which is useful in scenarios where file system case sensitivity is not required or not supported.</remarks>
    /// <returns>A 32-bit signed integer hash code that represents the current instance, derived from the lowercase version of
    /// the file path.</returns>
    public override int GetHashCode()
        => FilePath.ToLowerInvariant().GetHashCode();
}
