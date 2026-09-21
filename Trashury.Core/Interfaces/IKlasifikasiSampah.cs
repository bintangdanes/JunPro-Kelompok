using Trashury.Models;

namespace Trashury.Interfaces;

public interface IKlasifikasiSampah
{
    HasilKlasifikasi Klasifikasi(byte[] gambar);
}
