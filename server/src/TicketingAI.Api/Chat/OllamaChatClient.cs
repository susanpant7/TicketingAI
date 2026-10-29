using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

public class OllamaChatClient(HttpClient http, IOptions<OllamaOptions> options) : IChatModelClient
{
    private readonly OllamaOptions _options = options.Value;

    public async Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _options.ChatModel,
            stream = false,
            // A larger context than Ollama's default, so the knowledge sections are not cut off. Low temperature keeps answers factual.
            options = new { num_ctx = 4096, temperature = 0.2 },
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        };

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("/api/chat", request, cancellationToken);
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
                    $"Ollama returned {(int)response.StatusCode} for model '{_options.ChatModel}': {detail}. " +
                    $"If the model is missing, run: ollama pull {_options.ChatModel}");
            }

            var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            return body?.Message?.Content ?? throw new ChatModelException("Ollama returned an empty reply.");
        }
    }

    private record OllamaChatResponse(OllamaMessage? Message);

    private record OllamaMessage(string Content);
}
