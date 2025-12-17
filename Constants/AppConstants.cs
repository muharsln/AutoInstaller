namespace AutoInstaller.Constants;

public static class WingetExitCodes
{
    public const int Success = 0;
    public const int AlreadyInstalled1 = -1978335189;
    public const int AlreadyInstalled2 = 1978335217;
}

public static class AppConstants
{
    public const int DownloadTimeoutMinutes = 10;
    public const int FileSystemSyncDelayMs = 500;
    public const int ProcessCleanupDelayMs = 1000;
}

public static class UIConstants
{
    // Renkler
    public const string ColorCyan = "cyan";
    public const string ColorGreen = "green";
    public const string ColorYellow = "yellow";
    public const string ColorRed = "red";
    public const string ColorGrey = "grey";
    public const string ColorWhite = "white";
    public const string ColorBlue = "blue";
    public const string ColorPurple = "purple";
    
    // Mesajlar
    public const string PressKeyToContinue = "Devam etmek için bir tuşa basın...";
    public const string PressKeyToExit = "Çıkmak için bir tuşa basın...";
}
