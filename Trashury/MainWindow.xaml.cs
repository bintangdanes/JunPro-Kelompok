using System.Windows;
using Trashury.Models;
using Trashury.ViewModels;

namespace Trashury;

public partial class MainWindow : Window
{
    public MainWindow() : this(null)
    {
    }

    public MainWindow(Pengguna? penggunaAktif)
    {
        InitializeComponent();
        DataContext = new MainViewModel(penggunaAktif);
    }
}
