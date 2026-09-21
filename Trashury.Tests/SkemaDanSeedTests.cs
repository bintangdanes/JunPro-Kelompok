using Trashury.Repositories;
using Xunit;

namespace Trashury.Tests;

public class SkemaDanSeedTests
{
    [Fact]
    public void Migrasi_membuat_skema_dan_mengisi_kategori_bawaan()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();

        var kategori = new KategoriSampahRepository(db).GetAll().ToList();

        Assert.Equal(5, kategori.Count);
        Assert.Contains(kategori, k => k.Nama == "Plastik PET");
        Assert.Contains(kategori, k => k.Nama == "Kaleng Aluminium");
    }

    [Fact]
    public void Berkas_basis_data_benar_benar_dibuat_di_disk()
    {
        using var basisData = new BasisDataSementara();

        Assert.True(File.Exists(basisData.Path));
        Assert.True(new FileInfo(basisData.Path).Length > 0);
    }

    [Fact]
    public void Kategori_dapat_dicari_berdasarkan_nama()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();

        var kategori = new KategoriSampahRepository(db).GetByNama("Organik");

        Assert.Equal(300m, kategori.HargaPerKg);
        Assert.Equal(0.25m, kategori.FaktorEmisiCO2e);
    }
}
