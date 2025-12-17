using AutoInstaller.Constants;
using AutoInstaller.Models;
using System.Diagnostics;

namespace AutoInstaller.Services;

public class WingetInstaller : IAppInstaller
{
    public async Task<InstallResult> InstallAsync(AppInfo app)
    {
        try
        {
            var arguments = BuildWingetArguments(app);
            var exitCode = await ExecuteWingetAsync(arguments);
            
            return MapExitCodeToResult(exitCode);
        }
        catch (Exception ex)
        {
            return InstallResult.CreateFailed($"Winget hatas?: {ex.Message}");
        }
    }

    private static string BuildWingetArguments(AppInfo app)
    {
        var baseArgs = $"install --id {app.PackageId} " +
                      "--accept-source-agreements " +
                      "--accept-package-agreements " +
                      "--exact";
        
        var modeArg = app.Mode == InstallMode.Silent ? " --silent" : " --interactive";
        
        return baseArgs + modeArg;
    }

    private static async Task<int> ExecuteWingetAsync(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };
        
        process.Start();
        await Task.Run(() => process.WaitForExit());
        
        return process.ExitCode;
    }

    private static InstallResult MapExitCodeToResult(int exitCode)
    {
        return exitCode switch
        {
            WingetExitCodes.Success => InstallResult.CreateSuccess(),
            WingetExitCodes.AlreadyInstalled1 or WingetExitCodes.AlreadyInstalled2 => InstallResult.CreateAlreadyInstalled(),
            _ => InstallResult.CreateFailed($"Exit code: {exitCode}")
        };
    }
}
