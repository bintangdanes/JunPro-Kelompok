using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Services;

public class KlasifikasiOnnx : IKlasifikasiSampah
{
    public HasilKlasifikasi Klasifikasi(byte[] gambar)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        // TODO: load model ONNX dan jalankan inferensi.
        throw new NotImplementedException();
    }
}
