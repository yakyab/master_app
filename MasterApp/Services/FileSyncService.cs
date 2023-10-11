using System.IO;
using MasterApp.Models;
using MessagePack;
using System.Collections.Generic;
using CommonModels;

namespace MasterApp.Services
{
    // Serwis odpowiedzialny za synchronizację plików i komunikację z aplikacją Slave.
    public class FileSyncService
    {
        private FileSystemWatcher _fileWatcher;  // Obiekt śledzący zmiany w systemie plików.
        private readonly UdpCommunicationService _udpService;  // Serwis komunikacji UDP.
        private readonly Statistics _statistics;  // Statystyki operacji na plikach.
        private HashSet<string> _knownDirectories = new HashSet<string>();  // Zbiór znanych katalogów.

        // Konstruktor przyjmujący serwis komunikacji i obiekt statystyk jako zależności.
        public FileSyncService(UdpCommunicationService udpService, Statistics statistics)
        {
            _udpService = udpService;
            _statistics = statistics;
        }

        // Metoda rozpoczynająca śledzenie zmian w określonym folderze.
        public void StartWatching(string folderPath)
        {
            _fileWatcher = new FileSystemWatcher(folderPath);
            _fileWatcher.Created += OnFileChanged;  // Subskrypcja zdarzeń utworzenia pliku/katalogu.
            _fileWatcher.Deleted += OnFileChanged;  // Subskrypcja zdarzeń usunięcia pliku/katalogu.
            _fileWatcher.EnableRaisingEvents = true;  // Aktywacja śledzenia zdarzeń.
            foreach (var directoryPath in Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
            {
                _knownDirectories.Add(directoryPath);  // Dodanie wszystkich podkatalogów do zbioru.
            }
        }

        // Metoda zatrzymująca śledzenie zmian.
        public void StopWatching()
        {
            _fileWatcher.EnableRaisingEvents = false;  // Dezaktywacja śledzenia zdarzeń.
            _fileWatcher.Dispose();  // Zwolnienie zasobów.
        }

        // Obsługa zdarzeń zmiany (utworzenie/usunięcie) pliku/katalogu.
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var fileEvent = new FileEvent
            {
                FileName = e.Name,
                Type = e.ChangeType == WatcherChangeTypes.Created ? EventType.Created : EventType.Deleted,
                IsDirectory = Directory.Exists(e.FullPath) || _knownDirectories.Contains(e.FullPath)
            };

            if (fileEvent.Type == EventType.Created)
            {
                System.Threading.Thread.Sleep(100);  // Krótka pauza, aby zapewnić dostępność pliku.
                if (fileEvent.IsDirectory)
                {
                    _knownDirectories.Add(e.FullPath);  // Dodanie nowego katalogu do zbioru.
                }
                else if (File.Exists(e.FullPath))
                {
                    bool fileRead = TryReadFile(e.FullPath, out byte[] fileContent);
                    if (fileRead)
                    {
                        fileEvent.FileContent = fileContent;  // Przypisanie zawartości pliku do zdarzenia.
                        _statistics.FilesAdded++;  // Aktualizacja statystyk.
                        SendFileEvent(fileEvent);  // Wysłanie zdarzenia.
                    }
                }
            }
            else if (fileEvent.Type == EventType.Deleted && !fileEvent.IsDirectory)
            {
                _statistics.FilesRemoved++;  // Aktualizacja statystyk.
                SendFileEvent(fileEvent);  // Wysłanie zdarzenia.
            }
        }

        // Próba odczytu pliku z uwzględnieniem możliwych błędów dostępu.
        private bool TryReadFile(string filePath, out byte[] fileContent)
        {
            const int maxAttempts = 5;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    fileContent = File.ReadAllBytes(filePath);  // Próba odczytu pliku.
                    return true;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(100);  // Krótka pauza przed kolejną próbą.
                }
            }
            fileContent = null;
            return false;
        }

        // Wysłanie zdarzenia do aplikacji Slave.
        private void SendFileEvent(FileEvent fileEvent)
        {
            byte[] data = MessagePackSerializer.Serialize(fileEvent);  // Serializacja zdarzenia.
            _udpService.SendData(data);  // Wysłanie danych przez UDP.
        }
    }
}




