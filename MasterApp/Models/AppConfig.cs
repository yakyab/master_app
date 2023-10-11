namespace MasterApp.Models
{
    // Klasa przechowująca konfigurację aplikacji Master.
    public class AppConfig
    {
        // Ścieżka do folderu, który jest śledzony przez aplikację.
        public string TrackingFolderPath { get; set; }

        // Port UDP, na którym aplikacja nasłuchuje.
        public int UdpListenPort { get; set; }
    }
}

