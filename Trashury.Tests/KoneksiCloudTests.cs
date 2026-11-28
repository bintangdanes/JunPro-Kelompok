using Npgsql;
using Trashury.Data;
using Xunit;

namespace Trashury.Tests;

public class KoneksiCloudTests
{
    [Fact]
    public void Uri_supabase_diubah_ke_format_npgsql_dengan_ssl_wajib()
    {
        var hasil = new NpgsqlConnectionStringBuilder(TrashuryDbContextFactory.NormalisasiKoneksiPostgres(
            "postgresql://postgres.abcd:p%40ss%3Aword@aws-0-ap-southeast-1.pooler.supabase.com:5432/postgres"));

        Assert.Equal("aws-0-ap-southeast-1.pooler.supabase.com", hasil.Host);
        Assert.Equal(5432, hasil.Port);
        Assert.Equal("postgres", hasil.Database);
        Assert.Equal("postgres.abcd", hasil.Username);
        Assert.Equal("p@ss:word", hasil.Password);
        Assert.Equal(SslMode.Require, hasil.SslMode);
    }

    [Fact]
    public void Format_key_value_tetap_diterima_dan_ssl_dipaksa_untuk_server_cloud()
    {
        var hasil = new NpgsqlConnectionStringBuilder(TrashuryDbContextFactory.NormalisasiKoneksiPostgres(
            "Host=db.contoh.supabase.co;Database=postgres;Username=postgres;Password=rahasia"));

        Assert.Equal("db.contoh.supabase.co", hasil.Host);
        Assert.Equal(SslMode.Require, hasil.SslMode);
    }

    [Fact]
    public void Server_lokal_tidak_dipaksa_memakai_ssl()
    {
        var hasil = new NpgsqlConnectionStringBuilder(TrashuryDbContextFactory.NormalisasiKoneksiPostgres(
            "postgresql://postgres@localhost:55432/trashury"));

        Assert.Equal(55432, hasil.Port);
        Assert.Equal("trashury", hasil.Database);
        Assert.NotEqual(SslMode.Require, hasil.SslMode);
    }

    [Fact]
    public void Tanpa_variabel_lingkungan_aplikasi_memakai_sqlite_lokal()
    {
        var sebelumnya = Environment.GetEnvironmentVariable(TrashuryDbContextFactory.VariabelKoneksiCloud);
        try
        {
            Environment.SetEnvironmentVariable(TrashuryDbContextFactory.VariabelKoneksiCloud, null);
            using var db = TrashuryDbContextFactory.BuatSesuaiKonfigurasi();

            Assert.IsNotType<TrashuryPostgresDbContext>(db);
            Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TrashuryDbContextFactory.VariabelKoneksiCloud, sebelumnya);
        }
    }
}
