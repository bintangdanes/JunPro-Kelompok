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

    public IEnumerable<object> LaporanBulanan(int bulan, int tahun)
    {
        var awal = new DateTime(tahun, bulan, 1);
        var akhir = awal.AddMonths(1);
        return _transaksiRepository
            .GetAll()
            .Where(transaksi => transaksi.Tanggal >= awal && transaksi.Tanggal < akhir)
            .Cast<object>();
    }

    public void EksporCsv(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        // TODO: Implementasikan ekspor laporan ke CSV.
        throw new NotImplementedException();
    }
}
