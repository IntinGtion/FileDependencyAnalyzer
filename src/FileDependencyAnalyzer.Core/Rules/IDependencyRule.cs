using FileDependencyAnalyzer.Core.Graph;

namespace FileDependencyAnalyzer.Core.Rules;

/// <summary>
/// Defines a contract for a rule that determines how files and their content are processed and analyzed within a
/// dependency graph.
/// </summary>
/// <remarks>Implementations of this interface provide logic for identifying files that can be handled and for
/// analyzing file content to update the dependency graph. Use this interface to create custom rules for processing
/// different file types or content structures within a dependency analysis system.</remarks>
public interface IDependencyRule
{
    /// <summary>
    /// Determines whether the specified file can be processed by the current rule.
    /// </summary>
    /// <remarks>Use this method to check if a file meets the criteria required for processing before
    /// attempting further operations.</remarks>
    /// <param name="filePath">The full path of the file to evaluate. This parameter must not be null or empty.</param>
    /// <returns>true if the file can be handled by this rule; otherwise, false.</returns>
    bool CanHandle(string filePath);

    /// <summary>
    /// Analyzes the specified content and updates the dependency graph based on the analysis results.
    /// </summary>
    /// <remarks>This method performs a thorough analysis of the provided content and modifies the dependency
    /// graph accordingly. Ensure that the content is well-formed to avoid analysis errors.</remarks>
    /// <param name="filePath">The path to the file containing the content to be analyzed. This parameter cannot be null or empty.</param>
    /// <param name="content">The content to be analyzed, which should be a valid string representation of the data to process.</param>
    /// <param name="graph">The dependency graph that will be updated with the results of the analysis. This parameter must not be null.</param>
    void Analyze(
        string filePath,
        string content,
        DependencyGraph graph);
}
