using AgendaDashboard.Utilities;

namespace AgendaDashboard.Tests;

public class DataStoreTests : IDisposable
{
    private readonly string _tempConfigPath = Path.GetTempFileName();
    private readonly string _tempCredsPath = Path.GetTempFileName();

    [Fact]
    public void LoadConfiguration_FileNotFound_Throws()
    {
        var path = Path.GetTempFileName();
        File.Delete(path);
        Assert.Throws<FileNotFoundException>(() => DataStore.LoadConfiguration(path));
    }

    [Fact]
    public void LoadConfiguration_WorksCorrectly()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_config.yaml");
        var config = DataStore.LoadConfiguration(configPath);

        Assert.Equal(20, config.General.XPosition);
        Assert.Equal(20, config.General.YPosition);
        Assert.Equal(300, config.Todoist.RefreshInterval);
        Assert.Equal("today", config.Todoist.Query);
        Assert.Equal(600, config.GoogleCalendar.RefreshInterval);
        Assert.Equal(4, config.GoogleCalendar.SelectedIds.Count);
        Assert.Equal("67f247a1bc3c575043d150414ad44919d993863b8ecbe0761f47fe01062c94bf@group.calendar.google.com",
            config.GoogleCalendar.SelectedIds[0]);
        Assert.Equal(900, config.Carddav.RefreshInterval);
        Assert.Equal("https://carddav.fastmail.com/dav/addressbooks/user/testuser@fastmail.com/Default",
            config.Carddav.Url);
    }

    [Fact]
    public void SaveConfiguration_WorksCorrectly()
    {
        var config = new Configuration
        {
            General = new GeneralConfig { XPosition = 10, YPosition = 20 },
            Todoist = new TodoistConfig { RefreshInterval = 5, Query = "today" },
            GoogleCalendar = new GoogleCalendarConfig
                { RefreshInterval = 15, SelectedIds = new List<string> { "id1", "id2" } },
            Carddav = new CarddavConfig { RefreshInterval = 30, Url = "https://example.com" }
        };
        DataStore.SaveConfiguration(config, _tempConfigPath);
        var loaded = DataStore.LoadConfiguration(_tempConfigPath);
        Assert.Equal(config.General.XPosition, loaded.General.XPosition);
        Assert.Equal(config.General.YPosition, loaded.General.YPosition);
        Assert.Equal(config.Todoist.RefreshInterval, loaded.Todoist.RefreshInterval);
        Assert.Equal(config.Todoist.Query, loaded.Todoist.Query);
        Assert.Equal(config.GoogleCalendar.RefreshInterval, loaded.GoogleCalendar.RefreshInterval);
        Assert.Equal(config.GoogleCalendar.SelectedIds, loaded.GoogleCalendar.SelectedIds);
        Assert.Equal(config.Carddav.RefreshInterval, loaded.Carddav.RefreshInterval);
        Assert.Equal(config.Carddav.Url, loaded.Carddav.Url);
    }

    [Fact]
    public void LoadCredentials_FileNotFound_Throws()
    {
        var path = Path.GetTempFileName();
        File.Delete(path);
        Assert.Throws<FileNotFoundException>(() => DataStore.LoadCredentials(path));
    }

    [Fact]
    public void SaveCredentials_WorksCorrectly()
    {
        var creds = new Credentials
        {
            Todoist = new TodoistCredentials { ApiToken = "token123" },
            GoogleCalendar = new GoogleCalendar
            {
                Installed = new InstalledCredentials
                {
                    ClientId = "cid",
                    ProjectId = "pid",
                    AuthUri = "authuri",
                    TokenUri = "tokenuri",
                    AuthProviderX509CertUrl = "certurl",
                    ClientSecret = "secret",
                    RedirectUris = new List<string> { "uri1", "uri2" }
                }
            },
            Carddav = new CarddavCredentials { Username = "user", Password = "pass" }
        };
        DataStore.SaveCredentials(creds, _tempCredsPath);
        var loaded = DataStore.LoadCredentials(_tempCredsPath);
        Assert.Equal(creds.Todoist.ApiToken, loaded.Todoist.ApiToken);
        Assert.Equal(creds.GoogleCalendar.Installed.ClientId, loaded.GoogleCalendar.Installed.ClientId);
        Assert.Equal(creds.GoogleCalendar.Installed.ProjectId, loaded.GoogleCalendar.Installed.ProjectId);
        Assert.Equal(creds.GoogleCalendar.Installed.AuthUri, loaded.GoogleCalendar.Installed.AuthUri);
        Assert.Equal(creds.GoogleCalendar.Installed.TokenUri, loaded.GoogleCalendar.Installed.TokenUri);
        Assert.Equal(creds.GoogleCalendar.Installed.AuthProviderX509CertUrl,
            loaded.GoogleCalendar.Installed.AuthProviderX509CertUrl);
        Assert.Equal(creds.GoogleCalendar.Installed.ClientSecret, loaded.GoogleCalendar.Installed.ClientSecret);
        Assert.Equal(creds.GoogleCalendar.Installed.RedirectUris, loaded.GoogleCalendar.Installed.RedirectUris);
        Assert.Equal(creds.Carddav.Username, loaded.Carddav.Username);
        Assert.Equal(creds.Carddav.Password, loaded.Carddav.Password);
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigPath)) File.Delete(_tempConfigPath);
        if (File.Exists(_tempCredsPath)) File.Delete(_tempCredsPath);
    }
}