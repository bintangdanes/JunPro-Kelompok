using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Klien Cloud AI (SigLIP-SO400M + MLP2 Ensemble) pada Hugging Face Spaces melalui Gradio HTTP API.
/// </summary>
public sealed class KlasifikasiCloudSiglip : IKlasifikasiSampah, IDisposable
{
    public const string DefaultBaseUrl = "https://afiqmc777-trashury-siglip-api.hf.space";

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly bool _ownsHttpClient;

    public KlasifikasiCloudSiglip(
        string? baseUrl = null,
        TimeSpan? timeout = null,
        HttpClient? httpClient = null)
    {
        _baseUrl = (baseUrl
            ?? Environment.GetEnvironmentVariable("TRASHURY_CLOUD_AI_URL")
            ?? DefaultBaseUrl).TrimEnd('/');

        if (httpClient != null)
        {
            _http = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _http = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
            _ownsHttpClient = true;
        }
    }

    public HasilKlasifikasi Klasifikasi(byte[] gambar)
    {
        ArgumentNullException.ThrowIfNull(gambar);
        if (gambar.Length == 0)
            throw new ArgumentException("Data gambar tidak boleh kosong.", nameof(gambar));

        return KlasifikasiAsync(gambar).GetAwaiter().GetResult();
    }

    public async Task<HasilKlasifikasi> KlasifikasiAsync(byte[] gambar, CancellationToken ct = default)
    {
        // 1. Unggah gambar ke Gradio upload endpoint
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(gambar);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "files", "sampah.jpg");

        using var uploadRes = await _http.PostAsync($"{_baseUrl}/gradio_api/upload", form, ct);
        uploadRes.EnsureSuccessStatusCode();
        string uploadBody = await uploadRes.Content.ReadAsStringAsync(ct);

        using var uploadDoc = JsonDocument.Parse(uploadBody);
        string uploadedPath = uploadDoc.RootElement[0].GetString()
            ?? throw new InvalidOperationException("Respons upload Gradio tidak berisi path berkas.");

        // 2. Panggil endpoint /predict
        var payload = new
        {
            data = new object[]
            {
                new { path = uploadedPath, meta = new { _type = "gradio.FileData" } },
                1.0
            }
        };

        using var callContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var callRes = await _http.PostAsync($"{_baseUrl}/gradio_api/call/predict", callContent, ct);
        callRes.EnsureSuccessStatusCode();
        string callBody = await callRes.Content.ReadAsStringAsync(ct);

        using var callDoc = JsonDocument.Parse(callBody);
        string eventId = callDoc.RootElement.GetProperty("event_id").GetString()
            ?? throw new InvalidOperationException("Respons Gradio tidak berisi event_id.");

        // 3. Baca SSE stream hasil prediksi
        using var sseRes = await _http.GetAsync($"{_baseUrl}/gradio_api/call/predict/{eventId}", ct);
        sseRes.EnsureSuccessStatusCode();
        string sseText = await sseRes.Content.ReadAsStringAsync(ct);

        string? dataJson = EkstrakDataSseComplete(sseText);
        if (string.IsNullOrWhiteSpace(dataJson))
            throw new InvalidOperationException("Stream Gradio tidak mengembalikan event complete.");

        return ParseHasilGradio(dataJson);
    }

    private static string? EkstrakDataSseComplete(string sseText)
    {
        bool sedangComplete = false;
        foreach (string rawLine in sseText.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                string ev = line["event:".Length..].Trim();
                sedangComplete = string.Equals(ev, "complete", StringComparison.OrdinalIgnoreCase);
            }
            else if (sedangComplete && line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return line["data:".Length..].Trim();
            }
        }
        return null;
    }

    private static HasilKlasifikasi ParseHasilGradio(string dataJson)
    {
        using var doc = JsonDocument.Parse(dataJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 2)
            throw new InvalidOperationException("Format respons JSON Gradio tidak dikenali.");

        // Output ke-2 dari predict_gradio adalah JSON detail hasil run_prediction
        JsonElement detail = doc.RootElement[1];
        string labelModel = detail.GetProperty("label_model").GetString() ?? "Unknown";
        float confidence = detail.GetProperty("confidence").GetSingle();
        string kategori = KlasifikasiOnnx.PetakanKategori(labelModel);

        var kandidatTeratas = new List<string>();
        if (detail.TryGetProperty("kandidat_teratas", out JsonElement kandidatArr) &&
            kandidatArr.ValueKind == JsonValueKind.Array)
        {
            int peringkat = 1;
            foreach (JsonElement item in kandidatArr.EnumerateArray())
            {
                string lbl = item.GetProperty("label_model").GetString() ?? "Unknown";
                float conf = item.GetProperty("confidence").GetSingle();
                string kat = KlasifikasiOnnx.PetakanKategori(lbl);
                kandidatTeratas.Add($"#{peringkat++} {kat} [{lbl}] ({conf:P1})");
            }
        }

        var peringatan = new List<string>();
        if (detail.TryGetProperty("peringatan_kontaminasi", out JsonElement warnArr) &&
            warnArr.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement w in warnArr.EnumerateArray())
            {
                string? teks = w.GetString();
                if (!string.IsNullOrWhiteSpace(teks))
                    peringatan.Add(teks);
            }
        }

        return new HasilKlasifikasi(kategori, confidence)
        {
            LabelAsli = labelModel,
            SumberModel = "Cloud SigLIP-SO400M",
            KandidatTeratas = kandidatTeratas,
            Peringatan = peringatan
        };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }
}
