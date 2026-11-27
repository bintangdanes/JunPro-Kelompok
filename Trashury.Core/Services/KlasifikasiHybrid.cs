using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Strategi Hybrid Cloud-Edge: mencoba Cloud AI (SigLIP-SO400M) saat online,
/// dan otomatis fallback ke Edge ONNX (EfficientNet-B2) bila offline atau timeout.
/// </summary>
public sealed class KlasifikasiHybrid : IKlasifikasiSampah, IDisposable
{
    private readonly IKlasifikasiSampah _cloud;
    private readonly IKlasifikasiSampah _edge;

    public bool GunakanCloud { get; set; } = true;

    public KlasifikasiHybrid()
        : this(new KlasifikasiCloudSiglip(), new KlasifikasiOnnx())
    {
    }

    public KlasifikasiHybrid(IKlasifikasiSampah cloud, IKlasifikasiSampah edge, bool gunakanCloud = true)
    {
        _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
        GunakanCloud = gunakanCloud;
    }

    public HasilKlasifikasi Klasifikasi(byte[] gambar)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        if (gambar.Length == 0)
            throw new ArgumentException("Data gambar tidak boleh kosong.", nameof(gambar));

        if (GunakanCloud)
        {
            try
            {
                return _cloud.Klasifikasi(gambar);
            }
            catch
            {
                HasilKlasifikasi hasilFallback = _edge.Klasifikasi(gambar);
                hasilFallback.SumberModel = "Edge ONNX (Offline Fallback)";
                return hasilFallback;
            }
        }

        HasilKlasifikasi hasilEdge = _edge.Klasifikasi(gambar);
        hasilEdge.SumberModel = "Edge ONNX (EfficientNet-B2)";
        return hasilEdge;
    }

    public void Dispose()
    {
        (_cloud as IDisposable)?.Dispose();
        (_edge as IDisposable)?.Dispose();
    }
}
