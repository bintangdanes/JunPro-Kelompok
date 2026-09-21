using System.Linq;
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
        var nasabah = _nasabahRepository.GetById(nasabahId);
        var setoran = new SetoranSampah(detail)
        {
            Id = NomorTransaksiBerikutnya(),
            Tanggal = DateTime.Now
        };

        setoran.Terapkan(nasabah);
        _transaksiRepository.Tambah(setoran);
        _transaksiRepository.SimpanPerubahan();
        return setoran;
    }

    public PenarikanSaldo ProsesPenarikan(int nasabahId, decimal jumlah)
    {
        var nasabah = _nasabahRepository.GetById(nasabahId);
        var penarikan = new PenarikanSaldo(jumlah)
        {
            Id = NomorTransaksiBerikutnya(),
            Tanggal = DateTime.Now
        };

        penarikan.Terapkan(nasabah);
        _transaksiRepository.Tambah(penarikan);
        _transaksiRepository.SimpanPerubahan();
        return penarikan;
    }

    private int NomorTransaksiBerikutnya()
    {
        return _transaksiRepository.GetAll().Select(transaksi => transaksi.Id).DefaultIfEmpty(0).Max() + 1;
    }
}
