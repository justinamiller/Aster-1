namespace Aster.Release;

/// <summary>
/// Generates CI configuration templates for Aster projects.
/// </summary>
public sealed class CiTemplateGenerator
{
    /// <summary>
    /// Generate a GitHub Actions CI workflow for an Aster project.
    /// </summary>
    public static string GenerateGitHubActionsWorkflow()
    {
        return """
            name: Aster CI
            
            on:
              push:
                branches: [ main ]
              pull_request:
                branches: [ main ]
            
            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: '10.0.x'
                  - name: Build
                    run: dotnet build
                  - name: Test
                    run: dotnet test
                  - name: Lint
                    run: dotnet run --project src/Aster.CLI -- lint src/
                  - name: Format check
                    run: dotnet run --project src/Aster.CLI -- fmt --check src/
            """;
    }
}
