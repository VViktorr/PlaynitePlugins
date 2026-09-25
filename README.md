# Wtyczki Playnite: GryOnlinePL i SteamGridDB Gallery

Dwie niezależne wtyczki źródeł metadanych do [Playnite](https://playnite.link/). Autor: **Viktor by chatgpt**.

| Wtyczka | Wersja | Co robi |
| --- | --- | --- |
| **GryOnlinePL** | 1.2.5 | Wyszukuje grę w encyklopedii GRYOnline.pl i pobiera polski tytuł, opis, datę wydania, oceny, serię i inne metadane. Opcjonalnie pobiera plakat, tło oraz ikonę ze SteamGridDB. |
| **SteamGridDB Gallery** | 1.9.0 | Pokazuje galerię plakatów, teł i ikon przy ręcznej edycji gry; przy pobieraniu zbiorczym wybiera grafiki automatycznie. |

## GryOnlinePL

Wtyczka wyszukuje grę po nazwie z biblioteki Playnite. Opis można złożyć z wybranych części artykułu, między innymi krótkiego opisu, fabuły, rozgrywki i trybów gry. Pobiera także nazwę z karty gry (w tym polski tytuł, jeśli jest dostępny), twórcę, wydawcę, gatunki, platformy, serię, datę wydania oraz oceny krytyków i graczy. Dla daty wydania próbuje uwzględnić platformę przypisaną do gry.

W ustawieniach **Media** są osobne zakładki **Plakat**, **Tło** i **Ikona**, z przełącznikami oraz filtrami rozmiaru. Wszystkie grafiki pochodzą ze SteamGridDB i wymagają własnego klucza API. Wtyczka może korzystać z polskiej i oryginalnej nazwy gry oraz linków SteamGridDB lub Steam zapisanych przy grze. W ręcznym wyszukiwaniu niejednoznacznego tytułu pozwala wybrać właściwy wynik; podczas pobierania zbiorczego nie otwiera tego okna.

## SteamGridDB Gallery

Przy ręcznym pobieraniu metadanych otwiera okno z osobnymi zakładkami plakatów, teł i ikon. Każda zakładka ma własne filtry: **Dimensions, Styles, Languages, File Type, Sort, Types, Tags**. Galeria pokazuje wymiary i dostępne informacje o polubieniach. Można wybrać grafiki i kliknąć **Zastosuj wybrane**. Przy automatycznym pobieraniu dla wielu gier wtyczka wybiera grafiki bez otwierania okna.

Ustawienia obejmują osobne filtry dla każdego rodzaju grafiki i język interfejsu: automatyczny, polski, angielski, niemiecki, francuski lub hiszpański. Animowane pliki mogą być widoczne w wynikach, lecz możliwość użycia ich w Playnite zależy od obsługi formatu. Ikony ICO są przetwarzane na PNG z odpowiedniej klatki.

## Instalacja

Jeśli dostępne są gotowe pliki wydania `.pext`, zainstaluj wybraną wtyczkę w Playnite i uruchom program ponownie. **Archiwum źródeł ZIP nie jest instalatorem.** Wtyczki działają niezależnie; możesz zainstalować jedną albo obie.

Do samodzielnego zbudowania potrzebne są Windows, Visual Studio z obsługą **.NET Framework 4.6.2**, pakiety NuGet oraz instalacja Playnite zawierająca `Toolbox.exe`. W katalogu każdej wtyczki uruchom:

```powershell
powershell -ExecutionPolicy Bypass -File .\pack-pext.ps1 -PlayniteDirectory 'C:\Users\User\Desktop\10.60'
```

Zmień ścieżkę, jeśli Twój katalog Playnite jest inny. Wynik znajdziesz odpowiednio w `GryOnlineMetadata\dist\GryOnlineMetadata.pext` albo `SteamGridDBGallery\dist\SteamGridDBGallery.pext`. Zainstaluj pliki `.pext` w Playnite, a następnie uruchom Playnite ponownie.

## Konfiguracja

1. Uzyskaj własny [klucz API SteamGridDB](https://www.steamgriddb.com/profile/preferences/api) i wpisz go w ustawieniach wtyczki korzystającej z grafik. Klucz jest przechowywany w ustawieniach Playnite jako tekst; nie dodawaj go do repozytorium.
2. W ustawieniach pobierania metadanych Playnite wskaż **GryOnlinePL** dla danych tekstowych, których potrzebujesz. Jeśli chcesz używać polskiej nazwy, ustaw tę wtyczkę jako pierwsze źródło pola **Nazwa**.
3. Dla pól **Okładka**, **Tło** i **Ikona** wybierz preferowaną wtyczkę. Obie potrafią pobierać grafiki ze SteamGridDB; kolejność źródeł w Playnite określa, które z nich zostanie użyte jako pierwsze.
4. W ustawieniach wtyczek wybierz odpowiednie części opisu i filtry grafik. Pobierz metadane pojedynczej gry lub zaznacz wiele gier do aktualizacji zbiorczej.

Jeśli automatyczne wyszukiwanie nie rozpozna polskiego tytułu, przy grze można zapisać link do właściwej strony GRYOnline.pl, Steam lub SteamGridDB. Dostępność wyników zależy od tych serwisów i aktualności ich stron oraz API.

## Kod i publikacja

Źródła znajdują się w katalogach [`GryOnlineMetadata`](GryOnlineMetadata/) i [`SteamGridDBGallery`](SteamGridDBGallery/). Manifesty instalatorów są w [`installers`](installers/), a szablony zgłoszenia do katalogu dodatków Playnite w [`addons`](addons/). Instrukcję publikacji zawiera [`PUBLICATION.md`](PUBLICATION.md). Przed zgłoszeniem należy opublikować kod oraz skompilowane i sprawdzone pliki `.pext`, a następnie uzupełnić adresy repozytorium i daty wydań w manifestach.

Dostarczone paczki `.pext` zostały sprawdzone pod kątem zawartości i zgodności manifestów. Nie uruchamiano ich w Playnite w środowisku przygotowującym publikację; przed wydaniem przetestuj je na Windows.
