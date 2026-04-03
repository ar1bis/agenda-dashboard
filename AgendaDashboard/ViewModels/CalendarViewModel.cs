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

namespace AgendaDashboard.ViewModels;

public class CalendarViewModel : INotifyPropertyChanged
{
    public List<GcalEvent> GcalEvents { get; set; } = [];
    public List<string> DateLines { get; set; } = [];

    private DateTime _targetDate = DateTime.Now.Date;
    private List<string> _selectedIds = [];
    private CalendarService _serviceGcal = new();
    private HttpClient _clientCardDav = new();
    private string _urlCardDav = "";
    private readonly List<string> _allDayEventLines = [];
    private readonly List<string> _cardDavEventLines = [];

    public CalendarViewModel()
    {
        _ = StartupAsync();
    }

    private async Task StartupAsync()
    {
        var configCardDav = App.Current.Configuration.Carddav;
        var configGcal = App.Current.Configuration.GoogleCalendar;
        _selectedIds = configGcal.SelectedIds;
        _urlCardDav = configCardDav.Url;

        // Set up CardDAV client
        var usernameCardDav = App.Current.Credentials.Carddav.Username;
        var passwordCardDav = App.Current.Credentials.Carddav.Password;
        _clientCardDav = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{usernameCardDav}:{passwordCardDav}");
        _clientCardDav.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        // Set up Google Calendar API service
        string[] scopes = [CalendarService.Scope.CalendarReadonly];
        const string applicationName = "Agenda Dashboard";
        UserCredential credential;

        // GoogleWebAuthorizationBroker needs JSON input TODO: do this cleanly?
        var gcalCredsStream = new MemoryStream();
        var szrOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        JsonSerializer.Serialize(gcalCredsStream, App.Current.Credentials.GoogleCalendar, szrOpts);
        gcalCredsStream.Position = 0;
        credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(gcalCredsStream).Secrets,
            scopes,
            "user",
            CancellationToken.None,
            new FileDataStore("gcal_token", true));

        _serviceGcal = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName
        });

        // Set up timers to periodically refresh Google Calendar and CardDAV event models
        var timerGCal = new System.Timers.Timer(configGcal.RefreshInterval * 1000);
        timerGCal.Elapsed += (_, _) => { ResetTargetDate(); RefreshGcal(); };
        timerGCal.Start();
        var timerCardDav = new System.Timers.Timer(configGcal.RefreshInterval * 1000);
        timerCardDav.Elapsed += (_, _) => { ResetTargetDate(); RefreshCardDav(); };
        timerCardDav.Start();

        Refresh();
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

    private async Task UpdateDateLinesAsync()
    {
        // Create a new date lines list and insert the target date as the first line
        var dateLinesNew = new List<string> { $"{_targetDate:D}" };

        // TODO add date lines for public holidays
        dateLinesNew.AddRange(_allDayEventLines);
        dateLinesNew.AddRange(_cardDavEventLines);

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            DateLines = dateLinesNew;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DateLines)));
        }, DispatcherPriority.Normal);
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

        // Replace model and notify property change on Dispatcher
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            GcalEvents = gcalEventsNew;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GcalEvents)));
        }, DispatcherPriority.Normal);

        // Update DateLines - _allDayEventLines changed
        await UpdateDateLinesAsync();
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

        // TODO show birthdays in range +7d -5d
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
                    $"{vCard.FormattedName}'s {HelperMethods.YearDiffToOrdinal(bd, _targetDate)} birthday: {bd.ToShortDateString()}");
            }
        }

        // Update DateLines - _cardDavEventLines changed
        await UpdateDateLinesAsync();
    }

    private void RefreshGcal()
    {
        _ = HelperMethods.ExecAndNotifyAsync(LoadGcalEventsAsync, "Loaded Google Calendar events.");
    }

    private void RefreshCardDav()
    {
        _ = HelperMethods.ExecAndNotifyAsync(LoadCardDavEventsAsync, "Loaded CardDAV events.");
    }

    internal void Refresh()
    {
        RefreshCardDav();
        RefreshGcal();
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