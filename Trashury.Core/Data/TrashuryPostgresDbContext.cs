using Microsoft.EntityFrameworkCore;

namespace Trashury.Data;

/// <summary>
/// Sesi basis data PostgreSQL di cloud (Supabase). Dibuat sebagai subclass
/// supaya migrasi PostgreSQL tersimpan terpisah dari migrasi SQLite, sementara
/// seluruh repository dan layanan tetap menerima <see cref="TrashuryDbContext"/>.
/// </summary>
public class TrashuryPostgresDbContext : TrashuryDbContext
{
    public TrashuryPostgresDbContext(DbContextOptions<TrashuryPostgresDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplikasi mencatat waktu lokal (DateTime.Now). Kolom timestamptz di
        // Npgsql hanya menerima waktu UTC, jadi tanggal disimpan sebagai
        // timestamp tanpa zona waktu agar perilakunya sama dengan SQLite.
        foreach (var entitas in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var properti in entitas.GetProperties())
            {
                if (properti.ClrType == typeof(DateTime) || properti.ClrType == typeof(DateTime?))
                {
                    properti.SetColumnType("timestamp without time zone");
                }
            }
        }
    }
}
