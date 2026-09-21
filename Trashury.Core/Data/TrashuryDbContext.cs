using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Trashury.Models;

namespace Trashury.Data;

/// <summary>
/// Sesi basis data SQLite lokal TRASHURY. Seluruh data disimpan dalam satu
/// berkas di mesin operator sehingga aplikasi tetap berjalan tanpa internet.
/// </summary>
public class TrashuryDbContext : DbContext
{
    public DbSet<Nasabah> Nasabah => Set<Nasabah>();
    public DbSet<Transaksi> Transaksi => Set<Transaksi>();
    public DbSet<KategoriSampah> KategoriSampah => Set<KategoriSampah>();

    public TrashuryDbContext(DbContextOptions<TrashuryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Nasabah>(entity =>
        {
            entity.HasKey(nasabah => nasabah.Id);
            entity.Property(nasabah => nasabah.Nama).IsRequired().HasMaxLength(120);

            // Saldo tidak punya setter publik. Dipetakan lewat field _saldo
            // supaya enkapsulasi tetap utuh dan EF tetap bisa menulis nilainya.
            entity.Property(nasabah => nasabah.Saldo)
                .HasField("_saldo")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("Saldo");
        });

        modelBuilder.Entity<KategoriSampah>(entity =>
        {
            entity.HasKey(kategori => kategori.Id);
            entity.Property(kategori => kategori.Nama).IsRequired().HasMaxLength(80);
            entity.HasIndex(kategori => kategori.Nama).IsUnique();
        });

        modelBuilder.Entity<Transaksi>(entity =>
        {
            entity.HasKey(transaksi => transaksi.Id);

            // Table-per-hierarchy: SetoranSampah dan PenarikanSaldo berbagi
            // satu tabel, dibedakan oleh kolom diskriminator.
            entity.HasDiscriminator<string>("JenisTransaksi")
                .HasValue<SetoranSampah>("Setoran")
                .HasValue<PenarikanSaldo>("Penarikan");

            entity.HasOne<Nasabah>()
                .WithMany()
                .HasForeignKey(transaksi => transaksi.NasabahId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(transaksi => transaksi.NasabahId);
            entity.HasIndex(transaksi => transaksi.Tanggal);
        });

        modelBuilder.Entity<SetoranSampah>()
            .HasMany(setoran => setoran.DetailSetoran)
            .WithOne()
            .HasForeignKey(detail => detail.SetoranSampahId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DetailSetoran>(entity =>
        {
            entity.HasKey(detail => detail.Id);
            entity.Ignore(detail => detail.Subtotal);

            entity.HasOne(detail => detail.Kategori)
                .WithMany()
                .HasForeignKey(detail => detail.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedKategoriSampah(modelBuilder);
    }

    /// <summary>
    /// Kategori awal agar aplikasi langsung dapat dipakai setelah dipasang.
    /// Harga dan faktor emisi di bawah ini adalah nilai awal yang masih perlu
    /// disesuaikan dengan harga pengepul setempat dan rujukan resmi DLH.
    /// </summary>
    private static void SeedKategoriSampah(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KategoriSampah>().HasData(
            new { Id = 1, Nama = "Plastik PET", HargaPerKg = 4000m, FaktorEmisiCO2e = 1.5m },
            new { Id = 2, Nama = "Kertas & Kardus", HargaPerKg = 2000m, FaktorEmisiCO2e = 0.9m },
            new { Id = 3, Nama = "Kaleng Aluminium", HargaPerKg = 15000m, FaktorEmisiCO2e = 8.1m },
            new { Id = 4, Nama = "Kaca", HargaPerKg = 500m, FaktorEmisiCO2e = 0.3m },
            new { Id = 5, Nama = "Organik", HargaPerKg = 300m, FaktorEmisiCO2e = 0.25m });
    }
}
