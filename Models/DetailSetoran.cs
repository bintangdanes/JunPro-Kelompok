namespace Trashury.Models;

public class DetailSetoran
{
    public KategoriSampah Kategori { get; set; }
    public decimal BeratKg { get; set; }
    public decimal Subtotal => Kategori.HitungNilai(BeratKg);

    public DetailSetoran(KategoriSampah kategori, decimal beratKg)
    {
        Kategori = kategori ?? throw new ArgumentNullException(nameof(kategori));
        BeratKg = beratKg;
    }
}
