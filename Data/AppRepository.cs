using AutoInstaller.Models;

namespace AutoInstaller.Data;

public static class AppRepository
{
    public static List<AppInfo> GetAllApps()
    {
        return new List<AppInfo>
        {
            // Interactive (kurulum ekranı gösterilir)
            new("Google Chrome", "Google.Chrome", InstallType.Winget, InstallMode.Interactive),
            new("ONLYOFFICE Desktop", "ONLYOFFICE.DesktopEditors", InstallType.Winget, InstallMode.Interactive),
            new("OBS Studio", "OBSProject.OBSStudio", InstallType.Winget, InstallMode.Silent),
            
            // Silent (arka planda sessiz kurulum)
            new("7-Zip", "7zip.7zip", InstallType.Winget, InstallMode.Silent),
            new("AnyDesk", "AnyDesk.AnyDesk", InstallType.Winget, InstallMode.Silent),
            new("Telegram", "Telegram.TelegramDesktop", InstallType.Winget, InstallMode.Silent),
            new("WhatsApp", "9NKSQGP7F2NH", InstallType.Winget, InstallMode.Silent),
            new("Synology Drive Client", "Synology.DriveClient", InstallType.Winget, InstallMode.Silent),
            new("Handbrake", "HandBrake.HandBrake", InstallType.Winget, InstallMode.Silent),
            new(".Net Runtime 10", "Microsoft.DotNet.Runtime.10", InstallType.Winget, InstallMode.Silent),
            new("Veyon", "VeyonSolutions.Veyon", InstallType.Winget, InstallMode.Interactive),

            // Direct Download - Interactive
            new("Affinity", "https://downloads.affinity.studio/Affinity%20x64.msix", InstallType.DirectDownload, InstallMode.Interactive),
            new("Nvidia App", "https://tr.download.nvidia.com/nvapp/client/11.0.5.420/NVIDIA_app_v11.0.5.420.exe", InstallType.DirectDownload, InstallMode.Interactive),
            new("Intel Driver Support Asistant", "https://dsadata.intel.com/installer/weberror", InstallType.DirectDownload, InstallMode.Interactive),
        };
    }
}
