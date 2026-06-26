using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsTunTrafficTray;

public sealed class MihomoClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AppSettings _settings;

    public MihomoClient(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<MihomoConnection>> GetConnectionsAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        if (!string.IsNullOrWhiteSpace(_settings.Secret))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Secret);
        }

        var endpoint = new Uri(new Uri(_settings.ControllerUrl.TrimEnd('/') + "/"), "connections");
        using var response = await http.GetAsync(endpoint);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<MihomoConnectionsResponse>(stream, JsonOptions);
        return data?.Connections ?? [];
    }
}

public sealed class MihomoConnectionsResponse
{
    [JsonPropertyName("connections")]
    public List<MihomoConnection> Connections { get; set; } = [];
}

public sealed class MihomoConnection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("metadata")]
    public MihomoMetadata Metadata { get; set; } = new();

    [JsonPropertyName("chains")]
    public List<string> Chains { get; set; } = [];

    [JsonPropertyName("download")]
    public long Download { get; set; }

    [JsonPropertyName("upload")]
    public long Upload { get; set; }
}

public sealed class MihomoMetadata
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "";

    [JsonPropertyName("process")]
    public string Process { get; set; } = "";

    [JsonPropertyName("processPath")]
    public string ProcessPath { get; set; } = "";

    [JsonPropertyName("remoteDestination")]
    public string RemoteDestination { get; set; } = "";

    [JsonPropertyName("destinationPort")]
    public string DestinationPort { get; set; } = "";
}
