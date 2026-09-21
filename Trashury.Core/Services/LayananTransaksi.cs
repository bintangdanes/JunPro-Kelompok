using Trashury.Interfaces;
using Trashury.Models;
using Trashury.Repositories;

namespace Trashury.Services;

public class LayananTransaksi
{
    private readonly IRepository<Nasabah> _nasabahRepository;
    private readonly IRepository<Transaksi> _transaksiRepository;

    public LayananTransaksi(
        IRepository<Nasabah> nasabahRepository,
        IRepository<Transaksi> transaksiRepository)
    {
        _nasabahRepository = nasabahRepository ?? throw new ArgumentNullException(nameof(nasabahRepository));
        _transaksiRepository = transaksiRepository ?? throw new ArgumentNullException(nameof(transaksiRepository));
    }

    public LayananTransaksi(
        NasabahRepository nasabahRepository,
        TransaksiRepository transaksiRepository)
        : this((IRepository<Nasabah>)nasabahRepository, (IRepository<Transaksi>)transaksiRepository)
    {
    }

    public SetoranSampah CatatSetoran(int nasabahId, List<DetailSetoran> detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        if (detail.Count == 0)
        {
            throw new ArgumentException("Setoran harus memuat minimal satu rincian.", nameof(detail));
        }

        var nasabah = _nasabahRepository.GetById(nasabahId);
        var setoran = new SetoranSampah(detail)
        {
            Tanggal = DateTime.Now
        };

        // Terapkan() menambah saldo nasabah sekaligus mengisi NasabahId.
        setoran.Terapkan(nasabah);
        _transaksiRepository.Tambah(setoran);

        // Satu SaveChanges menyimpan transaksi baru dan saldo nasabah yang
        // berubah dalam satu operasi, sehingga keduanya tidak bisa terpisah.
        _transaksiRepository.SimpanPerubahan();
        return setoran;
    }

    public PenarikanSaldo ProsesPenarikan(int nasabahId, decimal jumlah)
    {
        var nasabah = _nasabahRepository.GetById(nasabahId);
        var penarikan = new PenarikanSaldo(jumlah)
        {
            Tanggal = DateTime.Now
        };

        penarikan.Terapkan(nasabah);
        _transaksiRepository.Tambah(penarikan);
        _transaksiRepository.SimpanPerubahan();
        return penarikan;
    }
}
