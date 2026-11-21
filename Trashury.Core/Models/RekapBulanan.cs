namespace Trashury.Models;

/// <summary>
/// Hasil <see cref="Trashury.Services.LayananLaporan.LaporanBulanan"/>:
/// ringkasan seluruh transaksi dalam satu bulan kalender. Objek ini hanya
/// membawa data (DTO), dipakai bersama oleh tampilan, ekspor CSV, dan ekspor PDF.
/// </summary>
public sealed record RekapBulanan
{
    public required int Bulan { get; init; }
    public required int Tahun { get; init; }

    public required int JumlahSetoran { get; init; }
    public required int JumlahPenarikan { get; init; }

    /// <summary>Banyaknya nasabah berbeda yang bertransaksi pada bulan ini.</summary>
    public required int JumlahNasabahAktif { get; init; }

    public required decimal TotalBeratKg { get; init; }
    public required decimal TotalNilaiSetoran { get; init; }
    public required decimal TotalPenarikan { get; init; }

    /// <summary>Estimasi kilogram CO2e yang dihindari dari seluruh setoran.</summary>
    public required decimal TotalCO2eKg { get; init; }

    /// <summary>Rincian per kategori, diurutkan berdasarkan nama kategori.</summary>
    public required IReadOnlyList<RekapKategori> PerKategori { get; init; }
}
