using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Repositories;

public class NasabahRepository : IRepository<Nasabah>
{
    private readonly TrashuryDbContext _db;

    public NasabahRepository(TrashuryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Nasabah GetById(int id)
    {
        return _db.Nasabah.Find(id)
            ?? throw new KeyNotFoundException($"Nasabah dengan Id {id} tidak ditemukan.");
    }

    public IEnumerable<Nasabah> GetAll()
    {
        return _db.Nasabah.OrderBy(nasabah => nasabah.Nama).ToList();
    }

    public void Tambah(Nasabah entitas)
    {
        ArgumentNullException.ThrowIfNull(entitas);
        _db.Nasabah.Add(entitas);
    }

    public int SimpanPerubahan() => _db.SaveChanges();

    public Nasabah CariByKode(string kode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kode);

        if (!int.TryParse(kode, out var id))
        {
            throw new KeyNotFoundException($"Nasabah dengan kode {kode} tidak ditemukan.");
        }

        return GetById(id);
    }

    public IEnumerable<Nasabah> CariByNama(string potonganNama)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(potonganNama);

        return _db.Nasabah
            .Where(nasabah => EF.Functions.Like(nasabah.Nama, $"%{potonganNama}%"))
            .OrderBy(nasabah => nasabah.Nama)
            .ToList();
    }
}
