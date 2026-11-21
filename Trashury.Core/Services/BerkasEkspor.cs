using System.Text;

namespace Trashury.Services;

/// <summary>
/// Utilitas bersama untuk menyimpan hasil ekspor (CSV/PDF) ke disk.
/// </summary>
internal static class BerkasEkspor
{
    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    /// <summary>Menulis berkas, membuat folder tujuan bila belum ada.</summary>
    public static void Tulis(string path, byte[] isi)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllBytes(path, isi);
    }

    public static byte[] Utf8DenganBom(string teks) => [.. Utf8Bom.GetPreamble(), .. Utf8Bom.GetBytes(teks)];
}
