using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Trashury.Data;
using Trashury.Interfaces;
using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;

namespace Trashury.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly TrashuryDbContext _db;
    private readonly NasabahRepository _nasabahRepo;
    private readonly KategoriSampahRepository _kategoriRepo;
    private readonly TransaksiRepository _transaksiRepo;
    private readonly LayananTransaksi _layananTransaksi;
    private readonly LayananLaporan _layananLaporan;
    private readonly LayananBukuTabungan _layananBukuTabungan;
    private readonly IKlasifikasiSampah _klasifikasiSampah;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // --- Peran Pengguna (Operator vs Pengurus) ---
    private bool _isPengurus = true;
    public bool IsPengurus
    {
        get => _isPengurus;
        set
        {
            _isPengurus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NamaPeranAktif));
        }
    }
    public string NamaPeranAktif => IsPengurus ? "Mode: Pengurus Bank Sampah (Akses Penuh)" : "Mode: Operator Meja Setoran";

    // --- Koleksi Data ---
    public ObservableCollection<Nasabah> DaftarNasabah { get; } = new();
    public ObservableCollection<KategoriSampah> DaftarKategori { get; } = new();
    public ObservableCollection<DetailSetoran> KeranjangSetoran { get; } = new();
    public ObservableCollection<RekapKategori> RincianLaporan { get; } = new();

    // --- Form Master Nasabah ---
    private string _namaNasabahBaru = string.Empty;
    public string NamaNasabahBaru { get => _namaNasabahBaru; set { _namaNasabahBaru = value; OnPropertyChanged(); } }

    // --- Form Master Kategori ---
    private string _namaKategoriBaru = string.Empty;
    public string NamaKategoriBaru { get => _namaKategoriBaru; set { _namaKategoriBaru = value; OnPropertyChanged(); } }

    private string _hargaKategoriBaru = "2000";
    public string HargaKategoriBaru { get => _hargaKategoriBaru; set { _hargaKategoriBaru = value; OnPropertyChanged(); } }

    private string _emisiKategoriBaru = "1.0";
    public string EmisiKategoriBaru { get => _emisiKategoriBaru; set { _emisiKategoriBaru = value; OnPropertyChanged(); } }

    // --- Form Transaksi Setoran ---
    private Nasabah? _nasabahSetoranTerpilih;
    public Nasabah? NasabahSetoranTerpilih { get => _nasabahSetoranTerpilih; set { _nasabahSetoranTerpilih = value; OnPropertyChanged(); } }

    private KategoriSampah? _kategoriSetoranTerpilih;
    public KategoriSampah? KategoriSetoranTerpilih { get => _kategoriSetoranTerpilih; set { _kategoriSetoranTerpilih = value; OnPropertyChanged(); } }

    private string _beratSetoranInput = "1.5";
    public string BeratSetoranInput { get => _beratSetoranInput; set { _beratSetoranInput = value; OnPropertyChanged(); } }

    private string _hasilPrediksiAi = "Belum ada foto sampah diklasifikasi.";
    public string HasilPrediksiAi { get => _hasilPrediksiAi; set { _hasilPrediksiAi = value; OnPropertyChanged(); } }

    public decimal TotalNominalKeranjang => KeranjangSetoran.Sum(d => d.Subtotal);
    public decimal TotalEmisiKeranjang => KeranjangSetoran.Sum(d => d.Kategori.HitungCO2e(d.BeratKg));

    // --- Form Penarikan & Buku Tabungan ---
    private Nasabah? _nasabahTarikTerpilih;
    public Nasabah? NasabahTarikTerpilih
    {
        get => _nasabahTarikTerpilih;
        set
        {
            _nasabahTarikTerpilih = value;
            OnPropertyChanged();
            MuatBukuTabungan();
        }
    }

    private string _nominalTarikInput = "5000";
    public string NominalTarikInput { get => _nominalTarikInput; set { _nominalTarikInput = value; OnPropertyChanged(); } }

    private string _ringkasanBukuTabungan = "Pilih nasabah untuk melihat riwayat buku tabungan.";
    public string RingkasanBukuTabungan { get => _ringkasanBukuTabungan; set { _ringkasanBukuTabungan = value; OnPropertyChanged(); } }

    // --- Dashboard Iklim & Laporan Bulanan ---
    private int _bulanLaporan = DateTime.Now.Month;
    public int BulanLaporan { get => _bulanLaporan; set { _bulanLaporan = value; OnPropertyChanged(); } }

    private int _tahunLaporan = DateTime.Now.Year;
    public int TahunLaporan { get => _tahunLaporan; set { _tahunLaporan = value; OnPropertyChanged(); } }

    private RekapBulanan? _rekapSaatIni;
    public RekapBulanan? RekapSaatIni
    {
        get => _rekapSaatIni;
        set
        {
            _rekapSaatIni = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalBeratDashboard));
            OnPropertyChanged(nameof(TotalEmisiDashboard));
            OnPropertyChanged(nameof(TotalSetoranDashboard));
            OnPropertyChanged(nameof(TotalPenarikanDashboard));
        }
    }

    public string TotalBeratDashboard => $"{RekapSaatIni?.TotalBeratKg ?? 0:N2} kg";
    public string TotalEmisiDashboard => $"{RekapSaatIni?.TotalCO2eKg ?? 0:N2} kg CO2e";
    public string TotalSetoranDashboard => $"Rp {RekapSaatIni?.TotalNilaiSetoran ?? 0:N0}";
    public string TotalPenarikanDashboard => $"Rp {RekapSaatIni?.TotalPenarikan ?? 0:N0}";

    // --- Commands ---
    public ICommand GantiPeranCommand { get; }
    public ICommand TambahNasabahCommand { get; }
    public ICommand TambahKategoriCommand { get; }
    public ICommand KlasifikasiFotoCommand { get; }
    public ICommand TambahBarisSetoranCommand { get; }
    public ICommand HapusBarisSetoranCommand { get; }
    public ICommand SimpanSetoranCommand { get; }
    public ICommand ProsesPenarikanCommand { get; }
    public ICommand CetakBukuTabunganPdfCommand { get; }
    public ICommand MuatLaporanCommand { get; }
    public ICommand EksporCsvCommand { get; }
    public ICommand EksporPdfCommand { get; }

    public MainViewModel()
    {
        _db = TrashuryDbContextFactory.Buat();
        _nasabahRepo = new NasabahRepository(_db);
        _kategoriRepo = new KategoriSampahRepository(_db);
        _transaksiRepo = new TransaksiRepository(_db);
        _layananTransaksi = new LayananTransaksi(_nasabahRepo, _transaksiRepo);
        _layananLaporan = new LayananLaporan(_transaksiRepo);
        _layananBukuTabungan = new LayananBukuTabungan(_nasabahRepo, _transaksiRepo);

        try
        {
            _klasifikasiSampah = new KlasifikasiOnnx();
        }
        catch
        {
            _klasifikasiSampah = new KlasifikasiDummy();
        }

        GantiPeranCommand = new RelayCommand(_ => IsPengurus = !IsPengurus);
        TambahNasabahCommand = new RelayCommand(_ => TambahNasabah());
        TambahKategoriCommand = new RelayCommand(_ => TambahKategori());
        KlasifikasiFotoCommand = new RelayCommand(_ => KlasifikasiFoto());
        TambahBarisSetoranCommand = new RelayCommand(_ => TambahBarisSetoran());
        HapusBarisSetoranCommand = new RelayCommand(obj => { if (obj is DetailSetoran d) { KeranjangSetoran.Remove(d); RefreshTotalKeranjang(); } });
        SimpanSetoranCommand = new RelayCommand(_ => SimpanSetoran());
        ProsesPenarikanCommand = new RelayCommand(_ => ProsesPenarikan());
        CetakBukuTabunganPdfCommand = new RelayCommand(_ => CetakBukuTabunganPdf());
        MuatLaporanCommand = new RelayCommand(_ => MuatLaporan());
        EksporCsvCommand = new RelayCommand(_ => EksporCsv());
        EksporPdfCommand = new RelayCommand(_ => EksporPdf());

        MuatSemuaData();
    }

    private void MuatSemuaData()
    {
        var idSetor = NasabahSetoranTerpilih?.Id;
        var idTarik = NasabahTarikTerpilih?.Id;

        DaftarNasabah.Clear();
        foreach (var n in _nasabahRepo.GetAll())
            DaftarNasabah.Add(n);

        DaftarKategori.Clear();
        foreach (var k in _kategoriRepo.GetAll())
            DaftarKategori.Add(k);

        NasabahSetoranTerpilih = DaftarNasabah.FirstOrDefault(n => n.Id == idSetor) ?? DaftarNasabah.FirstOrDefault();
        NasabahTarikTerpilih = DaftarNasabah.FirstOrDefault(n => n.Id == idTarik) ?? DaftarNasabah.FirstOrDefault();
        KategoriSetoranTerpilih ??= DaftarKategori.FirstOrDefault();

        MuatLaporan();
    }

    private void TambahNasabah()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NamaNasabahBaru))
            {
                MessageBox.Show("Nama nasabah tidak boleh kosong.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var nasabah = new Nasabah(NamaNasabahBaru.Trim());
            _nasabahRepo.Tambah(nasabah);
            _nasabahRepo.SimpanPerubahan();
            NamaNasabahBaru = string.Empty;
            MuatSemuaData();
            MessageBox.Show("Nasabah baru berhasil ditambahkan.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gagal Tambah Nasabah", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TambahKategori()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NamaKategoriBaru) ||
                !decimal.TryParse(HargaKategoriBaru, out decimal harga) ||
                !decimal.TryParse(EmisiKategoriBaru, out decimal emisi))
            {
                MessageBox.Show("Pastikan nama, harga per kg, dan faktor emisi CO2e diisi dengan benar.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var kat = new KategoriSampah(NamaKategoriBaru.Trim(), harga, emisi);
            _kategoriRepo.Tambah(kat);
            _kategoriRepo.SimpanPerubahan();
            NamaKategoriBaru = string.Empty;
            MuatSemuaData();
            MessageBox.Show("Kategori sampah berhasil disimpan.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gagal Tambah Kategori", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void KlasifikasiFoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Pilih Foto Sampah untuk Klasifikasi AI",
            Filter = "File Gambar|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(dialog.FileName);
                var hasil = _klasifikasiSampah.Klasifikasi(bytes);
                HasilPrediksiAi = $"Prediksi AI: {hasil.KategoriTerdeteksi} (Confidence: {hasil.Confidence:P1})";

                var cocok = DaftarKategori.FirstOrDefault(k =>
                    k.Nama.Contains(hasil.KategoriTerdeteksi, StringComparison.OrdinalIgnoreCase) ||
                    hasil.KategoriTerdeteksi.Contains(k.Nama, StringComparison.OrdinalIgnoreCase));
                if (cocok != null)
                    KategoriSetoranTerpilih = cocok;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menjalankan klasifikasi AI: {ex.Message}", "Error AI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void TambahBarisSetoran()
    {
        if (KategoriSetoranTerpilih == null)
        {
            MessageBox.Show("Pilih kategori sampah terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(BeratSetoranInput.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal berat) || berat <= 0)
        {
            MessageBox.Show("Masukkan berat sampah (kg) lebih dari 0.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        KeranjangSetoran.Add(new DetailSetoran(KategoriSetoranTerpilih, berat));
        RefreshTotalKeranjang();
    }

    private void RefreshTotalKeranjang()
    {
        OnPropertyChanged(nameof(TotalNominalKeranjang));
        OnPropertyChanged(nameof(TotalEmisiKeranjang));
    }

    private void SimpanSetoran()
    {
        try
        {
            if (NasabahSetoranTerpilih == null)
            {
                MessageBox.Show("Pilih nasabah terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (KeranjangSetoran.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal satu baris rincian sampah.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var transaksi = _layananTransaksi.CatatSetoran(NasabahSetoranTerpilih.Id, KeranjangSetoran.ToList());
            KeranjangSetoran.Clear();
            RefreshTotalKeranjang();
            MuatSemuaData();
            MessageBox.Show($"Setoran berhasil dicatat!\nTotal Nominal: Rp {transaksi.Nominal:N0}", "Transaksi Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gagal Mencatat Setoran", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProsesPenarikan()
    {
        try
        {
            if (NasabahTarikTerpilih == null)
            {
                MessageBox.Show("Pilih nasabah terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(NominalTarikInput, out decimal jumlah) || jumlah <= 0)
            {
                MessageBox.Show("Masukkan jumlah penarikan lebih dari 0.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _layananTransaksi.ProsesPenarikan(NasabahTarikTerpilih.Id, jumlah);
            MuatSemuaData();
            MessageBox.Show($"Penarikan sebesar Rp {jumlah:N0} berhasil diproses.", "Penarikan Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Penarikan Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MuatBukuTabungan()
    {
        if (NasabahTarikTerpilih == null)
        {
            RingkasanBukuTabungan = "Pilih nasabah untuk melihat buku tabungan.";
            return;
        }
        try
        {
            var buku = _layananBukuTabungan.Susun(NasabahTarikTerpilih.Id);
            var barisTeks = buku.Baris.Select(b =>
                $"{b.Tanggal:dd/MM/yyyy} | {b.Keterangan,-25} | Masuk: Rp {b.Kredit,8:N0} | Keluar: Rp {b.Debit,8:N0} | Saldo: Rp {b.Saldo,9:N0}");
            RingkasanBukuTabungan = $"Nasabah: {buku.NamaNasabah} (ID: {buku.NasabahId})\n" +
                                    $"Total Masuk: Rp {buku.TotalKredit:N0} | Total Keluar: Rp {buku.TotalDebit:N0} | Saldo Akhir: Rp {buku.SaldoAkhir:N0}\n" +
                                    new string('-', 85) + "\n" +
                                    (buku.Baris.Count == 0 ? "Belum ada riwayat transaksi." : string.Join("\n", barisTeks));
        }
        catch (Exception ex)
        {
            RingkasanBukuTabungan = $"Gagal memuat buku tabungan: {ex.Message}";
        }
    }

    private void CetakBukuTabunganPdf()
    {
        if (NasabahTarikTerpilih == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Simpan PDF Buku Tabungan",
            Filter = "PDF Document|*.pdf",
            FileName = $"BukuTabungan_{NasabahTarikTerpilih.Nama.Replace(' ', '_')}.pdf"
        };
        if (dialog.ShowDialog() == true)
        {
            var buku = _layananBukuTabungan.Susun(NasabahTarikTerpilih.Id);
            _layananBukuTabungan.EksporPdf(buku, dialog.FileName);
            MessageBox.Show($"Buku tabungan berhasil diekspor ke:\n{dialog.FileName}", "Ekspor PDF Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void MuatLaporan()
    {
        try
        {
            RekapSaatIni = _layananLaporan.LaporanBulanan(BulanLaporan, TahunLaporan);
            RincianLaporan.Clear();
            foreach (var item in RekapSaatIni.PerKategori)
                RincianLaporan.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal memuat laporan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EksporCsv()
    {
        if (RekapSaatIni == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Ekspor Laporan Bulanan CSV",
            Filter = "CSV File|*.csv",
            FileName = $"Laporan_TRASHURY_{TahunLaporan}_{BulanLaporan:D2}.csv"
        };
        if (dialog.ShowDialog() == true)
        {
            _layananLaporan.EksporCsv(RekapSaatIni, dialog.FileName);
            MessageBox.Show($"Laporan CSV berhasil disimpan ke:\n{dialog.FileName}", "Ekspor CSV Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EksporPdf()
    {
        if (RekapSaatIni == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Ekspor Laporan Bulanan PDF (DLH)",
            Filter = "PDF Document|*.pdf",
            FileName = $"Laporan_DLH_TRASHURY_{TahunLaporan}_{BulanLaporan:D2}.pdf"
        };
        if (dialog.ShowDialog() == true)
        {
            _layananLaporan.EksporPdf(RekapSaatIni, dialog.FileName);
            MessageBox.Show($"Laporan PDF berhasil disimpan ke:\n{dialog.FileName}", "Ekspor PDF Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}