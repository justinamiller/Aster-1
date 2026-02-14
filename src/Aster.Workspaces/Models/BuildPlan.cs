namespace Aster.Workspaces.Models;

/// <summary>
/// A build plan describes the ordered set of compilation steps.
/// </summary>
public sealed class BuildPlan
{
    public IReadOnlyList<BuildStep> Steps { get; }

    public BuildPlan(IReadOnlyList<BuildStep> steps)
    {
        Steps = steps;
    }

    public static BuildPlan FromGraph(BuildGraph graph)
    {
        var order = graph.TopologicalSort();
        var steps = order.Select((name, index) => new BuildStep(
            name,
            index,
            graph.GetDependencies(name)
        )).ToList();
        return new BuildPlan(steps);
    }
}

/// <summary>
/// A single compilation step in a build plan.
/// </summary>
public sealed record BuildStep(string PackageName, int Order, IReadOnlyList<string> DependsOn);
