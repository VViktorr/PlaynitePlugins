using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GryOnlineMetadata
{
    public sealed class GryOnlinePlugin : MetadataPlugin
    {
        public override Guid Id { get; } = Guid.Parse("7b963b1a-21e9-4a3b-a18c-49c74cf8d652");
        public override string Name => "GryOnlinePL";
        public override List<MetadataField> SupportedFields => Provider.Fields;
        private readonly GryOnlineSettings settings;
        public GryOnlinePlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new MetadataPluginProperties { HasSettings = true };
            settings = new GryOnlineSettings(this);
        }
        public override ISettings GetSettings(bool firstRunSettings) => settings;
        public override UserControl GetSettingsView(bool firstRunSettings) => new GryOnlineSettingsView();
        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
            => new Provider(options, PlayniteApi, settings.Snapshot());
    }

    public sealed class GryOnlineSettings : ObservableObject, ISettings
    {
        private readonly GryOnlinePlugin plugin;
        private GryOnlineSettings previous;
        public bool Summary { get; set; } = true;
        public bool Introduction { get; set; } = false;
        public bool Story { get; set; } = true;
        public bool Gameplay { get; set; } = false;
        public bool GameModes { get; set; } = false;
        public bool OpenWorld { get; set; } = false;
        public bool RpgElements { get; set; } = false;
        public bool Combat { get; set; } = false;
        public bool PlayableCharacters { get; set; } = false;
        public bool Engine { get; set; } = false;
        public bool CoverImage { get; set; } = true;
        public string SteamGridApiKey { get; set; } = "";
        public bool SteamGridCover { get; set; } = false;
        public bool SteamGridBackground { get; set; } = false;
        public bool SteamGridIcon { get; set; } = false;
        public string SteamGridCoverSize { get; set; } = "600x900";
        public string SteamGridBackgroundSize { get; set; } = "Dowolny";
        public string SteamGridIconSize { get; set; } = "Dowolny";
        public bool SteamGridCoverOrLarger { get; set; } = false;
        public bool SteamGridBackgroundOrLarger { get; set; } = false;
        public bool SteamGridIconOrLarger { get; set; } = false;
        public static readonly string[] CoverSizes = { "Dowolny", "600x900", "342x482", "660x930", "512x512", "1024x1024" };
        public static readonly string[] BackgroundSizes = { "Dowolny", "1600x650", "1920x620", "3840x1240" };
        public static readonly string[] IconSizes = { "Dowolny", "16x16", "24x24", "32x32", "48x48", "64x64",
            "96x96", "128x128", "192x192", "256x256", "512x512", "1024x1024" };

        public GryOnlineSettings() { }
        public GryOnlineSettings(GryOnlinePlugin plugin)
        {
            this.plugin = plugin;
            var saved = plugin.LoadPluginSettings<GryOnlineSettings>();
            if (saved != null) Copy(saved);
        }
        private void Copy(GryOnlineSettings other)
        {
            Summary = other.Summary;
            Introduction = other.Introduction;
            Story = other.Story;
            Gameplay = other.Gameplay;
            GameModes = other.GameModes;
            OpenWorld = other.OpenWorld;
            RpgElements = other.RpgElements;
            Combat = other.Combat;
            PlayableCharacters = other.PlayableCharacters;
            Engine = other.Engine;
            CoverImage = other.CoverImage;
            SteamGridApiKey = other.SteamGridApiKey;
            SteamGridCover = other.SteamGridCover;
            SteamGridBackground = other.SteamGridBackground;
            SteamGridIcon = other.SteamGridIcon;
            SteamGridCoverSize = CoverSizes.Contains(other.SteamGridCoverSize) ? other.SteamGridCoverSize : "600x900";
            SteamGridBackgroundSize = BackgroundSizes.Contains(other.SteamGridBackgroundSize) ? other.SteamGridBackgroundSize : "Dowolny";
            SteamGridIconSize = IconSizes.Contains(other.SteamGridIconSize) ? other.SteamGridIconSize : "Dowolny";
            SteamGridCoverOrLarger = other.SteamGridCoverOrLarger;
            SteamGridBackgroundOrLarger = other.SteamGridBackgroundOrLarger;
            SteamGridIconOrLarger = other.SteamGridIconOrLarger;
        }
        public GryOnlineSettings Snapshot()
        {
            var copy = new GryOnlineSettings();
            copy.Copy(this);
            return copy;
        }
        public void BeginEdit() => previous = Snapshot();
        public void CancelEdit() { if (previous != null) Copy(previous); }
        public void EndEdit() => plugin.SavePluginSettings(this);
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }

    public sealed class GryOnlineSettingsView : UserControl
    {
        public GryOnlineSettingsView()
        {
            var panel = new StackPanel { Margin = new System.Windows.Thickness(12) };
            panel.Children.Add(new TextBlock { Text = "Sekcje pobierane do pola Opis:", Margin = new System.Windows.Thickness(0, 0, 0, 8) });
            AddChoice(panel, "Krótki opis gry", "Summary");
            AddChoice(panel, "Wprowadzenie z artykułu", "Introduction");
            AddChoice(panel, "Fabuła", "Story");
            AddChoice(panel, "Gameplay / rozgrywka", "Gameplay");
            AddChoice(panel, "Tryby gry", "GameModes");
            AddChoice(panel, "Otwarty świat i mapa", "OpenWorld");
            AddChoice(panel, "Elementy RPG", "RpgElements");
            AddChoice(panel, "Walka", "Combat");
            AddChoice(panel, "Grywalne postacie", "PlayableCharacters");
            AddChoice(panel, "Silnik gry", "Engine");
            panel.Children.Add(new TextBlock { Text = "Media", FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 12, 0, 6) });
            panel.Children.Add(new TextBlock { Text = "SteamGridDB — klucz API:", Margin = new System.Windows.Thickness(0, 4, 0, 4) });
            var key = new PasswordBox();
            key.DataContextChanged += (sender, args) =>
            {
                var model = key.DataContext as GryOnlineSettings;
                if (model != null) key.Password = model.SteamGridApiKey ?? "";
            };
            key.PasswordChanged += (sender, args) =>
            {
                var model = key.DataContext as GryOnlineSettings;
                if (model != null) model.SteamGridApiKey = key.Password;
            };
            var apiKeyRow = new StackPanel { Orientation = Orientation.Horizontal };
            key.MinWidth = 230;
            key.Margin = new System.Windows.Thickness(0, 0, 8, 0);
            apiKeyRow.Children.Add(key);
            var getApiKey = new Button { Content = "Pobierz klucz API" };
            getApiKey.Click += (sender, args) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://www.steamgriddb.com/profile/preferences/api") { UseShellExecute = true });
            apiKeyRow.Children.Add(getApiKey);
            panel.Children.Add(apiKeyRow);
            var media = new TabControl { Margin = new System.Windows.Thickness(0, 12, 0, 0) };
            var posters = new StackPanel { Margin = new System.Windows.Thickness(8) };
            AddChoice(posters, "Pobieraj plakat ze SteamGridDB", "CoverImage");
            AddSizeChoice(posters, "Wielkość plakatu:", "SteamGridCoverSize", GryOnlineSettings.CoverSizes);
            AddChoice(posters, "Wybrany rozmiar lub większe", "SteamGridCoverOrLarger");
            media.Items.Add(new TabItem { Header = "Plakat", Content = posters });

            var backgrounds = new StackPanel { Margin = new System.Windows.Thickness(8) };
            AddChoice(backgrounds, "Pobieraj tło ze SteamGridDB", "SteamGridBackground");
            AddSizeChoice(backgrounds, "Wielkość tła:", "SteamGridBackgroundSize", GryOnlineSettings.BackgroundSizes);
            AddChoice(backgrounds, "Wybrany rozmiar lub większe", "SteamGridBackgroundOrLarger");
            media.Items.Add(new TabItem { Header = "Tło", Content = backgrounds });

            var icons = new StackPanel { Margin = new System.Windows.Thickness(8) };
            AddChoice(icons, "Pobieraj ikonę ze SteamGridDB", "SteamGridIcon");
            AddSizeChoice(icons, "Wielkość ikony:", "SteamGridIconSize", GryOnlineSettings.IconSizes);
            AddChoice(icons, "Wybrany rozmiar lub większe", "SteamGridIconOrLarger");
            media.Items.Add(new TabItem { Header = "Ikona", Content = icons });
            panel.Children.Add(media);
            panel.Children.Add(new TextBlock { Text = "Dostępne sekcje zostaną połączone w wybranej kolejności. Pozostałe pola metadanych wybierasz w ustawieniach Playnite.", TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new System.Windows.Thickness(0, 10, 0, 0) });
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }
        private static void AddChoice(StackPanel panel, string label, string property)
        {
            var box = new CheckBox { Content = label, Margin = new System.Windows.Thickness(0, 3, 0, 3) };
            box.SetBinding(CheckBox.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(box);
        }
        private static void AddSizeChoice(StackPanel panel, string label, string property, string[] sizes)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new System.Windows.Thickness(18, 4, 0, 2) });
            var list = new ComboBox { ItemsSource = sizes, Margin = new System.Windows.Thickness(18, 0, 0, 4), Width = 180,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            list.SetBinding(ComboBox.SelectedItemProperty, new Binding(property) { Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(list);
        }
    }

    internal sealed class Provider : OnDemandMetadataProvider
    {
        internal static readonly List<MetadataField> Fields = new List<MetadataField>
        {
            MetadataField.Name, MetadataField.Description, MetadataField.ReleaseDate, MetadataField.Developers,
            MetadataField.Publishers, MetadataField.Genres, MetadataField.Platform, MetadataField.Series,
            MetadataField.CriticScore, MetadataField.CommunityScore, MetadataField.CoverImage,
            MetadataField.BackgroundImage, MetadataField.Icon, MetadataField.Links
        };
        private readonly Dictionary<string, object> game;
        private readonly string url;
        private readonly string introductionHtml;
        private readonly string storyHtml;
        private readonly string gameplayHtml;
        private readonly string gameModesHtml;
        private readonly string[] sectionsHtml;
        private readonly GryOnlineSettings settings;
        private readonly string pageTitle;
        private readonly string originalTitle;
        private readonly ReleaseDate? releaseDate;
        private readonly int? criticScore;
        private readonly int? communityScore;
        private readonly string seriesName;
        private readonly string searchName;
        private readonly string steamGridGameId;
        private readonly string steamAppId;
        private bool steamGridSearched;
        private string steamGridCoverUrl;
        private string steamGridBackgroundUrl;
        private string steamGridIconUrl;
        private byte[] steamGridIconPng;
        public override List<MetadataField> AvailableFields { get; }

        public Provider(MetadataRequestOptions options, IPlayniteAPI api, GryOnlineSettings settings)
        {
            this.settings = settings;
            var input = options.GameData;
            searchName = input?.Name;
            steamGridGameId = FindLinkedId(input?.Links, @"^/game/(\d+)/?$");
            steamAppId = FindLinkedId(input?.Links, @"^/app/(\d+)(?:/|$)");
            url = input?.Links?.Select(x => x.Url).FirstOrDefault(IsGameUrl);
            if (url == null && !string.IsNullOrWhiteSpace(input?.Name))
                url = FindByName(input.Name, options.IsBackgroundDownload, api);
            if (url == null)
            {
                AvailableFields = Fields.Where(x => IsImageField(x) && HasField(x)).ToList();
                return;
            }
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 12000;
                req.UserAgent = "Mozilla/5.0 (compatible; Playnite-GRYOnline-Metadata/0.1)";
                using (var response = req.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var html = reader.ReadToEnd();
                    var sections = ExtractSections(html);
                    introductionHtml = sections[0];
                    storyHtml = sections[1];
                    gameplayHtml = sections[2];
                    gameModesHtml = sections[3];
                    sectionsHtml = sections;
                    releaseDate = ExtractReleaseDate(html, input);
                    criticScore = ExtractScore(html, "GRYOnline");
                    communityScore = ExtractScore(html, "Gracze");
                    seriesName = ExtractSeries(html);
                    var heading = Regex.Match(html, @"<h1\b[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (heading.Success)
                        pageTitle = WebUtility.HtmlDecode(Regex.Replace(heading.Groups[1].Value, "<[^>]+>", "")).Trim();
                    originalTitle = ExtractOriginalTitle(html, pageTitle);
                    foreach (Match match in Regex.Matches(html, "<script[^>]*type=[\"']application/ld\\+json[\"'][^>]*>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                    {
                        var obj = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 }.DeserializeObject(match.Groups[1].Value) as Dictionary<string, object>;
                        var about = GetObject(obj, "about");
                        if (String(about, "@type") == "VideoGame") { game = about; break; }
                    }
                }
            }
            catch (WebException) { }
            catch (InvalidOperationException) { }
            catch (ArgumentException) { }
            AvailableFields = Fields.Where(x => (game != null || IsImageField(x)) && HasField(x)).ToList();
        }
        private static bool IsImageField(MetadataField field) => field == MetadataField.CoverImage ||
            field == MetadataField.BackgroundImage || field == MetadataField.Icon;
        private static string FindByName(string title, bool background, IPlayniteAPI api)
        {
            var candidates = SearchCandidates(title);
            var key = MatchKey(title);
            var exact = candidates.Where(x => MatchKey(x.Key) == key).ToList();
            if (exact.Count == 1) return exact[0].Value;

            if (!background && api != null)
            {
                var selected = api.Dialogs.ChooseItemWithSearch(
                    ToOptions(candidates),
                    query => ToOptions(SearchCandidates(query)),
                    title,
                    "Wybierz grę z GRYOnline.pl");
                return IsGameUrl(selected?.Description) ? selected.Description : null;
            }

            // In automatic downloads, avoid guessing among editions or remasters.
            if (exact.Count > 1) return null;
            var queryKey = StripArticle(key);
            var possible = candidates.Where(x =>
                StripArticle(MatchKey(x.Key)).StartsWith(queryKey + " ", StringComparison.Ordinal) &&
                !Regex.IsMatch(x.Key, @"\b(remastered|legendary edition|dlc|expansion)\b", RegexOptions.IgnoreCase))
                .OrderBy(x => x.Key.Length).ToList();
            return possible.Count == 1 && StripArticle(MatchKey(possible[0].Key)).Length <= queryKey.Length + 25
                ? possible[0].Value : null;
        }
        private static List<GenericItemOption> ToOptions(List<KeyValuePair<string, string>> candidates)
            => candidates.Select(x => new GenericItemOption(x.Key, x.Value)).ToList();

        private static List<KeyValuePair<string, string>> SearchCandidates(string title)
        {
            var candidates = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(title)) return candidates;
            var alternate = MatchKey(title);
            foreach (var queryText in new[] { title.Trim(), alternate }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(
                        "https://www.gry-online.pl/ajax/xml/gry.asp?search=" + Uri.EscapeDataString(queryText));
                    request.Timeout = 12000;
                    request.UserAgent = "Mozilla/5.0 (compatible; Playnite-GryOnlinePL/1.0)";
                    var document = new XmlDocument { XmlResolver = null };
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream()) document.Load(stream);
                    foreach (XmlNode node in document.SelectNodes("/root/row"))
                    {
                        var candidate = node.Attributes?["name"]?.Value;
                        var candidateUrl = node.Attributes?["url"]?.Value;
                        if (!IsGameUrl(candidateUrl) || string.IsNullOrWhiteSpace(candidate)) continue;
                        if (!candidates.Any(x => string.Equals(x.Value, candidateUrl, StringComparison.OrdinalIgnoreCase)))
                            candidates.Add(new KeyValuePair<string, string>(candidate, candidateUrl));
                    }
                }
                catch (WebException) { }
                catch (XmlException) { }
                catch (ArgumentException) { }
            }
            return candidates;
        }
        private static string MatchKey(string text)
        {
            var decomposed = (text ?? "").ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder();
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
            var key = builder.ToString().Replace("ł", "l").Replace("'", "").Replace("’", "");
            key = Regex.Replace(key, @"\biii\b", "3");
            key = Regex.Replace(key, @"\bii\b", "2");
            key = Regex.Replace(key, @"\biv\b", "4");
            key = Regex.Replace(key, @"\bi\b", "1");
            return Regex.Replace(Regex.Replace(key, @"[\p{P}\p{S}]", " ").Trim(), @"\s+", " ");
        }
        private static string StripArticle(string text) => text.StartsWith("the ", StringComparison.Ordinal) ? text.Substring(4) : text;
        private static string[] ExtractSections(string html)
        {
            var result = new string[9];
            var description = html.IndexOf("id=\"description\"", StringComparison.OrdinalIgnoreCase);
            if (description < 0) return result;
            var end = html.IndexOf("id=\"konto-inline\"", description, StringComparison.OrdinalIgnoreCase);
            var block = html.Substring(description, (end < 0 ? html.Length : end) - description);
            var headings = Regex.Matches(block, @"<h2\b[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (headings.Count == 0) return result;
            result[0] = Paragraphs(block.Substring(0, headings[0].Index), 2);
            var slots = new int[headings.Count];
            for (var i = 0; i < headings.Count; i++)
            {
                var heading = WebUtility.HtmlDecode(Regex.Replace(headings[i].Groups[1].Value, "<[^>]+>", "")).Trim();
                var normalized = MatchKey(heading);
                if (normalized.StartsWith("fabula") || normalized.StartsWith("o czym opowiada") || normalized.StartsWith("historia")) slots[i] = 1;
                else if (normalized.StartsWith("gameplay") || normalized.StartsWith("rozgrywka") || normalized.StartsWith("mechanika") || normalized.StartsWith("jak sie gra")) slots[i] = 2;
                else if (normalized.StartsWith("tryb") || normalized.StartsWith("mode")) slots[i] = 3;
                else if (normalized.StartsWith("otwarty swiat") || normalized.StartsWith("mapa swiata")) slots[i] = 4;
                else if (normalized.StartsWith("elementy rpg") || normalized.StartsWith("rozwoj postaci")) slots[i] = 5;
                else if (normalized.StartsWith("walka") || normalized.StartsWith("system walki")) slots[i] = 6;
                else if (normalized.Contains("grywalne postacie") || normalized.StartsWith("postacie grywalne")) slots[i] = 7;
                else if (normalized.StartsWith("silnik")) slots[i] = 8;
            }
            // Use section order only for unnamed headings and only if the
            // expected section wasn't identified elsewhere on the page.
            for (var i = 0; i < Math.Min(2, headings.Count); i++)
                if (slots[i] == 0 && !slots.Contains(i + 1)) slots[i] = i + 1;
            for (var i = 0; i < headings.Count; i++)
            {
                var slot = slots[i];
                if (slot == 0) continue;
                if (result[slot] != null) continue;
                var heading = WebUtility.HtmlDecode(Regex.Replace(headings[i].Groups[1].Value, "<[^>]+>", "")).Trim();
                var start = headings[i].Index + headings[i].Length;
                var stop = i + 1 < headings.Count ? headings[i + 1].Index : block.Length;
                var content = Paragraphs(block.Substring(start, stop - start), 12);
                if (content != null) result[slot] = "<h2>" + EscapeText(heading) + "</h2>" + content;
            }
            return result;
        }
        private static string Paragraphs(string html, int maximum)
        {
            var paragraphs = Regex.Matches(html, @"<p\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>().Select(p => SafeInline(p.Groups[1].Value))
                .Where(p => Regex.Replace(p, "<[^>]+>", "").Trim().Length > 25)
                .Take(maximum).Select(p => "<p>" + p + "</p>");
            var content = string.Join("", paragraphs);
            return content.Length == 0 ? null : content;
        }
        private static string SafeInline(string html)
        {
            var result = new System.Text.StringBuilder();
            foreach (Match part in Regex.Matches(html, @"<[^>]*>|[^<]+", RegexOptions.Singleline))
            {
                var token = part.Value;
                if (token.StartsWith("<"))
                {
                    var tag = Regex.Match(token, @"^<\s*(/?)\s*(b|strong|i|em|br|a)\b", RegexOptions.IgnoreCase);
                    if (!tag.Success) continue;
                    var name = tag.Groups[2].Value.ToLowerInvariant();
                    if (name == "br") result.Append("<br>");
                    else if (name == "a")
                    {
                        if (tag.Groups[1].Value == "/") result.Append("</a>");
                        else
                        {
                            var href = Regex.Match(token, "\\bhref\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);
                            var value = href.Success ? WebUtility.HtmlDecode(href.Groups[1].Value) : "";
                            Uri target;
                            if (value.StartsWith("/")) value = "https://www.gry-online.pl" + value;
                            if (Uri.TryCreate(value, UriKind.Absolute, out target) && target.Scheme == "https" &&
                                (target.Host == "gry-online.pl" || target.Host == "www.gry-online.pl"))
                                result.Append("<a href=\"").Append(EscapeText(value)).Append("\">");
                        }
                    }
                    else result.Append(tag.Groups[1].Value == "/" ? "</" : "<")
                        .Append(name == "b" ? "strong" : name == "i" ? "em" : name).Append('>');
                }
                else result.Append(EscapeText(WebUtility.HtmlDecode(token)));
            }
            return result.ToString();
        }
        private static string EscapeText(string value) => (value ?? "").Replace("&", "&amp;")
            .Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        private static ReleaseDate? ExtractReleaseDate(string html, Game input)
        {
            var platformStart = html.IndexOf("id=\"platforms\"", StringComparison.OrdinalIgnoreCase);
            if (platformStart >= 0 && input?.Platforms != null)
            {
                var platformHtml = html.Substring(platformStart, Math.Min(18000, html.Length - platformStart));
                foreach (var platform in input.Platforms)
                {
                    var expected = platform?.Name;
                    if (string.IsNullOrWhiteSpace(expected)) continue;
                    foreach (Match card in Regex.Matches(platformHtml,
                        @"<div class=""S016meta-plat-box2"">(.*?)</div>\s*</div>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                    {
                        var platformName = Regex.Match(card.Value, "alt=\"([^\"]+)\"", RegexOptions.IgnoreCase);
                        if (!platformName.Success || !string.Equals(WebUtility.HtmlDecode(platformName.Groups[1].Value), expected, StringComparison.OrdinalIgnoreCase)) continue;
                        var date = Regex.Match(card.Value, @"S016meta-s-p5[^>]*>\s*<span>([^<]+)</span>", RegexOptions.IgnoreCase);
                        var parsed = ParseDate(date.Success ? date.Groups[1].Value : null);
                        if (parsed.HasValue) return parsed;
                    }
                }
            }
            var headline = Regex.Match(html, @"Data wydania:\s*<b>([^<]+)</b>", RegexOptions.IgnoreCase);
            return ParseDate(headline.Success ? headline.Groups[1].Value : null);
        }
        private static ReleaseDate? ParseDate(string value)
        {
            DateTime parsed;
            return !string.IsNullOrWhiteSpace(value) && DateTime.TryParse(
                WebUtility.HtmlDecode(value), new CultureInfo("pl-PL"), DateTimeStyles.None, out parsed)
                ? (ReleaseDate?)new ReleaseDate(parsed.Year, parsed.Month, parsed.Day) : null;
        }
        private static int? ExtractScore(string html, string label)
        {
            var start = html.IndexOf("class=\"oce-short-c\"", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;
            var fragment = html.Substring(start, Math.Min(3500, html.Length - start));
            foreach (Match box in Regex.Matches(fragment, @"<div class=""cf oce-short-box"">(.*?)(?=<div class=""cf oce-short-box""|</div>\s*</div>\s*<div class=""oce-short-open-pan)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(box.Value, @"<p[^>]*>\s*" + Regex.Escape(label) + @"\s*</p>", RegexOptions.IgnoreCase)) continue;
                var rating = Regex.Match(box.Value, @"class=""oce-rate"">\s*([0-9]+(?:[.,][0-9]+)?)", RegexOptions.IgnoreCase);
                double score;
                if (rating.Success && double.TryParse(rating.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out score))
                    return (int)Math.Max(0, Math.Min(100, Math.Round(score * 10)));
            }
            return null;
        }
        private static string ExtractSeries(string html)
        {
            var start = html.IndexOf("id=\"series\"", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;
            var section = html.Substring(start, Math.Min(4000, html.Length - start));
            var heading = Regex.Match(section, @"<h3>\s*<span>\s*Gry z serii\s*</span>\s*(.*?)</h3>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!heading.Success) return null;
            var name = WebUtility.HtmlDecode(Regex.Replace(heading.Groups[1].Value, "<[^>]+>", "")).Trim();
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        private static bool IsGameUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Scheme == "https" &&
                (uri.Host == "gry-online.pl" || uri.Host == "www.gry-online.pl") &&
                Regex.IsMatch(uri.AbsolutePath, @"^/gry/[^/]+/z[a-z0-9]+/?$", RegexOptions.IgnoreCase);
        }
        private static Dictionary<string, object> GetObject(Dictionary<string, object> obj, string key)
        {
            object value;
            return obj != null && obj.TryGetValue(key, out value) ? value as Dictionary<string, object> : null;
        }
        private static string String(Dictionary<string, object> obj, string key)
        {
            object value;
            return obj != null && obj.TryGetValue(key, out value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
        }
        private static IEnumerable<string> Names(object value)
        {
            if (value is string) return new[] { (string)value };
            if (value is Dictionary<string, object>) return new[] { String((Dictionary<string, object>)value, "name") };
            var array = value as object[];
            return array == null ? Enumerable.Empty<string>() : array.SelectMany(Names);
        }
        private IEnumerable<MetadataProperty> Properties(string key)
        {
            object value;
            return game != null && game.TryGetValue(key, out value)
                ? Names(value).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => (MetadataProperty)new MetadataNameProperty(x)).ToList()
                : Enumerable.Empty<MetadataProperty>();
        }
        private bool HasField(MetadataField field)
        {
            switch (field)
            {
                case MetadataField.Name: return !string.IsNullOrEmpty(GetName(null));
                case MetadataField.Description: return !string.IsNullOrEmpty(GetDescription(null));
                case MetadataField.ReleaseDate: return releaseDate.HasValue;
                case MetadataField.Developers: return Properties("author").Any();
                case MetadataField.Publishers: return Properties("publisher").Any();
                case MetadataField.Genres: return Properties("genre").Any();
                case MetadataField.Platform: return Properties("gamePlatform").Any();
                case MetadataField.Series: return !string.IsNullOrWhiteSpace(seriesName);
                case MetadataField.CriticScore: return GetCriticScore(null).HasValue;
                case MetadataField.CommunityScore: return GetCommunityScore(null).HasValue;
                case MetadataField.CoverImage: return GetCoverImage(null) != null;
                case MetadataField.BackgroundImage: return GetBackgroundImage(null) != null;
                case MetadataField.Icon: return GetIcon(null) != null;
                case MetadataField.Links: return true;
                default: return false;
            }
        }
        public override string GetName(GetMetadataFieldArgs args) =>
            !string.IsNullOrWhiteSpace(pageTitle) ? pageTitle : String(game, "name");
        public override string GetDescription(GetMetadataFieldArgs args)
        {
            var value = String(game, "description");
            var summary = string.IsNullOrWhiteSpace(value) ? "" : "<p>" + EscapeText(WebUtility.HtmlDecode(value)) + "</p>";
            var parts = new[]
            {
                settings.Summary ? summary : null,
                settings.Introduction ? introductionHtml : null,
                settings.Story ? storyHtml : null,
                settings.Gameplay ? gameplayHtml : null,
                settings.GameModes ? gameModesHtml : null,
                settings.OpenWorld ? sectionsHtml?[4] : null,
                settings.RpgElements ? sectionsHtml?[5] : null,
                settings.Combat ? sectionsHtml?[6] : null,
                settings.PlayableCharacters ? sectionsHtml?[7] : null,
                settings.Engine ? sectionsHtml?[8] : null
            };
            var description = string.Join("", parts.Where(x => !string.IsNullOrEmpty(x)));
            return description.Length == 0 ? null : description;
        }
        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args) => Properties("author");
        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args) => Properties("publisher");
        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args) => Properties("genre");
        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args) => Properties("gamePlatform");
        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args) =>
            string.IsNullOrWhiteSpace(seriesName) ? Enumerable.Empty<MetadataProperty>() :
                new MetadataProperty[] { new MetadataNameProperty(seriesName) };
        public override int? GetCriticScore(GetMetadataFieldArgs args)
        {
            if (criticScore.HasValue) return criticScore;
            var rating = GetObject(game, "aggregateRating");
            double score;
            return double.TryParse(String(rating, "ratingValue"), NumberStyles.Any, CultureInfo.InvariantCulture, out score)
                ? (int?)Math.Max(0, Math.Min(100, (int)Math.Round(score * 10))) : null;
        }
        public override int? GetCommunityScore(GetMetadataFieldArgs args) => communityScore;
        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args) => releaseDate;
        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            if (!settings.CoverImage) return null;
            EnsureSteamGridImages();
            return steamGridCoverUrl == null ? null : new MetadataFile(steamGridCoverUrl);
        }
        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            if (!settings.SteamGridBackground) return null;
            EnsureSteamGridImages();
            return steamGridBackgroundUrl == null ? null : new MetadataFile(steamGridBackgroundUrl);
        }
        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            if (!settings.SteamGridIcon) return null;
            EnsureSteamGridImages();
            if (steamGridIconUrl == null) return null;
            if (!new Uri(steamGridIconUrl).AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                return new MetadataFile(steamGridIconUrl);
            if (steamGridIconPng != null)
                return new MetadataFile("SteamGridDB-icon.png", steamGridIconPng, steamGridIconUrl);
            try
            {
                var request = WebRequest.Create(steamGridIconUrl);
                request.Timeout = 15000;
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad);
                    var frames = decoder.Frames.OrderByDescending(x => (long)x.PixelWidth * x.PixelHeight).ToList();
                    var selected = frames.FirstOrDefault();
                    if (settings.SteamGridIconSize != "Dowolny" && !settings.SteamGridIconOrLarger)
                    {
                        var parts = settings.SteamGridIconSize.Split('x');
                        int width, height;
                        if (parts.Length == 2 && int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height))
                            selected = frames.FirstOrDefault(x => x.PixelWidth == width && x.PixelHeight == height);
                    }
                    if (selected == null) return null;
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(selected));
                    using (var output = new MemoryStream())
                    {
                        encoder.Save(output);
                        steamGridIconPng = output.ToArray();
                        return new MetadataFile("SteamGridDB-icon.png", steamGridIconPng, steamGridIconUrl);
                    }
                }
            }
            catch (Exception) { return null; }
        }
        private static string ExtractOriginalTitle(string html, string polishTitle)
        {
            if (string.IsNullOrWhiteSpace(polishTitle)) return null;
            var title = Regex.Match(html ?? "", @"<title\b[^>]*>(.*?)</title>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!title.Success) return null;
            var text = WebUtility.HtmlDecode(Regex.Replace(title.Groups[1].Value, "<[^>]+>", "")).Trim();
            var prefix = polishTitle + ", ";
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
            var original = Regex.Replace(text.Substring(prefix.Length),
                @"\s+-\s+Wielka Encyklopedia Gier.*$", "", RegexOptions.IgnoreCase).Trim();
            return original.Length > 0 && original.Length <= 150 &&
                !string.Equals(original, polishTitle, StringComparison.OrdinalIgnoreCase) ? original : null;
        }
        private static string FindLinkedId(IEnumerable<Link> links, string pathPattern)
        {
            if (links == null) return null;
            foreach (var link in links)
            {
                Uri address;
                if (!Uri.TryCreate(link.Url, UriKind.Absolute, out address) || address.Scheme != "https") continue;
                var steamGrid = pathPattern.StartsWith("^/game/", StringComparison.Ordinal);
                if (steamGrid ? address.Host != "www.steamgriddb.com" && address.Host != "steamgriddb.com"
                    : address.Host != "store.steampowered.com") continue;
                var match = Regex.Match(address.AbsolutePath, pathPattern, RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups[1].Value;
            }
            return null;
        }
        private bool TrySteamGridImages(string source)
        {
            var coverTask = settings.CoverImage && steamGridCoverUrl == null
                ? Task.Run(() => SteamGridImage("grids/" + source, settings.SteamGridCoverSize,
                    GryOnlineSettings.CoverSizes, settings.SteamGridCoverOrLarger)) : null;
            var backgroundTask = settings.SteamGridBackground && steamGridBackgroundUrl == null
                ? Task.Run(() => SteamGridImage("heroes/" + source, settings.SteamGridBackgroundSize,
                    GryOnlineSettings.BackgroundSizes, settings.SteamGridBackgroundOrLarger)) : null;
            var iconTask = settings.SteamGridIcon && steamGridIconUrl == null
                ? Task.Run(() => SteamGridImage("icons/" + source, settings.SteamGridIconSize,
                    GryOnlineSettings.IconSizes, settings.SteamGridIconOrLarger)) : null;
            var pending = new[] { coverTask, backgroundTask, iconTask }.Where(x => x != null).ToArray();
            if (pending.Length > 0) Task.WaitAll(pending);
            if (coverTask != null) steamGridCoverUrl = coverTask.Result;
            if (backgroundTask != null) steamGridBackgroundUrl = backgroundTask.Result;
            if (iconTask != null) steamGridIconUrl = iconTask.Result;
            return (!settings.CoverImage || steamGridCoverUrl != null) &&
                (!settings.SteamGridBackground || steamGridBackgroundUrl != null) &&
                (!settings.SteamGridIcon || steamGridIconUrl != null);
        }
        private void EnsureSteamGridImages()
        {
            if (steamGridSearched) return;
            steamGridSearched = true;
            if (string.IsNullOrWhiteSpace(settings.SteamGridApiKey)) return;
            try
            {
                var attempted = new HashSet<string>(StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(steamGridGameId))
                {
                    attempted.Add("game/" + steamGridGameId);
                    if (TrySteamGridImages("game/" + steamGridGameId)) return;
                }
                if (!string.IsNullOrEmpty(steamAppId))
                {
                    attempted.Add("steam/" + steamAppId);
                    if (TrySteamGridImages("steam/" + steamAppId)) return;
                }
                object alternate;
                var alternateNames = game != null && game.TryGetValue("alternateName", out alternate)
                    ? Names(alternate) : Enumerable.Empty<string>();
                var titles = new[] { pageTitle, String(game, "name"), searchName, originalTitle }
                    .Concat(alternateNames)
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var names = titles.Concat(titles.Select(WithoutEdition))
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var name in names)
                {
                    int id;
                    // The Polish title is a verified alias of SteamGridDB game 5307973.
                    if (MatchKey(name) == MatchKey("Uncharted: Kolekcja Dziedzictwo Złodziei"))
                        id = 5307973;
                    else
                    {
                        List<Dictionary<string, object>> matches;
                        try { matches = SteamGridData("search/autocomplete/" + Uri.EscapeDataString(name.Trim())); }
                        catch (WebException) { continue; }
                        var exact = matches.FirstOrDefault(x => MatchKey(String(x, "name")) == MatchKey(name));
                        if (exact == null || !int.TryParse(String(exact, "id"), out id) || id <= 0) continue;
                    }
                    if (attempted.Add("game/" + id) && TrySteamGridImages("game/" + id)) break;
                }
            }
            catch (WebException) { }
            catch (InvalidOperationException) { }
            catch (ArgumentException) { }
            catch (AggregateException) { }
        }
        private static string WithoutEdition(string name)
        {
            var shorter = Regex.Replace(name ?? "", @"\s*(?:[:\-–]\s*)?(?:ultimate|complete|deluxe|gold|legendary)\s+edition\s*$",
                "", RegexOptions.IgnoreCase).Trim();
            return shorter == name ? null : shorter;
        }
        private static List<Tuple<int, int>> ReadIconFrames(string iconUrl)
        {
            try
            {
                var request = WebRequest.Create(iconUrl);
                request.Timeout = 10000;
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad);
                    return decoder.Frames.Select(x => Tuple.Create(x.PixelWidth, x.PixelHeight)).ToList();
                }
            }
            catch (Exception) { return new List<Tuple<int, int>>(); }
        }
        private string SteamGridImage(string path, string size, string[] allowedSizes, bool orLarger)
        {
            try
            {
                var selectedSize = allowedSizes.Contains(size) ? size : "Dowolny";
                var exact = selectedSize != "Dowolny" && !orLarger;
                var requested = selectedSize == "Dowolny" ? null : selectedSize.Split('x');
                var minimumWidth = requested == null ? 0 : int.Parse(requested[0], CultureInfo.InvariantCulture);
                var minimumHeight = requested == null ? 0 : int.Parse(requested[1], CultureInfo.InvariantCulture);
                var isPoster = path.StartsWith("grids/", StringComparison.Ordinal);
                var isIcon = path.StartsWith("icons/", StringComparison.Ordinal);
                var matching = new List<Dictionary<string, object>>();
                // The API pages results before client-side filtering. Search later pages as well.
                for (var page = 0; page < 3; page++)
                {
                    var query = path + "?limit=50&page=" + page;
                    if (exact && !isIcon) query += "&dimensions=" + Uri.EscapeDataString(selectedSize);
                    var batch = SteamGridData(query);
                    var framesByUrl = new ConcurrentDictionary<string, List<Tuple<int, int>>>();
                    if (isIcon && requested != null)
                        Parallel.ForEach(batch, new ParallelOptions { MaxDegreeOfParallelism = 4 }, image =>
                        {
                            var address = String(image, "url");
                            Uri iconAddress;
                            if (Uri.TryCreate(address, UriKind.Absolute, out iconAddress) &&
                                iconAddress.AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                                framesByUrl[address] = ReadIconFrames(address);
                        });
                    foreach (var image in batch)
                    {
                        var iconUrl = String(image, "url");
                        Uri uri;
                        if (!Uri.TryCreate(iconUrl, UriKind.Absolute, out uri) || uri.Scheme != "https") continue;
                        var width = Number(image, "width");
                        var height = Number(image, "height");
                        if (requested != null)
                        {
                            var ico = isIcon && uri.AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
                            if (ico)
                            {
                                List<Tuple<int, int>> frames;
                                if (!framesByUrl.TryGetValue(iconUrl, out frames)) continue;
                                if (!frames.Any(x => exact
                                    ? x.Item1 == minimumWidth && x.Item2 == minimumHeight
                                    : x.Item1 >= minimumWidth && x.Item2 >= minimumHeight)) continue;
                                width = frames.Max(x => x.Item1);
                                height = frames.Max(x => x.Item2);
                                image["width"] = width;
                                image["height"] = height;
                            }
                            else if (exact ? width != minimumWidth || height != minimumHeight
                                : width < minimumWidth || height < minimumHeight) continue;
                            if (isPoster && height > 0 &&
                                Math.Abs(width / height - (double)minimumWidth / minimumHeight) > 0.06) continue;
                        }
                        matching.Add(image);
                    }
                    if (batch.Count < 50 || matching.Count > 0) break;
                }
                return matching.OrderByDescending(x => Number(x, "score"))
                    .ThenByDescending(x => Number(x, "width") * Number(x, "height"))
                    .Select(x => String(x, "url")).FirstOrDefault();
            }
            catch (WebException) { return null; }
        }
        private static double Number(Dictionary<string, object> item, string key)
        {
            double value;
            return double.TryParse(String(item, key), NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : 0;
        }
        private List<Dictionary<string, object>> SteamGridData(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.steamgriddb.com/api/v2/" + path);
            request.Timeout = 12000;
            request.UserAgent = "GryOnlinePL/1.1";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + settings.SteamGridApiKey.Trim();
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var json = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }
                    .DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
                object data;
                var array = json != null && json.TryGetValue("data", out data) ? data as object[] : null;
                return array == null ? new List<Dictionary<string, object>>() :
                    array.OfType<Dictionary<string, object>>().ToList();
            }
        }
        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args) =>
            string.IsNullOrEmpty(url) ? Enumerable.Empty<Link>() : new[] { new Link("GRYOnline.pl", url) };
    }
}
