using System.Security.Cryptography;
using Trashury.Data;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Layanan autentikasi pengguna (PBKDF2-SHA256) dan manajemen akun RBAC (Pengurus / Operator).
/// </summary>
public class LayananAutentikasi
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private readonly TrashuryDbContext _db;

    public LayananAutentikasi(TrashuryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Memastikan akun bawaan tersedia pada instalasi baru:
    /// - pengurus / pengurus123 (Peran: Pengurus)
    /// - operator / operator123 (Peran: Operator)
    /// </summary>
    public void PastikanAkunBawaan()
    {
        if (_db.Pengguna.Any())
            return;

        _db.Pengguna.AddRange(
            new Pengguna("pengurus", "Pengurus Bank Sampah", HashPassword("pengurus123"), Pengguna.PeranPengurus),
            new Pengguna("operator", "Operator Meja Setoran", HashPassword("operator123"), Pengguna.PeranOperator));
        _db.SaveChanges();
    }

    public Pengguna? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        string normalized = username.Trim().ToLowerInvariant();
        Pengguna? user = _db.Pengguna.FirstOrDefault(p => p.Username == normalized);
        if (user == null)
            return null;

        return VerifikasiPassword(password, user.PasswordHash) ? user : null;
    }

    public Pengguna TambahPengguna(string username, string namaLengkap, string password, string peran)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < 6)
            throw new ArgumentException("Password minimal 6 karakter.", nameof(password));

        string normalized = username.Trim().ToLowerInvariant();
        if (_db.Pengguna.Any(p => p.Username == normalized))
            throw new InvalidOperationException($"Username '{normalized}' sudah terdaftar.");

        var pengguna = new Pengguna(normalized, namaLengkap, HashPassword(password), peran);
        _db.Pengguna.Add(pengguna);
        _db.SaveChanges();
        return pengguna;
    }

    public IEnumerable<Pengguna> GetSemua() =>
        _db.Pengguna.OrderBy(p => p.Username).ToList();

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifikasiPassword(string password, string storedHash)
    {
        string[] parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expectedHash = Convert.FromBase64String(parts[2]);
        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
