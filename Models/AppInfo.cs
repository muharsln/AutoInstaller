namespace AutoInstaller.Models;

public record AppInfo(
    string Name, 
    string PackageId, 
    InstallType Type, 
    InstallMode Mode, 
    string? SilentArgs = null)
{
    public string Name { get; init; } = Name;
    public string PackageId { get; init; } = PackageId;
    public InstallType Type { get; init; } = Type;
    public InstallMode Mode { get; init; } = Mode;
    public string? SilentArgs { get; init; } = SilentArgs;
}

public enum InstallType
{
    Winget,
    DirectDownload
}

public enum InstallMode
{
    Interactive,
    Silent
}

public enum InstallStatus
{
    Success,
    AlreadyInstalled,
    Failed
}
