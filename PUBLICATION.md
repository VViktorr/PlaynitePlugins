# Publikacja GryOnlinePL i SteamGridDB Gallery

To są szablony do publikacji obu wtyczek w katalogu dodatków Playnite. Nie zgłaszaj plików z wartościami `REPLACE_*`.

1. W projektach źródłowych uruchom na Windows osobno:

   `powershell -ExecutionPolicy Bypass -File .\pack-pext.ps1 -PlayniteDirectory 'C:\Users\User\Desktop\10.60'`

   Paczki dostarczone do zgłoszenia: `GryOnlineMetadata_7b963b1a-21e9-4a3b-a18c-49c74cf8d652_1_2_5.pext` i `SteamGridDBGallery_893e3841-75f7-4451-9e53-b0487cb98b60_1_9_0.pext`. Jeśli budujesz je ponownie, użyj nowych plików w wydaniach GitHub. Sprawdź instalację obu paczek w Playnite, w szczególności otwieranie galerii i pobieranie mediów automatycznie.

2. Autor `Viktor by chatgpt` jest już wpisany w obu plikach `extension.yaml` i manifestach. Utwórz publiczne repozytorium GitHub z folderami `GryOnlineMetadata`, `SteamGridDBGallery`, `installers`. Opublikuj kod źródłowy obu projektów, ich ikony oraz dwa manifesty z folderu `installers`. Nie umieszczaj prywatnego klucza SteamGridDB ani plików ustawień Playnite. Ustal autora i warunki udostępnienia kodu przed publikacją.

3. Na GitHub utwórz dwie wersje wydania: tag `gryonlinepl-v1.2.5` z plikiem `GryOnlineMetadata_7b963b1a-21e9-4a3b-a18c-49c74cf8d652_1_2_5.pext` oraz tag `steamgriddbgallery-v1.9.0` z plikiem `SteamGridDBGallery_893e3841-75f7-4451-9e53-b0487cb98b60_1_9_0.pext`. Możesz zmienić nazwy tagów, ale wówczas zmień też adresy `PackageUrl`.

4. Adres repozytorium `VViktorr/PlaynitePlugins` jest już wpisany w czterech szablonach. Zamień `REPLACE_RELEASE_DATE` na datę rzeczywistego wydania w formacie RRRR-MM-DD. Jeżeli zmienisz numer wersji, zaktualizuj `Version`, tag i `extension.yaml`. Potwierdź, że każdy adres `PackageUrl`, `InstallerManifestUrl`, `SourceUrl` i `IconUrl` jest publicznie dostępny. Wersja API 6.17.0 wynika z odwołania projektu do PlayniteSDK 6.17.0; zweryfikuj ją z lokalną wersją Playnite/Toolbox.

5. Dodaj dwa pliki z folderu `addons` do odpowiedniego katalogu repozytorium https://github.com/JosefNemec/PlayniteAddonDatabase i wyślij pull request. Toolbox.exe może zweryfikować manifesty. Dopiero po przyjęciu pull requestów dodatki pojawią się w przeglądarce dodatków Playnite i będą obsługiwać aktualizacje poprzez manifest instalatora.

Struktura `SourceUrl` zakłada wspólne repozytorium z dwoma folderami projektów. Przy osobnych repozytoriach zmień wszystkie adresy odpowiednio. Archiwum ZIP zawiera kod i szablony; nie jest paczką instalacyjną `.pext`. Paczki `.pext` dostarczono z Windows i sprawdzono ich strukturę ZIP, identyfikatory, wersje oraz autora; nie uruchamiano ich w Playnite w tym środowisku. Nie publikuj przed sprawdzeniem paczek `.pext` na Windows.
