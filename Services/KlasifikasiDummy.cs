using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Services;

public class KlasifikasiDummy : IKlasifikasiSampah
{
    public HasilKlasifikasi Klasifikasi(byte[] gambar)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        return new HasilKlasifikasi("Organik", 1.0f);
    }
}
