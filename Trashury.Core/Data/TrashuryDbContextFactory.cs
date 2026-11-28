using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Trashury.Data;

/// <summary>
/// Membuat <see cref="TrashuryDbContext"/> untuk pemakaian biasa maupun untuk
/// perkakas <c>dotnet ef</c> saat membuat migrasi.
/// </summary>
/// <remarks>
/// Penyedia dipilih dari variabel lingkungan <see cref="VariabelKoneksiCloud"/>:
/// bila terisi, data disimpan di PostgreSQL cloud (Supabase); bila kosong,
/// data disimpan di berkas SQLite lokal sehingga aplikasi tetap bisa offline.
/// </remarks>
public class TrashuryDbContextFactory : IDesignTimeDbContextFactory<TrashuryDbContext>
{
    /// <summary>
    /// Nama variabel lingkungan berisi connection string PostgreSQL cloud.
    /// Menerima format URI Supabase (<c>postgresql://user:pass@host:5432/db</c>)
    /// maupun format key=value Npgsql.
    /// </summary>
    public const string VariabelKoneksiCloud = "TRASHURY_DB_URL";

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

    /// <summary>Connection string cloud dari lingkungan, atau null bila tidak diatur.</summary>
    public static string? KoneksiCloudDariLingkungan()
    {
        var nilai = Environment.GetEnvironmentVariable(VariabelKoneksiCloud);
        return string.IsNullOrWhiteSpace(nilai) ? null : nilai.Trim();
    }

    /// <summary>
    /// Membuka basis data sesuai konfigurasi: cloud bila
    /// <see cref="VariabelKoneksiCloud"/> terisi, selain itu SQLite lokal.
    /// </summary>
    public static TrashuryDbContext BuatSesuaiKonfigurasi()
    {
        var koneksiCloud = KoneksiCloudDariLingkungan();
        return koneksiCloud is null ? Buat() : BuatCloud(koneksiCloud);
    }

    /// <summary>Membuka berkas SQLite lokal.</summary>
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

    /// <summary>Membuka basis data PostgreSQL di cloud.</summary>
    public static TrashuryDbContext BuatCloud(string koneksi)
    {
        var options = new DbContextOptionsBuilder<TrashuryPostgresDbContext>()
            .UseNpgsql(NormalisasiKoneksiPostgres(koneksi))
            .Options;

        return new TrashuryPostgresDbContext(options);
    }

    /// <summary>
    /// Mengubah URI <c>postgresql://</c> (format yang disalin dari dasbor
    /// Supabase) menjadi format key=value yang dipahami Npgsql. Koneksi ke
    /// cloud selalu dienkripsi; SSL hanya dilepas untuk server lokal.
    /// </summary>
    public static string NormalisasiKoneksiPostgres(string koneksi)
    {
        if (string.IsNullOrWhiteSpace(koneksi))
        {
            throw new ArgumentException("Connection string PostgreSQL kosong.", nameof(koneksi));
        }

        koneksi = koneksi.Trim();
        NpgsqlConnectionStringBuilder builder;

        if (koneksi.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || koneksi.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(koneksi);
            var info = uri.UserInfo.Split(':', 2);

            builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')) is { Length: > 0 } db ? db : "postgres",
                Username = Uri.UnescapeDataString(info[0]),
                Password = info.Length > 1 ? Uri.UnescapeDataString(info[1]) : null,
            };

            foreach (var pasangan in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var bagian = pasangan.Split('=', 2);
                if (bagian[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) && bagian.Length == 2)
                {
                    builder.SslMode = Enum.Parse<SslMode>(Uri.UnescapeDataString(bagian[1]), ignoreCase: true);
                }
            }
        }
        else
        {
            builder = new NpgsqlConnectionStringBuilder(koneksi);
        }

        var lokal = builder.Host is "localhost" or "127.0.0.1" or "::1";
        if (!lokal && builder.SslMode is SslMode.Disable or SslMode.Allow or SslMode.Prefer)
        {
            builder.SslMode = SslMode.Require;
        }

        return builder.ConnectionString;
    }

    public TrashuryDbContext CreateDbContext(string[] args) => Buat();
}

/// <summary>
/// Dipakai <c>dotnet ef migrations add ... --context TrashuryPostgresDbContext</c>.
/// Membuat migrasi tidak perlu terhubung ke server, jadi connection string
/// cadangan di bawah hanya pengisi bila variabel lingkungan tidak diatur.
/// </summary>
public class TrashuryPostgresDbContextFactory : IDesignTimeDbContextFactory<TrashuryPostgresDbContext>
{
    public TrashuryPostgresDbContext CreateDbContext(string[] args)
    {
        var koneksi = TrashuryDbContextFactory.KoneksiCloudDariLingkungan()
            ?? "Host=localhost;Database=trashury;Username=postgres";

        var options = new DbContextOptionsBuilder<TrashuryPostgresDbContext>()
            .UseNpgsql(TrashuryDbContextFactory.NormalisasiKoneksiPostgres(koneksi))
            .Options;

        return new TrashuryPostgresDbContext(options);
    }
}
