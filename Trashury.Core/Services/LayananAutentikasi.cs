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

    public void UbahPengguna(int id, string namaLengkap, string peran, string? passwordBaru = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namaLengkap);
        var user = _db.Pengguna.Find(id)
            ?? throw new KeyNotFoundException($"Pengguna dengan Id {id} tidak ditemukan.");

        string peranNormal = string.Equals(peran, Pengguna.PeranPengurus, StringComparison.OrdinalIgnoreCase)
            ? Pengguna.PeranPengurus
            : Pengguna.PeranOperator;

        if (user.IsPengurus && peranNormal != Pengguna.PeranPengurus &&
            _db.Pengguna.Count(p => p.Peran == Pengguna.PeranPengurus) <= 1)
        {
            throw new InvalidOperationException("Tidak dapat mengubah peran pengurus terakhir menjadi operator.");
        }

        user.NamaLengkap = namaLengkap.Trim();
        user.Peran = peranNormal;

        if (!string.IsNullOrWhiteSpace(passwordBaru))
        {
            if (passwordBaru.Length < 6)
                throw new ArgumentException("Password baru minimal 6 karakter.", nameof(passwordBaru));
            user.PasswordHash = HashPassword(passwordBaru);
        }

        _db.SaveChanges();
    }

    public void HapusPengguna(int id, int? idPenggunaAktif = null)
    {
        if (idPenggunaAktif.HasValue && id == idPenggunaAktif.Value)
            throw new InvalidOperationException("Tidak dapat menghapus akun yang sedang digunakan untuk login.");

        var user = _db.Pengguna.Find(id)
            ?? throw new KeyNotFoundException($"Pengguna dengan Id {id} tidak ditemukan.");

        if (user.IsPengurus && _db.Pengguna.Count(p => p.Peran == Pengguna.PeranPengurus) <= 1)
            throw new InvalidOperationException("Minimal harus tersisa satu akun Pengurus.");

        _db.Pengguna.Remove(user);
        _db.SaveChanges();
    }

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
