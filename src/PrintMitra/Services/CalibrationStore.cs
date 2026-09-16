using PrintMitra.Models;
using System.Text.Json;

namespace PrintMitra.Services;

public sealed class CalibrationStore
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrintMitra", "calibration.json");

    public async Task<CalibrationProfile> GetAsync(string printer)
    {
        var all = await ReadAsync();
        return all.TryGetValue(printer, out var profile) ? profile : CalibrationProfile.Default(printer);
    }

    public async Task SaveAsync(CalibrationProfile profile)
    {
        var all = await ReadAsync(); all[profile.PrinterName] = profile;
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<Dictionary<string, CalibrationProfile>> ReadAsync()
    {
        try
        {
            if (!File.Exists(_path)) return new(StringComparer.OrdinalIgnoreCase);
            return JsonSerializer.Deserialize<Dictionary<string, CalibrationProfile>>(await File.ReadAllTextAsync(_path))
                   ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch { return new(StringComparer.OrdinalIgnoreCase); }
    }
}
