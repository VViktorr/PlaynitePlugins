# GryOnlinePL dla Playnite

**To jest kod źródłowy, nie gotowe rozszerzenie do instalacji.** Archiwum projektu nie zawiera pliku `GryOnlineMetadata.dll`. Nie kopiuj całego archiwum do katalogu `Extensions` i nie zmieniaj jego rozszerzenia na `.pext`.

Źródło metadanych wyszukuje grę po nazwie zapisanej w bibliotece Playnite, a następnie odczytuje dane z odnalezionej karty GRYOnline.pl. W ustawieniach rozszerzenia wybierz osobno: krótki opis, wprowadzenie z artykułu, fabułę, gameplay/rozgrywkę, tryby gry, otwarty świat i mapę, elementy RPG, walkę, grywalne postacie, silnik gry oraz pobieranie okładki. Domyślnie pobiera krótki opis, fabułę i okładkę. Rozpoznaje także nagłówki „O czym opowiada Wiedźmin 3?” i „Jak się gra w Wiedźmina 3?”. W Crimson Desert „Gameplay” jest pierwszym nagłówkiem, a „Fabuła” szóstym: nazwy nagłówków mają pierwszeństwo przed regułą kolejności. Jeśli nie rozpozna nazwy pierwszego lub drugiego nagłówka, traktuje je odpowiednio jako fabułę i gameplay, pod warunkiem że danej sekcji nie rozpoznał już po nazwie w innym miejscu. Brakujące sekcje pomija. Zachowuje akapity, pogrubienia, kursywę oraz odnośniki wewnętrzne GRYOnline.pl. Wtyczka dostarcza też twórcę, wydawcę, gatunki, platformy, **serię**, datę wydania, ocenę GRYOnline jako ocenę krytyków, ocenę „Gracze” jako ocenę społeczności i odnośnik. Oceny są przeliczane ze skali 0–10 na skalę 0–100 Playnite. Nie używa ocen Steam ani OpenCritic.

Data wydania jest pobierana dla platformy już przypisanej do gry w Playnite, jeżeli nazwa platformy zgadza się z kartą GRYOnline.pl. W przeciwnym razie wykorzystywana jest ogólna data wydania widoczna u góry karty. Gdy brak konkretnej daty (np. gra niezapowiedziana), pole pozostaje puste.

Pole **Nazwa** korzysta z tytułu widocznego na karcie GRYOnline.pl, więc np. „The Witcher 3: Wild Hunt” może zostać zapisany jako „Wiedźmin 3: Dziki Gon”. Aby Playnite zmienił nazwę, ustaw GryOnlinePL jako pierwsze źródło pola **Nazwa** w ustawieniach pobierania metadanych; pierwsze źródło, które zwróci wartość, ma pierwszeństwo. Włącz też pobieranie pola **Linki** z GryOnlinePL: zapisany adres pozwoli odnaleźć tę samą grę przy kolejnym pobieraniu, nawet gdy wyszukiwarka nie rozpozna już polskiego tytułu. Jeśli polska nazwa nie istnieje na karcie, wtyczka zwraca tytuł tam widoczny.

## Media ze SteamGridDB

Wszystkie grafiki (plakat, tło, ikona) pochodzą wyłącznie ze SteamGridDB. W ustawieniach **Media** zakładki **Plakat**, **Tło** i **Ikona** mają osobne przełączniki i wybór rozmiaru lub większego. Wpisz własny klucz API SteamGridDB albo użyj przycisku **Pobierz klucz API**. Klucz jest zapisywany w ustawieniach Playnite jako tekst. Wtyczka wybiera grafiki według punktacji `score` API i pobiera oryginalny adres `url`, a nie miniaturę `thumb`. Jeśli dla danego filtra nie ma grafiki, pole pozostaje puste. W ustawieniach źródeł metadanych Playnite wybierz GryOnlinePL dla pól **Okładka**, **Tło** i **Ikona**.

## Budowanie i uruchomienie

1. Na Windows zainstaluj Visual Studio z obsługą .NET Framework 4.6.2 i otwórz `GryOnlineMetadata.csproj`. Przywróć pakiety NuGet.
2. Zbuduj projekt w konfiguracji Release. Uruchom `build-install-folder.ps1` w PowerShell z katalogu projektu. Skrypt sprawdza obecność DLL i tworzy folder `install/GryOnlineMetadata` z DLL, manifestem i ikoną `icon.png`.
3. Skopiuj **folder `install/GryOnlineMetadata`** do `%AppData%\Playnite\Extensions` (Playnite instalowany) albo do `Extensions` w katalogu Playnite (wersja portable). Uruchom Playnite ponownie.
4. Otwórz **Dodatki → Ustawienia rozszerzeń → GryOnlinePL** i zaznacz sekcje opisu. W ustawieniach pobierania metadanych Playnite wybierz GryOnlinePL jako źródło żądanych pól, w szczególności **Opis**, **Data wydania**, **Ocena krytyków** i **Ocena społeczności**. Pobierz metadane dla pojedynczej gry lub zaznacz wiele gier i uruchom pobieranie zbiorcze. Po zmianie DLL zamknij Playnite całkowicie i uruchom ponownie.
5. Nazwa gry w Playnite powinna odpowiadać tytułowi w encyklopedii. Jeśli nazwy się różnią, możesz dodać adres karty GRYOnline.pl w **Linkach** gry; ma on pierwszeństwo przed wyszukiwaniem.

## Paczka .pext

Jeśli chcesz otrzymać plik instalacyjny zamiast ręcznie kopiować folder, uruchom w PowerShell w katalogu projektu:

```powershell
powershell -ExecutionPolicy Bypass -File .\pack-pext.ps1 -PlayniteDirectory "C:\Users\User\Desktop\10.60"
```

Podaj faktyczny katalog swojej instalacji Playnite, zawierający `Toolbox.exe`. Skrypt kompiluje wtyczkę, tworzy folder z DLL, manifestem i ikoną, a następnie wywołuje `Toolbox.exe pack`. Gotowy plik: `dist\GryOnlineMetadata.pext`. Przeciągnij go na okno Playnite w trybie pulpitu, aby zainstalować rozszerzenie.

Wtyczka preferuje zgodną nazwę po ujednoliceniu apostrofów, interpunkcji, znaków diakrytycznych i numerów rzymskich (np. „Assassin's Creed Mirage”, „God of War Ragnarök”, „The Last of Us Part II Remastered”). Gdy pierwsze wyszukiwanie nie zwróci dopasowania, powtarza je z uproszczoną nazwą. Przy ręcznym pobieraniu metadanych dla jednej gry, gdy nie ma jednoznacznego dopasowania, pokazuje listę wyników GRYOnline.pl z polem do ponownego wyszukania. Anulowanie wyboru nie przypisuje gry. Przy pobieraniu automatycznym lub zbiorczym nie pokazuje okna; niejednoznaczne wyniki pomija. Gdy brak trafienia, nie zwraca metadanych i nie zgłasza błędu. Układ strony i dostępność serwisu mogą się zmienić. Nie przetestowano działania w Playnite na Windows w tym środowisku.

W ustawieniach przy polu klucza SteamGridDB jest przycisk **Pobierz klucz API**, otwierający stronę https://www.steamgriddb.com/profile/preferences/api w domyślnej przeglądarce.



Wersja 1.1.7 pobiera wszystkie trzy rodzaje mediów wyłącznie ze SteamGridDB. W sekcji Media są zakładki Plakat, Tło i Ikona, każda z przełącznikiem i rozmiarem. Dawne źródło okładki GRYOnline.pl nie jest już używane; do obrazów wymagany jest klucz API SteamGridDB.

Wersja 1.1.8 sprawdza rozmiar grafik po stronie wtyczki i szuka dopasowań również na następnych stronach wyników SteamGridDB. Opcja „wybrany rozmiar lub większe” wymaga podanych minimalnych wymiarów, a dokładny rozmiar wymaga zgodnej szerokości i wysokości. Przy plakatach zachowuje wybrane proporcje. Przy dokładnym wymiarze ICO filtrowanym przez API uwzględnia też pliki wieloklatkowe raportujące 0×0. Gdy nie ma dopasowania, nie pobiera obrazu o innym rozmiarze.

Wersja 1.1.9 rozpoznaje polski tytuł `Uncharted: Kolekcja Dziedzictwo Złodziei` jako grę `Uncharted: Legacy of Thieves Collection` o identyfikatorze SteamGridDB `5307973`. Filtry rozmiarów nadal obowiązują: jeśli w wybranym rozmiarze nie ma obrazu, pole pozostaje puste.

Wersja 1.2.0 szuka mediów także po dodatkowych nazwach z metadanych gry. Jeśli w Linkach gry Playnite jest adres `https://www.steamgriddb.com/game/ID`, wtyczka używa dokładnie tego ID. Jeśli jest adres `https://store.steampowered.com/app/ID/`, pobiera grafiki SteamGridDB przez ID aplikacji Steam. Tak można przypisać polski tytuł do właściwej gry bez dodawania kolejnej reguły do kodu. Jeśli brak dodatkowych nazw i linków, a SteamGridDB nie rozpoznaje polskiego tytułu, automatyczne dopasowanie nadal może się nie udać; wówczas dodaj link do właściwej gry SteamGridDB w edycji gry Playnite.

Wersja 1.2.1 odczytuje oryginalny tytuł z nagłówka strony GRYOnline.pl, jeśli tytuł strony ma postać „Polski tytuł, Oryginalny tytuł - Wielka Encyklopedia Gier”. Oryginalna nazwa jest używana po polskiej przy wyszukiwaniu grafik w SteamGridDB; nazwa gry w Playnite pozostaje polska. Jeśli nie ma takiego tytułu, działają też wcześniejsze metody (dodatkowe nazwy, link SteamGridDB lub Steam, dopasowanie nazwy).

Wersja 1.2.2 zmienia kolejność wyszukiwania według nazwy: najpierw tytuł polski z karty GRYOnline.pl, następnie inne zapisane nazwy i tytuł oryginalny. Jeśli znaleziono część rodzajów grafik, wtyczka sprawdza następną nazwę dla brakujących. Link do konkretnej gry SteamGridDB lub Steam w polu Linki pozostaje priorytetem, ponieważ wskazuje identyfikator gry.

Wersja 1.2.3 zamienia pliki ikon ICO ze SteamGridDB na PNG, wybierając największą klatkę przed przekazaniem do Playnite. Wcześniejsze bezpośrednie przekazanie adresu ICO mogło nie zadziałać. Jeśli wybrano rozmiar nieobecny w wynikach SteamGridDB (np. 512 px, gdy plik ma najwyżej 256 px), filtr nadal nie zwróci ikony.

Wersja 1.2.4 udostępnia rozmiary ikon od 16×16 do 1024×1024. Dla ICO filtr sprawdza każdą klatkę pliku, także jeśli API zwraca 0×0. Przy dokładnym rozmiarze wyodrębnia tę klatkę do PNG, a przy opcji „lub większe” wybiera największą dostępną. Dla obrazów PNG filtr działa według ich rzeczywistych wymiarów podanych przez API.

Wersja 1.2.5 przyspiesza automat: plakat, tło i ikona są wyszukiwane równolegle, każda kategoria zatrzymuje przeglądanie po znalezieniu dopasowania na stronie wyników, a tę samą grę SteamGridDB sprawdza tylko raz nawet przy polskiej i angielskiej nazwie. W razie braku dopasowania bada najwyżej trzy strony dla danej kategorii. Klatki ICO są odczytywane równolegle z ograniczeniem do czterech pobrań. Wybór najwyższej punktacji dotyczy sprawdzonej strony wyników.
