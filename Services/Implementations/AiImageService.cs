using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Services.Interfaces;


public class AiImageService : IAiImageService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private readonly ITranslateService _translateService;

    public AiImageService(HttpClient httpClient, IConfiguration config, ITranslateService translateService)
    {
        _httpClient = httpClient;
        _openAiApiKey = config["OpenAI:ApiKey"];
        _translateService = translateService;
    }

    // Đúng với interface
    public async Task<string> GenerateDesignImageAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt is required for AI image generation!");


        var encodedPrompt = Uri.EscapeDataString(prompt);
        var imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}";

        var response = await _httpClient.GetAsync(imageUrl);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Pollinations image generation failed: {(int)response.StatusCode}");

        return imageUrl;
    }

    // Overload mở rộng cho prompt mẫu
    public async Task<string> GenerateDesignImageAsync(
    int shirtType, int baseColor, int size, string? specialRequirements, string userPrompt)
    {
        // 1. Dịch chỉ phần user prompt
        var translatedUserPrompt = await _translateService.TranslateAsync(userPrompt, "en");

        // 2. Build prompt tiếng Anh như cũ, lắp phần đã dịch vào
        var prompt = BuildPrompt(shirtType, baseColor, size, specialRequirements, translatedUserPrompt);

        // 3. Tạo image như bình thường
        return await GenerateDesignImageAsync(prompt);
    }


    // Hàm map enum thành chuỗi mô tả tiếng Anh cho AI
    private string GetShirtTypeText(int shirtType)
    {
        return shirtType switch
        {
            0 => "t-shirt",
            1 => "hoodie",
            2 => "sweatshirt",
            3 => "tank top",
            4 => "long sleeve",
            5 => "jacket",
            _ => "t-shirt"
        };
    }
    private string GetBaseColorText(int baseColor)
    {
        return baseColor switch
        {
            0 => "black",
            1 => "white",
            2 => "gray",
            3 => "red",
            4 => "blue",
            5 => "navy",
            6 => "green",
            7 => "yellow",
            8 => "orange",
            9 => "purple",
            10 => "pink",
            11 => "brown",
            12 => "beige",
            _ => "white"
        };
    }
    private string GetSizeText(int size)
    {
        return size switch
        {
            0 => "XS",
            1 => "S",
            2 => "M",
            3 => "L",
            4 => "XL",
            5 => "XXL",
            6 => "XXXL",
            _ => "M"
        };
    }

    // Build prompt chuẩn cho Pollinations AI (có thể sửa template theo ý muốn)
    private string BuildPrompt(int shirtType, int baseColor, int size, string? specialRequirements, string userPrompt)
    {
        var shirtTypeText = GetShirtTypeText(shirtType);
        var baseColorText = GetBaseColorText(baseColor);
        var sizeText = GetSizeText(size);
        var special = string.IsNullOrWhiteSpace(specialRequirements) ? "" : $", {specialRequirements.Trim()}";

        return $"A centered photorealistic front view mockup of a {baseColorText} {shirtTypeText} inside a 1024x1024px square frame, shirt height about 700px, full shirt visible, no background, the following design printed on the {shirtTypeText}: \"{userPrompt}\". High quality, isolated, plain, professional.";
    }



}
