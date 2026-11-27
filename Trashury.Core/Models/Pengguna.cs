namespace Trashury.Models;

/// <summary>
/// Akun pengguna aplikasi untuk autentikasi dan pengendalian akses berbasis peran (RBAC).
/// </summary>
public class Pengguna
{
    public const string PeranPengurus = "Pengurus";
    public const string PeranOperator = "Operator";

    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NamaLengkap { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Peran { get; set; } = PeranOperator;

    public bool IsPengurus => string.Equals(Peran, PeranPengurus, StringComparison.OrdinalIgnoreCase);

    private Pengguna()
    {
    }

    public Pengguna(string username, string namaLengkap, string passwordHash, string peran)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(namaLengkap);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(peran);

        if (!string.Equals(peran, PeranPengurus, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(peran, PeranOperator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Peran tidak valid: {peran}. Gunakan '{PeranPengurus}' atau '{PeranOperator}'.", nameof(peran));
        }

        Username = username.Trim().ToLowerInvariant();
        NamaLengkap = namaLengkap.Trim();
        PasswordHash = passwordHash;
        Peran = string.Equals(peran, PeranPengurus, StringComparison.OrdinalIgnoreCase)
            ? PeranPengurus
            : PeranOperator;
    }
}
