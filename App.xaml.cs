using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BlankFiller.Services;

namespace BlankFiller;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<AppSettings>();
                services.AddSingleton<AuthService>();
                services.AddSingleton<FormDataService>();
                services.AddSingleton<RegistryService>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await Host!.StartAsync();

        var settings = Host.Services.GetRequiredService<AppSettings>();
        Directory.CreateDirectory(Path.GetDirectoryName(settings.DatabasePath)!);
        Directory.CreateDirectory(settings.ScreenshotsPath);
        Directory.CreateDirectory(settings.ExcelTemplatesPath);
        Directory.CreateDirectory(settings.ExcelOutputPath);

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await Host!.StopAsync();
        Host.Dispose();
        base.OnExit(e);
    }
}