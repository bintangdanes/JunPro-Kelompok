# Panduan Penggunaan & Konfigurasi Basis Data — TRASHURY

Dokumen ini menjelaskan cara menjalankan, mengonfigurasi basis data lokal, serta menggunakan aplikasi dan layanan Web API **TRASHURY** (*Offline-First Community Waste Bank System*).

---

## 1. Prasyarat & Instalasi

- **Sistem Operasi:** Windows 10/11 (untuk menjalankan aplikasi desktop WPF). Komponen `Trashury.Core`, `Trashury.Api`, `Trashury.DbTool`, dan `Trashury.Tests` berjalan lintas platform (Windows, macOS, Linux).
- **Runtime:** Tidak perlu menginstal .NET secara terpisah jika menggunakan paket rilis *self-contained*. Tersedia dua opsi kemasan aplikasi:
  1. **Opsi 1 — Berkas Tunggal `Trashury.exe` (*Single-File Executable*, `publish/SingleFile/Trashury.exe`):** Seluruh runtime .NET, library native (SQLite, ONNX Runtime, OpenCV), dan model AI sudah dibundel menjadi **1 berkas `.exe` mandiri** (~155 MB) yang dapat langsung diunduh dan dijalankan dengan klik ganda tanpa ekstraksi `.zip`.
  2. **Opsi 2 — Paket Folder `publish.zip` (`publish/Trashury/`):** Berisi `Trashury.exe` beserta folder `Assets/Models/` dan berkas `.dll` pendukung (ekstrak seluruh isi `.zip` ke satu folder lalu jalankan `Trashury.exe`).
- **Koneksi Internet:** Tidak diperlukan saat aplikasi dijalankan (*offline-first*); model AI ONNX (`klasifikasi-sampah-v3.onnx`) sudah tertanam di dalam paket aplikasi.

### Panduan Singkat Menjalankan `Trashury.exe` (Single-File)
1. Unduh atau salin 1 berkas **`Trashury.exe`** (~155 MB dari folder `publish/SingleFile/`) ke komputer Windows 10/11 (boleh ditaruh di `Desktop`, `Downloads`, atau folder mana saja sendirian tanpa folder tambahan).
2. Klik ganda **`Trashury.exe`**.
3. Jika Windows SmartScreen menampilkan *"Windows protected your PC"*, klik **More info** → **Run anyway**.
4. Tunggu sekitar 3–5 detik pada pembukaan pertama (aplikasi secara otomatis menyiapkan runtime .NET, SQLite, dan model AI ONNX di latar belakang), lalu jendela utama **TRASHURY** langsung siap digunakan.

---

## 2. Konfigurasi Basis Data (SQLite)

TRASHURY menggunakan **SQLite lokal** tanpa server basis data eksternal sehingga operator tidak perlu melakukan instalasi MySQL/PostgreSQL secara manual.

### a. Lokasi & Migrasi Otomatis
- Saat aplikasi atau `Trashury.Api` pertama kali dijalankan, `Database.Migrate()` otomatis membuat berkas basis data SQLite (`trashury.db`) di folder data aplikasi pengguna (`%APPDATA%\Trashury\trashury.db` pada Windows) beserta **5 kategori sampah awal**:
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

## 3. Cara Menggunakan Aplikasi (Login RBAC & Alur Operator)

Saat aplikasi dibuka, masuk menggunakan akun bawaan (password disimpan ter-hash PBKDF2-SHA256 di SQLite):
- **Pengurus (akses penuh):** `pengurus` / `pengurus123`
- **Operator (meja setoran):** `operator` / `operator123`

1. **Kelola Master Data & Backup/Restore DB (Khusus Pengurus)**
   - Tambah, ubah, hapus, atau reset data nasabah dan kategori sampah (harga per kg dan faktor reduksi emisi CO2e), serta gunakan kotak **Cari (Nama / ID)** untuk memfilter daftar nasabah secara langsung.
2. **Catat Setoran Sampah (+ Klasifikasi AI & Kamera Opsional)**
   - Pilih nasabah yang menyetor sampah.
   - *(Opsional)* Klik **Pilih Foto...** atau **Ambil Kamera** (dengan jendela pratinjau webcam langsung) untuk mengklasifikasikan foto sampah secara *offline* menggunakan model ONNX (`KlasifikasiOnnx`). Panel AI menampilkan pratinjau foto, rekomendasi utama, **Top-3 kandidat**, serta peringatan otomatis bila terdeteksi pecahan kaca (`Glass Shards`) atau sampah organik (`food waste`).
   - Masukkan berat (kg) per kategori lalu klik **+ Tambah ke Daftar Setoran** (gunakan tombol **Hapus** pada baris tabel jika ingin membatalkan item).
   - Klik **Simpan Transaksi Setoran** untuk menyimpan transaksi, menambah saldo nasabah, dan mencetak **Bukti Setoran (PDF)** bila diperlukan.
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
