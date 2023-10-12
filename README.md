# Aplikacja MASTER

## Przegląd

Aplikacja MASTER jest częścią systemu dwóch aplikacji opracowanego w C# przy użyciu WPF, zaprojektowanego do synchronizacji plików między aplikacją MASTER a SLAVE. Synchronizacja jest jednokierunkowa, co oznacza, że zmiany wykryte w monitorowanym katalogu MASTER są przesyłane do aplikacji SLAVE.

## Funkcje

- **Przycisk START**: Rozpoczyna synchronizację katalogu.
- **Przycisk STOP**: Zatrzymuje synchronizację katalogu.
- **Przycisk Wyboru Katalogu**: Wybiera katalog do synchronizacji. Ścieżka do wybranego katalogu jest zapisywana i aktualizowana w pliku konfiguracyjnym XML.
- **Pole tekstowe Portu Nasłuchu UDP**: Określa port, na którym MASTER będzie oczekiwał wiadomości od SLAVE. Aktywne tylko przed naciśnięciem przycisku START. Numer portu jest zapisywany i aktualizowany w pliku konfiguracyjnym XML.
- **Przycisk STATYSTYKI**: Wyświetla statystyki dotyczące liczby dodanych i usuniętych plików od momentu naciśnięcia przycisku START. Dostępne tylko po naciśnięciu przycisku START.

## Konfiguracja

Aplikacja wykorzystuje plik konfiguracyjny XML do przechowywania i pobierania danych konfiguracyjnych podczas uruchamiania, takich jak ścieżka do monitorowanego katalogu i numer portu UDP.

## Protokół

Komunikacja jest realizowana za pomocą własnoręcznie zaprojektowanego protokołu przy użyciu transmisji UDP. Protokół zapewnia mechanizmy inicjacji komunikacji, przesyłania informacji o utworzonych plikach (dane binarne do skopiowania) oraz typ zdarzenia (utworzenie lub usunięcie pliku).

## Testowanie

Ta aplikacja jest przeznaczona do testowania na jednym komputerze. Adres IP jest zakodowany na stałe w aplikacji jako `127.0.0.1`.

