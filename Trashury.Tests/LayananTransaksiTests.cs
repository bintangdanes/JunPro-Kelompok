using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class LayananTransaksiTests
{
    private static (NasabahRepository, KategoriSampahRepository, LayananTransaksi, int)
        Siapkan(Trashury.Data.TrashuryDbContext db, string nama = "Uji Coba")
    {
        var nasabahRepo = new NasabahRepository(db);
        var kategoriRepo = new KategoriSampahRepository(db);

        var nasabah = new Nasabah(nama);
        nasabahRepo.Tambah(nasabah);
        nasabahRepo.SimpanPerubahan();

        var layanan = new LayananTransaksi(nasabahRepo, new TransaksiRepository(db));
        return (nasabahRepo, kategoriRepo, layanan, nasabah.Id);
    }

    [Fact]
    public void Penarikan_melebihi_saldo_ditolak_dan_tidak_meninggalkan_transaksi()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var (nasabahRepo, _, layanan, nasabahId) = Siapkan(db);

        Assert.Throws<InvalidOperationException>(
            () => layanan.ProsesPenarikan(nasabahId, 50_000m));

        Assert.Equal(0m, nasabahRepo.GetById(nasabahId).Saldo);
        Assert.Empty(new TransaksiRepository(db).GetByNasabah(nasabahId));
    }

    [Fact]
    public void Setoran_tanpa_rincian_ditolak()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var (_, _, layanan, nasabahId) = Siapkan(db);

        Assert.Throws<ArgumentException>(
            () => layanan.CatatSetoran(nasabahId, new List<DetailSetoran>()));
    }

    [Fact]
    public void Setoran_untuk_nasabah_yang_tidak_terdaftar_ditolak()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var kategoriRepo = new KategoriSampahRepository(db);
        var layanan = new LayananTransaksi(new NasabahRepository(db), new TransaksiRepository(db));

        Assert.Throws<KeyNotFoundException>(() => layanan.CatatSetoran(
            9999,
            new List<DetailSetoran> { new(kategoriRepo.GetByNama("Kaca"), 1m) }));
    }

    [Fact]
    public void Saldo_hanya_berubah_lewat_transaksi_bukan_lewat_property()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var (nasabahRepo, kategoriRepo, layanan, nasabahId) = Siapkan(db);

        // Saldo tidak punya setter publik; property Saldo hanya dapat dibaca.
        Assert.Null(typeof(Nasabah).GetProperty(nameof(Nasabah.Saldo))!.SetMethod);

        layanan.CatatSetoran(nasabahId, new List<DetailSetoran>
        {
            new(kategoriRepo.GetByNama("Organik"), 4m)   // 4 x 300 = 1.200
        });

        Assert.Equal(1_200m, nasabahRepo.GetById(nasabahId).Saldo);
    }
}
