using System.Linq;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Repositories;

public class TransaksiRepository : IRepository<Transaksi>
{
    private readonly List<Transaksi> _transaksi = new();

    public Transaksi GetById(int id)
    {
        return _transaksi.FirstOrDefault(transaksi => transaksi.Id == id)
            ?? throw new KeyNotFoundException($"Transaksi dengan Id {id} tidak ditemukan.");
    }

    public IEnumerable<Transaksi> GetAll() => _transaksi;

    public void Tambah(Transaksi entitas)
    {
        ArgumentNullException.ThrowIfNull(entitas);
        _transaksi.Add(entitas);
    }

    public int SimpanPerubahan()
    {
        // TODO: Ganti penyimpanan in-memory dengan database.
        return 0;
    }

    public IEnumerable<Transaksi> GetByPeriode(DateTime awal, DateTime akhir)
    {
        return _transaksi.Where(transaksi => transaksi.Tanggal >= awal && transaksi.Tanggal <= akhir);
    }
}
