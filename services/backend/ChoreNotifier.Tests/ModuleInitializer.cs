using System.Runtime.CompilerServices;
using DotNetEnv;

namespace ChoreNotifier.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Load .env from the main project directory
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ChoreNotifier"));
        var envPath = Path.Combine(projectRoot, ".env");

        if (File.Exists(envPath))
            Env.Load(envPath);
    }
}
