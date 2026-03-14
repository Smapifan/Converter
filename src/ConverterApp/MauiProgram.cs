using ConverterApp.Services;
using ConverterApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace ConverterApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<ConversionEngine>();
        builder.Services.AddSingleton<FileService>();
        builder.Services.AddSingleton<GitHubImportService>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Register Pages
        builder.Services.AddTransient<Pages.MainPage>();
        builder.Services.AddTransient<Pages.SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
