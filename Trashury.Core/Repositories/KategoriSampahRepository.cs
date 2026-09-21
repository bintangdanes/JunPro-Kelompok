using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Repositories;

public class KategoriSampahRepository : IRepository<KategoriSampah>
{
    private readonly TrashuryDbContext _db;

    public KategoriSampahRepository(TrashuryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public KategoriSampah GetById(int id)
    {
        return _db.KategoriSampah.Find(id)
            ?? throw new KeyNotFoundException($"Kategori sampah dengan Id {id} tidak ditemukan.");
    }

    public IEnumerable<KategoriSampah> GetAll()
    {
        return _db.KategoriSampah.OrderBy(kategori => kategori.Nama).ToList();
    }

    public void Tambah(KategoriSampah entitas)
    {
        ArgumentNullException.ThrowIfNull(entitas);
        _db.KategoriSampah.Add(entitas);
    }

    public int SimpanPerubahan() => _db.SaveChanges();

    /// <summary>
    /// Memetakan nama kategori hasil klasifikasi foto ke kategori tersimpan.
    /// </summary>
    public KategoriSampah GetByNama(string nama)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nama);

        return _db.KategoriSampah.FirstOrDefault(kategori => kategori.Nama == nama)
            ?? throw new KeyNotFoundException($"Kategori sampah bernama {nama} tidak ditemukan.");
    }
}
