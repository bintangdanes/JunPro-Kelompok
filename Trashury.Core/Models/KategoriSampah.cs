namespace Trashury.Models;

public class KategoriSampah
{
    public int Id { get; set; }
    public string Nama { get; set; }
    public decimal HargaPerKg { get; set; }

    /// <summary>
    /// Estimasi kilogram CO2e yang dihindari per kilogram sampah yang
    /// berhasil didaur ulang, bukan emisi yang dihasilkan.
    /// </summary>
    public decimal FaktorEmisiCO2e { get; set; }

    public KategoriSampah(string nama, decimal hargaPerKg, decimal faktorEmisiCO2e)
    {
        Nama = nama ?? throw new ArgumentNullException(nameof(nama));
        HargaPerKg = hargaPerKg;
        FaktorEmisiCO2e = faktorEmisiCO2e;
    }

    public decimal HitungNilai(decimal berat)
    {
        if (berat < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(berat), "Berat tidak boleh negatif.");
        }

        return berat * HargaPerKg;
    }

    public decimal HitungCO2e(decimal berat)
    {
        if (berat < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(berat), "Berat tidak boleh negatif.");
        }

        return berat * FaktorEmisiCO2e;
    }
}
