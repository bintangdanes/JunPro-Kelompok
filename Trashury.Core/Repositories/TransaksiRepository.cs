using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Repositories;

public class TransaksiRepository : IRepository<Transaksi>
{
    private readonly TrashuryDbContext _db;

    public TransaksiRepository(TrashuryDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Transaksi GetById(int id)
    {
        return DenganRincian().FirstOrDefault(transaksi => transaksi.Id == id)
            ?? throw new KeyNotFoundException($"Transaksi dengan Id {id} tidak ditemukan.");
    }

    public IEnumerable<Transaksi> GetAll()
    {
        return DenganRincian()
            .OrderByDescending(transaksi => transaksi.Tanggal)
            .ToList();
    }

    public void Tambah(Transaksi entitas)
    {
        ArgumentNullException.ThrowIfNull(entitas);
        _db.Transaksi.Add(entitas);
    }

    public int SimpanPerubahan() => _db.SaveChanges();

    public IEnumerable<Transaksi> GetByPeriode(DateTime awal, DateTime akhir)
    {
        return DenganRincian()
            .Where(transaksi => transaksi.Tanggal >= awal && transaksi.Tanggal <= akhir)
            .OrderBy(transaksi => transaksi.Tanggal)
            .ToList();
    }

    /// <summary>
    /// Riwayat satu nasabah, dipakai untuk mencetak buku tabungan.
    /// </summary>
    public IEnumerable<Transaksi> GetByNasabah(int nasabahId)
    {
        return DenganRincian()
            .Where(transaksi => transaksi.NasabahId == nasabahId)
            .OrderBy(transaksi => transaksi.Tanggal)
            .ToList();
    }

    /// <summary>
    /// Rincian setoran dan kategorinya wajib ikut dimuat, karena
    /// <see cref="DetailSetoran.Subtotal"/> dihitung dari objek kategori.
    /// </summary>
    private IQueryable<Transaksi> DenganRincian()
    {
        return _db.Transaksi
            .Include(transaksi => (transaksi as SetoranSampah)!.DetailSetoran)
                .ThenInclude(detail => detail.Kategori);
    }
}
