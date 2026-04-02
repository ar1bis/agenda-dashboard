using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using AgendaDashboard.Utilities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using vCard.Net.CardComponents;
using vCard.Net.Serialization;
using YamlDotNet.RepresentationModel;

namespace AgendaDashboard.ViewModels;

public class CalendarViewModel : INotifyPropertyChanged
{
    public List<GcalEvent> GcalEvents { get; set; } = [];
    public List<string> DateLines { get; set; } = [];

    private NotifMgr _notifMgr;
    private DateTime _targetDate = DateTime.Now.Date;
    private IEnumerable<string?> _selectedIds = [];
    private CalendarService _serviceGcal = new();
    private HttpClient _clientCardDav = new();
    private string _urlCardDav = "";
    private readonly List<string> _allDayEventLines = [];
    private readonly List<string> _cardDavEventLines = [];

    public CalendarViewModel()
    {
        _ = StartupAsync();
    }

    internal void DecrementTargetDate()
    {
        _targetDate = _targetDate.AddDays(-1);
    }

    internal void IncrementTargetDate()
    {
        _targetDate = _targetDate.AddDays(1);
    }

    internal void ResetTargetDate()
    {
        _targetDate = DateTime.Now.Date;
    }

    internal void Refresh()
    {
        RefreshGcal();
        RefreshCardDav();
    }

    private void RefreshGcal()
    {
        _ = _notifMgr.ExecNotifyAsync(LoadGcalEventsAsync, "Loaded Google Calendar events.");
    }

    private void RefreshCardDav()
    {
        _ = _notifMgr.ExecNotifyAsync(LoadCardDavEventsAsync, "Loaded Google Calendar events.");
    }

    private async Task StartupAsync()
    {
        _notifMgr = App.Current.NotifMgr;

        // Get CardDAV settings from ConfigMgr
        var configCardDav = App.Current.ConfigMgr.Config["carddav"];
        var refreshIntervalCardDav =
            double.Parse((configCardDav["refresh interval"] as YamlScalarNode)!.Value!); // TODO: error handling
        _urlCardDav = ((YamlScalarNode)configCardDav["url"]).Value!;

        // Set up CardDAV client
        var credentials = JsonDocument.Parse(await File.ReadAllTextAsync("credentials_carddav.json"));
        var usernameCardDav = credentials.RootElement.GetProperty("username").GetString();
        var passwordCardDav = credentials.RootElement.GetProperty("password").GetString();
        _clientCardDav = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{usernameCardDav}:{passwordCardDav}");
        _clientCardDav.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        // Get Google Calendar settings from ConfigMgr
        var configGcal = App.Current.ConfigMgr.Config["google calendar"];
        var refreshIntervalGCal =
            double.Parse((configGcal["refresh interval"] as YamlScalarNode)!.Value!); // TODO: error handling
        _selectedIds = ((YamlSequenceNode)configGcal["selected ids"]).Children
            .OfType<YamlScalarNode>()
            .Select(node => node.Value);

        // Set up Google Calendar API service
        string[] scopes = [CalendarService.Scope.CalendarReadonly];
        const string applicationName = "Agenda Dashboard";
        UserCredential credential;

        // ReSharper disable once UseAwaitUsing - really small file
        using (var stream = new FileStream("credentials_gcal.json", FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("gcal_token", true));
        }

        _serviceGcal = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName
        });

        // Set up a repeating timer to refresh the Google Calendar events model
        var timerGCal = new DispatcherTimer { Interval = TimeSpan.FromSeconds(refreshIntervalGCal) };
        timerGCal.Tick += (_, _) =>
        {
            ResetTargetDate(); // Reset target date to today before loading events
            RefreshGcal();
        };
        timerGCal.Start();

        // Set up a repeating timer to refresh CardDAV events
        var timerCardDav = new DispatcherTimer { Interval = TimeSpan.FromSeconds(refreshIntervalCardDav) };
        timerCardDav.Tick += (_, _) =>
        {
            ResetTargetDate(); // Reset target date to today before loading events
            RefreshCardDav();
        };
        timerCardDav.Start();

        // Do initial refresh once the app is idle - InvokeAsync not necessary here, already forked from caller
        App.Current.Dispatcher.Invoke(Refresh, DispatcherPriority.ApplicationIdle);
    }

    // Must be called from the GUI disptacher!
    private void UpdateDateLines()
    {
        // Create a new date lines list and insert the target date as the first line
        var dateLinesNew = new List<string> { $"{_targetDate:D}" };

        dateLinesNew.AddRange(_allDayEventLines);
        dateLinesNew.AddRange(_cardDavEventLines);

        DateLines = dateLinesNew;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DateLines)));
    }

    private async Task LoadGcalEventsAsync()
    {
        var calendarList = await _serviceGcal.CalendarList.List().ExecuteAsync();
        var gcalEventsNew = new List<GcalEvent>();
        _allDayEventLines.Clear();

        foreach (var calendar in calendarList.Items)
        {
            // Skip calendars that are not selected
            if (!_selectedIds.Contains(calendar.Id)) continue;

            // Convert calendar hex color to Brush
            var color = (Color)ColorConverter.ConvertFromString(calendar.BackgroundColor);
            var brush = new SolidColorBrush(color);

            // Define parameters of request
            var request = _serviceGcal.Events.List(calendar.Id);
            request.TimeMinDateTimeOffset = _targetDate;
            request.TimeMaxDateTimeOffset = _targetDate.AddDays(1);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events
            var events = await request.ExecuteAsync();

            foreach (var ev in events.Items)
            {
                if (ev.Start.DateTimeDateTimeOffset == null || ev.End.DateTimeDateTimeOffset == null) // All-day event
                {
                    _allDayEventLines.Add($"All Day: {ev.Summary}");
                    continue;
                }

                gcalEventsNew.Add(new GcalEvent
                {
                    Title = ev.Summary,
                    Start = ev.Start.DateTimeDateTimeOffset.Value.LocalDateTime,
                    End = ev.End.DateTimeDateTimeOffset.Value.LocalDateTime,
                    CalendarName = calendar.Summary,
                    CalendarColor = brush
                });
            }
        }

        // Replace model and notify property change on Dispatcher - InvokeAsync not necessary, quick operations
        App.Current.Dispatcher.Invoke(() =>
        {
            GcalEvents = gcalEventsNew;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GcalEvents)));
            UpdateDateLines();
        });
    }

    private async Task LoadCardDavEventsAsync()
    {
        // CardDAV REPORT request body to get all vCards
        const string reportXml = "<?xml version='1.0' encoding='UTF-8'?>" +
                                 "<card:addressbook-query xmlns:card='urn:ietf:params:xml:ns:carddav'>" +
                                 "  <dav:prop xmlns:dav='DAV:'>" +
                                 "    <dav:getetag/>" +
                                 "    <card:address-data/>" +
                                 "  </dav:prop>" +
                                 "</card:addressbook-query>";

        var request = new HttpRequestMessage(new HttpMethod("REPORT"), _urlCardDav)
        {
            Content = new StringContent(reportXml, Encoding.UTF8, "application/xml")
        };
        request.Headers.Add("Depth", "1");

        // Send request
        var response = await _clientCardDav.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseXml = await response.Content.ReadAsStringAsync();

        // Parse vCard data from response
        var xdoc = XDocument.Parse(responseXml);
        XNamespace cardns = "urn:ietf:params:xml:ns:carddav";
        var vCardStrs = xdoc.Descendants(cardns + "address-data").Select(x => x.Value);
        var vCardStrsCombined = string.Join("\n", vCardStrs);
        var tr = new StringReader(vCardStrsCombined);
        var vCards = SimpleDeserializer.Default.Deserialize(tr); // Parse vCard string

        _cardDavEventLines.Clear();
        foreach (var vCardComponent in vCards)
        {
            var vCard = (vCardComponent as VCard)!; // TODO: error handling
            if (!DateTime.TryParseExact(vCard.Birthdate, ["yyyyMMdd", "yyyy-MM-dd"], null, DateTimeStyles.None,
                    out var bd)) continue;

            // Check if birthdate in vCard matches TargetDate
            if (bd.Month == _targetDate.Month && bd.Day == _targetDate.Day)
            {
                _cardDavEventLines.Add(
                    $"{vCard.FormattedName}'s {YearDiffToOrdinal(bd, _targetDate)} birthday: {bd.ToShortDateString()}");
            }
        }

        // Replace model and notify property change - InvokeAsync not necessary, quick operations
        App.Current.Dispatcher.Invoke(UpdateDateLines);
    }

    private static string YearDiffToOrdinal(DateTime start, DateTime end)
    {
        var years = end.Year - start.Year;
        if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day)) years--;

        var rem100 = years % 100;
        if (rem100 is >= 11 and <= 13) return $"{years}th";

        return (years % 10) switch
        {
            1 => $"{years}st",
            2 => $"{years}nd",
            3 => $"{years}rd",
            _ => $"{years}th"
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class GcalEvent
{
    public string Title { get; init; } = "";
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public string CalendarName { get; init; } = "";
    public Brush CalendarColor { get; init; } = Brushes.Gray;
}