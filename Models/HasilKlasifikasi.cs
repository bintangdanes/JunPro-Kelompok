namespace Trashury.Models;

public class HasilKlasifikasi
{
    public string KategoriTerdeteksi { get; set; }
    public float Confidence { get; set; }

    public HasilKlasifikasi(string kategoriTerdeteksi, float confidence)
    {
        KategoriTerdeteksi = kategoriTerdeteksi ?? throw new ArgumentNullException(nameof(kategoriTerdeteksi));
        Confidence = confidence;
    }
}
