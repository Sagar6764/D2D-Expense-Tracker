using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public interface INAVService
{
    Task<decimal> GetLatestNAVAsync(string schemeCode);
}

public class NAVService : INAVService
{
    private readonly HttpClient _client;

    public NAVService(HttpClient client)
    {
        _client = client;
    }

    public async Task<decimal> GetLatestNAVAsync(string schemeCode)
    {
        var res = await _client.GetAsync($"https://api.mfapi.in/mf/{schemeCode}");
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var latestDateNav = doc.RootElement
            .GetProperty("data")[0].GetProperty("nav").GetString();
        return decimal.Parse(latestDateNav);
    }
}
