using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Interfaces;
using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;

var builder = WebApplication.CreateBuilder(args);

string dbPath = Environment.GetEnvironmentVariable("TRASHURY_DB_PATH")
    ?? TrashuryDbContextFactory.LokasiBasisDataBawaan();

string? dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir))
    Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<TrashuryDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<NasabahRepository>();
builder.Services.AddScoped<TransaksiRepository>();
builder.Services.AddScoped<KategoriSampahRepository>();
builder.Services.AddScoped<LayananTransaksi>();
builder.Services.AddScoped<LayananLaporan>();
builder.Services.AddScoped<LayananBukuTabungan>();
builder.Services.AddSingleton<IKalkulatorDampak, KalkulatorCO2e>();
builder.Services.AddSingleton<IKlasifikasiSampah, KlasifikasiOnnx>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrashuryDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/api/kategori", (KategoriSampahRepository repo) =>
    Results.Ok(repo.GetAll().OrderBy(k => k.Id)));

app.MapGet("/api/nasabah", (NasabahRepository repo) =>
    Results.Ok(repo.GetAll().OrderBy(n => n.Id)));

app.MapPost("/api/nasabah", (BuatNasabahRequest req, NasabahRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.Nama))
        return Results.BadRequest(new { pesan = "Nama nasabah wajib diisi." });

    var nasabah = new Nasabah(req.Nama.Trim());
    repo.Tambah(nasabah);
    repo.SimpanPerubahan();
    return Results.Created($"/api/nasabah/{nasabah.Id}", nasabah);
});

app.MapGet("/api/transaksi", (TransaksiRepository repo) =>
{
    var daftar = repo.GetAll()
        .OrderByDescending(t => t.Tanggal)
        .Select(t => new
        {
            t.Id,
            t.NasabahId,
            t.Tanggal,
            Jenis = t is SetoranSampah ? "Setoran" : "Penarikan",
            Total = t.Nominal
        });
    return Results.Ok(daftar);
});

app.MapPost("/api/transaksi/setoran", (
    CatatSetoranRequest req,
    KategoriSampahRepository kategoriRepo,
    LayananTransaksi layanan) =>
{
    if (req.Detail is null || req.Detail.Count == 0)
        return Results.BadRequest(new { pesan = "Rincian setoran tidak boleh kosong." });

    try
    {
        var detail = req.Detail
            .Select(d => new DetailSetoran(kategoriRepo.GetById(d.KategoriId), d.BeratKg))
            .ToList();

        var setoran = layanan.CatatSetoran(req.NasabahId, detail);
        return Results.Created($"/api/transaksi/{setoran.Id}", new
        {
            setoran.Id,
            setoran.NasabahId,
            setoran.Tanggal,
            TotalNilai = setoran.Nominal
        });
    }
    catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException)
    {
        return Results.BadRequest(new { pesan = ex.Message });
    }
});

app.MapPost("/api/transaksi/penarikan", (PenarikanRequest req, LayananTransaksi layanan) =>
{
    try
    {
        var penarikan = layanan.ProsesPenarikan(req.NasabahId, req.Jumlah);
        return Results.Created($"/api/transaksi/{penarikan.Id}", new
        {
            penarikan.Id,
            penarikan.NasabahId,
            penarikan.Tanggal,
            Jumlah = penarikan.Nominal
        });
    }
    catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException or InvalidOperationException)
    {
        return Results.BadRequest(new { pesan = ex.Message });
    }
});

app.MapGet("/api/dampak", (TransaksiRepository repo, IKalkulatorDampak kalkulator) =>
{
    var setoranList = repo.GetAll().OfType<SetoranSampah>().ToList();
    decimal totalBeratKg = setoranList.Sum(s => s.DetailSetoran.Sum(d => d.BeratKg));
    decimal totalCo2eKg = setoranList.Sum(kalkulator.HitungCO2e);

    return Results.Ok(new
    {
        JumlahSetoran = setoranList.Count,
        TotalBeratKg = totalBeratKg,
        TotalCO2eDihindariKg = totalCo2eKg
    });
});

// Laporan bulanan untuk DLH: ringkasan JSON, unduhan CSV, dan unduhan PDF.
app.MapGet("/api/laporan/{tahun:int}/{bulan:int}", (int tahun, int bulan, LayananLaporan layanan) =>
    BuatRekap(layanan, tahun, bulan, rekap => Results.Ok(rekap)));

app.MapGet("/api/laporan/{tahun:int}/{bulan:int}/csv", (
    int tahun, int bulan, string? format, LayananLaporan layanan) =>
{
    // ?format=standar untuk koma + titik desimal; bawaan untuk Excel Indonesia.
    var formatCsv = string.Equals(format, "standar", StringComparison.OrdinalIgnoreCase)
        ? FormatCsv.Standar
        : FormatCsv.ExcelIndonesia;

    return BuatRekap(layanan, tahun, bulan, rekap => Results.File(
        LayananLaporan.BuatCsv(rekap, formatCsv),
        "text/csv; charset=utf-8",
        $"laporan-trashury-{tahun:D4}-{bulan:D2}.csv"));
});

app.MapGet("/api/laporan/{tahun:int}/{bulan:int}/pdf", (int tahun, int bulan, LayananLaporan layanan) =>
    BuatRekap(layanan, tahun, bulan, rekap => Results.File(
        LayananLaporan.BuatPdf(rekap),
        "application/pdf",
        $"laporan-trashury-{tahun:D4}-{bulan:D2}.pdf")));

// Buku tabungan satu nasabah: riwayat dengan saldo berjalan, JSON dan PDF.
app.MapGet("/api/nasabah/{id:int}/buku-tabungan", (int id, LayananBukuTabungan layanan) =>
    SusunBuku(layanan, id, buku => Results.Ok(buku)));

app.MapGet("/api/nasabah/{id:int}/buku-tabungan/pdf", (int id, LayananBukuTabungan layanan) =>
    SusunBuku(layanan, id, buku => Results.File(
        LayananBukuTabungan.BuatPdf(buku),
        "application/pdf",
        $"buku-tabungan-{id}.pdf")));

app.MapPost("/api/klasifikasi", async (IFormFile? file, IKlasifikasiSampah klasifikasi) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { pesan = "File gambar wajib diunggah." });

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var hasil = klasifikasi.Klasifikasi(ms.ToArray());
    return Results.Ok(hasil);
}).DisableAntiforgery();

app.Run();

static IResult BuatRekap(LayananLaporan layanan, int tahun, int bulan, Func<RekapBulanan, IResult> hasil)
{
    if (tahun is < 2000 or > 9999)
        return Results.BadRequest(new { pesan = "Tahun tidak valid." });

    try
    {
        return hasil(layanan.LaporanBulanan(bulan, tahun));
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { pesan = ex.Message });
    }
}

static IResult SusunBuku(LayananBukuTabungan layanan, int nasabahId, Func<BukuTabungan, IResult> hasil)
{
    try
    {
        return hasil(layanan.Susun(nasabahId));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { pesan = ex.Message });
    }
}

public record BuatNasabahRequest(string Nama);
public record DetailSetoranRequest(int KategoriId, decimal BeratKg);
public record CatatSetoranRequest(int NasabahId, List<DetailSetoranRequest> Detail);
public record PenarikanRequest(int NasabahId, decimal Jumlah);
