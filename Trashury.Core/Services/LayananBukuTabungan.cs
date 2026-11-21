using System.Globalization;
using System.Linq;
using Trashury.Interfaces;
using Trashury.Models;
using Trashury.Repositories;

namespace Trashury.Services;

public class LayananBukuTabungan
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly IRepository<Nasabah> _nasabahRepository;
    private readonly IRepository<Transaksi> _transaksiRepository;

    public LayananBukuTabungan(
        IRepository<Nasabah> nasabahRepository,
        IRepository<Transaksi> transaksiRepository)
    {
        _nasabahRepository = nasabahRepository ?? throw new ArgumentNullException(nameof(nasabahRepository));
        _transaksiRepository = transaksiRepository ?? throw new ArgumentNullException(nameof(transaksiRepository));
    }

    public LayananBukuTabungan(
        NasabahRepository nasabahRepository,
        TransaksiRepository transaksiRepository)
        : this((IRepository<Nasabah>)nasabahRepository, (IRepository<Transaksi>)transaksiRepository)
    {
    }

    /// <summary>
    /// Menyusun buku tabungan satu nasabah. Saldo berjalan dihitung ulang dari
    /// nol berdasarkan riwayat transaksi, urut tanggal lalu Id.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Nasabah tidak terdaftar.</exception>
    public BukuTabungan Susun(int nasabahId)
    {
        var nasabah = _nasabahRepository.GetById(nasabahId);

        var riwayat = _transaksiRepository is TransaksiRepository repoSqlite
            ? repoSqlite.GetByNasabah(nasabahId)
            : _transaksiRepository.GetAll().Where(t => t.NasabahId == nasabahId);

        var saldo = 0m;
        var baris = new List<BarisBukuTabungan>();

        foreach (var t in riwayat.OrderBy(t => t.Tanggal).ThenBy(t => t.Id))
        {
            var (keterangan, kredit, debit) = t switch
            {
                SetoranSampah s => ($"Setoran sampah {s.TotalBeratKg.ToString("#,0.##", Id)} kg", s.Nominal, 0m),
                PenarikanSaldo p => ("Penarikan saldo", 0m, p.Nominal),
                _ => throw new NotSupportedException($"Jenis transaksi {t.GetType().Name} belum didukung.")
            };

            saldo += kredit - debit;
            baris.Add(new BarisBukuTabungan(t.Id, t.Tanggal, keterangan, kredit, debit, saldo));
        }

        return new BukuTabungan(
            NasabahId: nasabah.Id,
            NamaNasabah: nasabah.Nama,
            TotalKredit: baris.Sum(b => b.Kredit),
            TotalDebit: baris.Sum(b => b.Debit),
            SaldoAkhir: saldo,
            Baris: baris);
    }

    /// <summary>Menulis buku tabungan ke berkas PDF A4 untuk dicetak.</summary>
    public void EksporPdf(BukuTabungan buku, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        BerkasEkspor.Tulis(path, BuatPdf(buku));
    }

    public static byte[] BuatPdf(BukuTabungan buku) => DokumenBukuTabunganPdf.Buat(buku, DateTime.Now);
}
