using AutoInstaller.Constants;
using AutoInstaller.Models;
using Spectre.Console;
using System.Diagnostics;

namespace AutoInstaller.UI;

public static class ConsoleUI
{
    #region Admin Warning
    
    public static bool ShowAdminWarning()
    {
        var panel = new Panel(
            new Markup($"[{UIConstants.ColorYellow}]![/] [bold]Yönetici yetkisi olmadan çalışıyorsunuz[/]\n\n" +
                       "[dim]Bazı uygulamalar düzgün kurulamayabilir.[/]\n" +
                       "[dim]Yönetici yetkisi ile başlatmak önerilir.[/]"))
            .Header($"[{UIConstants.ColorYellow}]Uyarı[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{UIConstants.ColorYellow}]Ne yapmak istersiniz?[/]")
                .AddChoices(new[] {
                    "Yönetici olarak yeniden başlat",
                    "Yetkisiz devam et",
                    "Çıkış"
                }));

        return choice switch
        {
            "Yönetici olarak yeniden başlat" => HandleRestartAsAdmin(),
            "Çıkış" => false,
            _ => HandleContinueWithoutAdmin()
        };
    }

    private static bool HandleRestartAsAdmin()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[{UIConstants.ColorRed}]Hata: {ex.Message.EscapeMarkup()}[/]");
            ShowPressKeyMessage(UIConstants.PressKeyToContinue);
        }
        
        return false;
    }

    private static bool HandleContinueWithoutAdmin()
    {
        AnsiConsole.Clear();
        return true;
    }
    
    #endregion

    #region Title and Headers
    
    public static void ShowTitle()
    {
        AnsiConsole.Write(
            new FigletText("Auto Installer")
                .Centered()
                .Color(Color.Cyan1));
        AnsiConsole.WriteLine();
    }

    public static void ShowInstallationStartHeader()
    {
        AnsiConsole.Clear();
        var rule = new Rule($"[{UIConstants.ColorCyan}]>> KURULUM BAŞLIYOR[/]")
        {
            Style = Style.Parse(UIConstants.ColorCyan)
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    public static void ShowInstallationComplete()
    {
        AnsiConsole.WriteLine();
        var successRule = new Rule($"[{UIConstants.ColorGreen}]TÜM KURULUMLAR TAMAMLANDI[/]")
        {
            Style = Style.Parse(UIConstants.ColorGreen)
        };
        AnsiConsole.Write(successRule);
        
        AnsiConsole.WriteLine();
        ShowPressKeyMessage(UIConstants.PressKeyToExit);
        Console.ReadKey(true);
    }
    
    #endregion

    #region App Selection
    
    public static List<AppInfo> ShowAppSelectionMenu(List<AppInfo> appPool)
    {
        var choices = appPool.Select(FormatAppChoice).ToList();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[{UIConstants.ColorCyan}]>> Kurulacak uygulamaları seçin:[/]")
                .PageSize(15)
                .MoreChoicesText($"[{UIConstants.ColorGrey}](Daha fazla görmek için yukarı/aşağı)[/")
                .InstructionsText(
                    $"[{UIConstants.ColorGrey}](Seçmek için [{UIConstants.ColorBlue}]<space>[/], onaylamak için [{UIConstants.ColorGreen}]<enter>[/])[/]")
                .AddChoices(choices));

        var result = selected
            .Select(sel => ExtractAppFromChoice(sel, appPool))
            .ToList();

        if (result.Count > 0)
        {
            ShowSelectionConfirmation(result.Count);
        }

        return result;
    }

    private static string FormatAppChoice(AppInfo app)
    {
        var typeIcon = app.Type == InstallType.Winget ? "W" : "D";
        var modeIcon = app.Mode == InstallMode.Silent ? "S" : "I";
        var safeName = app.Name.Replace("[", "[[").Replace("]", "]]");
        return $"{safeName,-35} {typeIcon} {modeIcon}";
    }

    private static AppInfo ExtractAppFromChoice(string choice, List<AppInfo> appPool)
    {
        var cleanChoice = choice.Replace("[[", "[").Replace("]]", "]");
        var appName = cleanChoice.Substring(0, cleanChoice.LastIndexOf(' ', cleanChoice.LastIndexOf(' ') - 1)).Trim();
        return appPool.First(a => a.Name == appName);
    }

    private static void ShowSelectionConfirmation(int count)
    {
        AnsiConsole.WriteLine();
        var infoPanel = new Panel($"[{UIConstants.ColorGreen}]{count}[/] uygulama seçildi")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);
        AnsiConsole.Write(infoPanel);
        
        AnsiConsole.WriteLine();
        ShowPressKeyMessage(UIConstants.PressKeyToContinue);
        Console.ReadKey(true);
    }

    public static void ShowNoAppSelectedMessage()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"\n[{UIConstants.ColorRed}]X Hiçbir uygulama seçilmedi[/]\n");
    }
    
    #endregion

    #region Installation Display
    
    public static void ShowInstallationTable(List<AppInfo> apps)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn($"[{UIConstants.ColorCyan}]#[/]").Centered())
            .AddColumn(new TableColumn($"[{UIConstants.ColorCyan}]Uygulama[/]"))
            .AddColumn(new TableColumn($"[{UIConstants.ColorCyan}]Tip[/]").Centered())
            .AddColumn(new TableColumn($"[{UIConstants.ColorCyan}]Mod[/]").Centered())
            .AddColumn(new TableColumn($"[{UIConstants.ColorCyan}]Durum[/]").Centered());

        for (int i = 0; i < apps.Count; i++)
        {
            var app = apps[i];
            var typeStr = app.Type == InstallType.Winget ? $"[{UIConstants.ColorBlue}]W[/]" : $"[{UIConstants.ColorPurple}]D[/]";
            var modeStr = app.Mode == InstallMode.Silent ? $"[{UIConstants.ColorGrey}]S[/]" : $"[{UIConstants.ColorYellow}]I[/]";
            table.AddRow($"[dim]{i + 1}[/]", $"[{UIConstants.ColorWhite}]{app.Name.EscapeMarkup()}[/]", typeStr, modeStr, "[dim]...[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public static void ShowInstallationResult(AppInfo app, InstallStatus status, string? errorMessage = null)
    {
        var (statusText, message) = GetStatusDisplay(status, errorMessage);
        AnsiConsole.MarkupLine($"  {statusText} [dim]{app.Name.EscapeMarkup()}[/] - {message}");
    }

    private static (string StatusText, string Message) GetStatusDisplay(InstallStatus status, string? errorMessage)
    {
        return status switch
        {
            InstallStatus.Success => ($"[{UIConstants.ColorGreen}]OK[/]", $"[{UIConstants.ColorGreen}]Tamamlandı[/]"),
            InstallStatus.AlreadyInstalled => ($"[{UIConstants.ColorYellow}]SKIP[/]", $"[{UIConstants.ColorYellow}]Zaten kurulu[/]"),
            InstallStatus.Failed => FormatFailedStatus(errorMessage),
            _ => ("[grey]?[/]", "[grey]Bilinmiyor[/]")
        };
    }

    private static (string StatusText, string Message) FormatFailedStatus(string? errorMessage)
    {
        var errorMsg = errorMessage != null ? $": {errorMessage.EscapeMarkup()}" : "";
        return ($"[{UIConstants.ColorRed}]FAIL[/]", $"[{UIConstants.ColorRed}]Hata{errorMsg}[/]");
    }

    public static void ShowInstallationError(AppInfo app, string errorMessage)
    {
        AnsiConsole.MarkupLine($"  [{UIConstants.ColorRed}]FAIL[/] [dim]{app.Name.EscapeMarkup()}[/] - [{UIConstants.ColorRed}]{errorMessage.EscapeMarkup()}[/]");
    }
    
    #endregion

    #region Helper Methods
    
    private static void ShowPressKeyMessage(string message)
    {
        AnsiConsole.Markup($"[dim]{message}[/]");
    }
    
    #endregion
}
