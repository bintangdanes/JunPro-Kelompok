using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

public record PilihanBulan(int Nomor, string Nama);

public record BarGrafikEmisi(string NamaKategori, decimal TotalCO2eKg, double Persentase);

public record ItemRiwayatTransaksi(
    int Id,
    DateTime Tanggal,
    int NasabahId,
    string NamaNasabah,
    string Jenis,
    string Rincian,
    decimal Nominal,
    bool IsSetoran);

public class MainViewModel : INotifyPropertyChanged
{
    private TrashuryDbContext _db;
    private NasabahRepository _nasabahRepo;
    private KategoriSampahRepository _kategoriRepo;
    private TransaksiRepository _transaksiRepo;
    private LayananTransaksi _layananTransaksi;
    private LayananLaporan _layananLaporan;
    private LayananBukuTabungan _layananBukuTabungan;
    private readonly IKlasifikasiSampah _klasifikasiSampah;
    private readonly KlasifikasiHybrid? _klasifikasiHybrid;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // --- Pengguna & RBAC (Operator vs Pengurus) ---
    private Pengguna? _penggunaAktif;
    public Pengguna? PenggunaAktif
    {
        get => _penggunaAktif;
        private set
        {
            _penggunaAktif = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPengurus));
            OnPropertyChanged(nameof(NamaPeranAktif));
        }
    }

    public bool IsPengurus => PenggunaAktif?.IsPengurus ?? true;
    public string NamaPeranAktif => PenggunaAktif?.NamaLengkap ?? "Pengurus Bank Sampah";

    // --- Koleksi Data ---
    public ObservableCollection<Nasabah> DaftarNasabah { get; } = new();
    public ObservableCollection<Nasabah> DaftarNasabahMaster { get; } = new();
    public ObservableCollection<KategoriSampah> DaftarKategori { get; } = new();
    public ObservableCollection<DetailSetoran> KeranjangSetoran { get; } = new();
    public ObservableCollection<RekapKategori> RincianLaporan { get; } = new();
    public ObservableCollection<BarGrafikEmisi> DataGrafikEmisi { get; } = new();
    public ObservableCollection<ItemRiwayatTransaksi> DaftarRiwayatTransaksi { get; } = new();
    public ObservableCollection<BarisBukuTabungan> DaftarBarisBukuTabungan { get; } = new();
    public ObservableCollection<string> DaftarKandidatAi { get; } = new();
    public ObservableCollection<string> DaftarPeringatanAi { get; } = new();
    public ObservableCollection<Pengguna> DaftarPengguna { get; } = new();

    public List<string> PilihanPeran { get; } = [Pengguna.PeranOperator, Pengguna.PeranPengurus];

    // --- Form Kelola Akun (Pengurus) ---
    private string _usernameAkunBaru = string.Empty;
    public string UsernameAkunBaru { get => _usernameAkunBaru; set { _usernameAkunBaru = value; OnPropertyChanged(); } }

    private string _namaLengkapAkunBaru = string.Empty;
    public string NamaLengkapAkunBaru { get => _namaLengkapAkunBaru; set { _namaLengkapAkunBaru = value; OnPropertyChanged(); } }

    private string _passwordAkunBaru = string.Empty;
    public string PasswordAkunBaru { get => _passwordAkunBaru; set { _passwordAkunBaru = value; OnPropertyChanged(); } }

    private string _peranAkunBaru = Pengguna.PeranOperator;
    public string PeranAkunBaru { get => _peranAkunBaru; set { _peranAkunBaru = value; OnPropertyChanged(); } }

    private Pengguna? _penggunaTerpilih;
    public Pengguna? PenggunaTerpilih
    {
        get => _penggunaTerpilih;
        set
        {
            _penggunaTerpilih = value;
            OnPropertyChanged();
            if (value != null)
            {
                UsernameAkunBaru = value.Username;
                NamaLengkapAkunBaru = value.NamaLengkap;
                PeranAkunBaru = value.Peran;
                PasswordAkunBaru = string.Empty;
            }
        }
    }

    public List<PilihanBulan> DaftarBulan { get; } =
    [
        new(1, "Januari"), new(2, "Februari"), new(3, "Maret"), new(4, "April"),
        new(5, "Mei"), new(6, "Juni"), new(7, "Juli"), new(8, "Agustus"),
        new(9, "September"), new(10, "Oktober"), new(11, "November"), new(12, "Desember")
    ];

    public List<string> PilihanFilterJenis { get; } = ["Semua", "Setoran", "Penarikan"];

    private string _filterJenisRiwayat = "Semua";
    public string FilterJenisRiwayat
    {
        get => _filterJenisRiwayat;
        set { _filterJenisRiwayat = value; OnPropertyChanged(); SegarkanRiwayatTransaksi(); }
    }

    private string _kataKunciRiwayat = string.Empty;
    public string KataKunciRiwayat
    {
        get => _kataKunciRiwayat;
        set { _kataKunciRiwayat = value; OnPropertyChanged(); SegarkanRiwayatTransaksi(); }
    }

    private DateTime? _tanggalDariRiwayat;
    public DateTime? TanggalDariRiwayat
    {
        get => _tanggalDariRiwayat;
        set { _tanggalDariRiwayat = value; OnPropertyChanged(); SegarkanRiwayatTransaksi(); }
    }

    private DateTime? _tanggalSampaiRiwayat;
    public DateTime? TanggalSampaiRiwayat
    {
        get => _tanggalSampaiRiwayat;
        set { _tanggalSampaiRiwayat = value; OnPropertyChanged(); SegarkanRiwayatTransaksi(); }
    }

    // --- Form Master Nasabah ---
    private string _namaNasabahBaru = string.Empty;
    public string NamaNasabahBaru { get => _namaNasabahBaru; set { _namaNasabahBaru = value; OnPropertyChanged(); } }

    private string _kataKunciCariNasabah = string.Empty;
    public string KataKunciCariNasabah
    {
        get => _kataKunciCariNasabah;
        set
        {
            _kataKunciCariNasabah = value;
            OnPropertyChanged();
            SegarkanDaftarNasabahMaster();
        }
    }

    private Nasabah? _nasabahMasterTerpilih;
    public Nasabah? NasabahMasterTerpilih
    {
        get => _nasabahMasterTerpilih;
        set
        {
            _nasabahMasterTerpilih = value;
            OnPropertyChanged();
            if (value != null)
                NamaNasabahBaru = value.Nama;
        }
    }

    // --- Form Master Kategori ---
    private string _namaKategoriBaru = string.Empty;
    public string NamaKategoriBaru { get => _namaKategoriBaru; set { _namaKategoriBaru = value; OnPropertyChanged(); } }

    private string _hargaKategoriBaru = "2000";
    public string HargaKategoriBaru { get => _hargaKategoriBaru; set { _hargaKategoriBaru = value; OnPropertyChanged(); } }

    private string _emisiKategoriBaru = "1.0";
    public string EmisiKategoriBaru { get => _emisiKategoriBaru; set { _emisiKategoriBaru = value; OnPropertyChanged(); } }

    private KategoriSampah? _kategoriMasterTerpilih;
    public KategoriSampah? KategoriMasterTerpilih
    {
        get => _kategoriMasterTerpilih;
        set
        {
            _kategoriMasterTerpilih = value;
            OnPropertyChanged();
            if (value != null)
            {
                NamaKategoriBaru = value.Nama;
                HargaKategoriBaru = value.HargaPerKg.ToString(System.Globalization.CultureInfo.InvariantCulture);
                EmisiKategoriBaru = value.FaktorEmisiCO2e.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    // --- Form Transaksi Setoran ---
    private Nasabah? _nasabahSetoranTerpilih;
    public Nasabah? NasabahSetoranTerpilih { get => _nasabahSetoranTerpilih; set { _nasabahSetoranTerpilih = value; OnPropertyChanged(); } }

    private KategoriSampah? _kategoriSetoranTerpilih;
    public KategoriSampah? KategoriSetoranTerpilih { get => _kategoriSetoranTerpilih; set { _kategoriSetoranTerpilih = value; OnPropertyChanged(); } }

    private string _beratSetoranInput = "1.5";
    public string BeratSetoranInput { get => _beratSetoranInput; set { _beratSetoranInput = value; OnPropertyChanged(); } }

    private string _hasilPrediksiAi = "Belum ada foto sampah diklasifikasi.";
    public string HasilPrediksiAi { get => _hasilPrediksiAi; set { _hasilPrediksiAi = value; OnPropertyChanged(); } }

    private string _sumberModelAi = string.Empty;
    public string SumberModelAi { get => _sumberModelAi; set { _sumberModelAi = value; OnPropertyChanged(); OnPropertyChanged(nameof(AdaSumberModelAi)); } }
    public bool AdaSumberModelAi => !string.IsNullOrWhiteSpace(SumberModelAi);
    public bool AdaKandidatAi => DaftarKandidatAi.Count > 0;
    public bool AdaPeringatanAi => DaftarPeringatanAi.Count > 0;

    private ImageSource? _previewGambarAi;
    public ImageSource? PreviewGambarAi { get => _previewGambarAi; set { _previewGambarAi = value; OnPropertyChanged(); } }

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

    private string _totalMasukBukuTabungan = "Rp 0";
    public string TotalMasukBukuTabungan { get => _totalMasukBukuTabungan; set { _totalMasukBukuTabungan = value; OnPropertyChanged(); } }

    private string _totalKeluarBukuTabungan = "Rp 0";
    public string TotalKeluarBukuTabungan { get => _totalKeluarBukuTabungan; set { _totalKeluarBukuTabungan = value; OnPropertyChanged(); } }

    private string _saldoAkhirBukuTabungan = "Rp 0";
    public string SaldoAkhirBukuTabungan { get => _saldoAkhirBukuTabungan; set { _saldoAkhirBukuTabungan = value; OnPropertyChanged(); } }

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
    public ICommand LogoutCommand { get; }
    public ICommand TambahNasabahCommand { get; }
    public ICommand UbahNasabahCommand { get; }
    public ICommand HapusNasabahCommand { get; }
    public ICommand ResetNasabahCommand { get; }
    public ICommand TambahKategoriCommand { get; }
    public ICommand UbahKategoriCommand { get; }
    public ICommand HapusKategoriCommand { get; }
    public ICommand ResetKategoriCommand { get; }
    public ICommand KlasifikasiFotoCommand { get; }
    public ICommand KlasifikasiKameraCommand { get; }
    public ICommand TambahBarisSetoranCommand { get; }
    public ICommand HapusBarisSetoranCommand { get; }
    public ICommand SimpanSetoranCommand { get; }
    public ICommand ProsesPenarikanCommand { get; }
    public ICommand CetakBukuTabunganPdfCommand { get; }
    public ICommand MuatLaporanCommand { get; }
    public ICommand EksporCsvCommand { get; }
    public ICommand EksporPdfCommand { get; }
    public ICommand BatalkanTransaksiCommand { get; }
    public ICommand CetakUlangBuktiCommand { get; }
    public ICommand FilterRiwayat7HariCommand { get; }
    public ICommand FilterRiwayat30HariCommand { get; }
    public ICommand FilterRiwayatSemuaCommand { get; }
    public ICommand EksporRiwayatCsvCommand { get; }
    public ICommand EksporRiwayatPdfCommand { get; }
    public ICommand BackupDatabaseCommand { get; }
    public ICommand RestoreDatabaseCommand { get; }
    public ICommand TambahAkunCommand { get; }
    public ICommand UbahAkunCommand { get; }
    public ICommand HapusAkunCommand { get; }
    public ICommand ResetFormAkunCommand { get; }

    public MainViewModel(Pengguna? penggunaAktif = null)
    {
        _penggunaAktif = penggunaAktif;
        (_db, _nasabahRepo, _kategoriRepo, _transaksiRepo, _layananTransaksi, _layananLaporan, _layananBukuTabungan) = BuatKonteksLayanan();

        try
        {
            _klasifikasiHybrid = new KlasifikasiHybrid();
            _klasifikasiSampah = _klasifikasiHybrid;
        }
        catch
        {
            _klasifikasiSampah = new KlasifikasiDummy();
        }

        LogoutCommand = new RelayCommand(_ => LogoutDanGantiAkun());
        TambahNasabahCommand = new RelayCommand(_ => TambahNasabah());
        UbahNasabahCommand = new RelayCommand(_ => UbahNasabah());
        HapusNasabahCommand = new RelayCommand(_ => HapusNasabah());
        ResetNasabahCommand = new RelayCommand(_ => ResetFormNasabah());
        TambahKategoriCommand = new RelayCommand(_ => TambahKategori());
        UbahKategoriCommand = new RelayCommand(_ => UbahKategori());
        HapusKategoriCommand = new RelayCommand(_ => HapusKategori());
        ResetKategoriCommand = new RelayCommand(_ => ResetFormKategori());
        KlasifikasiFotoCommand = new RelayCommand(_ => KlasifikasiFoto());
        KlasifikasiKameraCommand = new RelayCommand(_ => KlasifikasiKamera());
        TambahBarisSetoranCommand = new RelayCommand(_ => TambahBarisSetoran());
        HapusBarisSetoranCommand = new RelayCommand(obj => { if (obj is DetailSetoran d) { KeranjangSetoran.Remove(d); RefreshTotalKeranjang(); } });
        SimpanSetoranCommand = new RelayCommand(_ => SimpanSetoran());
        ProsesPenarikanCommand = new RelayCommand(_ => ProsesPenarikan());
        CetakBukuTabunganPdfCommand = new RelayCommand(_ => CetakBukuTabunganPdf());
        MuatLaporanCommand = new RelayCommand(_ => MuatLaporan());
        EksporCsvCommand = new RelayCommand(_ => EksporCsv());
        EksporPdfCommand = new RelayCommand(_ => EksporPdf());
        BatalkanTransaksiCommand = new RelayCommand(obj => { if (obj is ItemRiwayatTransaksi item) BatalkanTransaksi(item); });
        CetakUlangBuktiCommand = new RelayCommand(obj => { if (obj is ItemRiwayatTransaksi item) CetakUlangBuktiSetoran(item); });
        FilterRiwayat7HariCommand = new RelayCommand(_ => { TanggalDariRiwayat = DateTime.Today.AddDays(-6); TanggalSampaiRiwayat = DateTime.Today; });
        FilterRiwayat30HariCommand = new RelayCommand(_ => { TanggalDariRiwayat = DateTime.Today.AddDays(-29); TanggalSampaiRiwayat = DateTime.Today; });
        FilterRiwayatSemuaCommand = new RelayCommand(_ => { TanggalDariRiwayat = null; TanggalSampaiRiwayat = null; FilterJenisRiwayat = "Semua"; KataKunciRiwayat = string.Empty; });
        EksporRiwayatCsvCommand = new RelayCommand(_ => EksporRiwayatCsv());
        EksporRiwayatPdfCommand = new RelayCommand(_ => EksporRiwayatPdf());
        BackupDatabaseCommand = new RelayCommand(_ => BackupDatabase());
        RestoreDatabaseCommand = new RelayCommand(_ => RestoreDatabase());
        TambahAkunCommand = new RelayCommand(_ => TambahAkun());
        UbahAkunCommand = new RelayCommand(_ => UbahAkun());
        HapusAkunCommand = new RelayCommand(_ => HapusAkun());
        ResetFormAkunCommand = new RelayCommand(_ => ResetFormAkun());

        MuatSemuaData();
    }

    private static (TrashuryDbContext, NasabahRepository, KategoriSampahRepository, TransaksiRepository, LayananTransaksi, LayananLaporan, LayananBukuTabungan) BuatKonteksLayanan()
    {
        var db = TrashuryDbContextFactory.Buat();
        var nasabahRepo = new NasabahRepository(db);
        var kategoriRepo = new KategoriSampahRepository(db);
        var transaksiRepo = new TransaksiRepository(db);
        return (
            db,
            nasabahRepo,
            kategoriRepo,
            transaksiRepo,
            new LayananTransaksi(nasabahRepo, transaksiRepo),
            new LayananLaporan(transaksiRepo),
            new LayananBukuTabungan(nasabahRepo, transaksiRepo));
    }

    private void LogoutDanGantiAkun()
    {
        var login = new LoginWindow { Owner = Application.Current.MainWindow };
        if (login.ShowDialog() == true && login.PenggunaTerautentikasi != null)
            PenggunaAktif = login.PenggunaTerautentikasi;
    }

    private void MuatSemuaData()
    {
        var idSetor = NasabahSetoranTerpilih?.Id;
        var idTarik = NasabahTarikTerpilih?.Id;

        DaftarNasabah.Clear();
        foreach (var n in _nasabahRepo.GetAll())
            DaftarNasabah.Add(n);

        SegarkanDaftarNasabahMaster();

        DaftarKategori.Clear();
        foreach (var k in _kategoriRepo.GetAll())
            DaftarKategori.Add(k);

        NasabahSetoranTerpilih = DaftarNasabah.FirstOrDefault(n => n.Id == idSetor) ?? DaftarNasabah.FirstOrDefault();
        NasabahTarikTerpilih = DaftarNasabah.FirstOrDefault(n => n.Id == idTarik) ?? DaftarNasabah.FirstOrDefault();
        KategoriSetoranTerpilih ??= DaftarKategori.FirstOrDefault();

        MuatLaporan();
        SegarkanRiwayatTransaksi();
        SegarkanDaftarPengguna();
    }

    private void SegarkanDaftarPengguna()
    {
        DaftarPengguna.Clear();
        var auth = new LayananAutentikasi(_db);
        auth.PastikanAkunBawaan();
        foreach (var p in auth.GetSemua())
            DaftarPengguna.Add(p);
    }

    private void TambahAkun()
    {
        if (!IsPengurus) return;
        try
        {
            if (string.IsNullOrWhiteSpace(UsernameAkunBaru) || string.IsNullOrWhiteSpace(NamaLengkapAkunBaru) || string.IsNullOrWhiteSpace(PasswordAkunBaru))
            {
                MessageBox.Show("Username, Nama Lengkap, dan Password wajib diisi.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var auth = new LayananAutentikasi(_db);
            auth.TambahPengguna(UsernameAkunBaru, NamaLengkapAkunBaru, PasswordAkunBaru, PeranAkunBaru);
            ResetFormAkun();
            SegarkanDaftarPengguna();
            MessageBox.Show("Akun pengguna baru berhasil dibuat.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gagal Tambah Akun", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UbahAkun()
    {
        if (!IsPengurus) return;
        if (PenggunaTerpilih == null)
        {
            MessageBox.Show("Pilih akun di tabel terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var auth = new LayananAutentikasi(_db);
            auth.UbahPengguna(PenggunaTerpilih.Id, NamaLengkapAkunBaru, PeranAkunBaru, PasswordAkunBaru);
            ResetFormAkun();
            SegarkanDaftarPengguna();
            MessageBox.Show("Data akun berhasil diperbarui.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gagal Ubah Akun", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void HapusAkun()
    {
        if (!IsPengurus) return;
        if (PenggunaTerpilih == null)
        {
            MessageBox.Show("Pilih akun di tabel terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Hapus akun '{PenggunaTerpilih.Username}' ({PenggunaTerpilih.NamaLengkap})?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var auth = new LayananAutentikasi(_db);
            auth.HapusPengguna(PenggunaTerpilih.Id, PenggunaAktif?.Id);
            ResetFormAkun();
            SegarkanDaftarPengguna();
            MessageBox.Show("Akun berhasil dihapus.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Hapus Akun Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ResetFormAkun()
    {
        PenggunaTerpilih = null;
        UsernameAkunBaru = string.Empty;
        NamaLengkapAkunBaru = string.Empty;
        PasswordAkunBaru = string.Empty;
        PeranAkunBaru = Pengguna.PeranOperator;
    }

    private void SegarkanRiwayatTransaksi()
    {
        DaftarRiwayatTransaksi.Clear();
        var petaNasabah = DaftarNasabah.ToDictionary(n => n.Id, n => n.Nama);
        string kunci = KataKunciRiwayat.Trim();

        foreach (var t in _transaksiRepo.GetAll())
        {
            if (TanggalDariRiwayat.HasValue && t.Tanggal.Date < TanggalDariRiwayat.Value.Date)
                continue;
            if (TanggalSampaiRiwayat.HasValue && t.Tanggal.Date > TanggalSampaiRiwayat.Value.Date)
                continue;

            bool isSetoran = t is SetoranSampah;
            string jenis = isSetoran ? "Setoran" : "Penarikan";

            if (FilterJenisRiwayat != "Semua" && !string.Equals(FilterJenisRiwayat, jenis, StringComparison.OrdinalIgnoreCase))
                continue;

            string namaNasabah = petaNasabah.TryGetValue(t.NasabahId, out string? nama) ? nama : $"Nasabah #{t.NasabahId}";
            if (!string.IsNullOrEmpty(kunci) &&
                !namaNasabah.Contains(kunci, StringComparison.OrdinalIgnoreCase) &&
                !t.Id.ToString().Contains(kunci, StringComparison.OrdinalIgnoreCase))
                continue;

            string rincian = t is SetoranSampah s
                ? string.Join(", ", s.DetailSetoran.Select(d => $"{d.Kategori.Nama} ({d.BeratKg:N2} kg)"))
                : "Penarikan tunai saldo";

            DaftarRiwayatTransaksi.Add(new ItemRiwayatTransaksi(
                t.Id, t.Tanggal, t.NasabahId, namaNasabah, jenis, rincian, t.Nominal, isSetoran));
        }
    }

    private void EksporRiwayatCsv()
    {
        if (DaftarRiwayatTransaksi.Count == 0)
        {
            MessageBox.Show("Tidak ada data riwayat transaksi untuk diekspor.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Ekspor Riwayat Transaksi (CSV)",
            Filter = "CSV File|*.csv",
            FileName = $"Riwayat_Transaksi_TRASHURY_{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dialog.ShowDialog() == true)
        {
            var baris = DaftarRiwayatTransaksi.Select(r => new BarisEksporRiwayat(r.Id, r.Tanggal, r.NamaNasabah, r.Jenis, r.Rincian, r.Nominal));
            File.WriteAllBytes(dialog.FileName, DokumenRiwayatTransaksi.BuatCsv(baris));
            MessageBox.Show($"Riwayat transaksi ({DaftarRiwayatTransaksi.Count} baris) berhasil diekspor ke:\n{dialog.FileName}", "Ekspor CSV Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EksporRiwayatPdf()
    {
        if (DaftarRiwayatTransaksi.Count == 0)
        {
            MessageBox.Show("Tidak ada data riwayat transaksi untuk diekspor.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Ekspor Riwayat Transaksi (PDF)",
            Filter = "PDF Document|*.pdf",
            FileName = $"Riwayat_Transaksi_TRASHURY_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog() == true)
        {
            var baris = DaftarRiwayatTransaksi.Select(r => new BarisEksporRiwayat(r.Id, r.Tanggal, r.NamaNasabah, r.Jenis, r.Rincian, r.Nominal)).ToList();
            string rentang = TanggalDariRiwayat.HasValue || TanggalSampaiRiwayat.HasValue
                ? $"{TanggalDariRiwayat?.ToString("dd/MM/yyyy") ?? "..."} s/d {TanggalSampaiRiwayat?.ToString("dd/MM/yyyy") ?? "..."}"
                : "Semua Periode";
            string filterDesc = $"{FilterJenisRiwayat} | {rentang}";
            File.WriteAllBytes(dialog.FileName, DokumenRiwayatTransaksi.BuatPdf(baris, filterDesc));
            MessageBox.Show($"Riwayat transaksi ({DaftarRiwayatTransaksi.Count} baris) berhasil diekspor ke:\n{dialog.FileName}", "Ekspor PDF Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BatalkanTransaksi(ItemRiwayatTransaksi item)
    {
        if (!IsPengurus)
        {
            MessageBox.Show("Hanya Pengurus Bank Sampah yang dapat membatalkan transaksi.", "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"Batalkan transaksi #{item.Id} ({item.Jenis} — {item.NamaNasabah} — Rp {item.Nominal:N0})?\nSaldo nasabah akan dikembalikan otomatis.",
                "Konfirmasi Pembatalan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            _layananTransaksi.BatalkanTransaksi(item.Id);
            MuatSemuaData();
            MessageBox.Show($"Transaksi #{item.Id} berhasil dibatalkan dan saldo nasabah telah disesuaikan.", "Pembatalan Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal membatalkan transaksi: {ex.Message}", "Pembatalan Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CetakUlangBuktiSetoran(ItemRiwayatTransaksi item)
    {
        if (!item.IsSetoran)
        {
            MessageBox.Show("Cetak bukti setoran hanya tersedia untuk transaksi Setoran.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            if (_transaksiRepo.GetById(item.Id) is not SetoranSampah setoran)
                return;

            decimal co2e = new KalkulatorCO2e().HitungCO2e(setoran);
            var dialog = new SaveFileDialog
            {
                Title = "Simpan Bukti Setoran PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"BuktiSetoran_{setoran.Id}_{item.NamaNasabah.Replace(' ', '_')}.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, DokumenBuktiSetoranPdf.Buat(setoran, item.NamaNasabah, co2e));
                MessageBox.Show($"Bukti setoran berhasil disimpan ke:\n{dialog.FileName}", "Cetak Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal mencetak bukti setoran: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackupDatabase()
    {
        if (!IsPengurus) return;

        var dialog = new SaveFileDialog
        {
            Title = "Cadangkan Basis Data SQLite TRASHURY",
            Filter = "SQLite Database|*.db",
            FileName = $"trashury-backup-{DateTime.Now:yyyyMMdd-HHmm}.db"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            string sumber = TrashuryDbContextFactory.LokasiBasisDataBawaan();
            SqliteConnection.ClearAllPools();
            File.Copy(sumber, dialog.FileName, overwrite: true);
            MessageBox.Show($"Basis data berhasil dicadangkan ke:\n{dialog.FileName}", "Backup Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal mencadangkan basis data: {ex.Message}", "Error Backup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreDatabase()
    {
        if (!IsPengurus) return;

        var dialog = new OpenFileDialog
        {
            Title = "Pilih Berkas Backup Database SQLite (.db)",
            Filter = "SQLite Database|*.db"
        };

        if (dialog.ShowDialog() != true)
            return;

        if (MessageBox.Show(
                "Memulihkan basis data akan menimpa seluruh data saat ini. Lanjutkan?",
                "Konfirmasi Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            string tujuan = TrashuryDbContextFactory.LokasiBasisDataBawaan();
            _db.Dispose();
            SqliteConnection.ClearAllPools();
            File.Copy(dialog.FileName, tujuan, overwrite: true);

            (_db, _nasabahRepo, _kategoriRepo, _transaksiRepo, _layananTransaksi, _layananLaporan, _layananBukuTabungan) = BuatKonteksLayanan();
            _db.Database.Migrate();
            new LayananAutentikasi(_db).PastikanAkunBawaan();
            MuatSemuaData();
            MessageBox.Show("Basis data berhasil dipulihkan dan dimuat ulang.", "Restore Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            (_db, _nasabahRepo, _kategoriRepo, _transaksiRepo, _layananTransaksi, _layananLaporan, _layananBukuTabungan) = BuatKonteksLayanan();
            MessageBox.Show($"Gagal memulihkan basis data: {ex.Message}", "Error Restore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SegarkanDaftarNasabahMaster()
    {
        DaftarNasabahMaster.Clear();
        string kunci = KataKunciCariNasabah.Trim();

        IEnumerable<Nasabah> hasil;
        if (string.IsNullOrEmpty(kunci))
        {
            hasil = _nasabahRepo.GetAll();
        }
        else if (int.TryParse(kunci, out _))
        {
            try
            {
                hasil = new[] { _nasabahRepo.CariByKode(kunci) };
            }
            catch (KeyNotFoundException)
            {
                hasil = Enumerable.Empty<Nasabah>();
            }
        }
        else
        {
            hasil = _nasabahRepo.CariByNama(kunci);
        }

        foreach (var n in hasil)
            DaftarNasabahMaster.Add(n);
    }

    private void ResetFormNasabah()
    {
        NasabahMasterTerpilih = null;
        NamaNasabahBaru = string.Empty;
        KataKunciCariNasabah = string.Empty;
    }

    private void ResetFormKategori()
    {
        KategoriMasterTerpilih = null;
        NamaKategoriBaru = string.Empty;
        HargaKategoriBaru = "2000";
        EmisiKategoriBaru = "1.0";
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

    private void UbahNasabah()
    {
        if (NasabahMasterTerpilih == null)
        {
            MessageBox.Show("Pilih nasabah di tabel terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(NamaNasabahBaru))
        {
            MessageBox.Show("Nama nasabah tidak boleh kosong.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        NasabahMasterTerpilih.Nama = NamaNasabahBaru.Trim();
        _nasabahRepo.SimpanPerubahan();
        NasabahMasterTerpilih = null;
        NamaNasabahBaru = string.Empty;
        MuatSemuaData();
        MessageBox.Show("Data nasabah berhasil diperbarui.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void HapusNasabah()
    {
        if (NasabahMasterTerpilih == null)
        {
            MessageBox.Show("Pilih nasabah di tabel terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Hapus nasabah '{NasabahMasterTerpilih.Nama}'?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            _nasabahRepo.Hapus(NasabahMasterTerpilih);
            _nasabahRepo.SimpanPerubahan();
            NasabahMasterTerpilih = null;
            NamaNasabahBaru = string.Empty;
            MuatSemuaData();
            MessageBox.Show("Nasabah berhasil dihapus.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            MessageBox.Show("Nasabah tidak dapat dihapus karena sudah memiliki riwayat transaksi.", "Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private void UbahKategori()
    {
        if (KategoriMasterTerpilih == null)
        {
            MessageBox.Show("Pilih kategori di tabel terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryBacaInputKategori(out string nama, out decimal harga, out decimal emisi))
            return;

        KategoriMasterTerpilih.Nama = nama;
        KategoriMasterTerpilih.HargaPerKg = harga;
        KategoriMasterTerpilih.FaktorEmisiCO2e = emisi;
        _kategoriRepo.SimpanPerubahan();
        KategoriMasterTerpilih = null;
        NamaKategoriBaru = string.Empty;
        MuatSemuaData();
        MessageBox.Show("Kategori sampah berhasil diperbarui.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void HapusKategori()
    {
        if (KategoriMasterTerpilih == null)
        {
            MessageBox.Show("Pilih kategori di tabel terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Hapus kategori '{KategoriMasterTerpilih.Nama}'?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            _kategoriRepo.Hapus(KategoriMasterTerpilih);
            _kategoriRepo.SimpanPerubahan();
            KategoriMasterTerpilih = null;
            NamaKategoriBaru = string.Empty;
            MuatSemuaData();
            MessageBox.Show("Kategori sampah berhasil dihapus.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            MessageBox.Show("Kategori tidak dapat dihapus karena sudah dipakai di transaksi setoran.", "Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool TryBacaInputKategori(out string nama, out decimal harga, out decimal emisi)
    {
        nama = NamaKategoriBaru.Trim();
        if (string.IsNullOrWhiteSpace(nama) ||
            !decimal.TryParse(HargaKategoriBaru.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out harga) ||
            !decimal.TryParse(EmisiKategoriBaru.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out emisi) ||
            harga < 0 || emisi < 0)
        {
            harga = 0;
            emisi = 0;
            MessageBox.Show("Pastikan nama, harga per kg (>= 0), dan faktor emisi CO2e (>= 0) diisi dengan benar.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private void KlasifikasiFoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Pilih Foto Sampah untuk Klasifikasi AI",
            Filter = "File Gambar|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog() == true)
            JalankanKlasifikasiGambar(File.ReadAllBytes(dialog.FileName));
    }

    private void KlasifikasiKamera()
    {
        try
        {
            var kameraWindow = new KameraWindow
            {
                Owner = Application.Current.MainWindow
            };

            if (kameraWindow.ShowDialog() == true && kameraWindow.HasilGambar != null)
                JalankanKlasifikasiGambar(kameraWindow.HasilGambar);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal mengakses kamera: {ex.Message}", "Error Kamera", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void JalankanKlasifikasiGambar(byte[] bytes)
    {
        try
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            PreviewGambarAi = bitmap;

            HasilPrediksiAi = "Menganalisis foto...";
            SumberModelAi = string.Empty;
            DaftarKandidatAi.Clear();
            DaftarPeringatanAi.Clear();
            OnPropertyChanged(nameof(AdaKandidatAi));
            OnPropertyChanged(nameof(AdaPeringatanAi));

            var hasil = await Task.Run(() => _klasifikasiSampah.Klasifikasi(bytes));
            HasilPrediksiAi = $"{hasil.KategoriTerdeteksi} ({hasil.Confidence:P1})";
            SumberModelAi = hasil.SumberModel;

            foreach (string kandidat in hasil.KandidatTeratas)
                DaftarKandidatAi.Add(kandidat);

            foreach (string p in hasil.Peringatan)
                DaftarPeringatanAi.Add(p);

            OnPropertyChanged(nameof(AdaKandidatAi));
            OnPropertyChanged(nameof(AdaPeringatanAi));

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

            string namaNasabah = NasabahSetoranTerpilih.Nama;
            var transaksi = _layananTransaksi.CatatSetoran(NasabahSetoranTerpilih.Id, KeranjangSetoran.ToList());
            decimal co2e = new KalkulatorCO2e().HitungCO2e(transaksi);
            KeranjangSetoran.Clear();
            RefreshTotalKeranjang();
            MuatSemuaData();

            if (MessageBox.Show(
                    $"Setoran berhasil dicatat!\nTotal Nominal: Rp {transaksi.Nominal:N0}\nEstimasi CO2e Dihindari: {co2e:N2} kg\n\nCetak bukti setoran (PDF)?",
                    "Transaksi Berhasil",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Simpan Bukti Setoran PDF",
                    Filter = "PDF Document|*.pdf",
                    FileName = $"BuktiSetoran_{transaksi.Id}_{namaNasabah.Replace(' ', '_')}.pdf"
                };
                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(dialog.FileName, DokumenBuktiSetoranPdf.Buat(transaksi, namaNasabah, co2e));
                    MessageBox.Show($"Bukti setoran berhasil disimpan ke:\n{dialog.FileName}", "Cetak Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
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
        DaftarBarisBukuTabungan.Clear();

        if (NasabahTarikTerpilih == null)
        {
            RingkasanBukuTabungan = "Pilih nasabah untuk melihat buku tabungan.";
            TotalMasukBukuTabungan = "Rp 0";
            TotalKeluarBukuTabungan = "Rp 0";
            SaldoAkhirBukuTabungan = "Rp 0";
            return;
        }
        try
        {
            var buku = _layananBukuTabungan.Susun(NasabahTarikTerpilih.Id);
            TotalMasukBukuTabungan = $"Rp {buku.TotalKredit:N0}";
            TotalKeluarBukuTabungan = $"Rp {buku.TotalDebit:N0}";
            SaldoAkhirBukuTabungan = $"Rp {buku.SaldoAkhir:N0}";
            RingkasanBukuTabungan = $"Buku Tabungan — {buku.NamaNasabah}";

            foreach (var b in buku.Baris)
                DaftarBarisBukuTabungan.Add(b);
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
            DataGrafikEmisi.Clear();

            decimal maksEmisi = RekapSaatIni.PerKategori.Count > 0
                ? RekapSaatIni.PerKategori.Max(k => k.TotalCO2eKg)
                : 0m;

            foreach (var item in RekapSaatIni.PerKategori)
            {
                RincianLaporan.Add(item);
                double persen = maksEmisi > 0 ? (double)(item.TotalCO2eKg / maksEmisi * 100m) : 0;
                DataGrafikEmisi.Add(new BarGrafikEmisi(item.NamaKategori, item.TotalCO2eKg, persen));
            }
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