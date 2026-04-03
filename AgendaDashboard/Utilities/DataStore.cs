using System.IO;
using System.Text.Json;
using SharpYaml;

namespace AgendaDashboard.Utilities;

public static class DataStore
{
    private static YamlSerializerOptions _ysOptions = new YamlSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static Configuration LoadConfiguration(string path = "configuration.yaml")
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Configuration file not found: {path}");
        // TODO match against schema
        return YamlSerializer.Deserialize<Configuration>(File.ReadAllText(path), _ysOptions) ?? throw new InvalidOperationException();
    }

    public static void SaveConfiguration(Configuration config, string path = "configuration.yaml")
    {
        var yaml = YamlSerializer.Serialize(config, _ysOptions);
        File.WriteAllText(path, yaml);
    }

    public static Credentials LoadCredentials(string path = "credentials.yaml")
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Credentials file not found: {path}");
        // TODO: match against schema
        return YamlSerializer.Deserialize<Credentials>(File.ReadAllText(path), _ysOptions) ?? throw new InvalidOperationException();
    }

    public static void SaveCredentials(Credentials creds, string path = "credentials.yaml")
    {
        var yaml = YamlSerializer.Serialize(creds, _ysOptions);
        File.WriteAllText(path, yaml);
    }
}

// TODO: resolve non-nullable warnings

public class Configuration
{
    public GeneralConfig General { get; set; }
    public TodoistConfig Todoist { get; set; }
    public GoogleCalendarConfig GoogleCalendar { get; set; }
    public CarddavConfig Carddav { get; set; }
}

public class GeneralConfig
{
    public int XPosition { get; set; }
    public int YPosition { get; set; }
}

public class TodoistConfig
{
    public int RefreshInterval { get; set; }
    public string Query { get; set; }
}

public class GoogleCalendarConfig
{
    public int RefreshInterval { get; set; }
    public List<string> SelectedIds { get; set; }
}

public class CarddavConfig
{
    public int RefreshInterval { get; set; }
    public string Url { get; set; }
}

public class Credentials
{
    public TodoistCredentials Todoist { get; set; }
    public GoogleCalendar GoogleCalendar { get; set; }
    public CarddavCredentials Carddav { get; set; }
}

public class TodoistCredentials
{
    public string ApiToken { get; set; }
}

public class GoogleCalendar
{
    public InstalledCredentials Installed { get; set; }
}

public class InstalledCredentials
{
    public string ClientId { get; set; }
    public string ProjectId { get; set; }
    public string AuthUri { get; set; }
    public string TokenUri { get; set; }
    public string AuthProviderX509CertUrl { get; set; }
    public string ClientSecret { get; set; }
    public List<string> RedirectUris { get; set; }
}

public class CarddavCredentials
{
    public string Username { get; set; }
    public string Password { get; set; }
}