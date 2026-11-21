namespace Trashury.Models;

/// <summary>
/// Rekap satu kategori sampah dalam satu periode laporan. Satu baris ini
/// menjadi satu baris tabel pada laporan bulanan untuk DLH.
/// </summary>
public sealed record RekapKategori(
    string NamaKategori,
    decimal TotalBeratKg,
    decimal TotalNilai,
    decimal TotalCO2eKg);
