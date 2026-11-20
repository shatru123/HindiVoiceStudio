using System.Threading.RateLimiting;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("Sarvam", client => client.BaseAddress = new Uri("https://api.sarvam.ai/"));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration["ALLOWED_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (origins is { Length: > 0 }) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        else policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("tts", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
var app = builder.Build();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new { name = "HindiVoiceStudio API", status = "ready", provider = "Sarvam Bulbul v3" }));
app.MapPost("/api/tts", async (TtsRequest request, IHttpClientFactory clients, IConfiguration config, CancellationToken ct) =>
{
    var key = config["SARVAM_API_KEY"];
    if (string.IsNullOrWhiteSpace(key))
        return Results.Problem("SARVAM_API_KEY is not configured on the server.", statusCode: 503);

    if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 2500)
        return Results.BadRequest("Text is required and must be 1–2500 characters.");
    if (request.LanguageCode is not ("hi-IN" or "mr-IN"))
        return Results.BadRequest("Only hi-IN and mr-IN are currently supported.");
    if (string.IsNullOrWhiteSpace(request.Speaker))
        return Results.BadRequest("Speaker is required.");

    // Validate voice-language combinations to avoid accidental unsupported voices.
    var allowed = request.LanguageCode == "hi-IN"
        ? new[] { "shubh", "ashutosh", "priya", "suhani" }
        : new[] { "ratan", "priya", "ritu", "shubh" };
    if (!allowed.Contains(request.Speaker, StringComparer.Ordinal))
        return Results.BadRequest("Unsupported speaker for selected language.");

    var client = clients.CreateClient("Sarvam");
    using var message = new HttpRequestMessage(HttpMethod.Post, "text-to-speech");
    message.Headers.Add("api-subscription-key", key);
    message.Content = new StringContent(JsonSerializer.Serialize(new
    {
        text = request.Text,
        language_code = request.LanguageCode,
        model = "bulbul:v3",
        speaker = request.Speaker,
        output_audio_codec = "wav",
        speech_sample_rate = 22050,
        pace = 1.0,
        temperature = 0.6
    }), Encoding.UTF8, "application/json");

    using var response = await client.SendAsync(message, ct);
    var responseText = await response.Content.ReadAsStringAsync(ct);
    if (!response.IsSuccessStatusCode)
        return Results.Problem($"Sarvam API returned {(int)response.StatusCode}: {responseText}", statusCode: 502);

    try
    {
        using var doc = JsonDocument.Parse(responseText);
        var audioBase64 = doc.RootElement.GetProperty("audios")[0].GetString();
        if (string.IsNullOrWhiteSpace(audioBase64)) return Results.Problem("Sarvam returned empty audio.", statusCode: 502);
        var bytes = Convert.FromBase64String(audioBase64);
        return Results.File(bytes, "audio/wav", fileDownloadName: $"{request.Speaker}-{request.LanguageCode}.wav");
    }
    catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
    {
        return Results.Problem("Could not decode the audio response from Sarvam.", statusCode: 502);
    }
}).RequireRateLimiting("tts");

app.Run();

record TtsRequest(string Text, string LanguageCode, string Speaker);