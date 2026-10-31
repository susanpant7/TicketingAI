using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

public class OllamaChatClient(HttpClient http, IOptions<OllamaOptions> options) : IChatModelClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OllamaOptions _options = options.Value;

    public async Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken,
        Func<string, Task>? onText = null)
    {
        // Streamed, so the reply can be shown while it is written. keep_alive keeps the model loaded between messages.
        var body = new
        {
            model = chat.Model,
            stream = true,
            keep_alive = chat.KeepAlive,
            options = new { num_ctx = chat.NumCtx, temperature = chat.Temperature, top_p = chat.TopP, num_predict = chat.MaxTokens },
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat") { Content = JsonContent.Create(body) };
        HttpResponseMessage response;
        try
        {
            // ResponseHeadersRead: start reading as soon as Ollama starts sending, instead of waiting for the whole reply.
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

            // Ollama sends one JSON object per line, each with the next piece of the reply.
            var reply = new StringBuilder();
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (line.Length == 0) continue;
                var chunk = JsonSerializer.Deserialize<OllamaChunk>(line, Json);
                if (chunk?.Error is { } error) throw new ChatModelException($"Ollama failed while answering: {error}");
                if (chunk?.Message?.Content is { Length: > 0 } piece)
                {
                    reply.Append(piece);
                    if (onText is not null) await onText(reply.ToString());
                }
            }

            return reply.Length > 0 ? reply.ToString() : throw new ChatModelException("Ollama returned an empty reply.");
        }
    }

    private record OllamaChunk(OllamaMessage? Message, string? Error);

    private record OllamaMessage(string Content);
}
