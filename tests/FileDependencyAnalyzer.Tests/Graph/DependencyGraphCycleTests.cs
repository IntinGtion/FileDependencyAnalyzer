using FileDependencyAnalyzer.Core.Graph;
using Xunit;

namespace FileDependencyAnalyzer.Tests.Graph;

public sealed class DependencyGraphCycleTests
{
    [Fact]
    public void GetCycles_DetectsTwoNodeCycle()
    {
        var g = new DependencyGraph();

        g.AddDependency(@"C:\A.ps1", @"C:\B.ps1");
        g.AddDependency(@"C:\B.ps1", @"C:\A.ps1");

        var cycles = g.GetCycles();

        Assert.Single(cycles);
        Assert.Equal(2, cycles[0].Count);
        Assert.Contains(cycles[0], n => n.FilePath == @"C:\A.ps1");
        Assert.Contains(cycles[0], n => n.FilePath == @"C:\B.ps1");
    }

    [Fact]
    public void GetCycles_DetectsThreeNodeCycle()
    {
        var g = new DependencyGraph();

        g.AddDependency(@"C:\A.ps1", @"C:\B.ps1");
        g.AddDependency(@"C:\B.ps1", @"C:\C.ps1");
        g.AddDependency(@"C:\C.ps1", @"C:\A.ps1");

        var cycles = g.GetCycles();

        Assert.Single(cycles);
        Assert.Equal(3, cycles[0].Count);
        Assert.Contains(cycles[0], n => n.FilePath == @"C:\A.ps1");
        Assert.Contains(cycles[0], n => n.FilePath == @"C:\B.ps1");
        Assert.Contains(cycles[0], n => n.FilePath == @"C:\C.ps1");
    }

    [Fact]
    public void GetCycles_DetectsSelfLoop()
    {
        var g = new DependencyGraph();

        g.AddDependency(@"C:\A.ps1", @"C:\A.ps1");

        var cycles = g.GetCycles();

        Assert.Single(cycles);
        Assert.Single(cycles[0]);
        Assert.Equal(@"C:\A.ps1", cycles[0][0].FilePath);
    }

    [Fact]
    public void GetCycles_ReturnsEmpty_WhenNoCycles()
    {
        var g = new DependencyGraph();

        g.AddDependency(@"C:\A.ps1", @"C:\B.ps1");
        g.AddDependency(@"C:\B.ps1", @"C:\C.ps1");

        var cycles = g.GetCycles();

        Assert.Empty(cycles);
    }
}
