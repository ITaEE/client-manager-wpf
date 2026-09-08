using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.ClientManager.App.Services;
using Portfolio.ClientManager.App.ViewModels;
using Portfolio.ClientManager.App.Views;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Services;
using Portfolio.ClientManager.Infrastructure;
using Portfolio.ClientManager.Infrastructure.Data;

namespace Portfolio.ClientManager.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            await _serviceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            await viewModel.InitializeAsync();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Client Manager could not start.\n\n{exception.Message}",
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddInfrastructure();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClientService, ClientService>();
        services.AddSingleton<IClientImportService, ClientImportService>();
        services.AddSingleton<IClientCsvSerializer, ClientCsvSerializer>();
        services.AddSingleton<IUserDialogService, UserDialogService>();
        services.AddSingleton<ICsvFileService, CsvFileService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred.\n\n{e.Exception.Message}",
            "Client Manager",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
