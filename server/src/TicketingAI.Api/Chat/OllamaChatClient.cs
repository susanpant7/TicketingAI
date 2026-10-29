using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

public class OllamaChatClient(HttpClient http, IOptions<OllamaOptions> options) : IChatModelClient
{
    private readonly OllamaOptions _options = options.Value;

    public async Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken)
    {
        var request = new
        {
            model = chat.Model,
            stream = false,
            options = new { num_ctx = chat.NumCtx, temperature = chat.Temperature, top_p = chat.TopP, num_predict = chat.MaxTokens },
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
                    $"Ollama returned {(int)response.StatusCode} for model '{chat.Model}': {detail}. " +
                    $"If the model is missing, run: ollama pull {chat.Model}");
            }

            var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            return body?.Message?.Content ?? throw new ChatModelException("Ollama returned an empty reply.");
        }
    }

    private record OllamaChatResponse(OllamaMessage? Message);

    private record OllamaMessage(string Content);
}
