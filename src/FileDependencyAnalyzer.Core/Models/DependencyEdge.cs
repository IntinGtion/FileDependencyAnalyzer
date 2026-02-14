namespace FileDependencyAnalyzer.Core.Models;

/// <summary>
/// Represents a directed edge that defines a dependency relationship between two nodes in a dependency graph.
/// </summary>
/// <remarks>Use this class to model a connection from a source dependency node to a target dependency node within
/// a dependency graph. Both the source and target nodes must be valid and non-null to accurately represent the
/// dependency relationship. This type is typically used to analyze or visualize dependencies between components, files,
/// or other entities represented as nodes.</remarks>
public sealed class DependencyEdge
{
    /// <summary>
    /// Gets the dependency node from which this node originates.
    /// </summary>
    public DependencyNode From { get; }

    /// <summary>
    /// Gets the target dependency node that this edge points to.
    /// </summary>
    public DependencyNode To { get; }

    /// <summary>
    /// Initializes a new instance of the DependencyEdge class that represents a directed relationship between two
    /// dependency nodes.
    /// </summary>
    /// <remarks>Use this constructor to create a directed edge in a dependency graph, establishing a
    /// relationship from the specified source node to the target node. Both nodes must be valid and non-null to
    /// accurately represent the dependency.</remarks>
    /// <param name="from">The source dependency node from which the edge originates. Cannot be null.</param>
    /// <param name="to">The target dependency node to which the edge points. Cannot be null.</param>
    public DependencyEdge(DependencyNode from, DependencyNode to)
    {
        From = from;
        To = to;
    }
}
