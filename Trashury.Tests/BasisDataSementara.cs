using Microsoft.EntityFrameworkCore;
using Trashury.Data;

namespace Trashury.Tests;

/// <summary>
/// Berkas SQLite sungguhan di folder sementara. Sengaja tidak memakai
/// penyedia in-memory agar yang diuji benar-benar perilaku SQLite, termasuk
/// apakah data masih ada setelah koneksi ditutup.
/// </summary>
public sealed class BasisDataSementara : IDisposable
{
    private readonly string _folder;

    public string Path { get; }

    public BasisDataSementara()
    {
        _folder = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "trashury-test-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_folder);
        Path = System.IO.Path.Combine(_folder, "trashury.db");

        using var db = BukaSesi();
        db.Database.Migrate();
    }

    /// <summary>
    /// Membuka sesi baru ke berkas yang sama, meniru aplikasi yang ditutup
    /// lalu dibuka kembali.
    /// </summary>
    public TrashuryDbContext BukaSesi() => TrashuryDbContextFactory.Buat(Path);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Berkas sementara, aman diabaikan bila masih terkunci.
        }
    }
}
