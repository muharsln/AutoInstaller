using AutoInstaller.Models;
using AutoInstaller.Services;

namespace AutoInstaller.Factories;

public class InstallerFactory
{
    private readonly WingetInstaller _wingetInstaller;
    private readonly DirectDownloadInstaller _directDownloadInstaller;

    public InstallerFactory()
    {
        _wingetInstaller = new WingetInstaller();
        _directDownloadInstaller = new DirectDownloadInstaller();
    }

    public IAppInstaller GetInstaller(InstallType type)
    {
        return type switch
        {
            InstallType.Winget => _wingetInstaller,
            InstallType.DirectDownload => _directDownloadInstaller,
            _ => throw new ArgumentException($"Unsupported install type: {type}", nameof(type))
        };
    }
}
