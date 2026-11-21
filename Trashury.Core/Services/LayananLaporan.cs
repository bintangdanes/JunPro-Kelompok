using System.Linq;
using Trashury.Interfaces;
using Trashury.Models;
using Trashury.Repositories;

namespace Trashury.Services;

public class LayananLaporan
{
    private readonly IRepository<Transaksi> _transaksiRepository;

    public LayananLaporan(IRepository<Transaksi> transaksiRepository)
    {
        _transaksiRepository = transaksiRepository ?? throw new ArgumentNullException(nameof(transaksiRepository));
    }

    public LayananLaporan(TransaksiRepository transaksiRepository)
        : this((IRepository<Transaksi>)transaksiRepository)
    {
    }

    public RekapBulanan LaporanBulanan(int bulan, int tahun)
    {
        if (bulan is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(bulan), "Bulan harus antara 1 dan 12.");
        }

        var awal = new DateTime(tahun, bulan, 1);
        var akhir = awal.AddMonths(1);

        var transaksi = _transaksiRepository
            .GetAll()
            .Where(t => t.Tanggal >= awal && t.Tanggal < akhir)
            .ToList();

        var setoran = transaksi.OfType<SetoranSampah>().ToList();
        var penarikan = transaksi.OfType<PenarikanSaldo>().ToList();

        var perKategori = setoran
            .SelectMany(s => s.DetailSetoran)
            .GroupBy(d => d.Kategori.Nama)
            .Select(g => new RekapKategori(
                NamaKategori: g.Key,
                TotalBeratKg: g.Sum(d => d.BeratKg),
                TotalNilai: g.Sum(d => d.Subtotal),
                TotalCO2eKg: g.Sum(d => d.Kategori.HitungCO2e(d.BeratKg))))
            .OrderBy(r => r.NamaKategori, StringComparer.Ordinal)
            .ToList();

        return new RekapBulanan
        {
            Bulan = bulan,
            Tahun = tahun,
            JumlahSetoran = setoran.Count,
            JumlahPenarikan = penarikan.Count,
            JumlahNasabahAktif = transaksi.Select(t => t.NasabahId).Distinct().Count(),
            TotalBeratKg = perKategori.Sum(r => r.TotalBeratKg),
            TotalNilaiSetoran = setoran.Sum(s => s.Nominal),
            TotalPenarikan = penarikan.Sum(p => p.Nominal),
            TotalCO2eKg = perKategori.Sum(r => r.TotalCO2eKg),
            PerKategori = perKategori
        };
    }

    /// <summary>
    /// Menulis rincian per kategori dari <paramref name="rekap"/> ke berkas CSV,
    /// ditutup satu baris TOTAL. Tanpa <paramref name="format"/>, berkas memakai
    /// <see cref="FormatCsv.ExcelIndonesia"/> agar langsung terbaca per kolom di
    /// Excel berbahasa Indonesia.
    /// </summary>
    public void EksporCsv(RekapBulanan rekap, string path, FormatCsv? format = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        BerkasEkspor.Tulis(path, BuatCsv(rekap, format));
    }

    /// <summary>
    /// Isi CSV dalam bentuk byte (UTF-8 dengan BOM agar Excel membaca karakter
    /// dengan benar), misalnya untuk dikirim sebagai unduhan lewat API.
    /// </summary>
    public static byte[] BuatCsv(RekapBulanan rekap, FormatCsv? format = null)
    {
        using var writer = new StringWriter();
        TulisCsv(rekap, writer, format);
        return BerkasEkspor.Utf8DenganBom(writer.ToString());
    }

    public static void TulisCsv(RekapBulanan rekap, TextWriter writer, FormatCsv? format = null)
    {
        ArgumentNullException.ThrowIfNull(rekap);
        ArgumentNullException.ThrowIfNull(writer);
        format ??= FormatCsv.ExcelIndonesia;

        var periode = $"{rekap.Tahun:D4}-{rekap.Bulan:D2}";

        TulisBaris(writer, format, "Periode", "Kategori", "Berat (kg)", "Nilai (Rp)", "CO2e Dihindari (kg)");
        foreach (var k in rekap.PerKategori)
        {
            TulisBaris(writer, format, periode, k.NamaKategori,
                Angka(k.TotalBeratKg, format), Angka(k.TotalNilai, format), Angka(k.TotalCO2eKg, format));
        }

        TulisBaris(writer, format, periode, "TOTAL",
            Angka(rekap.TotalBeratKg, format), Angka(rekap.TotalNilaiSetoran, format), Angka(rekap.TotalCO2eKg, format));
    }

    /// <summary>
    /// Menulis laporan bulanan lengkap (ringkasan + rincian per kategori)
    /// ke berkas PDF A4, siap diserahkan ke DLH.
    /// </summary>
    public void EksporPdf(RekapBulanan rekap, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        BerkasEkspor.Tulis(path, BuatPdf(rekap));
    }

    public static byte[] BuatPdf(RekapBulanan rekap) => DokumenLaporanPdf.Buat(rekap, DateTime.Now);

    private static void TulisBaris(TextWriter writer, FormatCsv format, params string[] kolom)
    {
        writer.WriteLine(string.Join(format.Pemisah, kolom.Select(k => Kutip(k, format.Pemisah))));
    }

    // Tanpa pemisah ribuan, karena titik ribuan pada format Indonesia akan
    // dibaca sebagai desimal oleh program yang memakai format standar.
    private static string Angka(decimal nilai, FormatCsv format) => nilai.ToString("0.####", format.Budaya);

    private static string Kutip(string teks, char pemisah)
    {
        if (teks.IndexOfAny(new[] { pemisah, '"', '\n', '\r' }) < 0)
        {
            return teks;
        }

        return "\"" + teks.Replace("\"", "\"\"") + "\"";
    }
}
