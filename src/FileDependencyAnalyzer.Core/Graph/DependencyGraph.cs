using FileDependencyAnalyzer.Core.Models;

namespace FileDependencyAnalyzer.Core.Graph;

/// <summary>
/// Represents a directed graph of dependencies, allowing for the management and retrieval of dependency nodes and
/// edges.
/// </summary>
/// <remarks>The DependencyGraph class provides methods to add dependencies and retrieve nodes, ensuring that each
/// unique path is associated with a single dependency node. It supports case-insensitive lookups for dependency nodes,
/// making it suitable for scenarios where path casing may vary. The graph maintains collections of nodes and edges,
/// which reflect the current state of dependencies and are updated as the graph changes.</remarks>
public sealed class DependencyGraph
{
    /// <summary>
    /// Gets the dictionary that stores the dependency nodes, using a case-insensitive string comparer for keys.
    /// </summary>
    /// <remarks>This dictionary is initialized to allow for case-insensitive lookups, which is useful when
    /// the keys may vary in casing. It is intended to manage the relationships between different dependency nodes
    /// efficiently.</remarks>
    private readonly Dictionary<string, DependencyNode> _nodes =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 
    /// </summary>
    private readonly List<DependencyEdge> _edges = new();

    /// <summary>
    /// Gets the collection of dependency nodes in the graph as a read-only collection.
    /// </summary>
    /// <remarks>The returned collection reflects the current set of dependency nodes and is updated as the
    /// graph changes. The collection is read-only and cannot be modified directly.</remarks>
    public IReadOnlyCollection<DependencyNode> Nodes => _nodes.Values;

    /// <summary>
    /// Gets the collection of dependency edges associated with this graph.
    /// </summary>
    /// <remarks>The returned collection is read-only and represents the relationships between dependencies in
    /// the graph. The collection cannot be modified directly.</remarks>
    public IReadOnlyCollection<DependencyEdge> Edges => _edges;

    /// <summary>
    /// Gets the dependency node associated with the specified path, or adds a new node if one does not already exist.
    /// </summary>
    /// <remarks>This method ensures that each unique path is associated with a single dependency node. If a
    /// node for the given path already exists, it is returned; otherwise, a new node is created and added to the
    /// collection.</remarks>
    /// <param name="path">The path that identifies the dependency node. This parameter cannot be null or empty.</param>
    /// <returns>The existing dependency node for the specified path if it exists; otherwise, a new dependency node created for
    /// the path.</returns>
    public DependencyNode GetOrAddNode(string path)
    {
        if (_nodes.TryGetValue(path, out var existing))
            return existing;

        var node = new DependencyNode(path);
        _nodes[path] = node;
        return node;
    }

    /// <summary>
    /// Adds a dependency from a source node to a target node in the dependency graph.
    /// </summary>
    /// <remarks>If either the source or target node does not exist in the graph, it is created
    /// automatically.</remarks>
    /// <param name="fromPath">The path of the source node from which the dependency originates. This parameter cannot be null or empty.</param>
    /// <param name="toPath">The path of the target node to which the dependency points. This parameter cannot be null or empty.</param>
    public void AddDependency(string fromPath, string toPath)
    {
        var from = GetOrAddNode(fromPath);
        var to = GetOrAddNode(toPath);

        _edges.Add(new DependencyEdge(from, to));
    }

    /// <summary>
    /// Gets the number of inbound edges that are connected to the specified dependency node.
    /// </summary>
    /// <remarks>This method counts all edges in the graph where the provided node is the target. An inbound
    /// edge is one that points to the specified node from another node in the dependency graph.</remarks>
    /// <param name="node">The dependency node for which to count inbound edges. This parameter cannot be null.</param>
    /// <returns>The number of edges that have the specified node as their destination.</returns>
    public int GetInboundCount(DependencyNode node)
        => _edges.Count(e => ReferenceEquals(e.To, node));

    /// <summary>
    /// Gets the number of outbound edges that originate from the specified dependency node.
    /// </summary>
    /// <remarks>This method counts only edges where the <c>From</c> property matches the provided node.
    /// Ensure that the specified node is part of the graph to obtain an accurate count.</remarks>
    /// <param name="node">The dependency node for which to count outbound edges. This parameter cannot be null.</param>
    /// <returns>The number of outbound edges originating from the specified dependency node.</returns>
    public int GetOutboundCount(DependencyNode node)
        => _edges.Count(e => ReferenceEquals(e.From, node));

    /// <summary>
    /// Retrieves a collection of unique dependency nodes that have an outbound connection to the specified node.
    /// </summary>
    /// <remarks>This method returns each source node only once, even if multiple edges exist from the same
    /// source to the specified node.</remarks>
    /// <param name="node">The dependency node for which to find inbound source nodes. This parameter cannot be null.</param>
    /// <returns>An enumerable collection of distinct dependency nodes that are sources for the specified node.</returns>
    public IEnumerable<DependencyNode> GetInboundSources(DependencyNode node)
        => _edges
            .Where(e => ReferenceEquals(e.To, node))
            .Select(e => e.From)
            .Distinct();

    /// <summary>
    /// Retrieves a collection of distinct dependency nodes that are directly connected as outbound targets from the
    /// specified node.
    /// </summary>
    /// <remarks>This method filters the edges to find all unique targets that originate from the provided
    /// node. It is important to ensure that the node is part of the graph; otherwise, the result may be
    /// empty.</remarks>
    /// <param name="node">The dependency node for which to retrieve outbound targets. This parameter cannot be null.</param>
    /// <returns>An enumerable collection of distinct dependency nodes that are directly connected as outbound targets from the
    /// specified node.</returns>
    public IEnumerable<DependencyNode> GetOutboundTargets(DependencyNode node)
        => _edges
            .Where(e => ReferenceEquals(e.From, node))
            .Select(e => e.To)
            .Distinct();

    /// <summary>
    /// Retrieves a collection of dependency nodes that are not connected to any other nodes in the graph.
    /// </summary>
    /// <remarks>Orphan nodes are defined as nodes with no inbound or outbound dependencies. This method can
    /// be used to identify isolated nodes that are not part of any dependency chain.</remarks>
    /// <returns>An enumerable collection of <see cref="DependencyNode"/> instances representing the orphan nodes. The collection
    /// is empty if all nodes are connected.</returns>
    public IEnumerable<DependencyNode> GetOrphanNodes()
        => Nodes.Where(n => GetInboundCount(n) == 0 && GetOutboundCount(n) == 0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    public IEnumerable<(DependencyNode Node, int Inbound)> GetTopInbound(int top)
        => Nodes
            .Select(n => (Node: n, Inbound: GetInboundCount(n)))
            .OrderByDescending(x => x.Inbound)
            .ThenBy(x => x.Node.FilePath, StringComparer.OrdinalIgnoreCase)
            .Take(top);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    public IEnumerable<(DependencyNode Node, int Outbound)> GetTopOutbound(int top)
        => Nodes
            .Select(n => (Node: n, Outbound: GetOutboundCount(n)))
            .OrderByDescending(x => x.Outbound)
            .ThenBy(x => x.Node.FilePath, StringComparer.OrdinalIgnoreCase)
            .Take(top);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<IReadOnlyList<DependencyNode>> GetCycles()
    {
        // Adjazenzliste: Node -> Outbound-Nachbarn
        var adjacency = Nodes.ToDictionary(
            n => n,
            n => _edges.Where(e => ReferenceEquals(e.From, n)).Select(e => e.To).ToList());

        // Tarjan
        var index = 0;
        var stack = new Stack<DependencyNode>();
        var onStack = new HashSet<DependencyNode>();
        var indexes = new Dictionary<DependencyNode, int>();
        var lowlinks = new Dictionary<DependencyNode, int>();

        var sccs = new List<List<DependencyNode>>();

        void StrongConnect(DependencyNode v)
        {
            indexes[v] = index;
            lowlinks[v] = index;
            index++;

            stack.Push(v);
            onStack.Add(v);

            foreach (var w in adjacency[v])
            {
                if (!indexes.ContainsKey(w))
                {
                    StrongConnect(w);
                    lowlinks[v] = Math.Min(lowlinks[v], lowlinks[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowlinks[v] = Math.Min(lowlinks[v], indexes[w]);
                }
            }

            // Root einer SCC gefunden
            if (lowlinks[v] == indexes[v])
            {
                var component = new List<DependencyNode>();
                DependencyNode x;

                do
                {
                    x = stack.Pop();
                    onStack.Remove(x);
                    component.Add(x);
                }
                while (!ReferenceEquals(x, v));

                sccs.Add(component);
            }
        }

        foreach (var node in Nodes)
        {
            if (!indexes.ContainsKey(node))
                StrongConnect(node);
        }

        // Nur echte Zyklen zurückgeben:
        // - Größe >= 2 => zyklisch
        // - Größe == 1 => nur wenn Self-Loop existiert
        bool HasSelfLoop(DependencyNode n)
            => _edges.Any(e => ReferenceEquals(e.From, n) && ReferenceEquals(e.To, n));

        var cycles = sccs
            .Where(c => c.Count >= 2 || (c.Count == 1 && HasSelfLoop(c[0])))
            .Select(c => (IReadOnlyList<DependencyNode>)c
                .OrderBy(n => n.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToList())
            .OrderBy(c => c.Count)
            .ThenBy(c => c[0].FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return cycles;
    }


}
