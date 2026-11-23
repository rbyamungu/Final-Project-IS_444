using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Prototype_Final_Project.Services;
using Prototype_Final_Project.ViewModels;
using Prototype_Final_Project.Views;

namespace Prototype_Final_Project;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        InitializeSqliteNative();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register Core Infrastructure Services (Section 2)
        builder.Services.AddSingleton<LocalDatabaseService>();
        builder.Services.AddSingleton<RestDataService>();
        builder.Services.AddSingleton<NetworkMonitoringService>();

        // Register ViewModels (Section 2)
        builder.Services.AddTransient<DiscoveryFeedViewModel>();
        builder.Services.AddTransient<SubmitWishViewModel>();
        builder.Services.AddTransient<DeveloperDashboardViewModel>();
        builder.Services.AddTransient<WishDetailViewModel>();
        builder.Services.AddTransient<AboutProjectViewModel>();

        // Register Views (Section 2)
        builder.Services.AddTransient<DiscoveryFeedPage>();
        builder.Services.AddTransient<SubmitWishPage>();
        builder.Services.AddTransient<DeveloperDashboardPage>();
        builder.Services.AddTransient<WishDetailPage>();
        builder.Services.AddTransient<AboutProjectPage>();

        return builder.Build();
    }

    /// <summary>
    /// Explicitly initializes SQLite native batteries and registers a Windows DllImportResolver
    /// to ensure e_sqlite3.dll is correctly resolved in unpackaged Windows desktop execution.
    /// </summary>
    private static void InitializeSqliteNative()
    {
        try
        {
            SQLitePCL.Batteries_V2.Init();
        }
        catch { }

#if WINDOWS
        try
        {
            var resolver = new DllImportResolver((libraryName, assembly, searchPath) =>
            {
                if (libraryName.Contains("sqlite3", StringComparison.OrdinalIgnoreCase))
                {
                    string dllName = "e_sqlite3.dll";
                    string[] candidates =
                    {
                        Path.Combine(AppContext.BaseDirectory, dllName),
                        Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native", dllName),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName)
                    };

                    foreach (var path in candidates)
                    {
                        if (File.Exists(path) && NativeLibrary.TryLoad(path, out IntPtr handle))
                            return handle;
                    }
                }
                return IntPtr.Zero;
            });

            NativeLibrary.SetDllImportResolver(typeof(SQLite.SQLiteConnection).Assembly, resolver);
            NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.raw).Assembly, resolver);
        }
        catch { }
#endif
    }
}
