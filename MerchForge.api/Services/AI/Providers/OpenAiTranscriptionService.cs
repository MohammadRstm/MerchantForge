using System.Net.Http.Headers;
using System.Text.Json;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// Speech-to-text via OpenAI's transcription endpoint. Returns plain text; the
/// orchestration never learns that audio was involved.
/// </summary>
public class OpenAiTranscriptionService : IAiTranscriptionService
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiTranscriptionService(HttpClient http, IOptions<AiOptions> options)
    {
        _options = options.Value;
        _http = http;

        _http.BaseAddress ??= new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();

        var file = new StreamContent(audio);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        form.Add(file, "file", string.IsNullOrWhiteSpace(fileName) ? "audio.webm" : fileName);
        form.Add(new StringContent(_options.TranscriptionModel), "model");

        using var response = await _http.PostAsync("audio/transcriptions", form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AiConversationException(
                $"The transcription provider returned status {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.GetProperty("text").GetString() ?? string.Empty;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
        {
            throw new AiConversationException("The transcription response could not be read.", ex);
        }
    }
}
