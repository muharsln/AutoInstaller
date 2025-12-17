using System.Diagnostics;
using AutoInstaller.Constants;
using AutoInstaller.Models;
using Spectre.Console;

namespace AutoInstaller.Services;

public class DirectDownloadInstaller : IAppInstaller
{
    private readonly HttpClient _httpClient;

    public DirectDownloadInstaller()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(AppConstants.DownloadTimeoutMinutes)
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<InstallResult> InstallAsync(AppInfo app)
    {
        var downloadResult = await DownloadFileAsync(app);
        if (!downloadResult.Success)
        {
            return downloadResult;
        }

        var installResult = await InstallDownloadedFileAsync(app, downloadResult.FilePath!);
        
        CleanupTempFile(downloadResult.FilePath!);
        
        return installResult;
    }

    private async Task<DownloadResult> DownloadFileAsync(AppInfo app)
    {
        string? tempPath = null;
        
        try
        {
            var response = await _httpClient.GetAsync(app.PackageId, HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"HTTP {(int)response.StatusCode} ({response.StatusCode})";
                AnsiConsole.MarkupLine($"  [red]?ndirme hatas?: {errorMsg}[/]");
                return new DownloadResult(false, null, errorMsg);
            }
            
            var extension = DetermineFileExtension(response, app.PackageId);
            tempPath = Path.Combine(Path.GetTempPath(), $"{app.Name.Replace(" ", "")}_{Guid.NewGuid()}{extension}");
            
            // Büyük dosya için özel indirme göstergesi
            var fileSize = response.Content.Headers.ContentLength ?? 0;
            if (fileSize > 100 * 1024 * 1024) // 100MB'den büyükse
            {
                AnsiConsole.MarkupLine($"  [yellow]Büyük dosya ({fileSize / 1024 / 1024} MB) - ?ndirme biraz sürebilir...[/]");
            }
            
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
                await fs.FlushAsync();
            }
            
            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                return new DownloadResult(false, null, "?ndirilen dosya geçersiz");
            }
            
            await Task.Delay(AppConstants.FileSystemSyncDelayMs);
            
            return new DownloadResult(true, tempPath, null);
        }
        catch (HttpRequestException ex)
        {
            CleanupTempFileIfExists(tempPath);
            AnsiConsole.MarkupLine($"  [red]?ndirme hatas?: {ex.Message.EscapeMarkup()}[/]");
            return new DownloadResult(false, null, ex.Message);
        }
        catch (TaskCanceledException)
        {
            CleanupTempFileIfExists(tempPath);
            var timeoutMsg = $"?ndirme zaman a??m?na u?rad? ({AppConstants.DownloadTimeoutMinutes} dakika)";
            AnsiConsole.MarkupLine($"  [red]{timeoutMsg}[/]");
            return new DownloadResult(false, null, timeoutMsg);
        }
        catch (Exception ex)
        {
            CleanupTempFileIfExists(tempPath);
            AnsiConsole.MarkupLine($"  [red]Beklenmeyen hata: {ex.Message.EscapeMarkup()}[/]");
            return new DownloadResult(false, null, ex.Message);
        }
    }

    private static string DetermineFileExtension(HttpResponseMessage response, string originalUrl)
    {
        if (response.Content.Headers.ContentDisposition?.FileName != null)
        {
            var fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            var ext = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(ext))
                return ext;
        }
        
        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? originalUrl;
        if (finalUrl.Contains(".exe", StringComparison.OrdinalIgnoreCase))
            return ".exe";
        if (finalUrl.Contains(".msi", StringComparison.OrdinalIgnoreCase))
            return ".msi";
        if (finalUrl.Contains(".msix", StringComparison.OrdinalIgnoreCase))
            return ".msix";
        
        if (originalUrl.EndsWith(".msix", StringComparison.OrdinalIgnoreCase))
            return ".msix";
        if (originalUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            return ".msi";
        
        return ".exe";
    }

    private static async Task<InstallResult> InstallDownloadedFileAsync(AppInfo app, string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // MSIX dosyalar? için özel kurulum
            if (extension == ".msix")
            {
                return await InstallMsixPackageAsync(filePath);
            }
            
            // Normal EXE/MSI kurulumu
            return await InstallExecutableAsync(app, filePath);
        }
        catch (Exception ex)
        {
            return InstallResult.CreateFailed($"Kurulum hatas?: {ex.Message}");
        }
    }

    private static async Task<InstallResult> InstallMsixPackageAsync(string msixPath)
    {
        try
        {
            // PowerShell Add-AppxPackage komutu ile kur
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Add-AppxPackage -Path '{msixPath}'\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return InstallResult.CreateFailed("PowerShell process ba?lat?lamad?");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0)
            {
                return InstallResult.CreateSuccess();
            }
            else
            {
                var errorMsg = !string.IsNullOrWhiteSpace(error) ? error : "MSIX kurulum ba?ar?s?z";
                
                // Sertifika hatas? kontrolü
                if (errorMsg.Contains("0x800B0109") || errorMsg.Contains("certificate"))
                {
                    AnsiConsole.MarkupLine("[yellow]?pucu: MSIX paketi imzalanmam?? veya sertifika güvenilir de?il[/]");
                    AnsiConsole.MarkupLine("[yellow]Developer Mode'u aç?n veya dosyay? manuel olarak kurun[/]");
                }
                
                return InstallResult.CreateFailed(errorMsg);
            }
        }
        catch (Exception ex)
        {
            return InstallResult.CreateFailed($"MSIX kurulum hatas?: {ex.Message}");
        }
    }

    private static async Task<InstallResult> InstallExecutableAsync(AppInfo app, string filePath)
    {
        var installArgs = app.Mode == InstallMode.Silent ? (app.SilentArgs ?? "") : "";
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = installArgs,
                UseShellExecute = true,
                CreateNoWindow = app.Mode == InstallMode.Silent
            }
        };
        
        process.Start();
        await Task.Run(() => process.WaitForExit());

        return process.ExitCode == 0 
            ? InstallResult.CreateSuccess() 
            : InstallResult.CreateAlreadyInstalled();
    }

    private static void CleanupTempFileIfExists(string? filePath)
    {
        if (filePath != null && File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Silme ba?ar?s?z olsa da devam et
            }
        }
    }

    private static void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Thread.Sleep(AppConstants.ProcessCleanupDelayMs);
                File.Delete(filePath);
            }
        }
        catch
        {
            // Silme ba?ar?s?z olsa da devam et
        }
    }

    private record DownloadResult(bool Success, string? FilePath, string? ErrorMessage)
    {
        public static implicit operator InstallResult(DownloadResult download)
        {
            return InstallResult.CreateFailed(download.ErrorMessage);
        }
    }
}
