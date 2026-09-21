using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class KalkulatorCO2eTests
{
    [Fact]
    public void Menjumlahkan_emisi_yang_dihindari_dari_seluruh_rincian()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var kategoriRepo = new KategoriSampahRepository(db);

        var setoran = new SetoranSampah(new List<DetailSetoran>
        {
            new(kategoriRepo.GetByNama("Plastik PET"), 2m),       // 2 x 1,5 = 3,0
            new(kategoriRepo.GetByNama("Kaleng Aluminium"), 0.5m) // 0,5 x 8,1 = 4,05
        });

        Assert.Equal(7.05m, new KalkulatorCO2e().HitungCO2e(setoran));
    }

    [Fact]
    public void Berat_negatif_ditolak()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var kaca = new KategoriSampahRepository(db).GetByNama("Kaca");

        Assert.Throws<ArgumentOutOfRangeException>(() => kaca.HitungCO2e(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => kaca.HitungNilai(-1m));
    }
}
