namespace Trashury.Models;

public class DetailSetoran
{
    public int Id { get; set; }
    public int SetoranSampahId { get; set; }
    public int KategoriId { get; set; }
    public KategoriSampah Kategori { get; set; } = null!;
    public decimal BeratKg { get; set; }
    public decimal Subtotal => Kategori.HitungNilai(BeratKg);

    // Dipakai Entity Framework saat memuat data dari basis data.
    private DetailSetoran()
    {
    }

    public DetailSetoran(KategoriSampah kategori, decimal beratKg)
    {
        Kategori = kategori ?? throw new ArgumentNullException(nameof(kategori));
        KategoriId = kategori.Id;
        BeratKg = beratKg;
    }
}
