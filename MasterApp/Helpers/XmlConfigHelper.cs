using System.IO;
using System.Xml.Serialization;

namespace MasterApp.Helpers
{
    public static class XmlConfigHelper
    {
        // Metoda wczytująca konfigurację z pliku XML
        public static T LoadConfig<T>(string filePath) where T : new()
        {
            // Sprawdzenie, czy plik istnieje
            if (!File.Exists(filePath))
                return new T();  // Jeśli nie, zwróć nową instancję typu T

            // Otwarcie strumienia do pliku
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                // Deserializacja danych z pliku XML do obiektu typu T
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stream);
            }
        }

        // Metoda zapisująca konfigurację do pliku XML
        public static void SaveConfig<T>(T config, string filePath)
        {
            // Otwarcie strumienia do pliku
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                // Serializacja obiektu typu T do pliku XML
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stream, config);
            }
        }
    }
}




