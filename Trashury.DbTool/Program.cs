using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;

// Perkakas baris perintah untuk menyiapkan dan memeriksa basis data TRASHURY.
// Dipakai saat demo dan pemeriksaan praktikum, bukan bagian dari aplikasi WPF.

var perintah = args.FirstOrDefault() ?? "bantuan";
var path = AmbilOpsi(args, "--db") ?? TrashuryDbContextFactory.LokasiBasisDataBawaan();

switch (perintah)
{
    case "migrate":
        Migrate(path);
        break;
    case "seed":
        Migrate(path);
        Seed(path, segar: args.Contains("--fresh"));
        break;
    case "info":
        Info(path);
        break;
    default:
        Console.WriteLine("""
            Perkakas basis data TRASHURY

              dotnet run --project Trashury.DbTool -- migrate   Terapkan migrasi
              dotnet run --project Trashury.DbTool -- seed      Isi data contoh
              dotnet run --project Trashury.DbTool -- info      Tampilkan tabel dan isinya

            Opsi:
              --db <path>   Gunakan berkas basis data tertentu
              --fresh       Kosongkan data transaksi sebelum mengisi contoh
            """);
        break;
}

static void Migrate(string path)
{
    using var db = TrashuryDbContextFactory.Buat(path);
    db.Database.Migrate();
    Console.WriteLine($"Migrasi diterapkan pada: {path}");
}

static void Seed(string path, bool segar)
{
    using var db = TrashuryDbContextFactory.Buat(path);

    if (segar)
    {
        db.Database.ExecuteSqlRaw("DELETE FROM DetailSetoran; DELETE FROM Transaksi; DELETE FROM Nasabah;");
    }

    if (db.Nasabah.Any())
    {
        Console.WriteLine("Data nasabah sudah ada, pengisian contoh dilewati. Gunakan --fresh untuk mengulang.");
        return;
    }

    var nasabahRepo = new NasabahRepository(db);
    var kategoriRepo = new KategoriSampahRepository(db);
    var transaksiRepo = new TransaksiRepository(db);
    var layanan = new LayananTransaksi(nasabahRepo, transaksiRepo);

    foreach (var nama in new[] { "Budi Santoso", "Siti Aminah", "Joko Prasetyo" })
    {
        nasabahRepo.Tambah(new Nasabah(nama));
    }

    nasabahRepo.SimpanPerubahan();

    var daftarNasabah = nasabahRepo.GetAll().ToList();
    var pet = kategoriRepo.GetByNama("Plastik PET");
    var kardus = kategoriRepo.GetByNama("Kertas & Kardus");
    var aluminium = kategoriRepo.GetByNama("Kaleng Aluminium");

    var budi = daftarNasabah.Single(n => n.Nama == "Budi Santoso");
    var siti = daftarNasabah.Single(n => n.Nama == "Siti Aminah");
    var joko = daftarNasabah.Single(n => n.Nama == "Joko Prasetyo");

    layanan.CatatSetoran(budi.Id, [new(pet, 3.5m), new(kardus, 5m)]);
    layanan.CatatSetoran(siti.Id, [new(aluminium, 1.2m)]);
    layanan.CatatSetoran(joko.Id, [new(kardus, 8m), new(pet, 1m)]);
    layanan.ProsesPenarikan(budi.Id, 10_000m);

    Console.WriteLine("Data contoh berhasil diisi.");
}

static void Info(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Basis data belum ada pada: {path}");
        return;
    }

    using var db = TrashuryDbContextFactory.Buat(path);

    Console.WriteLine($"Berkas basis data : {path}");
    Console.WriteLine($"Ukuran            : {new FileInfo(path).Length:n0} byte");
    Console.WriteLine($"Migrasi diterapkan: {string.Join(", ", db.Database.GetAppliedMigrations())}");
    Console.WriteLine();

    Console.WriteLine("Jumlah baris per tabel");
    Console.WriteLine($"  KategoriSampah : {db.KategoriSampah.Count()}");
    Console.WriteLine($"  Nasabah        : {db.Nasabah.Count()}");
    Console.WriteLine($"  Transaksi      : {db.Transaksi.Count()}");
    Console.WriteLine($"  DetailSetoran  : {db.Set<DetailSetoran>().Count()}");
    Console.WriteLine();

    Console.WriteLine("Nasabah");
    foreach (var nasabah in db.Nasabah.OrderBy(n => n.Id))
    {
        Console.WriteLine($"  [{nasabah.Id}] {nasabah.Nama,-16} saldo Rp{nasabah.Saldo:n0}");
    }

    Console.WriteLine();
    Console.WriteLine("Transaksi");
    var transaksi = new TransaksiRepository(db).GetAll().OrderBy(t => t.Id);
    foreach (var t in transaksi)
    {
        var jenis = t is SetoranSampah ? "Setoran  " : "Penarikan";
        var rincian = t is SetoranSampah s
            ? $"  ({string.Join(", ", s.DetailSetoran.Select(d => $"{d.Kategori.Nama} {d.BeratKg:0.##} kg"))})"
            : string.Empty;

        Console.WriteLine($"  [{t.Id}] {t.Tanggal:yyyy-MM-dd} nasabah {t.NasabahId} {jenis} Rp{t.Nominal,9:n0}{rincian}");
    }
}

static string? AmbilOpsi(string[] args, string nama)
{
    var posisi = Array.IndexOf(args, nama);
    return posisi >= 0 && posisi + 1 < args.Length ? args[posisi + 1] : null;
}
