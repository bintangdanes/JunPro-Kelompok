using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Layanan klasifikasi gambar sampah secara offline menggunakan model ONNX.
/// </summary>
public sealed class KlasifikasiOnnx : IKlasifikasiSampah, IDisposable
{
    private static readonly float[] ImageNetMean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] ImageNetStd = [0.229f, 0.224f, 0.225f];

    // Memetakan 12 label bawaan model (labels.txt) ke 5 kategori seed TrashuryDbContext.
    private static readonly Dictionary<string, string> PemetaanKategori = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Plastic Bottle"] = "Plastik PET",
        ["Plastic Cup"] = "Plastik PET",
        ["Plastic bag"] = "Plastik PET",
        ["Jug"] = "Plastik PET",
        ["Cardboard Boxes"] = "Kertas & Kardus",
        ["Paper"] = "Kertas & Kardus",
        ["Paper Bag"] = "Kertas & Kardus",
        ["Paper Cup"] = "Kertas & Kardus",
        ["Metal Can"] = "Kaleng Aluminium",
        ["Glass Bottles"] = "Kaca",
        ["Glass Shards"] = "Kaca",
        ["food waste"] = "Organik"
    };

    private readonly InferenceSession _session;
    private readonly string[] _labels;
    private readonly string _inputName;
    private readonly int _height;
    private readonly int _width;
    private readonly bool _isNchw;

    public KlasifikasiOnnx()
        : this(
            Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "klasifikasi-sampah-v3.onnx"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "labels.txt"))
    {
    }

    public KlasifikasiOnnx(string modelPath, string? labelsPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        if (!File.Exists(modelPath))
            throw new FileNotFoundException("Berkas model ONNX tidak ditemukan.", modelPath);

        string resolvedLabelsPath = labelsPath
            ?? Path.Combine(Path.GetDirectoryName(modelPath)!, "labels.txt");

        if (!File.Exists(resolvedLabelsPath))
            throw new FileNotFoundException("Berkas label model tidak ditemukan.", resolvedLabelsPath);

        _labels = File.ReadAllLines(resolvedLabelsPath)
            .Select(baris => baris.Trim())
            .Where(baris => !string.IsNullOrEmpty(baris))
            .ToArray();

        if (_labels.Length == 0)
            throw new InvalidOperationException("Berkas labels.txt kosong.");

        _session = new InferenceSession(modelPath);

        var inputMeta = _session.InputMetadata.First();
        _inputName = inputMeta.Key;
        (_isNchw, _height, _width) = BacaDimensiInput(inputMeta.Value.Dimensions);
    }

    public HasilKlasifikasi Klasifikasi(byte[] gambar)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        if (gambar.Length == 0)
            throw new ArgumentException("Data gambar tidak boleh kosong.", nameof(gambar));

        DenseTensor<float> tensor = BuatTensorGambar(gambar);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor)
        };

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> hasil = _session.Run(inputs);
        float[] skor = hasil.First().AsEnumerable<float>().ToArray();

        if (skor.Length == 0)
            throw new InvalidOperationException("Model ONNX tidak mengembalikan skor klasifikasi.");

        float[] probabilitas = KonversiKeProbabilitas(skor);
        int[] urutanIndeks = Enumerable.Range(0, probabilitas.Length)
            .OrderByDescending(i => probabilitas[i])
            .Take(3)
            .ToArray();

        int indeksTerbaik = urutanIndeks[0];
        string labelTerbaik = AmbilLabel(indeksTerbaik);
        string kategoriTerbaik = PetakanKategori(labelTerbaik);

        var kandidatTeratas = new List<string>();
        var peringatan = new List<string>();

        for (int peringkat = 0; peringkat < urutanIndeks.Length; peringkat++)
        {
            int idx = urutanIndeks[peringkat];
            string lbl = AmbilLabel(idx);
            string kat = PetakanKategori(lbl);
            float conf = probabilitas[idx];
            kandidatTeratas.Add($"#{peringkat + 1} {kat} [{lbl}] ({conf:P1})");

            if (conf >= 0.20f)
            {
                if (string.Equals(lbl, "Glass Shards", StringComparison.OrdinalIgnoreCase))
                    peringatan.Add("PERINGATAN: Terdeteksi pecahan kaca (Glass Shards). Gunakan sarung tangan.");
                else if (string.Equals(lbl, "food waste", StringComparison.OrdinalIgnoreCase))
                    peringatan.Add("PERINGATAN: Terdeteksi sampah organik / sisa makanan (food waste).");
            }
        }

        return new HasilKlasifikasi(kategoriTerbaik, probabilitas[indeksTerbaik])
        {
            LabelAsli = labelTerbaik,
            KandidatTeratas = kandidatTeratas,
            Peringatan = peringatan.Distinct().ToList()
        };
    }

    private string AmbilLabel(int indeks) =>
        indeks >= 0 && indeks < _labels.Length ? _labels[indeks] : $"Kelas-{indeks}";

    /// <summary>
    /// Mengubah label mentah model menjadi nama KategoriSampah yang terdaftar di basis data.
    /// </summary>
    public static string PetakanKategori(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return PemetaanKategori.TryGetValue(label.Trim(), out string? terpetakan)
            ? terpetakan
            : label.Trim();
    }

    private DenseTensor<float> BuatTensorGambar(byte[] gambar)
    {
        using Image<Rgb24> image = Image.Load<Rgb24>(gambar);
        image.Mutate(ctx => ctx.Resize(_width, _height));

        int[] shape = _isNchw
            ? [1, 3, _height, _width]
            : [1, _height, _width, 3];

        var tensor = new DenseTensor<float>(shape);

        // Normalisasi standar ImageNet (sesuaikan jika model dilatih dengan skala [0, 1] saja).
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < _height; y++)
            {
                Span<Rgb24> row = accessor.GetRowSpan(y);
                for (int x = 0; x < _width; x++)
                {
                    Rgb24 pixel = row[x];
                    float r = (pixel.R / 255f - ImageNetMean[0]) / ImageNetStd[0];
                    float g = (pixel.G / 255f - ImageNetMean[1]) / ImageNetStd[1];
                    float b = (pixel.B / 255f - ImageNetMean[2]) / ImageNetStd[2];

                    if (_isNchw)
                    {
                        tensor[0, 0, y, x] = r;
                        tensor[0, 1, y, x] = g;
                        tensor[0, 2, y, x] = b;
                    }
                    else
                    {
                        tensor[0, y, x, 0] = r;
                        tensor[0, y, x, 1] = g;
                        tensor[0, y, x, 2] = b;
                    }
                }
            }
        });

        return tensor;
    }

    private static (bool isNchw, int height, int width) BacaDimensiInput(int[] dims)
    {
        if (dims.Length == 4)
        {
            // NCHW: [N, 3, H, W], NHWC: [N, H, W, 3]
            if (dims[1] == 3)
                return (true, dims[2] > 0 ? dims[2] : 224, dims[3] > 0 ? dims[3] : 224);

            if (dims[3] == 3)
                return (false, dims[1] > 0 ? dims[1] : 224, dims[2] > 0 ? dims[2] : 224);
        }

        return (true, 224, 224);
    }

    private static float[] KonversiKeProbabilitas(float[] skor)
    {
        bool sudahProbabilitas = skor.All(s => s >= 0f && s <= 1f)
            && Math.Abs(skor.Sum() - 1f) < 0.02f;

        if (sudahProbabilitas)
            return skor;

        float max = skor.Max();
        float[] exp = new float[skor.Length];
        float total = 0f;

        for (int i = 0; i < skor.Length; i++)
        {
            exp[i] = MathF.Exp(skor[i] - max);
            total += exp[i];
        }

        for (int i = 0; i < exp.Length; i++)
            exp[i] /= total;

        return exp;
    }

    public void Dispose() => _session.Dispose();
}
