using FileDependencyAnalyzer.Core.Graph;
using Xunit;

namespace FileDependencyAnalyzer.Tests.Graph;

public sealed class DependencyGraphAnalysisTests
{
    [Fact]
    public void InboundOutboundCounts_AreCalculatedCorrectly()
    {
        var g = new DependencyGraph();

        var a = g.GetOrAddNode(@"C:\A.ps1");
        var b = g.GetOrAddNode(@"C:\B.ps1");
        var helper = g.GetOrAddNode(@"C:\helper.ps1");

        g.AddDependency(a.FilePath, helper.FilePath);
        g.AddDependency(b.FilePath, helper.FilePath);

        Assert.Equal(1, g.GetOutboundCount(a));
        Assert.Equal(1, g.GetOutboundCount(b));
        Assert.Equal(0, g.GetOutboundCount(helper));

        Assert.Equal(0, g.GetInboundCount(a));
        Assert.Equal(0, g.GetInboundCount(b));
        Assert.Equal(2, g.GetInboundCount(helper));
    }

    [Fact]
    public void OrphanNodes_ReturnsNodesWithNoInboundAndNoOutbound()
    {
        var g = new DependencyGraph();

        var orphan = g.GetOrAddNode(@"C:\orphan.md");
        var a = g.GetOrAddNode(@"C:\A.md");
        var b = g.GetOrAddNode(@"C:\B.md");

        g.AddDependency(a.FilePath, b.FilePath);

        var orphans = g.GetOrphanNodes().Select(n => n.FilePath).ToList();

        Assert.Contains(orphan.FilePath, orphans);
        Assert.DoesNotContain(a.FilePath, orphans);
        Assert.DoesNotContain(b.FilePath, orphans);
    }

    [Fact]
    public void TopInbound_ReturnsMostReferencedNodes()
    {
        var g = new DependencyGraph();

        var a = g.GetOrAddNode(@"C:\A.md");
        var b = g.GetOrAddNode(@"C:\B.md");
        var c = g.GetOrAddNode(@"C:\C.md");

        // B wird 2x referenziert, C 1x, A 0x
        g.AddDependency(a.FilePath, b.FilePath);
        g.AddDependency(c.FilePath, b.FilePath);
        g.AddDependency(a.FilePath, c.FilePath);

        var top = g.GetTopInbound(2).ToList();

        Assert.Equal(@"C:\B.md", top[0].Node.FilePath);
        Assert.Equal(2, top[0].Inbound);

        Assert.Equal(@"C:\C.md", top[1].Node.FilePath);
        Assert.Equal(1, top[1].Inbound);
    }
}
