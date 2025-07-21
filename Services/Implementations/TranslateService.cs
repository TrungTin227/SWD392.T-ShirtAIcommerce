using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using System.Text.Json;

public class TranslateService : ITranslateService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public TranslateService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["MyMemory:ApiKey"]; // Lưu key vào appsettings.json hoặc secrets
    }

    public async Task<string> TranslateAsync(string text, string targetLang = "en")
    {
        // Cách đơn giản: Assume tiếng Việt cho prompt
        var sourceLang = "vi";
        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={sourceLang}|{targetLang}";
        if (!string.IsNullOrEmpty(_apiKey))
            url += $"&key={_apiKey}";

        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine("MyMemory raw response: " + content);

        using var json = System.Text.Json.JsonDocument.Parse(content);
        var root = json.RootElement;

        // Quota check
        if (root.TryGetProperty("quotaFinished", out var quota) && quota.ValueKind == JsonValueKind.True)
        {
            throw new Exception("MyMemory: Quota finished for this API key.");
        }

        if (root.TryGetProperty("responseData", out var responseData))
        {
            var translatedText = responseData.GetProperty("translatedText").GetString();
            if (!string.IsNullOrEmpty(translatedText))
                return translatedText;
        }

        throw new Exception("Translation failed or empty response from MyMemory.");
    }



}