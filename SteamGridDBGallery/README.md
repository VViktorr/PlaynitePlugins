# SteamGridDB Gallery dla Playnite

Osobna wtyczka źródła metadanych Playnite: okładki, tła i ikony ze SteamGridDB. Nie zastępuje GryOnlinePL i ma osobny identyfikator oraz ustawienia.

## Instalacja

1. Otwórz `SteamGridDBGallery.csproj` w Visual Studio na Windows; wymagany jest .NET Framework 4.6.2 i przywrócenie pakietów NuGet. Skrypty PowerShell zapisano jako UTF-8 z BOM dla Windows PowerShell 5.1.
2. Skompiluj Release. Uruchom `build-install-folder.ps1`, aby przygotować folder `install\SteamGridDBGallery` razem z ikoną `icon.png`.
3. Jeśli masz `Toolbox.exe` w folderze projektu, uruchom z PowerShell w tym folderze: `powershell -ExecutionPolicy Bypass -File .\pack-pext.ps1 -PlayniteDirectory .`. W innym przypadku podaj katalog zawierający `Toolbox.exe`.
4. Zainstaluj paczkę `.pext` z `dist` w Playnite, uruchom Playnite ponownie, a w ustawieniach SteamGridDB Gallery wpisz własny [klucz API SteamGridDB](https://www.steamgriddb.com/profile/preferences). Klucz jest zapisywany jako tekst w danych ustawień Playnite.
5. W ustawieniach pobierania metadanych Playnite wybierz **SteamGridDB Gallery** dla pól **Okładka**, **Tło** i **Ikona**. Przy ręcznej edycji gry kliknij „Pobierz metadane”.

Galeria pokazuje trzy zakładki z miniaturami. Każda grafika ma podpis z wymiarami i oceną `score` z API. Kliknij po jednej grafice w wybranych zakładkach, następnie **Zastosuj wybrane**. Puste zakładki nie zmieniają odpowiadających im pól. **Anuluj** nie przekazuje grafik. Przeglądanie podglądów używa adresów miniatur; do Playnite trafiają adresy pełnych obrazów. Wtyczka sprawdza również podstawową nazwę gry po usunięciu końcówki „Ultimate Edition” i podobnych, a gdy nie znajdzie wyników, pokazuje listę gier do ręcznego wyboru.

Filtry **Dimensions, Styles, Languages, File Type, Sort, Types, Tags** są dostępne osobno wewnątrz zakładek okładek, teł i ikon; zmiana odświeża tylko wybraną zakładkę. Zestawy opcji różnią się zależnie od rodzaju grafiki. Ustawienia wtyczki określają wartości początkowe okna. Strona SteamGridDB wyświetla polubienia z pola `hearts`. Wtyczka pobiera grafiki z publicznego wyszukiwania używanego przez stronę (`/api/public/search/assets`), które zwraca `hearts` oraz stosuje sortowanie `score_desc`, `score_asc`, `score_old_desc`, `score_old_asc`, `age_desc` i `age_asc`. Po zmianie Sort ładuje pierwszą stronę od nowa. Jeśli wyszukiwanie publiczne jest niedostępne, wtyczka wraca do API v2; wtedy licznik może pokazywać „niedostępne”, a sortowanie wyników rezerwowych może się różnić od strony. Dla brakującego licznika wtyczka próbuje też szczegółów `/api/public/asset/{typ}/{id}` i strony obrazu. Dodatkowe żądania działają w tle, najwyżej trzy równolegle. Sortowanie wyników publicznego wyszukiwania pochodzi z serwera SteamGridDB. Wtyczka pobiera kolejne strony po przewinięciu zakładki na dół; można też kliknąć „Wczytaj kolejne grafiki”. Pobrane strony są zachowywane po zmianie filtrów poza Sort; zmiana Sort pobiera wyniki od początku. Niektóre style i typy plików nie występują w każdej zakładce; wtedy lista może być pusta.

Pobieranie zbiorcze nie otwiera galerii i wybiera grafiki automatycznie. Projektu nie przetestowano w Playnite na Windows w tym środowisku.

## Animacje i ikony

Karty WEBM są widoczne w galerii z informacją o animacji. Podgląd wideo jest wyłączony w oknie wyboru, aby otwieranie galerii nie blokowało Playnite. Playnite może nie przyjąć WEBM jako okładki lub tła, ponieważ obsługa tego formatu zależy od wersji Playnite i środowiska. Gdy API nie zwraca rozmiaru obrazu, wtyczka odczytuje wymiary oryginalnego pliku w tle. Ikony poniżej 128 px otrzymują oznaczenie „mała ikona”. Przy wyborze ICO wtyczka zapisuje największą klatkę z pliku jako PNG, aby uniknąć użycia małej klatki (np. 16 × 16 px). Niskiej rozdzielczości plików PNG nie da się poprawić samym powiększeniem.

## Język interfejsu

W ustawieniach wtyczki wybierz **Język interfejsu**: Automatycznie (według języka systemu), Polski, English, Deutsch, Français lub Español. Zmiana języka jest widoczna przy następnym otwarciu ustawień i galerii. Filtr **Languages** dotyczy języka napisów na grafikach i jest niezależny od języka interfejsu. Dotychczas zapisane filtry pozostają zgodne z nową wersją.

Wersja 1.8.3 poprawia błędy kompilacji z 1.8.2 (kolizje nazw i sygnatura otwierania galerii).

Wersja 1.8.4 odczytuje wszystkie rozmiary klatek plików ICO, pokazuje je w galerii, wybiera największą klatkę do podglądu i do zapisania w Playnite jako PNG. Dane API o jednym wymiarze nie zastępują rozmiarów rzeczywistego pliku ICO.

Wersja 1.8.5 wyświetla zakres rozmiarów klatek ICO, np. `16–512 px`, zamiast ich pełnej listy.

Wersja 1.8.6 dodaje w Sort opcje **Most Likes** i **Fewest Likes** (z tłumaczeniami). Wtyczka pobiera brakujące polubienia dla aktualnie załadowanych grafik w tle i sortuje lokalnie tę część wyników; kolejne strony są pobierane według punktacji SteamGridDB i dołączane do lokalnie posortowanej listy. Brakujące polubienia są na końcu. Jest to sortowanie wczytanych grafik, nie całej bazy SteamGridDB.

Wersja 1.8.7 po otwarciu galerii automatycznie pobiera wszystkie kolejne strony okładek, teł i ikon w tle. Pasek podaje postęp. Okno można zamknąć w dowolnym momencie. Sortowanie po polubieniach obejmuje komplet pobranych wyników, jeśli serwer udostępni wszystkie strony; gdy pobieranie się nie powiedzie, zostają dotąd pobrane grafiki.

Wersja 1.8.8 zachowuje automatyczne pobieranie wszystkich stron, ale nie przebudowuje galerii po każdej stronie. Wyświetla pierwsze 72 grafiki na zakładkę, a kolejne porcje po kliknięciu przycisku. Liczba na zakładce obejmuje wszystkie pobrane grafiki; sortowanie i filtry działają na całym pobranym zbiorze. Dzięki temu otwarcie i przewijanie nie tworzy od razu setek kontrolek i żądań o dane obrazów.

W ustawieniach przy polu klucza SteamGridDB jest przycisk **Pobierz klucz API**, otwierający stronę https://www.steamgriddb.com/profile/preferences/api w domyślnej przeglądarce.

## Automatyczne pobieranie i filtry osobne

Wersja 1.9.0 pobiera okładkę, tło i ikonę także podczas automatycznego/zbiorczego pobierania metadanych Playnite, bez okna wyboru. Używa dokładnego dopasowania nazwy gry (również bez końcówki edycji) i najlepszego wyniku z osobnych filtrów dla okładek, teł i ikon. Przy braku dopasowania nie przypisuje grafiki. Jeśli filtr nie znajdzie obrazu na pierwszej stronie, sprawdza do pięciu następnych stron. W ręcznej edycji nadal wyświetla galerię. Dotychczasowy wspólny zestaw filtrów zostaje skopiowany do trzech zakładek przy pierwszym uruchomieniu nowej wersji. Wybierz SteamGridDB Gallery jako źródło pól Okładka, Tło i Ikona w ustawieniach metadanych Playnite.

Wersja 1.9.1 rozszerza listę Dimensions w ustawieniach i w oknie galerii. Ikony mają teraz wybór od 16×16 do 1024×1024 px, zgodnie z GryOnlinePL. Dodano także rozmiary 720×1080 i 1080×1620 dla plakatów oraz 1920×1080, 2560×1440 i 3840×2160 dla teł. Obie listy korzystają z tych samych wartości, a filtr nadal wymaga dokładnych wymiarów. Wybranie rozmiaru bez dostępnych grafik może dać pustą listę.

Wersja 1.9.2 przy automatycznym pobieraniu wybiera najbliższy rozmiar spośród sprawdzonych grafik, jeśli nie znalazła wymiarów dokładnie zgodnych z filtrem. Zachowuje pozostałe filtry oraz proporcje plakatów. Wybór ręczny nadal pokazuje wyłącznie grafiki zgodne z ustawionym filtrem Dimensions. Pliki bez znanych wymiarów nie są uwzględniane w awaryjnym dopasowaniu.

Wersja 1.9.3 sprawdza trzy kategorie grafik równolegle również po wczytaniu pierwszej strony. Gdy pierwsza strona ma mniej niż 48 grafik, nie pyta o następne. Błąd odpowiedzi dla jednego rodzaju grafiki nie blokuje pozostałych kategorii. Galeria ręczna działa jak wcześniej.
