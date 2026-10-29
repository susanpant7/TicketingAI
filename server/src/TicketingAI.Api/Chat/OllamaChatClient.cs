using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

public class OllamaChatClient(HttpClient http, IOptions<OllamaOptions> options) : IChatModelClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OllamaOptions _options = options.Value;

    public async IAsyncEnumerable<string> ChatStreamAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new
        {
            model = chat.Model,
            stream = true,
            options = new { num_ctx = chat.NumCtx, temperature = chat.Temperature, top_p = chat.TopP, num_predict = chat.MaxTokens },
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat") { Content = JsonContent.Create(request) };

        HttpResponseMessage response;
        try
        {
            // ResponseHeadersRead: return as soon as Ollama starts answering, instead of waiting for the whole reply.
            response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ChatModelException($"Cannot reach Ollama at {_options.BaseUrl}. Make sure Ollama is running.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new ChatModelException(
                    $"Ollama returned {(int)response.StatusCode} for model '{chat.Model}': {detail}. " +
                    $"If the model is missing, run: ollama pull {chat.Model}");
            }

            // Ollama streams one JSON object per line.
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (line.Length == 0)
                    continue;

                var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line, JsonOptions);
                if (chunk?.Error is not null)
                    throw new ChatModelException(chunk.Error);

                if (!string.IsNullOrEmpty(chunk?.Message?.Content))
                    yield return chunk.Message.Content;

                if (chunk?.Done == true)
                    yield break;
            }
        }
    }

    private record OllamaStreamChunk(OllamaMessage? Message, bool Done, string? Error);

    private record OllamaMessage(string Content);
}
