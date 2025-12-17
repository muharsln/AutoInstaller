using AutoInstaller;

// Uygulamayı çalıştır
// (Encoding ayarları AppInstallationService içinde yapılıyor)
var service = new AppInstallationService();
await service.RunAsync();
