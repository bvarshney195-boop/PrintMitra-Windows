using System.Security.Cryptography;

namespace PrintMitra.Services;

public sealed class PrivacyWorkspace : IDisposable
{
    public string DirectoryPath { get; }

    public PrivacyWorkspace()
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "PrintMitra", Convert.ToHexString(RandomNumberGenerator.GetBytes(12)));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string NewFile(string extension) => Path.Combine(DirectoryPath, $"{Guid.NewGuid():N}{extension}");

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
        catch { /* Windows may briefly retain print spooler handles; cleanup retries on next start. */ }
    }

    public static void CleanupStale()
    {
        var root = Path.Combine(Path.GetTempPath(), "PrintMitra");
        if (!Directory.Exists(root)) return;
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            try
            {
                if (Directory.GetCreationTimeUtc(dir) < DateTime.UtcNow.AddHours(-12)) Directory.Delete(dir, true);
            }
            catch { }
        }
    }
}
