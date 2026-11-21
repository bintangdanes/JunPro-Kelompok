using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class LayananBukuTabunganTests
{
    private static void Catat(
        TransaksiRepository repo, Nasabah nasabah, Transaksi transaksi, DateTime tanggal)
    {
        transaksi.Tanggal = tanggal;
        transaksi.Terapkan(nasabah);
        repo.Tambah(transaksi);
        repo.SimpanPerubahan();
    }

    [Fact]
    public void Saldo_berjalan_urut_tanggal_dan_saldo_akhir_sama_dengan_saldo_nasabah()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var nasabahRepo = new NasabahRepository(db);
        var kategoriRepo = new KategoriSampahRepository(db);
        var transaksiRepo = new TransaksiRepository(db);

        var ani = new Nasabah("Ani");
        var budi = new Nasabah("Budi");
        nasabahRepo.Tambah(ani);
        nasabahRepo.Tambah(budi);
        nasabahRepo.SimpanPerubahan();

        var pet = kategoriRepo.GetByNama("Plastik PET"); // 4.000/kg

        // Dicatat tidak urut tanggal, buku tabungan harus tetap urut.
        Catat(transaksiRepo, ani, new SetoranSampah(new List<DetailSetoran> { new(pet, 2.5m) }),
            new DateTime(2026, 3, 1));                                  // +10.000
        Catat(transaksiRepo, ani, new SetoranSampah(new List<DetailSetoran> { new(pet, 1m) }),
            new DateTime(2026, 3, 20));                                 // +4.000
        Catat(transaksiRepo, ani, new PenarikanSaldo(6_000m), new DateTime(2026, 3, 10)); // -6.000

        // Transaksi nasabah lain tidak boleh ikut.
        Catat(transaksiRepo, budi, new SetoranSampah(new List<DetailSetoran> { new(pet, 5m) }),
            new DateTime(2026, 3, 5));

        var buku = new LayananBukuTabungan(nasabahRepo, transaksiRepo).Susun(ani.Id);

        Assert.Equal("Ani", buku.NamaNasabah);
        Assert.Collection(buku.Baris,
            b =>
            {
                Assert.Equal(new DateTime(2026, 3, 1), b.Tanggal);
                Assert.Equal("Setoran sampah 2,5 kg", b.Keterangan);
                Assert.Equal((10_000m, 0m, 10_000m), (b.Kredit, b.Debit, b.Saldo));
            },
            b =>
            {
                Assert.Equal("Penarikan saldo", b.Keterangan);
                Assert.Equal((0m, 6_000m, 4_000m), (b.Kredit, b.Debit, b.Saldo));
            },
            b => Assert.Equal((4_000m, 0m, 8_000m), (b.Kredit, b.Debit, b.Saldo)));

        Assert.Equal(14_000m, buku.TotalKredit);
        Assert.Equal(6_000m, buku.TotalDebit);
        Assert.Equal(8_000m, buku.SaldoAkhir);
        Assert.Equal(nasabahRepo.GetById(ani.Id).Saldo, buku.SaldoAkhir);
    }

    [Fact]
    public void Nasabah_tanpa_transaksi_mendapat_buku_kosong()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var nasabahRepo = new NasabahRepository(db);
        var nasabah = new Nasabah("Citra");
        nasabahRepo.Tambah(nasabah);
        nasabahRepo.SimpanPerubahan();

        var buku = new LayananBukuTabungan(nasabahRepo, new TransaksiRepository(db)).Susun(nasabah.Id);

        Assert.Empty(buku.Baris);
        Assert.Equal(0m, buku.SaldoAkhir);
    }

    [Fact]
    public void Nasabah_tidak_terdaftar_ditolak()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var layanan = new LayananBukuTabungan(new NasabahRepository(db), new TransaksiRepository(db));

        Assert.Throws<KeyNotFoundException>(() => layanan.Susun(9999));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pdf_buku_tabungan_terbentuk(bool berisi)
    {
        var baris = berisi
            ? new List<BarisBukuTabungan>
            {
                new(1, new DateTime(2026, 3, 1), "Setoran sampah 2,5 kg", 10_000m, 0m, 10_000m),
                new(2, new DateTime(2026, 3, 10), "Penarikan saldo", 0m, 6_000m, 4_000m)
            }
            : new List<BarisBukuTabungan>();
        var buku = new BukuTabungan(1, "Ani", 10_000m, 6_000m, 4_000m, baris);

        var bytes = LayananBukuTabungan.BuatPdf(buku);

        Assert.True(bytes.Length > 1_000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }
}
