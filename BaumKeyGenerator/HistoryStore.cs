using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BaumKeyGenerator;

/// <summary>
/// Persists history to %APPDATA%\BaumKeyGenerator\history.dat
/// encrypted with Windows DPAPI (CurrentUser scope — unreadable by other users).
/// </summary>
internal static class HistoryStore
{
    private static readonly string _dir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BaumKeyGenerator");

    private static readonly string _file = Path.Combine(_dir, "history.dat");

    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
    };

    public static List<HistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(_file)) return [];

            byte[] cipher  = File.ReadAllBytes(_file);
            byte[] plain   = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            string json    = Encoding.UTF8.GetString(plain);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json, _json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Save(List<HistoryEntry> entries)
    {
        Directory.CreateDirectory(_dir);
        string json   = JsonSerializer.Serialize(entries, _json);
        byte[] plain  = Encoding.UTF8.GetBytes(json);
        byte[] cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_file, cipher);
    }

    public static void Append(HistoryEntry entry)
    {
        var entries = Load();
        entries.Insert(0, entry);   // newest first
        Save(entries);
    }

    public static void Clear()
    {
        if (File.Exists(_file))
            File.Delete(_file);
    }
}
