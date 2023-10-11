namespace MasterApp.Models
{
    // Klasa przechowująca statystyki dotyczące operacji na plikach.
    public class Statistics
    {
        // Liczba plików dodanych od rozpoczęcia śledzenia.
        public int FilesAdded { get; set; }

        // Liczba plików usuniętych od rozpoczęcia śledzenia.
        public int FilesRemoved { get; set; }

        // Metoda resetująca statystyki do wartości początkowych.
        public void Reset()
        {
            FilesAdded = 0;
            FilesRemoved = 0;
        }
    }
}

