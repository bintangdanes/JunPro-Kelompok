using System.Globalization;

namespace Trashury.Services;

/// <summary>
/// Aturan penulisan CSV: karakter pemisah kolom dan format angka.
/// </summary>
public sealed record FormatCsv(char Pemisah, CultureInfo Budaya)
{
    /// <summary>
    /// Untuk Excel dengan pengaturan wilayah Indonesia, yang memakai titik koma
    /// sebagai pemisah kolom dan koma sebagai pemisah desimal. Format bawaan
    /// karena berkas ini umumnya dibuka pengurus bank sampah dan DLH di Excel.
    /// </summary>
    public static FormatCsv ExcelIndonesia { get; } = new(';', CultureInfo.GetCultureInfo("id-ID"));

    /// <summary>
    /// RFC 4180: pemisah koma dan titik desimal. Cocok untuk diolah program
    /// lain atau Excel berpengaturan wilayah Inggris/AS.
    /// </summary>
    public static FormatCsv Standar { get; } = new(',', CultureInfo.InvariantCulture);
}
