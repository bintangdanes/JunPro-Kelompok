# TRASHURY

**Ubah sampah jadi harta, kelola bank sampah lebih rapi.**

Aplikasi kasir dan manajemen data lokal berbasis desktop untuk memodernisasi operasional bank sampah tingkat RW–kalurahan tanpa ketergantungan internet penuh, sekaligus menghitung otomatis dampak iklim dari sampah yang berhasil dialihkan dari TPA.

| | |
|---|---|
| **Tema** | Climate Action |
| **Mata Kuliah** | Praktikum Junior Project TI |
| **Instansi** | Departemen Teknik Elektro dan Teknologi Informasi, Fakultas Teknik, Universitas Gadjah Mada |
| **Platform** | Aplikasi desktop Windows — C# / .NET 8 / WPF (MVVM) |
| **Kelompok** | Kelompok Keren |

## Anggota Kelompok

| Nama | NIM | Peran |
|---|---|---|
| Muhammad Afiq Mirza Choiruzan | 24/537942/TK/59646 | Ketua Kelompok — AI Engineer (model klasifikasi sampah ONNX) |
| Wangsit Nursyahada | 24/545092/TK/60594 | Backend Developer (`Models/`, `Interfaces/`, `Repositories/`, `Services/`) |
| Bintang Daneswara | 24/541599/TK/60084 | Frontend Developer (`Views/`, `ViewModels/`) |

> **Catatan.** Instruksi praktikum menyebut tiga peran baku: *software architect*, *backend developer*, dan *frontend developer*. Pemetaan di atas menyesuaikan kebutuhan produk, karena TRASHURY memuat komponen AI. Konfirmasikan ke asisten praktikum bila penamaan peran harus persis mengikuti instruksi.

---

## Modul 1 — Ide Aplikasi

**Nama Produk:** TRASHURY (*Trash* + *Treasury*)

**Jenis Produk:** Aplikasi desktop Windows (WPF + MVVM), *offline-first*, untuk pengelola/operator bank sampah lokal — bukan untuk konsumen akhir.

### Latar Belakang & Permasalahan

Mayoritas pengurus bank sampah tingkat RW–kalurahan masih mencatat transaksi warga secara manual di buku tulis atau spreadsheet Excel, sehingga rentan hilang dan salah hitung. Selain itu belum ada sistem otomatis untuk menghitung dampak lingkungan (estimasi CO2e yang dihindari) yang dibutuhkan saat pelaporan ke Dinas Lingkungan Hidup (DLH). Koneksi internet di lokasi juga sering tidak stabil, sehingga solusi berbasis web penuh tidak realistis.

### Ide / Solusi

Aplikasi manajemen bank sampah lokal berbasis desktop dengan enam fitur utama:

1. **Master Data** — pengelolaan data nasabah dan kategori sampah (harga per kg + faktor emisi).
2. **Transaksi Setoran** — pencatatan setoran multi-kategori dengan perhitungan nilai otomatis.
3. **Penarikan Saldo** — penarikan tabungan nasabah dan cetak riwayat buku tabungan.
4. **Dashboard Dampak Iklim** — estimasi CO2e yang dihindari dari sampah terkumpul.
5. **Laporan Bulanan** — rekap periodik dan ekspor CSV/PDF untuk pelaporan DLH.
6. **Klasifikasi Sampah berbasis AI** — inferensi model ONNX secara *offline* dari foto sampah.

### Relevansi dengan Tema Climate Action

Setiap kilogram sampah yang berhasil didaur ulang melalui bank sampah berarti emisi gas rumah kaca yang tidak jadi dilepaskan dari TPA. TRASHURY mengubah angka itu dari perkiraan kasar menjadi data terukur per transaksi, sehingga kontribusi iklim bank sampah tingkat kalurahan menjadi dapat dilaporkan dan diverifikasi.

### Analisis Kompetitor

| Solusi | Model | Keterbatasan |
|---|---|---|
| Smash.id, Rapel | Aplikasi mobile berbasis internet | Berorientasi konsumen/penjemputan, butuh koneksi, tidak mengelola pembukuan internal bank sampah |
| Buku tulis / Excel | Manual | Rentan hilang & salah hitung, tidak ada perhitungan dampak iklim |

**Diferensiasi TRASHURY:** *offline-first*, menyasar operator bank sampah (bukan konsumen), dan satu-satunya yang menghitung estimasi CO2e secara otomatis per transaksi.

---

## Modul 2 — Perancangan Perangkat Lunak dengan Pendekatan Objek (UML)

### 2.1 Use Case Diagram

**Aktor:**

| Aktor | Peran dalam sistem |
|---|---|
| **Operator Bank Sampah** | Aktor utama. Melayani nasabah di meja setoran: mencatat setoran, memproses penarikan, mencetak buku tabungan. |
| **Pengurus Bank Sampah** | *Generalization* dari Operator. Selain semua kewenangan operator, dapat mengelola master data dan menghasilkan laporan bulanan. |
| **Nasabah (Warga)** | Aktor tidak langsung. Menyetor sampah dan menarik saldo, dilayani lewat Operator. |
| **DLH** | Aktor eksternal sekunder. Penerima berkas laporan bulanan hasil ekspor. |

```mermaid
flowchart LR
    Nasabah(["Nasabah<br/>(Warga)"])
    Operator(["Operator<br/>Bank Sampah"])
    Pengurus(["Pengurus<br/>Bank Sampah"])
    DLH(["DLH"])

    subgraph SISTEM["Sistem TRASHURY"]
        UC1(["Kelola Data Nasabah"])
        UC2(["Kelola Kategori Sampah"])
        UC3(["Catat Setoran Sampah"])
        UC4(["Klasifikasi Sampah<br/>dari Foto"])
        UC5(["Hitung Nilai dan CO2e"])
        UC6(["Proses Penarikan Saldo"])
        UC7(["Cetak Buku Tabungan"])
        UC8(["Lihat Dashboard<br/>Dampak Iklim"])
        UC9(["Buat Laporan Bulanan"])
        UC10(["Ekspor Laporan CSV/PDF"])
    end

    Nasabah --- UC3
    Nasabah --- UC6
    Operator --- UC3
    Operator --- UC6
    Operator --- UC7
    Pengurus --- UC1
    Pengurus --- UC2
    Pengurus --- UC8
    Pengurus --- UC9
    UC10 --- DLH

    Pengurus -.->|generalization| Operator
    UC3 -.->|include| UC5
    UC6 -.->|include| UC5
    UC4 -.->|extend| UC3
    UC9 -.->|include| UC10
```

**Penjelasan relasi:**

- **Generalization** — `Pengurus` adalah spesialisasi dari `Operator`; seluruh use case operator otomatis dapat diakses pengurus.
- **Include** — `Catat Setoran Sampah` selalu memanggil `Hitung Nilai & CO2e`; tanpa langkah ini setoran tidak punya nominal. `Buat Laporan Bulanan` selalu menyertakan `Ekspor Laporan`.
- **Extend** — `Klasifikasi Sampah dari Foto` bersifat opsional: setoran tetap bisa dicatat manual bila operator sudah tahu kategorinya.
- **Association** — garis lurus antara aktor dan use case yang langsung dipicu aktor tersebut.

### 2.2 Activity Diagram

#### (a) Catat Setoran Sampah

```mermaid
flowchart TD
    A([Mulai]) --> B[Operator memilih nasabah]
    B --> C{Nasabah<br/>ditemukan?}
    C -->|Tidak| D[Tampilkan pesan<br/>nasabah tidak ditemukan]
    D --> B
    C -->|Ya| E[Timbang sampah<br/>per kategori]
    E --> F{Gunakan<br/>klasifikasi foto?}
    F -->|Ya| G[Ambil foto sampah]
    G --> H[Model ONNX mengembalikan<br/>kategori + confidence]
    H --> I[Operator konfirmasi kategori]
    F -->|Tidak| I
    I --> J[Tambah baris DetailSetoran]
    J --> K{Ada kategori<br/>lain?}
    K -->|Ya| E
    K -->|Tidak| L[Hitung subtotal<br/>dan total nominal]
    L --> M[Hitung estimasi CO2e]
    M --> N[Kredit saldo nasabah]
    N --> O[Simpan transaksi]
    O --> P[Cetak bukti setoran]
    P --> Q([Selesai])
```

#### (b) Proses Penarikan Saldo

```mermaid
flowchart TD
    A([Mulai]) --> B[Operator memilih nasabah]
    B --> C[Tampilkan saldo terkini]
    C --> D[Masukkan jumlah penarikan]
    D --> E{"Jumlah lebih dari 0?"}
    E -->|Tidak| F[Tampilkan pesan<br/>jumlah tidak valid]
    F --> D
    E -->|Ya| G{Saldo<br/>mencukupi?}
    G -->|Tidak| H[Tampilkan pesan<br/>saldo tidak mencukupi]
    H --> D
    G -->|Ya| I[Debit saldo nasabah]
    I --> J[Simpan transaksi penarikan]
    J --> K[Cetak riwayat buku tabungan]
    K --> L([Selesai])
```

### 2.3 Class Diagram — Domain Model

Sesuai instruksi Modul 2, diagram berikut adalah **domain model**: hanya entitas dan relasinya, tanpa detail atribut maupun method implementasi. Versi lengkap dengan atribut dan operasi ada di Modul 3.

```mermaid
classDiagram
    direction LR
    class Nasabah
    class Transaksi
    class SetoranSampah
    class PenarikanSaldo
    class DetailSetoran
    class KategoriSampah
    class HasilKlasifikasi

    Transaksi <|-- SetoranSampah : generalization
    Transaksi <|-- PenarikanSaldo : generalization
    Nasabah "1" -- "0..*" Transaksi : melakukan
    SetoranSampah "1" *-- "1..*" DetailSetoran : composition
    DetailSetoran "0..*" --> "1" KategoriSampah : association
    HasilKlasifikasi ..> KategoriSampah : dependency
```

**Penjelasan relasi:**

- **Generalization** — `SetoranSampah` dan `PenarikanSaldo` adalah spesialisasi dari `Transaksi`.
- **Composition** — `DetailSetoran` tidak punya makna di luar induknya; bila satu `SetoranSampah` dihapus, seluruh barisnya ikut hilang.
- **Association + multiplicity** — satu `Nasabah` dapat memiliki nol sampai banyak `Transaksi`; satu setoran memuat minimal satu `DetailSetoran`.
- **Dependency** — `HasilKlasifikasi` hanya mengacu pada nama kategori untuk dipetakan ke `KategoriSampah`, tanpa menyimpan objeknya.

---

## Modul 3 — Desain Class

### Class Diagram

![Class Diagram TRASHURY](docs/class-diagram.png)

### Struktur Class

| Class / Interface | Peran |
|---|---|
| `Nasabah` | Data nasabah dan saldo tabungan |
| `Transaksi` *(abstract)* | Induk seluruh transaksi |
| `SetoranSampah` | Subclass `Transaksi`, menambah saldo |
| `PenarikanSaldo` | Subclass `Transaksi`, mengurangi saldo |
| `DetailSetoran` | Baris rincian dalam satu setoran |
| `KategoriSampah` | Harga per kg dan faktor emisi CO2e |
| `HasilKlasifikasi` | Keluaran model klasifikasi foto |
| `IRepository<T>` | Kontrak akses data |
| `TrashuryDbContext` | Sesi basis data SQLite dan pemetaan seluruh entitas |
| `NasabahRepository` | Implementasi repository nasabah |
| `TransaksiRepository` | Implementasi repository transaksi |
| `KategoriSampahRepository` | Implementasi repository kategori sampah |
| `LayananTransaksi` | Alur catat setoran dan proses penarikan |
| `LayananLaporan` | Rekap laporan bulanan dan ekspor CSV |
| `IKalkulatorDampak` / `KalkulatorCO2e` | Perhitungan emisi yang dihindari |
| `IKlasifikasiSampah` | Kontrak klasifikasi sampah dari foto |
| `KlasifikasiOnnx` / `KlasifikasiDummy` | Implementasi model ONNX dan versi dummy untuk pengujian |

### Penerapan Konsep PBO

**Encapsulation.** Field `_saldo` pada `Nasabah` bersifat `private` dan property `Saldo` dibuat `get`-only. Satu-satunya jalan mengubah saldo adalah lewat `Kredit()` dan `Debit()`, yang sekaligus memvalidasi jumlah dan mencegah saldo minus.

**Inheritance.** `SetoranSampah` dan `PenarikanSaldo` mewarisi class abstract `Transaksi`, sehingga atribut `Id`, `Tanggal`, dan `Nominal` cukup ditulis satu kali.

**Polymorphism.** Method `Terapkan(Nasabah)` di-`override` tiap subclass dengan perilaku berbeda — `SetoranSampah` memanggil `Kredit()`, `PenarikanSaldo` memanggil `Debit()`. Kode pemanggil cukup menangani tipe `Transaksi` tanpa mengecek jenisnya satu per satu.

**Interface.** `IRepository<T>`, `IKalkulatorDampak`, dan `IKlasifikasiSampah` memisahkan kontrak dari implementasi, sehingga penyimpanan data maupun model klasifikasi bisa ditukar tanpa mengubah class layanan.

### Analisis Kualitas Class

**Coupling.** `LayananTransaksi` menerima repository dan kalkulator lewat constructor, bukan membuatnya sendiri, sehingga ikatan antar modul tetap longgar. Karena bergantung pada `IRepository<T>` dan bukan pada kelas konkretnya, penggantian penyimpanan in-memory menjadi SQLite tidak mengubah satu baris pun di dalam class layanan.

**Cohesion.** Tiap class mengurus satu urusan: `KategoriSampah` hanya menghitung nilai dan emisi, `LayananLaporan` hanya mengurus pelaporan.

**Sufficiency, completeness, primitiveness.** Operasi dipecah ke satuan terkecil, misalnya `KategoriSampah.HitungNilai()` dipanggil kembali oleh `DetailSetoran` dan `SetoranSampah` tanpa menduplikasi rumus.

---

## Modul 5 — Pembuatan Basis Data

### Pilihan DBMS

TRASHURY memakai **SQLite 3**, diakses lewat **Entity Framework Core 8**.

Pilihan ini mengikuti sifat produk yang *offline-first*. Bank sampah tingkat RW–kalurahan sering tidak punya koneksi internet yang stabil dan tidak punya petugas TI untuk merawat server basis data. SQLite menyimpan seluruh data dalam satu berkas di komputer operator, tanpa proses server yang harus dijalankan, tanpa instalasi terpisah, dan tanpa konfigurasi jaringan. Pencadangan cukup dengan menyalin satu berkas.

MySQL atau SQL Server tidak dipilih karena keduanya menuntut server yang harus dipasang dan dijaga tetap hidup — beban yang tidak realistis untuk pengurus bank sampah, dan bertentangan dengan tujuan aplikasi ini berjalan tanpa ketergantungan internet.

### Lokasi Berkas Basis Data

Basis data diletakkan di folder data aplikasi milik pengguna, bukan di folder instalasi, agar tidak ikut terhapus saat aplikasi diperbarui:

| Sistem | Lokasi |
|---|---|
| Windows | `%APPDATA%\Trashury\trashury.db` |
| macOS | `~/Library/Application Support/Trashury/trashury.db` |
| Linux | `~/.config/Trashury/trashury.db` |

### Entity Relationship Diagram

```mermaid
erDiagram
    NASABAH ||--o{ TRANSAKSI : melakukan
    TRANSAKSI ||--o{ DETAIL_SETORAN : memuat
    KATEGORI_SAMPAH ||--o{ DETAIL_SETORAN : mengklasifikasi

    NASABAH {
        int Id PK
        string Nama
        decimal Saldo
    }
    KATEGORI_SAMPAH {
        int Id PK
        string Nama UK
        decimal HargaPerKg
        decimal FaktorEmisiCO2e
    }
    TRANSAKSI {
        int Id PK
        int NasabahId FK
        datetime Tanggal
        decimal Nominal
        string JenisTransaksi
        decimal JumlahTarik "null untuk setoran"
    }
    DETAIL_SETORAN {
        int Id PK
        int SetoranSampahId FK
        int KategoriId FK
        decimal BeratKg
    }
```

### Struktur Tabel

Skema lengkap beserta indeks dan data awal tersedia di **[`docs/skema-basis-data.sql`](docs/skema-basis-data.sql)**.

#### `Nasabah`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER | Primary key, `AUTOINCREMENT` |
| `Nama` | TEXT | Wajib, maks. 120 karakter |
| `Saldo` | TEXT | Saldo tabungan; hanya berubah lewat transaksi |

#### `KategoriSampah`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER | Primary key, `AUTOINCREMENT` |
| `Nama` | TEXT | Wajib, maks. 80 karakter, **unik** |
| `HargaPerKg` | TEXT | Harga beli per kilogram |
| `FaktorEmisiCO2e` | TEXT | Kg CO2e yang dihindari per kg sampah |

#### `Transaksi`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER | Primary key, `AUTOINCREMENT` |
| `NasabahId` | INTEGER | Foreign key ke `Nasabah`, `ON DELETE RESTRICT` |
| `Tanggal` | TEXT | Waktu transaksi |
| `Nominal` | TEXT | Nilai rupiah transaksi |
| `JenisTransaksi` | TEXT | Diskriminator: `Setoran` atau `Penarikan` |
| `JumlahTarik` | TEXT | Hanya terisi pada penarikan, `NULL` untuk setoran |

#### `DetailSetoran`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER | Primary key, `AUTOINCREMENT` |
| `SetoranSampahId` | INTEGER | Foreign key ke `Transaksi`, `ON DELETE CASCADE` |
| `KategoriId` | INTEGER | Foreign key ke `KategoriSampah`, `ON DELETE RESTRICT` |
| `BeratKg` | TEXT | Berat sampah pada baris ini |

> **Catatan tipe kolom.** `decimal` dipetakan EF Core ke `TEXT` pada SQLite. Ini disengaja: SQLite tidak punya tipe desimal presisi tetap, dan menyimpannya sebagai `REAL` berisiko menimbulkan galat pembulatan pada nilai uang. Penyimpanan sebagai teks menjaga nilai rupiah tetap persis.

### Keputusan Perancangan

- **Table-per-hierarchy.** `SetoranSampah` dan `PenarikanSaldo` berbagi satu tabel `Transaksi`, dibedakan kolom diskriminator `JenisTransaksi`. Inheritance pada desain class Modul 3 terpetakan langsung ke skema tanpa tabel tambahan.
- **Enkapsulasi tetap utuh.** Kolom `Saldo` dipetakan ke field privat `_saldo`, bukan ke property publik. Basis data tetap dapat menulis nilainya, tetapi kode aplikasi tetap wajib lewat `Kredit()`/`Debit()` yang memvalidasi jumlah dan mencegah saldo minus.
- **`ON DELETE RESTRICT` pada `Nasabah` dan `KategoriSampah`.** Nasabah yang masih punya riwayat transaksi dan kategori yang pernah dipakai tidak bisa dihapus. Ini menjaga laporan historis ke DLH tetap utuh.
- **`ON DELETE CASCADE` pada `DetailSetoran`.** Baris rincian tidak bermakna tanpa setoran induknya, sesuai relasi *composition* pada domain model Modul 2.
- **Indeks.** `Transaksi.NasabahId` untuk cetak buku tabungan, `Transaksi.Tanggal` untuk laporan bulanan, dan indeks unik pada `KategoriSampah.Nama` agar kategori tidak terduplikasi.

### Membuat dan Memeriksa Basis Data

Skema dibuat lewat migrasi EF Core, bukan SQL manual, sehingga perubahan skema terlacak di Git bersama kodenya.

```bash
# Membuat basis data beserta seluruh tabel
dotnet run --project Trashury.DbTool -- migrate

# Mengisi data contoh untuk keperluan demo
dotnet run --project Trashury.DbTool -- seed --fresh

# Menampilkan isi seluruh tabel
dotnet run --project Trashury.DbTool -- info
```

### Bukti Basis Data dan Tabel

Daftar tabel yang terbentuk:

```
$ sqlite3 trashury.db ".tables"
DetailSetoran    KategoriSampah    Nasabah    Transaksi    __EFMigrationsHistory
```

Isi basis data setelah data contoh dimasukkan:

```
$ dotnet run --project Trashury.DbTool -- info

Berkas basis data : ~/Library/Application Support/Trashury/trashury.db
Ukuran            : 53.248 byte
Migrasi diterapkan: 20260921090656_InitialCreate

Jumlah baris per tabel
  KategoriSampah : 5
  Nasabah        : 3
  Transaksi      : 4
  DetailSetoran  : 5

Nasabah
  [1] Budi Santoso     saldo Rp14.000
  [2] Siti Aminah      saldo Rp18.000
  [3] Joko Prasetyo    saldo Rp20.000

Transaksi
  [1] 2026-09-21 nasabah 1 Setoran   Rp   24.000  (Plastik PET 3,5 kg, Kertas & Kardus 5 kg)
  [2] 2026-09-21 nasabah 2 Setoran   Rp   18.000  (Kaleng Aluminium 1,2 kg)
  [3] 2026-09-21 nasabah 3 Setoran   Rp   20.000  (Kertas & Kardus 8 kg, Plastik PET 1 kg)
  [4] 2026-09-21 nasabah 1 Penarikan Rp   10.000
```

Angka di atas dapat ditelusuri: Budi menyetor 3,5 kg PET (Rp14.000) dan 5 kg kardus (Rp10.000) sehingga saldonya Rp24.000, lalu menarik Rp10.000 dan menyisakan Rp14.000. Tabel `__EFMigrationsHistory` adalah tabel bawaan EF Core yang mencatat migrasi mana saja yang sudah diterapkan.

### Mengubah Skema di Kemudian Hari

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add NamaPerubahan --project Trashury.Core --output-dir Data/Migrations
```

Migrasi diterapkan otomatis lewat `Database.Migrate()` saat aplikasi dijalankan, jadi pengguna tidak perlu melakukan langkah manual apa pun.

---

## Status Implementasi

Per commit terakhir, berikut kondisi nyata kode di repo ini:

| Komponen | Status | Keterangan |
|---|---|---|
| `Models/` (7 class) | ✅ Selesai | Enkapsulasi saldo, inheritance, polymorphism sudah berjalan |
| `Interfaces/` (3 interface) | ✅ Selesai | Kontrak repository, kalkulator dampak, klasifikasi |
| `TrashuryDbContext` + migrasi | ✅ Selesai | SQLite, 4 tabel, seed kategori bawaan |
| `NasabahRepository`, `TransaksiRepository`, `KategoriSampahRepository` | ✅ Selesai | Menulis ke SQLite; `SimpanPerubahan()` memanggil `SaveChanges()` |
| `KalkulatorCO2e` | ✅ Selesai | Menjumlahkan CO2e seluruh baris setoran |
| `LayananTransaksi` | ✅ Selesai | `CatatSetoran()` dan `ProsesPenarikan()` tersimpan permanen |
| `Trashury.Tests` | ✅ Selesai | 15 test lulus, termasuk uji persistensi lintas sesi |
| `Trashury.DbTool` | ✅ Selesai | Perkakas migrate / seed / info untuk demo dan pemeriksaan |
| `LayananLaporan.LaporanBulanan()` | ⚠️ Sementara | Berjalan, tetapi masih mengembalikan `IEnumerable<object>` — perlu tipe DTO khusus |
| `LayananLaporan.EksporCsv()` | ❌ Belum | Masih `NotImplementedException` |
| `KlasifikasiOnnx` | ❌ Belum | Masih `NotImplementedException`; sementara pakai `KlasifikasiDummy` |
| `Views/`, `ViewModels/` | ❌ Belum | Folder masih kosong, belum ada `App.xaml`/`MainWindow.xaml` |

### Rencana Berikutnya

1. Buat DTO laporan menggantikan `IEnumerable<object>`, lalu implementasikan `EksporCsv()` serta ekspor PDF.
2. Integrasikan runtime ONNX untuk `KlasifikasiOnnx`.
3. Bangun lapisan `Views/` + `ViewModels/` (WPF MVVM) beserta entry point `App.xaml`, termasuk Dashboard Dampak Iklim.

---

## Menjalankan Proyek

### Prasyarat

- **.NET SDK 8.0** atau lebih baru.
- **Windows** — hanya diperlukan untuk menjalankan antarmuka WPF. Pengembangan dan pengujian `Trashury.Core` dapat dilakukan di macOS maupun Linux.
- Visual Studio 2022 atau Visual Studio Code dengan ekstensi C# Dev Kit.

### Langkah

```bash
# 1. Clone repository
git clone https://github.com/bintangdanes/JunPro-Kelompok.git
cd JunPro-Kelompok

# 2. Pulihkan dependensi dan build seluruh solusi
dotnet restore
dotnet build

# 3. Jalankan pengujian (berjalan di Windows, macOS, maupun Linux)
dotnet test

# 4. Siapkan basis data beserta data contoh untuk demo
dotnet run --project Trashury.DbTool -- seed --fresh
dotnet run --project Trashury.DbTool -- info
```

> **Status saat ini.** `Trashury.Core` dan `Trashury.Tests` berjalan penuh di semua sistem operasi — `dotnet test` meluluskan 15 pengujian termasuk uji persistensi SQLite. Project `Trashury` (WPF) belum memiliki `App.xaml`, sehingga masih ter-*build* sebagai *class library* dan `dotnet run` belum tersedia. Perintah tersebut akan aktif setelah entry point WPF ditambahkan.

### Informasi Akses Demo

- **Repository:** https://github.com/bintangdanes/JunPro-Kelompok
- **Halaman dokumentasi (GitHub Pages):** https://bintangdanes.github.io/JunPro-Kelompok/
- **Akun demo:** aplikasi berjalan sepenuhnya lokal (*offline-first*) dan belum menggunakan autentikasi, sehingga tidak ada kredensial yang perlu dibagikan.
- **Data awal:** lima kategori sampah ter-*seed* otomatis saat basis data pertama kali dibuat.
- **Data contoh demo:** jalankan `dotnet run --project Trashury.DbTool -- seed --fresh` untuk mengisi 3 nasabah dan 4 transaksi, lalu `-- info` untuk menampilkan isinya.

---

## Struktur Repository

```
JunPro-Kelompok/
├── Trashury.Core/              # net8.0 — logika bisnis, lintas platform
│   ├── Models/                 # Entitas domain (Nasabah, Transaksi, ...)
│   ├── Interfaces/             # IRepository<T>, IKalkulatorDampak, IKlasifikasiSampah
│   ├── Data/                   # TrashuryDbContext, factory, dan migrasi EF Core
│   ├── Repositories/           # Implementasi akses data di atas SQLite
│   └── Services/               # Transaksi, laporan, CO2e, klasifikasi
├── Trashury/                   # net8.0-windows — aplikasi WPF
│   ├── Views/                  # (belum diisi)
│   └── ViewModels/             # (belum diisi)
├── Trashury.Tests/             # net8.0 — pengujian xUnit
├── Trashury.DbTool/            # net8.0 — perkakas basis data (migrate/seed/info)
├── docs/                       # GitHub Pages, class diagram, skema basis data
├── Trashury.sln
└── README.md
```

Pemisahan ini membuat seluruh logika bisnis bebas dari ketergantungan WPF, sehingga dapat dikembangkan dan diuji di sistem operasi mana pun sementara antarmuka tetap khusus Windows.

## Alur Kerja Git

Tiap anggota bekerja di branch bernomor NIM masing-masing, lalu digabungkan ke `main` lewat Pull Request.

| Branch | Pemilik |
|---|---|
| `main` | Branch integrasi — hasil merge seluruh anggota |
| `537942` | Muhammad Afiq Mirza Choiruzan |
| `545092` | Wangsit Nursyahada |
| `541599` | Bintang Daneswara |
