using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Trashury.Data;

/// <summary>
/// Membuat <see cref="TrashuryDbContext"/> untuk pemakaian biasa maupun untuk
/// perkakas <c>dotnet ef</c> saat membuat migrasi.
/// </summary>
public class TrashuryDbContextFactory : IDesignTimeDbContextFactory<TrashuryDbContext>
{
    /// <summary>
    /// Berkas basis data disimpan di folder data aplikasi milik pengguna,
    /// bukan di folder instalasi, agar tidak hilang saat aplikasi diperbarui.
    /// </summary>
    public static string LokasiBasisDataBawaan()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Trashury");

        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "trashury.db");
    }

    public static TrashuryDbContext Buat(string? pathBasisData = null)
    {
        var path = pathBasisData ?? LokasiBasisDataBawaan();

        // SQLite membuat berkas basis data secara otomatis, tetapi tidak
        // membuat foldernya. Tanpa langkah ini, path khusus yang foldernya
        // belum ada akan gagal dibuka.
        var folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var options = new DbContextOptionsBuilder<TrashuryDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        return new TrashuryDbContext(options);
    }

    public TrashuryDbContext CreateDbContext(string[] args) => Buat();
}
