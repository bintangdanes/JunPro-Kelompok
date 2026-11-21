using System.Globalization;
using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class LayananLaporanTests
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
    public void Rekap_bulanan_menjumlahkan_setoran_penarikan_dan_CO2e_per_kategori()
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

        var pet = kategoriRepo.GetByNama("Plastik PET");     // 4.000/kg, 1,5 CO2e/kg
        var kaleng = kategoriRepo.GetByNama("Kaleng Aluminium"); // 15.000/kg, 8,1 CO2e/kg

        // Maret 2026: dua setoran dari dua nasabah, satu penarikan.
        Catat(transaksiRepo, ani, new SetoranSampah(new List<DetailSetoran>
        {
            new(pet, 2m),      // 8.000 ; 3,0 CO2e
            new(kaleng, 0.5m)  // 7.500 ; 4,05 CO2e
        }), new DateTime(2026, 3, 1, 8, 0, 0));

        Catat(transaksiRepo, budi, new SetoranSampah(new List<DetailSetoran>
        {
            new(pet, 1m)       // 4.000 ; 1,5 CO2e
        }), new DateTime(2026, 3, 31, 23, 59, 0));

        Catat(transaksiRepo, ani, new PenarikanSaldo(5_000m), new DateTime(2026, 3, 15));

        // Di luar periode: tidak boleh ikut terhitung.
        Catat(transaksiRepo, budi, new SetoranSampah(new List<DetailSetoran>
        {
            new(kaleng, 10m)
        }), new DateTime(2026, 4, 1));

        var rekap = new LayananLaporan(transaksiRepo).LaporanBulanan(3, 2026);

        Assert.Equal(3, rekap.Bulan);
        Assert.Equal(2026, rekap.Tahun);
        Assert.Equal(2, rekap.JumlahSetoran);
        Assert.Equal(1, rekap.JumlahPenarikan);
        Assert.Equal(2, rekap.JumlahNasabahAktif);
        Assert.Equal(3.5m, rekap.TotalBeratKg);
        Assert.Equal(19_500m, rekap.TotalNilaiSetoran);
        Assert.Equal(5_000m, rekap.TotalPenarikan);
        Assert.Equal(8.55m, rekap.TotalCO2eKg);

        Assert.Collection(rekap.PerKategori,
            k => Assert.Equal(new RekapKategori("Kaleng Aluminium", 0.5m, 7_500m, 4.05m), k),
            k => Assert.Equal(new RekapKategori("Plastik PET", 3m, 12_000m, 4.5m), k));
    }

    [Fact]
    public void Bulan_tanpa_transaksi_menghasilkan_rekap_kosong()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();

        var rekap = new LayananLaporan(new TransaksiRepository(db)).LaporanBulanan(1, 2026);

        Assert.Equal(0, rekap.JumlahSetoran);
        Assert.Equal(0, rekap.JumlahNasabahAktif);
        Assert.Equal(0m, rekap.TotalNilaiSetoran);
        Assert.Equal(0m, rekap.TotalCO2eKg);
        Assert.Empty(rekap.PerKategori);
    }

    private static RekapBulanan ContohRekap(params RekapKategori[] perKategori) => new()
    {
        Bulan = 3,
        Tahun = 2026,
        JumlahSetoran = 2,
        JumlahPenarikan = 1,
        JumlahNasabahAktif = 2,
        TotalBeratKg = perKategori.Sum(k => k.TotalBeratKg),
        TotalNilaiSetoran = perKategori.Sum(k => k.TotalNilai),
        TotalPenarikan = 5_000m,
        TotalCO2eKg = perKategori.Sum(k => k.TotalCO2eKg),
        PerKategori = perKategori
    };

    [Fact]
    public void Ekspor_csv_menulis_baris_per_kategori_dan_total_ke_folder_baru()
    {
        var rekap = ContohRekap(
            new RekapKategori("Kaleng Aluminium", 0.5m, 7_500m, 4.05m),
            new RekapKategori("Plastik PET", 3m, 12_000m, 4.5m));

        var folder = Path.Combine(Path.GetTempPath(), "trashury-csv-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "laporan", "2026-03.csv");

        try
        {
            new LayananLaporan(new TransaksiRepositoryKosong()).EksporCsv(rekap, path);

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);

            // Bawaan: format Excel Indonesia (titik koma, koma desimal).
            Assert.Equal(new[]
            {
                "Periode;Kategori;Berat (kg);Nilai (Rp);CO2e Dihindari (kg)",
                "2026-03;Kaleng Aluminium;0,5;7500;4,05",
                "2026-03;Plastik PET;3;12000;4,5",
                "2026-03;TOTAL;3,5;19500;8,55"
            }, File.ReadAllLines(path));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    public void Ekspor_csv_format_standar_memakai_koma_dan_titik_desimal_apa_pun_locale_komputer()
    {
        var rekap = ContohRekap(new RekapKategori("Kertas, \"Kardus\"", 1.25m, 2_500m, 1.125m));
        var budayaAsal = CultureInfo.CurrentCulture;

        try
        {
            // Locale Indonesia memakai koma sebagai pemisah desimal.
            CultureInfo.CurrentCulture = new CultureInfo("id-ID");
            using var writer = new StringWriter();
            LayananLaporan.TulisCsv(rekap, writer, FormatCsv.Standar);

            var baris = writer.ToString().Split(Environment.NewLine);
            Assert.Equal("Periode,Kategori,Berat (kg),Nilai (Rp),CO2e Dihindari (kg)", baris[0]);
            Assert.Equal("2026-03,\"Kertas, \"\"Kardus\"\"\",1.25,2500,1.125", baris[1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = budayaAsal;
        }
    }

    [Fact]
    public void Ekspor_csv_format_excel_indonesia_mengutip_nama_bertitik_koma_saja()
    {
        var rekap = ContohRekap(
            new RekapKategori("Kertas, Kardus", 1m, 2_000m, 0.9m),
            new RekapKategori("Botol; Kaca", 2m, 1_000m, 0.6m));

        using var writer = new StringWriter();
        LayananLaporan.TulisCsv(rekap, writer, FormatCsv.ExcelIndonesia);

        var baris = writer.ToString().Split(Environment.NewLine);
        Assert.Equal("2026-03;Kertas, Kardus;1;2000;0,9", baris[1]);
        Assert.Equal("2026-03;\"Botol; Kaca\";2;1000;0,6", baris[2]);
    }

    [Fact]
    public void Buat_csv_diawali_BOM_UTF8()
    {
        var bytes = LayananLaporan.BuatCsv(ContohRekap());
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ekspor_pdf_menghasilkan_berkas_pdf_untuk_rekap_berisi_maupun_kosong(bool berisi)
    {
        var rekap = berisi
            ? ContohRekap(
                new RekapKategori("Kaleng Aluminium", 0.5m, 7_500m, 4.05m),
                new RekapKategori("Plastik PET", 3m, 12_000m, 4.5m))
            : ContohRekap();

        var folder = Path.Combine(Path.GetTempPath(), "trashury-pdf-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "laporan", "2026-03.pdf");

        try
        {
            new LayananLaporan(new TransaksiRepositoryKosong()).EksporPdf(rekap, path);

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 1_000);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    private sealed class TransaksiRepositoryKosong : Trashury.Interfaces.IRepository<Transaksi>
    {
        public Transaksi GetById(int id) => throw new KeyNotFoundException();
        public IEnumerable<Transaksi> GetAll() => Array.Empty<Transaksi>();
        public void Tambah(Transaksi entitas) => throw new NotSupportedException();
        public int SimpanPerubahan() => 0;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Bulan_di_luar_1_sampai_12_ditolak(int bulan)
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var layanan = new LayananLaporan(new TransaksiRepository(db));

        Assert.Throws<ArgumentOutOfRangeException>(() => layanan.LaporanBulanan(bulan, 2026));
    }
}
