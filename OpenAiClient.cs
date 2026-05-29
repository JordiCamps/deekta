using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Deekta;

/// <summary>Raised when transcription fails, with a short user-facing message.</summary>
internal sealed class TranscriptionException : Exception
{
    public TranscriptionException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Thin client over OpenAI's audio transcription endpoint. Sends the WAV file as
/// multipart/form-data with the configured model and language hint, and returns the
/// transcribed text. The API key is supplied per-call and never persisted here.
/// </summary>
internal sealed class OpenAiClient : IDisposable
{
    private const string Endpoint = "https://api.openai.com/v1/audio/transcriptions";

    // One HttpClient for the app lifetime (socket reuse). 60s covers upload + transcription.
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<string> TranscribeAsync(
        string audioFilePath, string apiKey, string model, string? language,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new TranscriptionException(Localization.Get(Tr.ErrApiKeyNotSet));
        }

        try
        {
            await using var fileStream = File.OpenRead(audioFilePath);

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(fileContent, "file", Path.GetFileName(audioFilePath));
            content.Add(new StringContent(model), "model");
            if (!string.IsNullOrWhiteSpace(language))
            {
                content.Add(new StringContent(language), "language");
            }
            content.Add(new StringContent("json"), "response_format");

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using HttpResponseMessage response =
                await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new TranscriptionException(MapHttpError(response.StatusCode, body));
            }

            return ParseText(body);
        }
        catch (TranscriptionException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancelled operation we didn't request.
            throw new TranscriptionException(Localization.Get(Tr.ErrTimeout));
        }
        catch (HttpRequestException ex)
        {
            throw new TranscriptionException(Localization.Get(Tr.ErrConnect), ex);
        }
        catch (Exception ex)
        {
            throw new TranscriptionException(Localization.Get(Tr.ErrUnexpectedAudio), ex);
        }
    }

    private static string MapHttpError(HttpStatusCode status, string body)
    {
        string? apiMessage = TryExtractErrorMessage(body);
        return status switch
        {
            HttpStatusCode.Unauthorized => Localization.Get(Tr.ErrApiKeyInvalid),
            HttpStatusCode.TooManyRequests => Localization.Get(Tr.ErrRateLimit),
            _ => apiMessage is not null
                ? Localization.Get(Tr.ErrApiWithMsg, apiMessage)
                : Localization.Get(Tr.ErrApiCode, (int)status),
        };
    }

    private static string ParseText(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("text", out JsonElement text))
            {
                return text.GetString()?.Trim() ?? string.Empty;
            }
        }
        catch (JsonException ex)
        {
            Logger.Error("Failed to parse OpenAI response JSON.", ex);
        }

        throw new TranscriptionException(Localization.Get(Tr.ErrUnrecognized));
    }

    private static string? TryExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out JsonElement error) &&
                error.TryGetProperty("message", out JsonElement msg))
            {
                return msg.GetString();
            }
        }
        catch
        {
            // Non-JSON error body; ignore.
        }
        return null;
    }

    public void Dispose() => _http.Dispose();
}
