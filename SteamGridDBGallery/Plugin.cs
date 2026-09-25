using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace SteamGridDBGallery
{
    public sealed class GalleryPlugin : MetadataPlugin
    {
        private readonly GallerySettings settings;
        public override Guid Id { get; } = Guid.Parse("893e3841-75f7-4451-9e53-b0487cb98b60");
        public override string Name => "SteamGridDB Gallery";
        public override List<MetadataField> SupportedFields => new List<MetadataField>
        {
            MetadataField.CoverImage, MetadataField.BackgroundImage, MetadataField.Icon
        };
        public GalleryPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new MetadataPluginProperties { HasSettings = true };
            settings = new GallerySettings(this);
        }
        public override ISettings GetSettings(bool firstRunSettings) => settings;
        public override UserControl GetSettingsView(bool firstRunSettings) => new GallerySettingsView(settings);
        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
            => new GalleryProvider(PlayniteApi, options, settings);
    }

    public sealed class GalleryFilters
    {
        public string Styles { get; set; } = "Any";
        public string Languages { get; set; } = "Any";
        public string FileType { get; set; } = "Any";
        public string Dimensions { get; set; } = "Any";
        public string Sort { get; set; } = "Highest Score (Beta)";
        public string Types { get; set; } = "Any";
        public string Tags { get; set; } = "Any";
        public GalleryFilters Snapshot() => new GalleryFilters
        {
            Styles = Styles ?? "Any", Languages = Languages ?? "Any", FileType = FileType ?? "Any",
            Dimensions = Dimensions ?? "Any", Sort = Sort ?? "Highest Score (Beta)",
            Types = Types ?? "Any", Tags = Tags ?? "Any"
        };
    }

    public sealed class GallerySettings : ObservableObject, ISettings
    {
        private readonly GalleryPlugin plugin;
        private GallerySettings previous;
        public string ApiKey { get; set; } = "";
        public string UiLanguage { get; set; } = "Auto";
        public string Styles { get; set; } = "Any";
        public string Languages { get; set; } = "Any";
        public string FileType { get; set; } = "Any";
        public string Dimensions { get; set; } = "Any";
        public string Sort { get; set; } = "Highest Score (Beta)";
        public string Types { get; set; } = "Any";
        public string Tags { get; set; } = "Any";
        public int FilterSettingsVersion { get; set; } = 0;
        public GalleryFilters Covers { get; set; } = new GalleryFilters();
        public GalleryFilters Backgrounds { get; set; } = new GalleryFilters();
        public GalleryFilters Icons { get; set; } = new GalleryFilters();
        public GallerySettings() { }
        public GallerySettings(GalleryPlugin plugin)
        {
            this.plugin = plugin;
            var saved = plugin.LoadPluginSettings<GallerySettings>();
            if (saved != null) Copy(saved);
            FilterSettingsVersion = 1;
        }
        private void Copy(GallerySettings other)
        {
            ApiKey = other.ApiKey ?? "";
            UiLanguage = other.UiLanguage ?? "Auto";
            Styles = other.Styles ?? "Any";
            Languages = other.Languages ?? "Any";
            FileType = other.FileType ?? "Any";
            Dimensions = other.Dimensions ?? "Any";
            Sort = other.Sort ?? "Highest Score (Beta)";
            Types = other.Types ?? "Any";
            Tags = other.Tags ?? "Any";
            // Existing installations used one shared set of filters. Keep its values on upgrade.
            var old = new GalleryFilters { Styles = Styles, Languages = Languages, FileType = FileType,
                Dimensions = Dimensions, Sort = Sort, Types = Types, Tags = Tags };
            FilterSettingsVersion = other.FilterSettingsVersion;
            Covers = (FilterSettingsVersion == 0 ? old : other.Covers ?? old).Snapshot();
            Backgrounds = (FilterSettingsVersion == 0 ? old : other.Backgrounds ?? old).Snapshot();
            Icons = (FilterSettingsVersion == 0 ? old : other.Icons ?? old).Snapshot();
        }
        public GallerySettings Snapshot()
        {
            var result = new GallerySettings();
            result.Copy(this);
            return result;
        }
        public void BeginEdit() => previous = Snapshot();
        public void CancelEdit() { if (previous != null) Copy(previous); }
        public void EndEdit()
        {
            FilterSettingsVersion = 1;
            plugin.SavePluginSettings(this);
        }
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }

    public sealed class GallerySettingsView : UserControl
    {
        public GallerySettingsView(GallerySettings settings)
        {
            var l = new GalleryText(settings.UiLanguage);
            var panel = new StackPanel { Margin = new Thickness(12) };
            AddChoice(panel, "Język interfejsu", "UiLanguage", l, "Auto", "Polski", "English", "Deutsch", "Français", "Español");
            panel.Children.Add(new TextBlock { Text = l.T("Klucz API SteamGridDB:"), Foreground = Brushes.White });
            var key = new PasswordBox { Margin = new Thickness(0, 6, 0, 8) };
            key.DataContextChanged += (sender, args) =>
            {
                var currentSettings = key.DataContext as GallerySettings;
                if (currentSettings != null) key.Password = currentSettings.ApiKey ?? "";
            };
            key.PasswordChanged += (sender, args) =>
            {
                var currentSettings = key.DataContext as GallerySettings;
                if (currentSettings != null) currentSettings.ApiKey = key.Password;
            };
            var apiKeyRow = new StackPanel { Orientation = Orientation.Horizontal };
            key.MinWidth = 230;
            apiKeyRow.Children.Add(key);
            var getApiKey = new Button { Content = l.T("Pobierz klucz API"),
                Margin = new Thickness(8, 6, 0, 8) };
            getApiKey.Click += (sender, args) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://www.steamgriddb.com/profile/preferences/api") { UseShellExecute = true });
            apiKeyRow.Children.Add(getApiKey);
            panel.Children.Add(apiKeyRow);
            panel.Children.Add(new TextBlock
            {
                Text = l.T("Klucz utworzysz w profilu SteamGridDB. Przy ręcznym pobieraniu metadanych pojawi się galeria okładek, teł i ikon. Kliknij grafiki, które chcesz przypisać."),
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White
            });
            var filterTabs = new TabControl { Margin = new Thickness(0, 14, 0, 0) };
            filterTabs.Items.Add(SettingsTab(l.T("Okładki"), "Covers", "grids", l));
            filterTabs.Items.Add(SettingsTab(l.T("Tła"), "Backgrounds", "heroes", l));
            filterTabs.Items.Add(SettingsTab(l.T("Ikony"), "Icons", "icons", l));
            panel.Children.Add(filterTabs);
            panel.Children.Add(new TextBlock { Text = l.T("Filtr Tags wybiera jeden rodzaj tagu. Dostępność grafik zależy od rodzaju obrazu i gry."),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), Foreground = Brushes.White });
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }
        private static TabItem SettingsTab(string title, string property, string kind, GalleryText l)
        {
            var panel = new StackPanel { Margin = new Thickness(8) };
            AddChoice(panel, "Dimensions", property + ".Dimensions", l, kind == "icons"
                ? new[] { "Any", "256x256", "512x512", "1024x1024" }
                : kind == "heroes" ? new[] { "Any", "1920x620", "1600x650", "3840x1240" }
                : new[] { "Any", "1024x1024", "512x512", "342x482", "660x930", "460x215", "920x430", "600x900" });
            AddChoice(panel, "Styles", property + ".Styles", l, kind == "icons"
                ? new[] { "Any", "Official", "Custom" }
                : kind == "heroes" ? new[] { "Any", "Official", "Custom", "Alternate", "Blurred" }
                : new[] { "Any", "Official", "Custom", "Alternate", "Blurred", "Material" });
            if (kind != "icons")
                AddChoice(panel, "Languages", property + ".Languages", l, "Any", "Polski", "English", "Deutsch", "Français", "Español", "日本語");
            AddChoice(panel, "File Type", property + ".FileType", l, kind == "icons"
                ? new[] { "Any", "PNG", "ICO" } : new[] { "Any", "PNG", "JPEG", "WEBP" });
            AddChoice(panel, "Sort", property + ".Sort", l, "Highest Score (Beta)", "Lowest Score (Beta)",
                "Highest Score (Old)", "Lowest Score (Old)", "Newest First", "Oldest First", "Most Likes", "Fewest Likes");
            AddChoice(panel, "Types", property + ".Types", l, "Any", "Static", "Animated");
            AddChoice(panel, "Tags", property + ".Tags", l, "Any", "Untagged", "Humor", "Adult Content", "Epilepsy");
            return new TabItem { Header = title, Content = panel };
        }
        private static void AddChoice(Panel panel, string label, string property, GalleryText l, params string[] values)
        {
            panel.Children.Add(new TextBlock { Text = l.T(label), Margin = new Thickness(0, 8, 0, 3), Foreground = Brushes.White });
            var combo = new ComboBox { ItemsSource = values.Select(x => new GalleryChoice(x, l.T(x))).ToArray(), DisplayMemberPath = "Label", SelectedValuePath = "Value", MinWidth = 210,
                HorizontalAlignment = HorizontalAlignment.Left, Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(34, 51, 72)) };
            var style = new Style(typeof(ComboBoxItem));
            style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(34, 51, 72))));
            style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            combo.ItemContainerStyle = style;
            combo.SetBinding(ComboBox.SelectedValueProperty, new Binding(property)
            { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(combo);
        }
    }

    internal sealed class GalleryChoice
    {
        public string Value { get; }
        public string Label { get; }
        public GalleryChoice(string value, string label) { Value = value; Label = label; }
    }

    internal sealed class GalleryText
    {
        private readonly string language;
        private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>
        {
            // English, German, French, Spanish. Missing entries fall back to Polish.
            { "Język interfejsu", new[] { "Interface language", "Sprache der Oberfläche", "Langue de l'interface", "Idioma de la interfaz" } },
            { "Auto", new[] { "Automatic", "Automatisch", "Automatique", "Automático" } },
            { "Pobierz klucz API", new[] { "Get API key", "API-Schlüssel abrufen", "Obtenir une clé API", "Obtener clave API" } },
            { "Klucz API SteamGridDB:", new[] { "SteamGridDB API key:", "SteamGridDB API-Schlüssel:", "Clé API SteamGridDB :", "Clave API de SteamGridDB:" } },
            { "Klucz utworzysz w profilu SteamGridDB. Przy ręcznym pobieraniu metadanych pojawi się galeria okładek, teł i ikon. Kliknij grafiki, które chcesz przypisać.", new[] { "Create a key in your SteamGridDB profile. During manual metadata download, select covers, backgrounds and icons in the gallery.", "Erstelle einen Schlüssel in deinem SteamGridDB-Profil. Beim manuellen Abruf wähle Cover, Hintergründe und Symbole in der Galerie.", "Créez une clé dans votre profil SteamGridDB. Lors du téléchargement manuel, choisissez les couvertures, arrière-plans et icônes dans la galerie.", "Crea una clave en tu perfil de SteamGridDB. Durante la descarga manual, elige portadas, fondos e iconos en la galería." } },
            { "Filtry galerii", new[] { "Gallery filters", "Galeriefilter", "Filtres de la galerie", "Filtros de la galería" } },
            { "Filtr Tags wybiera jeden rodzaj tagu. Dostępność grafik zależy od rodzaju obrazu i gry.", new[] { "The Tags filter selects one tag category. Available artwork depends on the image type and game.", "Der Tag-Filter wählt eine Tag-Kategorie. Verfügbare Bilder hängen von Bildtyp und Spiel ab.", "Le filtre Tags sélectionne une catégorie. Les images disponibles dépendent du type et du jeu.", "El filtro Tags selecciona una categoría. Las imágenes disponibles dependen del tipo y del juego." } },
            { "Wpisz klucz API w ustawieniach SteamGridDB Gallery.", new[] { "Enter an API key in SteamGridDB Gallery settings.", "Gib einen API-Schlüssel in den SteamGridDB Gallery-Einstellungen ein.", "Saisissez une clé API dans les paramètres de SteamGridDB Gallery.", "Introduce una clave API en los ajustes de SteamGridDB Gallery." } },
            { "Wybierz grę w SteamGridDB", new[] { "Choose a game on SteamGridDB", "Spiel auf SteamGridDB auswählen", "Choisir un jeu sur SteamGridDB", "Elegir un juego en SteamGridDB" } },
            { "SteamGridDB nie odpowiedział. Sprawdź połączenie i klucz API.", new[] { "SteamGridDB did not respond. Check your connection and API key.", "SteamGridDB antwortet nicht. Prüfe Verbindung und API-Schlüssel.", "SteamGridDB ne répond pas. Vérifiez la connexion et la clé API.", "SteamGridDB no responde. Comprueba la conexión y la clave API." } },
            { "Nie udało się odczytać odpowiedzi SteamGridDB.", new[] { "Could not read the SteamGridDB response.", "SteamGridDB-Antwort konnte nicht gelesen werden.", "Impossible de lire la réponse de SteamGridDB.", "No se pudo leer la respuesta de SteamGridDB." } },
            { "Pobieranie grafik nie powiodło się. Sprawdź połączenie ze SteamGridDB.", new[] { "Artwork download failed. Check your connection to SteamGridDB.", "Bilddownload fehlgeschlagen. Prüfe die Verbindung zu SteamGridDB.", "Échec du téléchargement des images. Vérifiez la connexion à SteamGridDB.", "Falló la descarga de imágenes. Comprueba la conexión con SteamGridDB." } },
            { "SteamGridDB — wybierz grafiki: ", new[] { "SteamGridDB — choose artwork: ", "SteamGridDB — Bilder auswählen: ", "SteamGridDB — choisir des images : ", "SteamGridDB — elegir imágenes: " } },
            { "Zastosuj wybrane", new[] { "Apply selected", "Auswahl übernehmen", "Appliquer la sélection", "Aplicar selección" } },
            { "Anuluj", new[] { "Cancel", "Abbrechen", "Annuler", "Cancelar" } },
            { "Okładki", new[] { "Covers", "Cover", "Couvertures", "Portadas" } },
            { "Tła", new[] { "Backgrounds", "Hintergründe", "Arrière-plans", "Fondos" } },
            { "Ikony", new[] { "Icons", "Symbole", "Icônes", "Iconos" } },
            { "Nie udało się pobrać kolejnej strony grafik SteamGridDB.", new[] { "Could not load more SteamGridDB artwork.", "Weitere SteamGridDB-Bilder konnten nicht geladen werden.", "Impossible de charger d'autres images SteamGridDB.", "No se pudieron cargar más imágenes de SteamGridDB." } },
            { "Nie udało się zmienić sortowania grafik.", new[] { "Could not change artwork sorting.", "Bildsortierung konnte nicht geändert werden.", "Impossible de changer le tri des images.", "No se pudo cambiar el orden de las imágenes." } },
            { "Wczytaj kolejne grafiki", new[] { "Load more artwork", "Weitere Bilder laden", "Charger plus d'images", "Cargar más imágenes" } },
            { "Brak pasujących grafik. Spróbuj innej nazwy gry lub sprawdź klucz API.", new[] { "No matching artwork. Try another game title or check your API key.", "Keine passenden Bilder. Versuche einen anderen Spielnamen oder prüfe den API-Schlüssel.", "Aucune image correspondante. Essayez un autre titre ou vérifiez la clé API.", "No hay imágenes coincidentes. Prueba otro título o comprueba la clave API." } },
            { "Animacja WEBM\nPodgląd wideo niedostępny", new[] { "WEBM animation\nVideo preview unavailable", "WEBM-Animation\nVideovorschau nicht verfügbar", "Animation WEBM\nAperçu vidéo indisponible", "Animación WEBM\nVista previa no disponible" } },
            { "Rozmiar: wczytywanie", new[] { "Size: loading", "Größe: wird geladen", "Taille : chargement", "Tamaño: cargando" } },
            { "Rozmiar: nieznany", new[] { "Size: unknown", "Größe: unbekannt", "Taille : inconnue", "Tamaño: desconocido" } },
            { "Polubienia: wczytywanie", new[] { "Likes: loading", "Likes: werden geladen", "J'aime : chargement", "Me gusta: cargando" } },
            { "Polubienia: ", new[] { "Likes: ", "Likes: ", "J'aime : ", "Me gusta: " } },
            { "Polubienia: niedostępne", new[] { "Likes: unavailable", "Likes: nicht verfügbar", "J'aime : indisponibles", "Me gusta: no disponibles" } },
            { " (mała ikona)", new[] { " (small icon)", " (kleines Symbol)", " (petite icône)", " (icono pequeño)" } },
            { "Dimensions", new[] { "Dimensions", "Abmessungen", "Dimensions", "Dimensiones" } },
            { "Styles", new[] { "Styles", "Stile", "Styles", "Estilos" } },
            { "Languages", new[] { "Languages", "Sprachen", "Langues", "Idiomas" } },
            { "File Type", new[] { "File type", "Dateityp", "Type de fichier", "Tipo de archivo" } },
            { "Sort", new[] { "Sort", "Sortieren", "Tri", "Ordenar" } },
            { "Types", new[] { "Types", "Typen", "Types", "Tipos" } },
            { "Tags", new[] { "Tags", "Tags", "Étiquettes", "Etiquetas" } },
            { "Any", new[] { "Any", "Alle", "Tous", "Todos" } },
            { "Official", new[] { "Official", "Offiziell", "Officiel", "Oficial" } },
            { "Custom", new[] { "Custom", "Benutzerdefiniert", "Personnalisé", "Personalizado" } },
            { "Alternate", new[] { "Alternate", "Alternativ", "Alternatif", "Alternativo" } },
            { "Blurred", new[] { "Blurred", "Unscharf", "Flou", "Difuminado" } },
            { "Material", new[] { "Material", "Material", "Matériau", "Material" } },
            { "Static", new[] { "Static", "Statisch", "Statique", "Estático" } },
            { "Animated", new[] { "Animated", "Animiert", "Animé", "Animado" } },
            { "Untagged", new[] { "Untagged", "Ohne Tags", "Sans étiquette", "Sin etiquetas" } },
            { "Humor", new[] { "Humor", "Humor", "Humour", "Humor" } },
            { "Adult Content", new[] { "Adult content", "Nicht jugendfrei", "Contenu adulte", "Contenido adulto" } },
            { "Epilepsy", new[] { "Epilepsy", "Epilepsie", "Épilepsie", "Epilepsia" } },
            { "Highest Score (Beta)", new[] { "Highest score (Beta)", "Höchste Bewertung (Beta)", "Meilleur score (bêta)", "Mayor puntuación (beta)" } },
            { "Lowest Score (Beta)", new[] { "Lowest score (Beta)", "Niedrigste Bewertung (Beta)", "Score le plus bas (bêta)", "Menor puntuación (beta)" } },
            { "Highest Score (Old)", new[] { "Highest score (Old)", "Höchste Bewertung (alt)", "Meilleur score (ancien)", "Mayor puntuación (antiguo)" } },
            { "Lowest Score (Old)", new[] { "Lowest score (Old)", "Niedrigste Bewertung (alt)", "Score le plus bas (ancien)", "Menor puntuación (antiguo)" } },
            { "Ładowanie wszystkich grafik…", new[] { "Loading all artwork…", "Alle Bilder werden geladen…", "Chargement de toutes les images…", "Cargando todas las imágenes…" } },
            { "Wszystkie grafiki wczytane", new[] { "All artwork loaded", "Alle Bilder geladen", "Toutes les images sont chargées", "Todas las imágenes cargadas" } },
            { "Most Likes", new[] { "Most likes", "Meiste Likes", "Plus de mentions J'aime", "Más me gusta" } },
            { "Fewest Likes", new[] { "Fewest likes", "Wenigste Likes", "Moins de mentions J'aime", "Menos me gusta" } },
            { "Newest First", new[] { "Newest first", "Neueste zuerst", "Plus récents d'abord", "Más recientes primero" } },
            { "Oldest First", new[] { "Oldest first", "Älteste zuerst", "Plus anciens d'abord", "Más antiguos primero" } }
        };
        public GalleryText(string requested)
        {
            language = requested == "Auto" || string.IsNullOrEmpty(requested)
                ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : requested == "Polski" ? "pl"
                : requested == "Deutsch" ? "de" : requested == "Français" ? "fr"
                : requested == "Español" ? "es" : requested == "English" ? "en" : "pl";
        }
        public string T(string source)
        {
            if (language == "pl") return source;
            string[] variants;
            if (!Translations.TryGetValue(source, out variants)) return source;
            var index = language == "de" ? 1 : language == "fr" ? 2 : language == "es" ? 3 : 0;
            return variants[index];
        }
    }

    internal sealed class Artwork
    {
        public string Url;
        public string Thumb;
        public int Width;
        public int Height;
        public string[] IconSizes;
        public int IconMinSize;
        public int IconMaxSize;
        public bool DimensionsChecked;
        public double Score;
        public bool HasScore;
        public int? Likes;
        public bool LikesRequested;
        public bool LikesCompleted;
        public event Action<int?> LikesChanged;
        public void SetLikes(int? value)
        {
            Likes = value;
            LikesCompleted = true;
            LikesChanged?.Invoke(value);
        }
        public long Id;
        public string Style;
        public string Language;
        public string Mime;
        public string Type;
        public bool Animated;
        public string FakePng;
        public string[] Tags;
        public string Title;
    }

    internal sealed class GalleryProvider : OnDemandMetadataProvider
    {
        private readonly string cover;
        private readonly string background;
        private readonly string icon;
        public override List<MetadataField> AvailableFields { get; }

        public GalleryProvider(IPlayniteAPI api, MetadataRequestOptions options, GallerySettings settings)
        {
            var filters = settings.Snapshot();
            var l = new GalleryText(filters.UiLanguage);
            AvailableFields = new List<MetadataField>();
            if (string.IsNullOrWhiteSpace(options.GameData?.Name)) return;
            if (string.IsNullOrWhiteSpace(filters.ApiKey))
            {
                if (!options.IsBackgroundDownload)
                    RunOnUi(api, () => { api.Dialogs.ShowMessage(l.T("Wpisz klucz API w ustawieniach SteamGridDB Gallery.")); });
                return;
            }
            try
            {
                var client = new SteamGridClient(filters.ApiKey.Trim());
                var names = new[] { options.GameData.Name, WithoutEdition(options.GameData.Name) }
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);
                var covers = new List<Artwork>();
                var backgrounds = new List<Artwork>();
                var icons = new List<Artwork>();
                var matches = new List<Tuple<int, string>>();
                foreach (var name in names)
                {
                    int? id;
                    try { id = client.FindExactGame(name); }
                    catch (WebException) { continue; }
                    if (!id.HasValue) continue;
                    if (matches.All(x => x.Item1 != id.Value)) matches.Add(Tuple.Create(id.Value, name));
                    var coverTask = Task.Run(() => client.GetArtwork("grids", id.Value, name, 0, filters.Covers.Sort));
                    var heroTask = Task.Run(() => client.GetArtwork("heroes", id.Value, name, 0, filters.Backgrounds.Sort));
                    var iconTask = Task.Run(() => client.GetArtwork("icons", id.Value, name, 0, filters.Icons.Sort));
                    Task.WaitAll(coverTask, heroTask, iconTask);
                    covers.AddRange(coverTask.Result);
                    backgrounds.AddRange(heroTask.Result);
                    icons.AddRange(iconTask.Result);
                }
                if (covers.Count == 0 && backgrounds.Count == 0 && icons.Count == 0 && !options.IsBackgroundDownload)
                {
                    var chosen = RunOnUi(api, () => api.Dialogs.ChooseItemWithSearch(
                        client.SearchGames(options.GameData.Name),
                        query => client.SearchGames(query), options.GameData.Name,
                        l.T("Wybierz grę w SteamGridDB")));
                    if (chosen == null) return;
                    int chosenId;
                    if (chosen != null && int.TryParse(chosen.Description, out chosenId) && chosenId > 0)
                    {
                        matches.Add(Tuple.Create(chosenId, chosen.Name));
                        var coverTask = Task.Run(() => client.GetArtwork("grids", chosenId, chosen.Name, 0, filters.Covers.Sort));
                        var heroTask = Task.Run(() => client.GetArtwork("heroes", chosenId, chosen.Name, 0, filters.Backgrounds.Sort));
                        var iconTask = Task.Run(() => client.GetArtwork("icons", chosenId, chosen.Name, 0, filters.Icons.Sort));
                        Task.WaitAll(coverTask, heroTask, iconTask);
                        covers.AddRange(coverTask.Result);
                        backgrounds.AddRange(heroTask.Result);
                        icons.AddRange(iconTask.Result);
                    }
                }
                if (options.IsBackgroundDownload)
                {
                    cover = SelectAutomatic(client, matches, covers, "grids", filters.Covers)?.Url;
                    background = SelectAutomatic(client, matches, backgrounds, "heroes", filters.Backgrounds)?.Url;
                    icon = SelectAutomatic(client, matches, icons, "icons", filters.Icons)?.Url;
                    if (cover != null) AvailableFields.Add(MetadataField.CoverImage);
                    if (background != null) AvailableFields.Add(MetadataField.BackgroundImage);
                    if (icon != null) AvailableFields.Add(MetadataField.Icon);
                    return;
                }
                var result = RunOnUi(api, () => ArtworkGallery.Show(api, options.GameData.Name,
                    covers, backgrounds, icons, filters, (kind, page, order) =>
                    {
                        var loaded = new List<Artwork>();
                        foreach (var match in matches)
                            loaded.AddRange(client.GetArtwork(kind, match.Item1, match.Item2, page, order));
                        return loaded;
                    }, (kind, imageId) => client.GetLikes(kind, imageId)));
                if (result == null) return;
                cover = result[0];
                background = result[1];
                icon = result[2];
                if (cover != null) AvailableFields.Add(MetadataField.CoverImage);
                if (background != null) AvailableFields.Add(MetadataField.BackgroundImage);
                if (icon != null) AvailableFields.Add(MetadataField.Icon);
            }
            catch (WebException)
            {
                if (!options.IsBackgroundDownload)
                    RunOnUi(api, () => { api.Dialogs.ShowMessage(l.T("SteamGridDB nie odpowiedział. Sprawdź połączenie i klucz API.")); });
            }
            catch (InvalidOperationException)
            {
                if (!options.IsBackgroundDownload)
                    RunOnUi(api, () => { api.Dialogs.ShowMessage(l.T("Nie udało się odczytać odpowiedzi SteamGridDB.")); });
            }
            catch (AggregateException)
            {
                if (!options.IsBackgroundDownload)
                    RunOnUi(api, () => { api.Dialogs.ShowMessage(l.T("Pobieranie grafik nie powiodło się. Sprawdź połączenie ze SteamGridDB.")); });
            }
        }
        private static Artwork SelectAutomatic(SteamGridClient client, List<Tuple<int, string>> matches,
            List<Artwork> firstPage, string kind, GalleryFilters options)
        {
            var artwork = ArtworkGallery.Filter(firstPage, options).FirstOrDefault();
            if (artwork != null) return artwork;
            // A restrictive filter may have no match on the first page.
            for (var page = 1; page < 6; page++)
            {
                var batch = new List<Artwork>();
                foreach (var match in matches)
                    batch.AddRange(client.GetArtwork(kind, match.Item1, match.Item2, page, options.Sort));
                artwork = ArtworkGallery.Filter(batch, options).FirstOrDefault();
                if (artwork != null || batch.Count < 48) return artwork;
            }
            return null;
        }
        private static string WithoutEdition(string title)
        {
            var shorter = Regex.Replace(title ?? "", @"\s*(?:[:\-–]\s*)?(?:ultimate|complete|deluxe|gold|legendary)\s+edition\s*$",
                "", RegexOptions.IgnoreCase).Trim();
            return shorter == title ? null : shorter;
        }
        private static T RunOnUi<T>(IPlayniteAPI api, Func<T> action) =>
            api.MainView.UIDispatcher.CheckAccess() ? action() : api.MainView.UIDispatcher.Invoke(action);
        private static void RunOnUi(IPlayniteAPI api, Action action)
        {
            if (api.MainView.UIDispatcher.CheckAccess()) action();
            else api.MainView.UIDispatcher.Invoke(action);
        }
        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args) => cover == null ? null : new MetadataFile(cover);
        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args) => background == null ? null : new MetadataFile(background);
        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            if (icon == null) return null;
            try
            {
                if (!new Uri(icon).AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                    return new MetadataFile(icon);
                var request = WebRequest.Create(icon);
                request.Timeout = 15000;
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var best = decoder.Frames.OrderByDescending(x => (long)x.PixelWidth * x.PixelHeight).FirstOrDefault();
                    if (best == null) return new MetadataFile(icon);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(best));
                    using (var output = new MemoryStream())
                    {
                        encoder.Save(output);
                        return new MetadataFile("SteamGridDB-icon.png", output.ToArray(), icon);
                    }
                }
            }
            catch (Exception) { return new MetadataFile(icon); }
        }
    }

    internal sealed class SteamGridClient
    {
        private readonly string key;
        public SteamGridClient(string key) { this.key = key; }
        public int? FindExactGame(string name)
        {
            foreach (var game in GetData("search/autocomplete/" + Uri.EscapeDataString(name.Trim())))
            {
                if (Normalize(GetString(game, "name")) != Normalize(name)) continue;
                int id;
                if (int.TryParse(GetString(game, "id"), out id) && id > 0) return id;
            }
            return null;
        }
        public List<GenericItemOption> SearchGames(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<GenericItemOption>();
            try
            {
                return GetData("search/autocomplete/" + Uri.EscapeDataString(name.Trim()))
                    .Select(x => new GenericItemOption(GetString(x, "name"), GetString(x, "id")))
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
            }
            catch (WebException) { return new List<GenericItemOption>(); }
        }
        public List<Artwork> GetArtwork(string kind, int id, string title, int page = 0, string sort = "Highest Score (Beta)")
        {
            try
            {
                var publicResults = GetPublicAssets(kind, id, page, sort);
                if (publicResults != null) return publicResults.Select(x => ParseArtwork(x, title))
                    .Where(x => IsHttps(x.Url)).ToList();
                var path = kind + "/game/" + id + "?limit=50" + (page > 0 ? "&page=" + page : "");
                return GetData(path).Select(x => ParseArtwork(x, title))
                    .Where(x => IsHttps(x.Url)).ToList();
            }
            catch (WebException) { return new List<Artwork>(); }
        }
        private static Artwork ParseArtwork(Dictionary<string, object> x, string title)
        {
            return new Artwork
                {
                    Url = GetString(x, "url"), Thumb = GetString(x, "thumb"),
                    Width = GetInt(x, "width"), Height = GetInt(x, "height"),
                    Score = GetDouble(x, "score"), HasScore = x.ContainsKey("score") && x["score"] != null,
                    Likes = GetOptionalInt(x, "hearts"),
                    LikesCompleted = GetOptionalInt(x, "hearts").HasValue,
                    Id = GetLong(x, "id"), Title = title, Style = GetString(x, "style"),
                    Language = GetString(x, "language"), Mime = GetString(x, "mime"),
                    Type = GetString(x, "type"), Animated = GetBool(x, "is_animated"),
                    FakePng = GetString(x, "fake_png"), Tags = GetTags(x)
                };
        }
        private List<Dictionary<string, object>> GetPublicAssets(string kind, int id, int page, string sort)
        {
            var order = sort == "Lowest Score (Beta)" ? "score_asc" :
                sort == "Highest Score (Old)" ? "score_old_desc" :
                sort == "Lowest Score (Old)" ? "score_old_asc" :
                sort == "Newest First" ? "age_desc" :
                sort == "Oldest First" ? "age_asc" : "score_desc";
            var body = new Dictionary<string, object>
            {
                { "styles", new[] { "all" } }, { "languages", new[] { "all" } },
                { "dimensions", new[] { "all" } }, { "formats", new[] { "all" } },
                { "order", order }, { "game_id", new[] { id } },
                { "static", true }, { "animated", true }, { "nsfw", true },
                { "epilepsy", true }, { "humor", true }, { "untagged", true },
                { "asset_type", kind == "grids" ? "grid" : kind == "heroes" ? "hero" : "icon" },
                { "page", page }, { "limit", 48 }, { "user_steam64", null }, { "user_steam64_likes", null }
            };
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://www.steamgriddb.com/api/public/search/assets");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.UserAgent = "SteamGridDBGallery/1.7";
                request.Timeout = 7000;
                using (var stream = new StreamWriter(request.GetRequestStream()))
                    stream.Write(new JavaScriptSerializer().Serialize(body));
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var root = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }
                        .DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
                    object value, assets;
                    if (root != null && root.TryGetValue("success", out value) && value is bool && (bool)value &&
                        root.TryGetValue("data", out value) && value is Dictionary<string, object> &&
                        ((Dictionary<string, object>)value).TryGetValue("assets", out assets) && assets is object[])
                        return ((object[])assets).OfType<Dictionary<string, object>>().ToList();
                }
            }
            catch (WebException) { }
            return null;
        }
        public int? GetLikes(string kind, long id)
        {
            if (id <= 0) return null;
            var singular = kind == "grids" ? "grid" : kind == "heroes" ? "hero" : "icon";
            // This is the public asset endpoint used to preload the website's detail view.
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://www.steamgriddb.com/api/public/asset/" + singular + "/" + id);
                request.Timeout = 10000;
                request.UserAgent = "SteamGridDBGallery/1.7";
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }
                        .DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
                    object data;
                    if (json != null && json.TryGetValue("data", out data))
                    {
                        var detail = data as Dictionary<string, object>;
                        if (detail != null)
                        {
                            var count = GetOptionalInt(detail, "hearts");
                            if (count.HasValue) return count;
                            object nested;
                            if (detail.TryGetValue("asset", out nested))
                            {
                                var asset = nested as Dictionary<string, object>;
                                if (asset != null) return GetOptionalInt(asset, "hearts");
                            }
                        }
                    }
                }
            }
            catch (WebException) { }
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://www.steamgriddb.com/" + singular + "/" + id);
                request.Timeout = 10000;
                request.UserAgent = "Mozilla/5.0 (compatible; SteamGridDBGallery/1.7)";
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var html = reader.ReadToEnd();
                    var match = Regex.Match(html, @"<button\b[^>]*class\s*=\s*['""'][^'""']*btn-link[^'""']*['""'][^>]*>\s*(\d[\d,. ]*)\s+likes?\s*</button>", RegexOptions.IgnoreCase);
                    int likes;
                    return match.Success && int.TryParse(Regex.Replace(match.Groups[1].Value, @"\D", ""), out likes) ? (int?)likes : null;
                }
            }
            catch (WebException) { return null; }
        }
        private static string[] GetTags(Dictionary<string, object> obj)
        {
            object value;
            return obj.TryGetValue("tags", out value) && value is object[]
                ? ((object[])value).Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)).ToArray()
                : new string[0];
        }
        private List<Dictionary<string, object>> GetData(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.steamgriddb.com/api/v2/" + path);
            request.Timeout = 12000;
            request.UserAgent = "SteamGridDBGallery/1.1";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + key;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var json = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }
                    .DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
                object value;
                var data = json != null && json.TryGetValue("data", out value) ? value as object[] : null;
                return data == null ? new List<Dictionary<string, object>>() :
                    data.OfType<Dictionary<string, object>>().ToList();
            }
        }
        private static string Normalize(string value)
        {
            var normalized = (value ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var text = new StringBuilder();
            foreach (var ch in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) text.Append(ch);
            return Regex.Replace(text.ToString().Replace("ł", "l"), @"[^\p{L}\p{N}]+", " ").Trim();
        }
        private static string GetString(Dictionary<string, object> obj, string key)
        {
            object value;
            return obj != null && obj.TryGetValue(key, out value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
        }
        private static int GetInt(Dictionary<string, object> obj, string key)
        {
            int value;
            return int.TryParse(GetString(obj, key), out value) ? value : 0;
        }
        private static int? GetOptionalInt(Dictionary<string, object> obj, string key)
        {
            int value;
            return obj.ContainsKey(key) && int.TryParse(GetString(obj, key), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value) ? (int?)value : null;
        }
        private static bool GetBool(Dictionary<string, object> obj, string key)
        {
            object value;
            return obj.TryGetValue(key, out value) && value != null &&
                (value is bool ? (bool)value : string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), "true", StringComparison.OrdinalIgnoreCase));
        }
        private static long GetLong(Dictionary<string, object> obj, string key)
        {
            long value;
            return long.TryParse(GetString(obj, key), out value) ? value : 0;
        }
        private static double GetDouble(Dictionary<string, object> obj, string key)
        {
            double value;
            return double.TryParse(GetString(obj, key), NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : 0;
        }
        private static bool IsHttps(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Scheme == "https";
        }
    }

    internal static class ArtworkGallery
    {
        private static readonly SemaphoreSlim LikeRequests = new SemaphoreSlim(3);
        private static readonly SemaphoreSlim SizeRequests = new SemaphoreSlim(3);
        private sealed class ImageDimensions
        {
            public int Width;
            public int Height;
            public string[] Sizes;
            public int MinSize;
            public int MaxSize;
            public BitmapSource Preview;
        }
        private static bool IsIco(string url)
        {
            Uri parsed;
            return Uri.TryCreate(url, UriKind.Absolute, out parsed) &&
                parsed.AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
        }
        private static ImageDimensions ReadDimensions(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                request.Timeout = 10000;
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var decoder = IsIco(url)
                        ? (BitmapDecoder)new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad)
                        : BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var frames = decoder.Frames.OrderByDescending(f => (long)f.PixelWidth * f.PixelHeight).ToList();
                    var best = frames.FirstOrDefault();
                    if (best == null) return null;
                    var image = new ImageDimensions { Width = best.PixelWidth, Height = best.PixelHeight };
                    if (IsIco(url))
                    {
                        image.Sizes = frames.Select(f => f.PixelWidth + "×" + f.PixelHeight)
                            .Distinct().ToArray();
                        image.MinSize = frames.Min(f => Math.Min(f.PixelWidth, f.PixelHeight));
                        image.MaxSize = frames.Max(f => Math.Max(f.PixelWidth, f.PixelHeight));
                        best.Freeze();
                        image.Preview = best;
                    }
                    return image;
                }
            }
            catch (Exception) { return null; }
        }
        public static string[] Show(IPlayniteAPI api, string game,
            List<Artwork> covers, List<Artwork> backgrounds, List<Artwork> icons, GallerySettings filters,
            Func<string, int, string, List<Artwork>> loadPage, Func<string, long, int?> loadLikes)
        {
            var l = new GalleryText(filters.UiLanguage);
            var selected = new string[3];
            var window = api.Dialogs.CreateWindow(new WindowCreationOptions { ShowMinimizeButton = false });
            window.Title = l.T("SteamGridDB — wybierz grafiki: ") + game;
            window.Width = 950;
            window.Height = 720;
            window.MinWidth = 650;
            window.MinHeight = 450;
            window.Owner = api.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var root = new DockPanel { Margin = new Thickness(12) };
            var actions = new StackPanel { Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var accept = new Button { Content = l.T("Zastosuj wybrane"), Width = 150, IsDefault = true,
                Margin = new Thickness(0, 0, 8, 0) };
            accept.Click += (sender, args) => window.DialogResult = true;
            var cancel = new Button { Content = l.T("Anuluj"), Width = 100, IsCancel = true };
            cancel.Click += (sender, args) => window.DialogResult = false;
            actions.Children.Add(accept);
            actions.Children.Add(cancel);
            DockPanel.SetDock(actions, Dock.Bottom);
            root.Children.Add(actions);

            var tabs = new TabControl();
            var all = new[] { covers, backgrounds, icons };
            var kinds = new[] { "grids", "heroes", "icons" };
            var labels = new[] { l.T("Okładki"), l.T("Tła"), l.T("Ikony") };
            var tabFilters = new[] { filters.Covers.Snapshot(), filters.Backgrounds.Snapshot(), filters.Icons.Snapshot() };
            var nextPage = new[] { 1, 1, 1 };
            var hasMore = new[] { covers.Count >= 48, backgrounds.Count >= 48, icons.Count >= 48 };
            var loading = new bool[3];
            // Download all pages, but create controls for only a small visible batch.
            var shown = new[] { 72, 72, 72 };
            var progress = new TextBlock { Foreground = Brushes.White, Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center, Text = l.T("Ładowanie wszystkich grafik…") };
            actions.Children.Insert(0, progress);
            Action refresh = null;
            Func<int, Task> updateLikeSort = async index =>
            {
                if (tabFilters[index].Sort != "Most Likes" && tabFilters[index].Sort != "Fewest Likes") return;
                var pending = all[index].Where(x => !x.LikesCompleted).ToList();
                foreach (var artwork in pending) artwork.LikesRequested = true;
                var tasks = pending.Select(async artwork =>
                {
                    await LikeRequests.WaitAsync();
                    try { return await Task.Run(() => loadLikes(kinds[index], artwork.Id)); }
                    catch (Exception) { return (int?)null; }
                    finally { LikeRequests.Release(); }
                }).ToArray();
                var counts = await Task.WhenAll(tasks);
                for (var i = 0; i < pending.Count; i++) pending[i].SetLikes(counts[i]);
            };
            Func<int, Task> fetch = null;
            Func<int, Task> loadAll = null;
            Action<int> reload = null;
            refresh = () =>
            {
                var active = tabs.SelectedIndex;
                tabs.Items.Clear();
                for (var i = 0; i < 3; i++)
                {
                    var index = i;
                    var filtered = Filter(all[i], tabFilters[i]);
                    tabs.Items.Add(MakeTab(labels[i], filtered, shown[i], selected, i,
                        kinds[i], tabFilters[i], refresh, () => reload(index), filtered.Count > shown[i],
                        () => { shown[index] += 72; refresh(); }, loadLikes, l));
                }
                tabs.SelectedIndex = active >= 0 ? active : 0;
            };
            fetch = async index =>
            {
                if (loading[index] || !hasMore[index]) return;
                loading[index] = true;
                try
                {
                    var page = nextPage[index];
                    var batch = await Task.Run(() => loadPage(kinds[index], page, tabFilters[index].Sort));
                    if (!window.IsVisible) return;
                    nextPage[index]++;
                    var existing = new HashSet<string>(all[index].Select(x => x.Url), StringComparer.OrdinalIgnoreCase);
                    var fresh = batch.Where(x => existing.Add(x.Url)).ToList();
                    all[index].AddRange(fresh);
                    hasMore[index] = batch.Count >= 48 && fresh.Count > 0;
                    progress.Text = l.T("Ładowanie wszystkich grafik…") + " " + labels[index] + ": " + all[index].Count;
                }
                catch (Exception)
                {
                    hasMore[index] = false;
                    if (window.IsVisible) progress.Text = l.T("Nie udało się pobrać kolejnej strony grafik SteamGridDB.");
                }
                finally { loading[index] = false; }
            };
            loadAll = async index =>
            {
                while (window.IsVisible && hasMore[index])
                {
                    if (loading[index]) { await Task.Delay(100); continue; }
                    await fetch(index);
                }
                if (window.IsVisible)
                {
                    await updateLikeSort(index);
                    refresh();
                }
            };
            reload = async index =>
            {
                if (loading[index]) return;
                loading[index] = true;
                try
                {
                    var batch = await Task.Run(() => loadPage(kinds[index], 0, tabFilters[index].Sort));
                    if (!window.IsVisible) return;
                    all[index].Clear();
                    all[index].AddRange(batch);
                    nextPage[index] = 1;
                    hasMore[index] = batch.Count >= 48;
                    refresh();
                }
                catch (Exception)
                {
                    hasMore[index] = false;
                    if (window.IsVisible) api.Dialogs.ShowMessage(l.T("Nie udało się zmienić sortowania grafik."));
                }
                finally { loading[index] = false; }
                if (window.IsVisible) await loadAll(index);
            };
            refresh();
            root.Children.Add(tabs);
            window.Content = root;
            window.Loaded += async (sender, args) =>
            {
                await Task.WhenAll(Enumerable.Range(0, 3).Select(loadAll));
                if (window.IsVisible) progress.Text = l.T("Wszystkie grafiki wczytane");
            };
            return window.ShowDialog() == true ? selected : null;
        }
        private static void AddTabFilters(Panel bar, string kind, GalleryFilters f, Action refresh, Action reload, GalleryText l)
        {
            AddFilter(bar, "Dimensions", f.Dimensions, kind == "icons"
                ? new[] { "Any", "256x256", "512x512", "1024x1024" }
                : kind == "heroes" ? new[] { "Any", "1920x620", "1600x650", "3840x1240" }
                : new[] { "Any", "1024x1024", "512x512", "342x482", "660x930", "460x215", "920x430", "600x900" }, x => f.Dimensions = x, refresh, l);
            AddFilter(bar, "Styles", f.Styles, kind == "icons"
                ? new[] { "Any", "Official", "Custom" }
                : kind == "heroes" ? new[] { "Any", "Official", "Custom", "Alternate", "Blurred" }
                : new[] { "Any", "Official", "Custom", "Alternate", "Blurred", "Material" }, x => f.Styles = x, refresh, l);
            if (kind != "icons")
                AddFilter(bar, "Languages", f.Languages, new[] { "Any", "Polski", "English", "Deutsch", "Français", "Español", "日本語" }, x => f.Languages = x, refresh, l);
            AddFilter(bar, "File Type", f.FileType, kind == "icons"
                ? new[] { "Any", "PNG", "ICO" } : new[] { "Any", "PNG", "JPEG", "WEBP" }, x => f.FileType = x, refresh, l);
            AddFilter(bar, "Sort", f.Sort, new[] { "Highest Score (Beta)", "Lowest Score (Beta)", "Highest Score (Old)", "Lowest Score (Old)", "Newest First", "Oldest First", "Most Likes", "Fewest Likes" }, x => f.Sort = x, reload, l);
            AddFilter(bar, "Types", f.Types, new[] { "Any", "Static", "Animated" }, x => f.Types = x, refresh, l);
            AddFilter(bar, "Tags", f.Tags, new[] { "Any", "Untagged", "Humor", "Adult Content", "Epilepsy" }, x => f.Tags = x, refresh, l);
        }
        private static void AddFilter(Panel bar, string label, string value, string[] values,
            Action<string> update, Action refresh, GalleryText l)
        {
            var box = new StackPanel { Margin = new Thickness(0, 0, 8, 5) };
            box.Children.Add(new TextBlock { Text = l.T(label), Foreground = Brushes.White });
            var combo = new ComboBox { ItemsSource = values.Select(x => new GalleryChoice(x, l.T(x))).ToArray(), DisplayMemberPath = "Label", SelectedValuePath = "Value", SelectedValue = value, Width = 135,
                Background = new SolidColorBrush(Color.FromRgb(34, 51, 72)), Foreground = Brushes.White };
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(34, 51, 72))));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            combo.ItemContainerStyle = itemStyle;
            combo.SelectionChanged += (sender, args) =>
            {
                if (combo.SelectedItem == null) return;
                update((string)combo.SelectedValue);
                refresh();
            };
            box.Children.Add(combo);
            bar.Children.Add(box);
        }
        internal static List<Artwork> Filter(IEnumerable<Artwork> source, GalleryFilters filters)
        {
            var items = source.Where(x => filters.Styles == "Any" ||
                    string.Equals(x.Style, filters.Styles, StringComparison.OrdinalIgnoreCase))
                .Where(x => filters.Dimensions == "Any" ||
                    (x.Width + "x" + x.Height) == filters.Dimensions ||
                    (x.IconSizes != null && x.IconSizes.Contains(filters.Dimensions.Replace("x", "×"))))
                .Where(x => filters.Languages == "Any" ||
                    string.Equals(x.Language, LanguageCode(filters.Languages), StringComparison.OrdinalIgnoreCase))
                .Where(x => filters.FileType == "Any" ||
                    string.Equals(Path.GetExtension(new Uri(x.Url).AbsolutePath).TrimStart('.'),
                        filters.FileType == "JPEG" ? "jpg" : filters.FileType, StringComparison.OrdinalIgnoreCase) ||
                    (filters.FileType == "JPEG" && x.Url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)))
                .Where(x => filters.Types == "Any" ||
                    (filters.Types == "Animated") == (x.Animated || string.Equals(x.Type, "animated", StringComparison.OrdinalIgnoreCase) ||
                    x.Url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)))
                .Where(x => filters.Tags == "Any" || (filters.Tags == "Untagged"
                    ? x.Tags.Length == 0 : x.Tags.Any(t => string.Equals(t, TagCode(filters.Tags), StringComparison.OrdinalIgnoreCase))));
            var unique = items.GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase).Select(x => x.First());
            switch (filters.Sort)
            {
                case "Newest First": return unique.OrderByDescending(x => x.Id).ToList();
                case "Oldest First": return unique.OrderBy(x => x.Id).ToList();
                case "Most Likes": return unique.OrderBy(x => !x.Likes.HasValue)
                    .ThenByDescending(x => x.Likes ?? 0).ThenByDescending(x => x.Score).ToList();
                case "Fewest Likes": return unique.OrderBy(x => !x.Likes.HasValue)
                    .ThenBy(x => x.Likes ?? 0).ThenByDescending(x => x.Score).ToList();
                default: return unique.ToList(); // The public endpoint already applied the requested score ranking.
            }
        }
        private static string LanguageCode(string name)
        {
            switch (name)
            {
                case "Polski": return "pl"; case "English": return "en"; case "Deutsch": return "de";
                case "Français": return "fr"; case "Español": return "es"; case "日本語": return "ja";
                default: return name;
            }
        }
        private static string TagCode(string name) => name == "Adult Content" ? "nsfw" : name.ToLowerInvariant();
        private static TabItem MakeTab(string label, List<Artwork> artworks, int visibleLimit, string[] selection, int index,
            string kind, GalleryFilters filters, Action refresh, Action reload, bool hasMore, Action loadMore,
            Func<string, long, int?> loadLikes, GalleryText l)
        {
            var tab = new TabItem { Header = label + " (" + artworks.Count + ")" };
            var layout = new DockPanel();
            var bar = new WrapPanel { Margin = new Thickness(5, 8, 5, 8) };
            AddTabFilters(bar, kind, filters, refresh, reload, l);
            DockPanel.SetDock(bar, Dock.Top);
            layout.Children.Add(bar);
            if (hasMore)
            {
                var more = new Button { Content = l.T("Wczytaj kolejne grafiki"), Margin = new Thickness(8),
                    Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center };
                more.Click += (sender, args) => loadMore();
                DockPanel.SetDock(more, Dock.Bottom);
                layout.Children.Add(more);
            }
            var wrap = new WrapPanel();
            if (artworks.Count == 0)
                wrap.Children.Add(new TextBlock { Text = l.T("Brak pasujących grafik. Spróbuj innej nazwy gry lub sprawdź klucz API."),
                    Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White });
            foreach (var artwork in artworks.Take(visibleLimit))
            {
                var card = new StackPanel { Width = 175 };
                var video = artwork.Animated && (artwork.Thumb ?? artwork.Url).EndsWith(".webm", StringComparison.OrdinalIgnoreCase);
                if (video)
                {
                    var fallback = new TextBlock { Text = l.T("Animacja WEBM\nPodgląd wideo niedostępny"),
                        Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center, Height = 165,
                        VerticalAlignment = VerticalAlignment.Center };
                    card.Children.Add(fallback);
                }
                else try
                {
                    var preview = new BitmapImage();
                    preview.BeginInit();
                    preview.UriSource = new Uri(string.IsNullOrWhiteSpace(artwork.Thumb) ? artwork.Url : artwork.Thumb);
                    preview.DecodePixelWidth = 175;
                    preview.EndInit();
                    var previewControl = new Image { Source = preview, Height = 165, Stretch = Stretch.Uniform };
                    card.Children.Add(previewControl);
                }
                catch (UriFormatException) { }
                var sizeLabel = new TextBlock { Text = artwork.Width > 0 && artwork.Height > 0
                    ? SizeText(artwork, index, l) : l.T("Rozmiar: wczytywanie"),
                    HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap, MaxWidth = 165, TextAlignment = TextAlignment.Center };
                if (!video && ((artwork.Width <= 0 || artwork.Height <= 0) ||
                    (index == 2 && IsIco(artwork.Url))))
                    sizeLabel.Loaded += async (sender, args) =>
                    {
                        if (artwork.DimensionsChecked)
                        {
                            sizeLabel.Text = SizeText(artwork, index, l);
                            return;
                        }
                        artwork.DimensionsChecked = true;
                        await SizeRequests.WaitAsync();
                        ImageDimensions size;
                        try { size = await Task.Run(() => ReadDimensions(artwork.Url)); }
                        finally { SizeRequests.Release(); }
                        if (size != null)
                        {
                            artwork.Width = size.Width;
                            artwork.Height = size.Height;
                            artwork.IconSizes = size.Sizes;
                            artwork.IconMinSize = size.MinSize;
                            artwork.IconMaxSize = size.MaxSize;
                            sizeLabel.Text = SizeText(artwork, index, l);
                            if (size.Preview != null)
                            {
                                var previewControl = card.Children.OfType<Image>().FirstOrDefault();
                                if (previewControl != null) previewControl.Source = size.Preview;
                            }
                        }
                        else sizeLabel.Text = l.T("Rozmiar: nieznany");
                    };
                else if (video && (artwork.Width <= 0 || artwork.Height <= 0))
                    sizeLabel.Text = l.T("Rozmiar: nieznany");
                card.Children.Add(sizeLabel);
                var likesLabel = new TextBlock { Text = artwork.LikesCompleted
                    ? LikesText(artwork.Likes, l) : l.T("Polubienia: wczytywanie"),
                    HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.White };
                Action<int?> updateLikes = value => likesLabel.Text = LikesText(value, l);
                likesLabel.Unloaded += (sender, args) => artwork.LikesChanged -= updateLikes;
                likesLabel.Loaded += async (sender, args) =>
                {
                    artwork.LikesChanged -= updateLikes;
                    artwork.LikesChanged += updateLikes;
                    if (artwork.LikesCompleted) { likesLabel.Text = LikesText(artwork.Likes, l); return; }
                    if (artwork.LikesRequested) return;
                    artwork.LikesRequested = true;
                    await LikeRequests.WaitAsync();
                    int? value;
                    try { value = await Task.Run(() => loadLikes(kind, artwork.Id)); }
                    catch (Exception) { value = null; }
                    finally { LikeRequests.Release(); }
                    artwork.SetLikes(value);
                };
                card.Children.Add(likesLabel);
                card.Children.Add(new TextBlock { Text = artwork.Title, TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 165, Foreground = Brushes.White });
                var radio = new RadioButton { Content = card, GroupName = "SteamGridDBGallery_" + index,
                    Margin = new Thickness(6), Padding = new Thickness(5), IsChecked = selection[index] == artwork.Url };
                var url = artwork.Url;
                radio.Checked += (sender, args) => selection[index] = url;
                wrap.Children.Add(radio);
            }
            var scroll = new ScrollViewer { Content = wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            layout.Children.Add(scroll);
            tab.Content = layout;
            return tab;
        }
        private static string LikesText(int? count, GalleryText l) => count.HasValue
            ? l.T("Polubienia: ") + count.Value : l.T("Polubienia: niedostępne");
        private static string SizeText(Artwork artwork, int index, GalleryText l)
        {
            if (index == 2 && artwork.IconSizes != null && artwork.IconSizes.Length > 0)
                return artwork.IconMinSize == artwork.IconMaxSize
                    ? "ICO: " + artwork.IconMaxSize + " px"
                    : "ICO: " + artwork.IconMinSize + "–" + artwork.IconMaxSize + " px";
            var label = artwork.Width + " × " + artwork.Height + " px";
            return index == 2 && (artwork.Width < 128 || artwork.Height < 128)
                ? label + l.T(" (mała ikona)") : label;
        }
    }
}
