# TRASHURY

**Ubah sampah jadi harta, kelola bank sampah lebih rapi.**

Aplikasi kasir dan manajemen data lokal berbasis desktop untuk memodernisasi operasional bank sampah tingkat RW–kalurahan tanpa ketergantungan internet penuh, sekaligus menghitung otomatis dampak iklim dari sampah yang berhasil dialihkan dari TPA.

---

## Identitas Proyek

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
| Muhammad Afiq Mirza Choiruzan | 24/537942/TK/59646 | Ketua Kelompok — AI Engineer |
| Wangsit Nursyahada | 24/545092/TK/60594 | Backend Developer |
| Bintang Daneswara | 24/541599/TK/60084 | Frontend Developer |

---

## Modul 1 — Ide Aplikasi

**Nama Produk:** TRASHURY (*Trash* + *Treasury*)

**Jenis Produk:** Aplikasi desktop Windows (WPF + MVVM), *offline-first*, untuk pengelola/operator bank sampah lokal — bukan untuk konsumen akhir.

### Latar Belakang & Permasalahan

Mayoritas pengurus bank sampah tingkat RW–kalurahan masih mencatat transaksi warga secara manual di buku tulis atau spreadsheet Excel, sehingga rentan hilang dan salah hitung. Belum ada sistem otomatis untuk menghitung dampak lingkungan (estimasi CO2e yang dihindari) yang dibutuhkan saat pelaporan ke Dinas Lingkungan Hidup (DLH). Koneksi internet di lokasi juga sering tidak stabil, sehingga solusi berbasis web penuh tidak realistis.

### Ide / Solusi

Enam fitur utama:

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

## Modul 2 — Perancangan dengan UML

Perancangan berorientasi objek TRASHURY dituangkan dalam tiga diagram UML:

- **Use Case Diagram** — empat aktor (Operator, Pengurus, Nasabah, DLH) dan sepuluh use case, lengkap dengan relasi *generalization*, *include*, dan *extend*.
- **Activity Diagram** — alur *Catat Setoran Sampah* (termasuk cabang klasifikasi foto) dan alur *Proses Penarikan Saldo* (termasuk validasi saldo).
- **Class Diagram — Domain Model** — entitas dan relasinya tanpa detail implementasi, mencakup *generalization*, *composition*, *association* dengan *multiplicity*, dan *dependency*.

Seluruh diagram beserta penjelasan relasinya dapat dilihat di
[README repository »](https://github.com/bintangdanes/JunPro-Kelompok#modul-2--perancangan-perangkat-lunak-dengan-pendekatan-objek-uml)

---

## Modul 3 — Desain Class

![Class Diagram TRASHURY](class-diagram.png)

Desain class TRASHURY menerapkan empat konsep PBO:

- **Encapsulation** — field `_saldo` pada `Nasabah` bersifat `private` dengan property `get`-only; saldo hanya dapat diubah lewat `Kredit()` dan `Debit()` yang memvalidasi jumlah dan mencegah saldo minus.
- **Inheritance** — `SetoranSampah` dan `PenarikanSaldo` mewarisi class abstract `Transaksi`.
- **Polymorphism** — method `Terapkan(Nasabah)` di-`override` tiap subclass dengan perilaku berbeda.
- **Interface** — `IRepository<T>`, `IKalkulatorDampak`, dan `IKlasifikasiSampah` memisahkan kontrak dari implementasi.

Tabel struktur class dan analisis *coupling*, *cohesion*, serta *sufficiency* selengkapnya ada di
[README repository »](https://github.com/bintangdanes/JunPro-Kelompok#modul-3--desain-class)

---

## Status & Akses

Logika bisnis (`Models`, `Interfaces`, `Services`) sudah berjalan. Lapisan antarmuka WPF (`Views/`, `ViewModels/`), penyimpanan database, ekspor CSV/PDF, dan integrasi model ONNX masih dalam pengerjaan — rincian statusnya ada di README.

- **Repository:** <https://github.com/bintangdanes/JunPro-Kelompok>
- **Cara menjalankan:** [Petunjuk build & run »](https://github.com/bintangdanes/JunPro-Kelompok#menjalankan-proyek)
- **Akun demo:** tidak diperlukan — aplikasi berjalan sepenuhnya lokal (*offline-first*) dan belum menggunakan autentikasi.
