using System.Windows;
using MasterApp.ViewModels;

namespace MasterApp
{
    // Główne okno aplikacji
    public partial class MainWindow : Window
    {
        // Konstruktor głównego okna
        public MainWindow()
        {
            InitializeComponent();  // Inicjalizacja komponentów interfejsu użytkownika zdefiniowanych w XAML
            DataContext = new MainViewModel();  // Ustawienie kontekstu danych na nową instancję MainViewModel
        }
    }
}
