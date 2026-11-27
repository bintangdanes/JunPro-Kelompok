namespace Trashury.Models;

public class HasilKlasifikasi
{
    public string KategoriTerdeteksi { get; set; }
    public float Confidence { get; set; }
    public string LabelAsli { get; set; } = string.Empty;
    public string SumberModel { get; set; } = "Edge ONNX (EfficientNet-B2)";
    public List<string> KandidatTeratas { get; set; } = new();
    public List<string> Peringatan { get; set; } = new();

    public HasilKlasifikasi(string kategoriTerdeteksi, float confidence)
    {
        KategoriTerdeteksi = kategoriTerdeteksi ?? throw new ArgumentNullException(nameof(kategoriTerdeteksi));
        Confidence = confidence;
    }
}
