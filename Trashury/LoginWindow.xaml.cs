using System.Windows;
using Trashury.Data;
using Trashury.Models;
using Trashury.Services;

namespace Trashury;

public partial class LoginWindow : Window
{
    public Pengguna? PenggunaTerautentikasi { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void Masuk_Click(object sender, RoutedEventArgs e)
    {
        using var db = TrashuryDbContextFactory.Buat();
        var auth = new LayananAutentikasi(db);
        auth.PastikanAkunBawaan();

        Pengguna? user = auth.Login(UsernameBox.Text, PasswordBox.Password);
        if (user == null)
        {
            MessageBox.Show("Username atau password salah.", "Login Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordBox.Clear();
            PasswordBox.Focus();
            return;
        }

        PenggunaTerautentikasi = user;
        DialogResult = true;
        Close();
    }
}
