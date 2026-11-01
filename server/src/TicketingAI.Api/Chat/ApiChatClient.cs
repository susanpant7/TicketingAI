using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using TicketingAI.Api.Settings;

namespace TicketingAI.Api.Chat;

/// <summary>
/// Calls an external chat service that speaks the OpenAI chat format (POST {address}/chat/completions with a Bearer key):
/// Google Gemini, Cloudflare Workers AI, OpenAI, Groq and others. The reply is streamed as server-sent events.
/// </summary>
public class ApiChatClient(HttpClient http, ApiKeyResolver keys) : IChatModelClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> messages, ChatModelOptions chat, CancellationToken cancellationToken,
        Func<string, Task>? onText = null)
    {
        var key = keys.Get(chat.ApiKeyName, chat.BaseUrl)
            ?? throw new ChatModelException(
                "The chat model is an online service, but no key is set: the environment variable " +
                $"{keys.EffectiveName(chat.ApiKeyName, chat.BaseUrl)} is missing in the .env file, or the " +
                "'Chat API key name' is not valid. Set it and restart the server.");

        var body = new Dictionary<string, object>
        {
            ["model"] = chat.Model,
            ["stream"] = true,
            ["temperature"] = chat.Temperature,
            ["top_p"] = chat.TopP,
            ["max_tokens"] = chat.MaxTokens,
            ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content })
        };

        // Only sent when it is set, because not every model accepts it.
        if (!string.IsNullOrWhiteSpace(chat.ReasoningEffort))
            body["reasoning_effort"] = chat.ReasoningEffort.Trim();

        using var request = new HttpRequestMessage(HttpMethod.Post, chat.BaseUrl.TrimEnd('/') + "/chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ChatModelException($"Cannot reach the chat API at {chat.BaseUrl}.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new ChatModelException(await DescribeFailureAsync(response, chat, cancellationToken));

            // Each event is a line "data: {json}", and the last is "data: [DONE]".
            var reply = new StringBuilder();
            var cutOff = false;
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (!line.StartsWith("data:")) continue;

                var data = line["data:".Length..].Trim();
                if (data == "[DONE]") break;
                if (data.Length == 0) continue;

                var chunk = JsonSerializer.Deserialize<ApiChunk>(data, Json);
                if (chunk?.Error?.Message is { } error) throw new ChatModelException($"The chat API failed while answering: {error}");
                var choice = chunk?.Choices?.FirstOrDefault();
                if (choice?.FinishReason == "length") cutOff = true;
                if (choice?.Delta?.Content is { Length: > 0 } piece)
                {
                    reply.Append(piece);
                    if (onText is not null) await onText(reply.ToString());
                }
            }

            // The service stopped at the limit. A half answer would look like a real one, so say what happened instead.
            if (cutOff)
                throw new ChatModelException(
                    $"The reply was cut off because it reached the 'Max answer length' setting ({chat.MaxTokens} tokens). A model that thinks before it " +
                    "answers counts its thinking in that limit. Raise 'Max answer length' (for example to 4000), or set 'Reasoning effort' to a lower value.");

            return reply.Length > 0 ? reply.ToString() : throw new ChatModelException("The chat API returned an empty reply.");
        }
    }

    // A message that says what to fix. The key is never part of it.
    private async Task<string> DescribeFailureAsync(HttpResponseMessage response, ChatModelOptions chat, CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (detail.Length > 300) detail = detail[..300];

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                $"The chat API rejected the key ({(int)response.StatusCode}). Check the key named {keys.EffectiveName(chat.ApiKeyName, chat.BaseUrl)}: it must be a key of this service. {detail}",
            HttpStatusCode.NotFound =>
                $"The chat API did not find '{chat.Model}' or the address {chat.BaseUrl} (404). The 'Chat API base URL' and the 'Chat model' must belong together. A service retires old model names from time to time (and may keep listing a model that new accounts can no longer use), so pick a current one from the service's own page. {detail}",
            HttpStatusCode.TooManyRequests =>
                $"The chat API limit was reached (429). Wait a moment and try again, or check the plan's limits. {detail}",
            _ => $"The chat API returned {(int)response.StatusCode} for model '{chat.Model}': {detail}"
        };
    }

    private record ApiChunk(List<ApiChoice>? Choices, ApiError? Error);

    private record ApiChoice(ApiDelta? Delta, [property: JsonPropertyName("finish_reason")] string? FinishReason);

    private record ApiDelta(string? Content);

    private record ApiError(string? Message);
}
