using MessagePack;

namespace CommonModels
{
    // Typ wyliczeniowy reprezentujący typ zdarzenia pliku
    public enum EventType
    {
        Created,  // Plik został utworzony
        Deleted   // Plik został usunięty
    }

    // Klasa modelu zdarzenia pliku
    [MessagePackObject]
    public class FileEvent
    {
        // Typ zdarzenia
        [Key(0)]
        public EventType Type { get; set; }

        // Nazwa pliku
        [Key(1)]
        public string FileName { get; set; }

        // Zawartość pliku
        [Key(2)]
        public byte[] FileContent { get; set; }

        // Flaga wskazująca, czy obiekt jest katalogiem
        [Key(3)]
        public bool IsDirectory { get; set; }
    }
}


