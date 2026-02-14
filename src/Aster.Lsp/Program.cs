namespace Aster.Lsp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var server = new LspServer(Console.OpenStandardInput(), Console.OpenStandardOutput());
        await server.RunAsync();
    }
}
