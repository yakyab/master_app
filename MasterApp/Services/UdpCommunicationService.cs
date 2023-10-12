using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MasterApp.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za komunikację UDP między aplikacjami master i slave.
    /// </summary>
    public class UdpCommunicationService : IDisposable
    {
        // Klient UDP do komunikacji.
        private UdpClient _udpClient;
        // Endpoint zdalnego hosta.
        private IPEndPoint _remoteEndPoint;
        // Flaga kontrolująca, czy serwis powinien nasłuchiwać na wiadomości.
        private bool _shouldListen;
        // Zmienna przechowująca czas otrzymania ostatniego sygnału "heartbeat".
        private DateTime _lastHeartbeatReceived = DateTime.MinValue;
        // Właściwość informująca, czy aplikacja Slave jest aktywna.
        public bool IsSlaveActive { get; private set; }
        // Zdarzenie informujące o zmianie statusu połączenia.
        public event EventHandler<bool> ConnectionStatusChanged;
        // Timer sprawdzający, czy nie przekroczono czasu oczekiwania na sygnał "heartbeat".
        private Timer _heartbeatCheckTimer;


        // Metoda inicjalizująca serwis komunikacji.
        public void Initialize(int listenPort)
        {
            // Inicjalizacja klienta UDP i bindowanie go do lokalnego endpointu.
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), listenPort));
            // Inicjalizacja zdalnego endpointu.
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // Ustawienie flagi aktywności Slave na false.
            IsSlaveActive = false;
            // Inicjalizacja i uruchomienie timera sprawdzającego sygnał "heartbeat".
            _heartbeatCheckTimer = new Timer(CheckLastHeartbeat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        // Metoda sprawdzająca, czy nie przekroczono czasu oczekiwania na sygnał "heartbeat".
        private void CheckLastHeartbeat(object state)
        {
            // Jeśli czas od ostatniego sygnału "heartbeat" przekracza 3 sekundy, ustaw flagę aktywności Slave na false.
            if (DateTime.UtcNow - _lastHeartbeatReceived > TimeSpan.FromSeconds(3))
            {
                IsSlaveActive = false;
                // Wywołanie zdarzenia informującego o zmianie statusu połączenia.
                ConnectionStatusChanged?.Invoke(this, IsSlaveActive);
            }
        }
        // Metoda wysyłająca dane do aplikacji Slave.
        public void SendData(byte[] data)
        {
            // Sprawdzenie, czy Slave jest aktywny przed wysłaniem danych.
            if (IsSlaveActive)
            {
                try
                {
                    // Definiowanie bajtów rozpoczynających ramkę danych.
                    byte[] frameStart = { 0xA0, 0xA1 };
                    // Inicjalizacja tablicy, która będzie przechowywać całą ramkę danych.
                    byte[] frameData = new byte[frameStart.Length + data.Length + 2];

                    // Kopiowanie bajtów rozpoczynających i danych do tablicy frameData.
                    Buffer.BlockCopy(frameStart, 0, frameData, 0, frameStart.Length);
                    Buffer.BlockCopy(data, 0, frameData, frameStart.Length, data.Length);

                    // Obliczenie i dodanie sumy kontrolnej do ramki danych.
                    ushort checksum = CalculateChecksum(frameData, frameData.Length - 2);
                    frameData[^2] = (byte)(checksum >> 8);
                    frameData[^1] = (byte)(checksum & 0xFF);

                    // Wysłanie ramki danych do Slave.
                    _udpClient.Send(frameData, frameData.Length, _remoteEndPoint);
                }
                catch (Exception e)
                {
                    // Informowanie użytkownika o błędzie przy wysyłaniu danych.
                    MessageBox.Show($"Failed to send data: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Oblicza sumę kontrolną dla danych.
        /// </summary>
        /// <param name="data">Dane, dla których obliczana jest suma kontrolna.</param>
        /// <param name="length">Długość danych, dla których obliczana jest suma kontrolna.</param>
        /// <returns>Obliczona suma kontrolna.</returns>
        private ushort CalculateChecksum(byte[] data, int length)
        {
            ushort checksum = 0;
            // Sumowanie wartości wszystkich bajtów.
            for (int i = 0; i < length; i++)
            {
                checksum += data[i];
            }
            return checksum;
        }

        // Metoda rozpoczynająca nasłuchiwanie na sygnały aktywności i inne wiadomości od aplikacji Slave.
        public void ListenForActivationSignal()
        {
            // Ustawienie flagi nasłuchiwania na true.
            _shouldListen = true;

            // Uruchomienie asynchronicznej pętli nasłuchującej.
            Task.Run(() =>
            {
                while (_shouldListen)
                {
                    try
                    {
                        // Odbieranie danych od Slave.
                        byte[] data = _udpClient.Receive(ref _remoteEndPoint);

                        // Sprawdzenie, czy ramka danych jest poprawna.
                        if (data.Length > 4 && data[0] == 0xA0 && data[1] == 0xA1)
                        {
                            // Weryfikacja sumy kontrolnej.
                            ushort receivedChecksum = (ushort)((data[^2] << 8) | data[^1]);
                            ushort calculatedChecksum = CalculateChecksum(data, data.Length - 2);

                            // Jeśli suma kontrolna jest poprawna, przetwarzanie danych.
                            if (receivedChecksum == calculatedChecksum)
                            {
                                // Odczytanie wiadomości z ramki danych.
                                var message = Encoding.UTF8.GetString(data, 2, data.Length - 4);

                                // Jeśli wiadomość to "SLAVE_ALIVE", aktualizacja czasu ostatniego sygnału i ustawienie flagi aktywności Slave na true.
                                if (message.StartsWith("SLAVE_ALIVE"))
                                {
                                    var parts = message.Split(';');
                                    if (parts.Length == 3)
                                    {
                                        var slaveIp = parts[1];
                                        var slavePort = int.Parse(parts[2]);
                                        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(slaveIp), slavePort);
                                        _lastHeartbeatReceived = DateTime.UtcNow;
                                        if (!IsSlaveActive)
                                        {
                                            IsSlaveActive = true;
                                            // Wywołanie zdarzenia informującego o zmianie statusu połączenia.
                                            ConnectionStatusChanged?.Invoke(this, IsSlaveActive);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Obsługa wyjątków może być dodana tutaj w przyszłości.
                    }
                }
            });
        }

        /// <summary>
        /// Zatrzymuje nasłuchiwanie sygnału aktywacji i informuje o zmianie statusu połączenia.
        /// </summary>
        public void StopListening()
        {
            // Ustawienie flagi nasłuchiwania na false.
            _shouldListen = false;
            // Ustawienie flagi aktywności Slave na false.
            IsSlaveActive = false;
            // Wywołanie zdarzenia informującego o zmianie statusu połączenia.
            ConnectionStatusChanged?.Invoke(this, IsSlaveActive);
            // Dezaktywacja timera sprawdzającego sygnał "heartbeat".
            _heartbeatCheckTimer?.Dispose();
        }

        /// <summary>
        /// Zwalnia zasoby używane przez serwis komunikacji UDP.
        /// </summary>
        public void Dispose()
        {
            // Zwolnienie zasobów klienta UDP i timera.
            _udpClient?.Close();
            _udpClient?.Dispose();
            _heartbeatCheckTimer?.Dispose();
        }
    }
}










