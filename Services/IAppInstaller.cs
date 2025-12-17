using AutoInstaller.Models;

namespace AutoInstaller.Services;

public interface IAppInstaller
{
    Task<InstallResult> InstallAsync(AppInfo app);
}

public record InstallResult(bool Success, InstallStatus Status, string? ErrorMessage = null)
{
    public static InstallResult CreateSuccess() 
        => new(true, InstallStatus.Success);
    
    public static InstallResult CreateAlreadyInstalled() 
        => new(true, InstallStatus.AlreadyInstalled);
    
    public static InstallResult CreateFailed(string? errorMessage = null) 
        => new(false, InstallStatus.Failed, errorMessage);
    
    public string GetStatusText() => Status switch
    {
        InstallStatus.Success => "OK",
        InstallStatus.AlreadyInstalled => "SKIP",
        InstallStatus.Failed => "FAIL",
        _ => "UNKNOWN"
    };
}
