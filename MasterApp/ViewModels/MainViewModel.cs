using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using Microsoft.Win32;
using MasterApp.Services;
using MasterApp.Models;
using MasterApp.Helpers;
using System.Net.NetworkInformation;
using System.IO;
using System.Threading.Tasks;

namespace MasterApp.ViewModels
{
    // Główny model widoku aplikacji Master, zarządza logiką interfejsu użytkownika.
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FileSyncService _fileSyncService;  // Serwis synchronizacji plików.
        private readonly UdpCommunicationService _udpService;  // Serwis komunikacji UDP.
        private Statistics _statistics = new Statistics();  // Statystyki operacji na plikach.
        private string _trackingFolderPath;  // Ścieżka do śledzonego folderu.
        private int _udpListenPort;  // Port nasłuchu UDP.
        private bool _isSyncing;  // Flaga wskazująca, czy trwa synchronizacja.

        // Właściwości bindowane do interfejsu użytkownika.
        public string CurrentTrackingPath => $"Tracking Path: {_trackingFolderPath}";
        public string CurrentUdpPort => $"UDP Listen Port: {_udpListenPort}";
        public string UdpListenPort
        {
            get => _udpListenPort.ToString();
            set
            {
                if (int.TryParse(value, out int parsedValue) && _udpListenPort != parsedValue)
                {
                    _udpListenPort = parsedValue;
                    OnPropertyChanged(nameof(UdpListenPort));
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                else
                {
                    MessageBox.Show("Invalid input. Please enter a valid port number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public bool IsUdpPortEditable => !_isSyncing;
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ChooseFolderCommand { get; }
        public ICommand ShowStatisticsCommand { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        // Konstruktor inicjalizujący serwisy i komendy.
        public MainViewModel()
        {
            _udpService = new UdpCommunicationService();
            _fileSyncService = new FileSyncService(_udpService, _statistics);
            StartCommand = new RelayCommand(StartSync, CanStartSync);
            StopCommand = new RelayCommand(StopSync, CanStopSync);
            ChooseFolderCommand = new RelayCommand(ChooseFolder, CanChooseFolder);
            ShowStatisticsCommand = new RelayCommand(ShowStatistics, CanShowStatistics);
            LoadConfig();
        }

        // Destruktor zapewniający zatrzymanie synchronizacji przed zakończeniem działania aplikacji.
        ~MainViewModel()
        {
            StopSync();
        }

        // Metoda rozpoczynająca synchronizację.
        private void StartSync()
        {
            try
            {
                if (!CanStartSync())
                {
                    MessageBox.Show("Cannot start syncing due to invalid port numbers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _statistics.Reset();  // Resetowanie statystyk.
                _fileSyncService.StartWatching(_trackingFolderPath);  // Rozpoczęcie śledzenia folderu.
                _udpService.Initialize(_udpListenPort);  // Inicjalizacja serwisu UDP.
                _isSyncing = true;  // Ustawienie flagi synchronizacji.
                OnPropertyChanged(nameof(IsUdpPortEditable));
                OnPropertyChanged(nameof(CurrentTrackingPath));
                OnPropertyChanged(nameof(CurrentUdpPort));
                SaveConfig();  // Zapis konfiguracji.

                // Uruchomienie nasłuchiwania sygnałów w tle.
                Task.Run(() => _udpService.ListenForActivationSignal());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sprawdzenie, czy można rozpocząć synchronizację.
        private bool CanStartSync()
        {
            if (_isSyncing || string.IsNullOrEmpty(_trackingFolderPath) || _udpListenPort <= 0)
            {
                return false;
            }
            if (!IsPortAvailable(_udpListenPort))
            {
                return false;
            }
            return true;
        }

        // Sprawdzenie, czy port jest dostępny.
        private bool IsPortAvailable(int port)
        {
            bool isAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }
            return isAvailable;
        }

        // Metoda zatrzymująca synchronizację.
        private void StopSync()
        {
            try
            {
                _udpService.StopListening();  // Zatrzymanie nasłuchiwania UDP.
                _fileSyncService.StopWatching();  // Zatrzymanie śledzenia folderu.
                _udpService.Dispose();  // Zwolnienie zasobów serwisu UDP.
                _isSyncing = false;  // Zresetowanie flagi synchronizacji.
                OnPropertyChanged(nameof(IsUdpPortEditable));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sprawdzenie, czy można zatrzymać synchronizację.
        private bool CanStopSync() => _isSyncing;

        // Metoda pozwalająca użytkownikowi wybrać folder do śledzenia.
        private void ChooseFolder()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Folder Selection.",
                    Title = "Select a folder to track"
                };
                if (dialog.ShowDialog() == true)
                {
                    _trackingFolderPath = Path.GetDirectoryName(dialog.FileName);
                    OnPropertyChanged(nameof(CurrentTrackingPath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sprawdzenie, czy można wybrać folder.
        private bool CanChooseFolder() => !_isSyncing;

        // Wyświetlenie statystyk użytkownikowi.
        private void ShowStatistics()
        {
            try
            {
                MessageBox.Show($"Files Added: {_statistics.FilesAdded}\nFiles Removed: {_statistics.FilesRemoved}", "Statistics");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sprawdzenie, czy można wyświetlić statystyki.
        private bool CanShowStatistics() => _isSyncing;

        // Zapisanie konfiguracji do pliku XML.
        private void SaveConfig()
        {
            try
            {
                var config = new AppConfig
                {
                    TrackingFolderPath = _trackingFolderPath,
                    UdpListenPort = _udpListenPort,
                };
                XmlConfigHelper.SaveConfig(config, "AppConfig.xml");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Wczytanie konfiguracji z pliku XML.
        private void LoadConfig()
        {
            try
            {
                var config = XmlConfigHelper.LoadConfig<AppConfig>("AppConfig.xml");
                _trackingFolderPath = config.TrackingFolderPath;
                _udpListenPort = config.UdpListenPort;
            }
            catch (Exception ex)
            {
                SetDefaultConfig();
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Ustawienie domyślnej konfiguracji.
        private void SetDefaultConfig()
        {
            try
            {
                _trackingFolderPath = "default_path";
                _udpListenPort = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Powiadamianie o zmianie właściwości.
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Klasa reprezentująca komendę
    public class RelayCommand : ICommand
    {
        // Prywatne pola przechowujące delegaty do metod
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        // Konstruktor przyjmujący delegaty do metod
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            // Przypisanie delegatów do pól
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Zdarzenie informujące o zmianie możliwości wykonania komendy
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // Metoda wywołująca zdarzenie CanExecuteChanged
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        // Metoda sprawdzająca, czy komenda może być wykonana
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        // Metoda wykonująca komendę
        public void Execute(object parameter) => _execute();
    }
}




