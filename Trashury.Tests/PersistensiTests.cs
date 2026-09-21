using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

/// <summary>
/// Pengujian utama penggantian penyimpanan in-memory ke SQLite: data harus
/// tetap ada setelah sesi basis data ditutup dan dibuka kembali.
/// </summary>
public class PersistensiTests
{
    [Fact]
    public void Setoran_dan_saldo_tetap_ada_setelah_sesi_dibuka_ulang()
    {
        using var basisData = new BasisDataSementara();
        int nasabahId;

        // Sesi pertama: catat satu setoran, lalu tutup sesi.
        using (var db = basisData.BukaSesi())
        {
            var nasabahRepo = new NasabahRepository(db);
            var kategoriRepo = new KategoriSampahRepository(db);
            var transaksiRepo = new TransaksiRepository(db);

            var budi = new Nasabah("Budi Santoso");
            nasabahRepo.Tambah(budi);
            nasabahRepo.SimpanPerubahan();
            nasabahId = budi.Id;

            var plastik = kategoriRepo.GetByNama("Plastik PET");
            var layanan = new LayananTransaksi(nasabahRepo, transaksiRepo);

            layanan.CatatSetoran(nasabahId, new List<DetailSetoran>
            {
                new(plastik, 2.5m)
            });
        }

        // Sesi kedua: berkas yang sama, objek DbContext yang sepenuhnya baru.
        using (var db = basisData.BukaSesi())
        {
            var nasabah = new NasabahRepository(db).GetById(nasabahId);
            var riwayat = new TransaksiRepository(db).GetByNasabah(nasabahId).ToList();

            Assert.Equal(10_000m, nasabah.Saldo);     // 2,5 kg x Rp4.000
            Assert.Single(riwayat);

            var setoran = Assert.IsType<SetoranSampah>(riwayat[0]);
            Assert.Equal(2.5m, setoran.TotalBeratKg);
            Assert.Equal(10_000m, setoran.Nominal);
            Assert.Equal(nasabahId, setoran.NasabahId);
        }
    }

    [Fact]
    public void Id_transaksi_diberikan_oleh_basis_data_dan_selalu_unik()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();

        var nasabahRepo = new NasabahRepository(db);
        var kategoriRepo = new KategoriSampahRepository(db);
        var transaksiRepo = new TransaksiRepository(db);

        var nasabah = new Nasabah("Siti Aminah");
        nasabahRepo.Tambah(nasabah);
        nasabahRepo.SimpanPerubahan();

        var kardus = kategoriRepo.GetByNama("Kertas & Kardus");
        var layanan = new LayananTransaksi(nasabahRepo, transaksiRepo);

        var pertama = layanan.CatatSetoran(nasabah.Id, new List<DetailSetoran> { new(kardus, 1m) });
        var kedua = layanan.CatatSetoran(nasabah.Id, new List<DetailSetoran> { new(kardus, 1m) });

        Assert.True(pertama.Id > 0);
        Assert.True(kedua.Id > 0);
        Assert.NotEqual(pertama.Id, kedua.Id);
    }

    [Fact]
    public void Rincian_setoran_ikut_termuat_sehingga_subtotal_dapat_dihitung()
    {
        using var basisData = new BasisDataSementara();
        int nasabahId;

        using (var db = basisData.BukaSesi())
        {
            var nasabahRepo = new NasabahRepository(db);
            var kategoriRepo = new KategoriSampahRepository(db);

            var nasabah = new Nasabah("Joko Prasetyo");
            nasabahRepo.Tambah(nasabah);
            nasabahRepo.SimpanPerubahan();
            nasabahId = nasabah.Id;

            var layanan = new LayananTransaksi(nasabahRepo, new TransaksiRepository(db));
            layanan.CatatSetoran(nasabahId, new List<DetailSetoran>
            {
                new(kategoriRepo.GetByNama("Plastik PET"), 2m),      // 2 x 4.000  =  8.000
                new(kategoriRepo.GetByNama("Kaleng Aluminium"), 1m)  // 1 x 15.000 = 15.000
            });
        }

        using (var db = basisData.BukaSesi())
        {
            var setoran = Assert.IsType<SetoranSampah>(
                new TransaksiRepository(db).GetByNasabah(nasabahId).Single());

            Assert.Equal(2, setoran.DetailSetoran.Count);
            Assert.Equal(23_000m, setoran.Nominal);
            Assert.Equal(23_000m, setoran.DetailSetoran.Sum(d => d.Subtotal));
            Assert.Equal(3m, setoran.TotalBeratKg);
        }
    }

    [Fact]
    public void Setoran_dan_penarikan_tersimpan_dalam_satu_tabel_dengan_tipe_yang_benar()
    {
        using var basisData = new BasisDataSementara();
        int nasabahId;

        using (var db = basisData.BukaSesi())
        {
            var nasabahRepo = new NasabahRepository(db);
            var nasabah = new Nasabah("Rina Wulandari");
            nasabahRepo.Tambah(nasabah);
            nasabahRepo.SimpanPerubahan();
            nasabahId = nasabah.Id;

            var layanan = new LayananTransaksi(nasabahRepo, new TransaksiRepository(db));
            layanan.CatatSetoran(nasabahId, new List<DetailSetoran>
            {
                new(new KategoriSampahRepository(db).GetByNama("Kaca"), 10m)  // 10 x 500 = 5.000
            });
            layanan.ProsesPenarikan(nasabahId, 2_000m);
        }

        using (var db = basisData.BukaSesi())
        {
            var riwayat = new TransaksiRepository(db).GetByNasabah(nasabahId).ToList();
            var nasabah = new NasabahRepository(db).GetById(nasabahId);

            Assert.Equal(2, riwayat.Count);
            Assert.IsType<SetoranSampah>(riwayat[0]);
            Assert.IsType<PenarikanSaldo>(riwayat[1]);
            Assert.Equal(3_000m, nasabah.Saldo);       // 5.000 - 2.000
        }
    }
}
