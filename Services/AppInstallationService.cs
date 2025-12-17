using AutoInstaller.Data;
using AutoInstaller.Factories;
using AutoInstaller.Helpers;
using AutoInstaller.Models;
using AutoInstaller.UI;
using Spectre.Console;
using System.Text;

namespace AutoInstaller;

public class AppInstallationService
{
    private readonly InstallerFactory _installerFactory;

    public AppInstallationService()
    {
        _installerFactory = new InstallerFactory();
    }

    public async Task RunAsync()
    {
        SetupConsoleEncoding();

        if (!AdminHelper.IsRunningAsAdministrator())
        {
            if (!ConsoleUI.ShowAdminWarning())
            {
                return;
            }
        }

        ConsoleUI.ShowTitle();

        var appPool = AppRepository.GetAllApps();
        var selectedApps = ConsoleUI.ShowAppSelectionMenu(appPool);

        if (selectedApps.Count == 0)
        {
            ConsoleUI.ShowNoAppSelectedMessage();
            return;
        }

        ConsoleUI.ShowInstallationStartHeader();
        ConsoleUI.ShowInstallationTable(selectedApps);

        await InstallAppsAsync(selectedApps);

        ConsoleUI.ShowInstallationComplete();
    }

    private static void SetupConsoleEncoding()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                
                var encoding = Encoding.GetEncoding(65001);
                Console.OutputEncoding = encoding;
                Console.InputEncoding = encoding;
            }
            else
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
            }
        }
        catch
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
            }
            catch
            {
                // Default encoding kullan
            }
        }
    }

    private async Task InstallAppsAsync(List<AppInfo> apps)
    {
        foreach (var app in apps)
        {
            await InstallAppAsync(app);
        }
    }

    private async Task InstallAppAsync(AppInfo app)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync($"[cyan]{app.Name.EscapeMarkup()}[/] kuruluyor...", async ctx =>
            {
                try
                {
                    var installer = _installerFactory.GetInstaller(app.Type);
                    var result = await installer.InstallAsync(app);
                    
                    ConsoleUI.ShowInstallationResult(app, result.Status, result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    ConsoleUI.ShowInstallationError(app, ex.Message);
                }
            });
    }
}
