# Panduan Penggunaan & Konfigurasi Basis Data — TRASHURY

Dokumen ini menjelaskan cara menjalankan, mengonfigurasi basis data lokal, serta menggunakan aplikasi dan layanan Web API **TRASHURY** (*Offline-First Community Waste Bank System*).

---

## 1. Prasyarat & Instalasi

- **Sistem Operasi:** Windows 10/11 (untuk menjalankan aplikasi desktop WPF). Komponen `Trashury.Core`, `Trashury.Api`, `Trashury.DbTool`, dan `Trashury.Tests` berjalan lintas platform (Windows, macOS, Linux).
- **Runtime:** .NET 8.0 SDK atau .NET 8.0 Desktop Runtime (tidak perlu jika memakai rilis *self-contained* `.exe`).
- **Koneksi Internet:** Tidak diperlukan saat aplikasi dijalankan (*offline-first*); model AI ONNX (`klasifikasi-sampah-v3.onnx`) sudah tertanam di dalam paket aplikasi (`Assets/Models/`).

---

## 2. Konfigurasi Basis Data (SQLite)

TRASHURY menggunakan **SQLite lokal** tanpa server basis data eksternal sehingga operator tidak perlu melakukan instalasi MySQL/PostgreSQL secara manual.

### a. Lokasi & Migrasi Otomatis
- Saat aplikasi atau `Trashury.Api` pertama kali dijalankan, `Database.Migrate()` otomatis membuat berkas basis data SQLite (`trashury.db`) di folder data aplikasi pengguna (`LocalApplicationData\Trashury\trashury.db` pada Windows) beserta **5 kategori sampah awal**:
  1. `Plastik PET` (Rp4.000/kg — 1,5 kg CO2e/kg)
  2. `Kertas & Kardus` (Rp2.000/kg — 0,9 kg CO2e/kg)
  3. `Kaleng Aluminium` (Rp15.000/kg — 8,1 kg CO2e/kg)
  4. `Kaca` (Rp500/kg — 0,3 kg CO2e/kg)
  5. `Organik` (Rp300/kg — 0,25 kg CO2e/kg)

### b. Lokasi Basis Data Kustom (Khusus Web API)
Untuk mengarahkan `Trashury.Api` ke lokasi berkas SQLite tertentu, atur *environment variable* `TRASHURY_DB_PATH` sebelum menjalankan server:

```powershell
$env:TRASHURY_DB_PATH = "C:\data\trashury-demo.db"
dotnet run --project Trashury.Api
```

### c. Perkakas Pengisian Data Contoh (`Trashury.DbTool`)
Untuk kebutuhan demo atau pengujian awal, gunakan `Trashury.DbTool`:

```bash
# Isi ulang basis data dengan data contoh (3 nasabah & 4 transaksi)
dotnet run --project Trashury.DbTool -- seed --fresh

# Tampilkan isi data nasabah, kategori, dan transaksi saat ini
dotnet run --project Trashury.DbTool -- info
```

---

## 3. Cara Menggunakan Aplikasi (Alur Operator)

1. **Kelola Master Data (Nasabah & Kategori Sampah)**
   - Daftarkan nasabah baru (nama) dan sesuaikan daftar kategori sampah (harga per kg dan faktor reduksi emisi CO2e) sesuai ketentuan pengepul/DLH setempat.
2. **Catat Setoran Sampah (+ Klasifikasi AI Opsional)**
   - Pilih nasabah yang menyetor sampah.
   - *(Opsional)* Unggah foto sampah untuk diklasifikasikan otomatis secara *offline* oleh model ONNX (`KlasifikasiOnnx`), lalu konfirmasi kategori yang sesuai.
   - Masukkan berat (kg) per kategori. Sistem otomatis menghitung subtotal rupiah, total nominal setoran, estimasi CO2e yang dihindari, dan menambah saldo nasabah.
3. **Proses Penarikan Saldo & Cetak Buku Tabungan**
   - Pilih nasabah dan masukkan nominal penarikan (> 0 dan tidak melebihi saldo terkini).
   - Cetak/ekspor riwayat mutasi buku tabungan nasabah ke format PDF (`LayananBukuTabungan`).
4. **Pantau Dampak Iklim & Ekspor Laporan Bulanan**
   - Lihat akumulasi berat sampah terkelola dan estimasi reduksi emisi CO2e.
   - Ekspor rekap bulanan ke format **CSV** (standar maupun format Excel Indonesia) atau **PDF** untuk pelaporan ke Dinas Lingkungan Hidup (DLH).

---

## 4. Menjalankan Layanan Web API (`Trashury.Api`)

Selain aplikasi desktop, TRASHURY menyediakan layanan REST Web API beserta **Swagger UI**:

```bash
dotnet run --project Trashury.Api
```

Buka alamat yang tampil di terminal lalu akses `/swagger` di peramban. Endpoint utama yang tersedia:

| Method | Endpoint | Fungsi |
|---|---|---|
| `GET` | `/api/kategori` | Daftar kategori sampah, harga, & faktor CO2e |
| `GET` | `/api/nasabah` | Daftar seluruh nasabah beserta saldo |
| `POST` | `/api/nasabah` | Tambah nasabah baru (`{ "nama": "..." }`) |
| `GET` | `/api/transaksi` | Riwayat transaksi setoran dan penarikan |
| `POST` | `/api/transaksi/setoran` | Catat setoran multi-kategori |
| `POST` | `/api/transaksi/penarikan` | Proses penarikan saldo nasabah |
| `GET` | `/api/dampak` | Ringkasan total berat & estimasi CO2e dihindari |
| `GET` | `/api/laporan/{tahun}/{bulan}` | Rekap bulanan JSON |
| `GET` | `/api/laporan/{tahun}/{bulan}/csv` | Unduh laporan bulanan CSV (`?format=standar` opsional) |
| `GET` | `/api/laporan/{tahun}/{bulan}/pdf` | Unduh laporan bulanan PDF |
| `GET` | `/api/nasabah/{id}/buku-tabungan` | Riwayat buku tabungan nasabah (JSON) |
| `GET` | `/api/nasabah/{id}/buku-tabungan/pdf` | Unduh buku tabungan nasabah (PDF) |
| `POST` | `/api/klasifikasi` | Unggah gambar (`multipart/form-data`) untuk klasifikasi AI ONNX |
