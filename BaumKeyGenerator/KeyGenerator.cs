using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace BaumKeyGenerator;

public enum KeyType
{
    GeneralHex32,
    GeneralBase64,
    VaultwardenToken,
    DatabasePassword,
    JwtSecret,
    AlphanumericKey,
}

internal static class KeyGenerator
{
    // ── General ────────────────────────────────────────────────────────────────

    /// <summary>32 random bytes as lowercase hex (matches `openssl rand -hex 32`).</summary>
    public static string GeneralHex32()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>32 random bytes as URL-safe base64 (no padding).</summary>
    public static string GeneralBase64()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>48 random bytes as hex — common for JWT HS512 secrets.</summary>
    public static string JwtSecret()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Random alphanumeric string of <paramref name="length"/> chars.</summary>
    public static string Alphanumeric(int length = 40)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder sb = new(length);
        byte[] buf = RandomNumberGenerator.GetBytes(length * 2);
        for (int i = 0; i < length; i++)
            sb.Append(chars[buf[i * 2] % chars.Length]);
        return sb.ToString();
    }

    /// <summary>
    /// Secure random password suitable for databases:
    /// 24 chars mixing upper, lower, digits, safe symbols.
    /// </summary>
    public static string DatabasePassword()
    {
        const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#%^&*-_=+";
        int len = 24;
        StringBuilder sb = new(len);
        byte[] buf = RandomNumberGenerator.GetBytes(len * 2);
        for (int i = 0; i < len; i++)
            sb.Append(pool[buf[i * 2] % pool.Length]);
        return sb.ToString();
    }

    // ── Vaultwarden ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a random password and its Argon2id PHC hash for use as
    /// Vaultwarden's ADMIN_TOKEN, matching Vaultwarden's --preset owasp defaults:
    ///   m=65540, t=3, p=4, tagLength=32.
    ///
    /// Returns (plainPassword, phcString).
    /// Set ADMIN_TOKEN=phcString in your .env, log in with plainPassword.
    /// </summary>
    public static (string password, string phcString) VaultwardenToken(string? password = null)
    {
        password ??= Alphanumeric(32);

        byte[] salt   = RandomNumberGenerator.GetBytes(32);
        byte[] pwd    = Encoding.UTF8.GetBytes(password);

        using var argon2 = new Argon2id(pwd)
        {
            Salt            = salt,
            MemorySize      = 65540,    // kilobytes  (~64 MB, Vaultwarden owasp preset)
            Iterations      = 3,
            DegreeOfParallelism = 4,
        };

        byte[] hash = argon2.GetBytes(32);

        // PHC format: $argon2id$v=19$m=65540,t=3,p=4$<salt_b64>$<hash_b64>
        string saltB64 = ToBase64NoPad(salt);
        string hashB64 = ToBase64NoPad(hash);
        string phc     = $"$argon2id$v=19$m=65540,t=3,p=4${saltB64}${hashB64}";

        return (password, phc);
    }

    private static string ToBase64NoPad(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=');

    // ── Dispatch ───────────────────────────────────────────────────────────────

    public static string DisplayName(KeyType t) => t switch
    {
        KeyType.GeneralHex32     => "General Secret (hex-32)",
        KeyType.GeneralBase64    => "General Secret (base64)",
        KeyType.VaultwardenToken => "Vaultwarden Admin Token",
        KeyType.DatabasePassword => "Database Password",
        KeyType.JwtSecret        => "JWT Secret (hex-48)",
        KeyType.AlphanumericKey  => "Alphanumeric Key",
        _                        => t.ToString(),
    };
}
