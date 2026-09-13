using System.Linq;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Repositories;

public class NasabahRepository : IRepository<Nasabah>
{
    private readonly List<Nasabah> _nasabah = new();

    public Nasabah GetById(int id)
    {
        return _nasabah.FirstOrDefault(nasabah => nasabah.Id == id)
            ?? throw new KeyNotFoundException($"Nasabah dengan Id {id} tidak ditemukan.");
    }

    public IEnumerable<Nasabah> GetAll() => _nasabah;

    public void Tambah(Nasabah entitas)
    {
        ArgumentNullException.ThrowIfNull(entitas);
        _nasabah.Add(entitas);
    }

    public int SimpanPerubahan()
    {
        // TODO: Ganti penyimpanan in-memory dengan database.
        return 0;
    }

    public Nasabah CariByKode(string kode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kode);
        return _nasabah.FirstOrDefault(nasabah => nasabah.Id.ToString() == kode)
            ?? throw new KeyNotFoundException($"Nasabah dengan kode {kode} tidak ditemukan.");
    }
}
