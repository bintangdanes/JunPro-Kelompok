# Laporan Pengujian Aplikasi — Modul 10 (TRASHURY)

Dokumen ini merangkum hasil pengujian otomatis (*unit & persistence testing*) serta skenario pengujian fungsional (*black-box testing*) untuk **TRASHURY**.

---

## 1. Pengujian Otomatis (`Trashury.Tests`)

Pengujian otomatis dijalankan menggunakan `dotnet test` (xUnit) pada `.NET 8.0`:

| Kelompok Pengujian | Berkas Uji | Cakupan | Status |
|---|---|---|---|
| **Skema & Seed DB** | `SkemaDanSeedTests.cs` | Migrasi EF Core SQLite & seed 5 kategori awal | ✅ Lulus |
| **Lokasi Basis Data** | `LokasiBasisDataTests.cs` | Pembuatan folder DB otomatis & lokasi bawaan Windows | ✅ Lulus |
| **Persistensi Lintas Sesi** | `PersistensiTests.cs` | Penyimpanan & pembacaan ulang nasabah, setoran, penarikan | ✅ Lulus |
| **Transaksi & Saldo** | `LayananTransaksiTests.cs` | Kredit saldo setoran, debit penarikan, validasi saldo kurang | ✅ Lulus |
| **Kalkulator Dampak Iklim** | `KalkulatorCO2eTests.cs` | Akumulasi CO2e multi-kategori & penolakan berat negatif | ✅ Lulus |
| **Laporan & Ekspor** | `LayananLaporanTests.cs` | Rekap bulanan `RekapBulanan`, ekspor CSV & PDF | ✅ Lulus |
| **Buku Tabungan** | `LayananBukuTabunganTests.cs` | Saldo berjalan per mutasi & ekspor buku tabungan PDF | ✅ Lulus |
| **Klasifikasi AI ONNX** | `KlasifikasiOnnxTests.cs` | Load `klasifikasi-sampah-v3.onnx`, pemetaan 12 label, inferensi gambar | ✅ Lulus |

**Ringkasan eksekusi terakhir (`dotnet test`):** **34/34 lulus** (`Failed: 0, Passed: 34, Skipped: 0`).

---

## 2. Pengujian Fungsional Aplikasi & Web API (*Black-Box*)

> *Catatan: Skenario UI desktop WPF diisi setelah antarmuka `Trashury/` selesai diintegrasikan; skenario API dapat langsung diuji melalui Swagger (`/swagger`).*

| No | Fitur / Skenario Uji | Input / Langkah | Hasil yang Diharapkan | Hasil Aktual | Status |
|---|---|---|---|---|---|
| 1 | Inisialisasi basis data otomatis | Jalankan aplikasi / `Trashury.Api` pada kondisi belum ada `trashury.db` | File `trashury.db` terbuat otomatis dan 5 kategori awal tersedia | File DB terbuat otomatis + 5 kategori ter-seed (`GET /api/kategori`) | ✅ Pass (API) / ⏳ UI |
| 2 | Tambah data nasabah | Masukkan nama nasabah baru lalu simpan | Nasabah tersimpan dengan saldo awal `Rp0` dan tampil di daftar | `POST /api/nasabah` mengembalikan HTTP 201 | ✅ Pass (API) / ⏳ UI |
| 3 | Klasifikasi sampah dari foto (ONNX) | Unggah foto sampah (mis. botol plastik / kaleng) | Model ONNX mengembalikan kategori terdekat dan nilai *confidence* | Botol PET → `Plastik PET` (`50,75%`), Kaleng → `Kaleng Aluminium` (`65,00%`) | ✅ Pass |
| 4 | Catat setoran multi-kategori | Pilih nasabah, isi 2 kategori sampah & berat > 0, simpan | Total rupiah & estimasi CO2e dihitung otomatis, saldo nasabah bertambah | Terverifikasi di `LayananTransaksiTests` & `POST /api/transaksi/setoran` | ✅ Pass (Core/API) / ⏳ UI |
| 5 | Proses penarikan saldo | Masukkan nominal penarikan ≤ saldo nasabah | Saldo berkurang sesuai nominal dan transaksi tercatat | Terverifikasi di `LayananTransaksiTests` & `POST /api/transaksi/penarikan` | ✅ Pass (Core/API) / ⏳ UI |
| 6 | Validasi penarikan melebihi saldo | Masukkan nominal penarikan > saldo nasabah | Sistem menolak penarikan dengan pesan kesalahan; saldo tidak berubah | Mengembalikan error validasi (`InvalidOperationException` / HTTP 400) | ✅ Pass (Core/API) / ⏳ UI |
| 7 | Cetak / unduh buku tabungan PDF | Pilih nasabah lalu pilih ekspor buku tabungan PDF | File PDF berisi riwayat setoran, penarikan, dan saldo berjalan | Terverifikasi di `LayananBukuTabunganTests` & endpoint `/buku-tabungan/pdf` | ✅ Pass (Core/API) / ⏳ UI |
| 8 | Ekspor laporan bulanan CSV & PDF | Pilih bulan & tahun lalu ekspor ke CSV dan PDF | File CSV dan PDF terunduh sesuai transaksi pada bulan tersebut | Terverifikasi di `LayananLaporanTests` & endpoint `/api/laporan/{tahun}/{bulan}/csv|pdf` | ✅ Pass (Core/API) / ⏳ UI |
