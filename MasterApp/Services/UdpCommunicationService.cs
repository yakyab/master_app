using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace MasterApp.Services
{
    // Serwis odpowiedzialny za komunikację UDP między aplikacjami Master i Slave.
    public class UdpCommunicationService : IDisposable
    {
        private UdpClient _udpClient;  // Klient UDP do wysyłania i odbierania danych.
        private IPEndPoint _remoteEndPoint;  // Endpoint zdalnego hosta (aplikacji Slave).
        private bool _shouldListen;  // Flaga kontrolująca nasłuchiwanie na wiadomości.
        private DateTime _lastHeartbeatReceived = DateTime.MinValue;  // Czas otrzymania ostatniego sygnału aktywności.
        public bool IsSlaveActive { get; private set; }  // Flaga wskazująca, czy aplikacja Slave jest aktywna.
        private Timer _heartbeatCheckTimer;  // Timer do sprawdzania, czy nie przekroczono czasu oczekiwania na sygnał aktywności.

        // Inicjalizacja serwisu z określonym portem nasłuchu.
        public void Initialize(int listenPort)
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), listenPort));
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            IsSlaveActive = false;
            _heartbeatCheckTimer = new Timer(CheckLastHeartbeat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        // Sprawdzenie, czy nie przekroczono czasu oczekiwania na sygnał aktywności.
        private void CheckLastHeartbeat(object state)
        {
            if (DateTime.UtcNow - _lastHeartbeatReceived > TimeSpan.FromSeconds(5))
            {
                IsSlaveActive = false;
            }
        }

        // Wysłanie danych do aplikacji Slave.
        public void SendData(byte[] data)
        {
            if (IsSlaveActive)
            {
                try
                {
                    _udpClient.Send(data, data.Length, _remoteEndPoint);
                }
                catch (SocketException se)
                {
                    MessageBox.Show($"Failed to send data: {se.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (ObjectDisposedException ode)
                {
                    MessageBox.Show($"Failed to send data: {ode.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"An unexpected error occurred while sending data: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Nasłuchiwanie na sygnały aktywności i inne wiadomości od aplikacji Slave.
        public void ListenForActivationSignal()
        {
            _shouldListen = true;

            while (_shouldListen)
            {
                try
                {
                    var data = _udpClient.Receive(ref _remoteEndPoint);
                    var message = Encoding.UTF8.GetString(data);
                    if (message.StartsWith("SLAVE_ALIVE"))
                    {
                        var parts = message.Split(';');
                        if (parts.Length == 3)
                        {
                            var slaveIp = parts[1];
                            var slavePort = int.Parse(parts[2]);
                            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(slaveIp), slavePort);
                            IsSlaveActive = true;
                            _lastHeartbeatReceived = DateTime.UtcNow;
                        }
                    }
                }
                catch (SocketException)
                {
                    // Obsługa wyjątków związanych z gniazdami.
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Obsługa pozostałych wyjątków.
                }
            }
        }

        // Zatrzymanie nasłuchiwania i dezaktywacja serwisu.
        public void StopListening()
        {
            _shouldListen = false;
            IsSlaveActive = false;
            _heartbeatCheckTimer?.Dispose();
        }

        // Zwolnienie zasobów.
        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _heartbeatCheckTimer?.Dispose();
        }
    }
}






