using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace OnlineComputerStore.Core.Services
{
    // Shared plumbing for every AI-powered feature, wired to OpenAI's Chat
    // Completions API (https://api.openai.com/v1/chat/completions). When no
    // API key is configured yet, callers get a friendly placeholder instead
    // of a crash or a hung request — so the site is fully usable before
    // you've added a real key, and starts answering for real the moment you do.
    public abstract class AiServiceBase
    {
        private readonly HttpClient _http;
        protected readonly AiOptions Options;

        protected AiServiceBase(HttpClient http, IOptions<AiOptions> options)
        {
            _http = http;
            Options = options.Value;
        }

        protected bool IsConfigured => Options.Enabled && !string.IsNullOrWhiteSpace(Options.ApiKey);

        protected async Task<string> AskAsync(string prompt, double temperature = 0.3)
        {
            if (!IsConfigured)
            {
                return "AI not configured yet — add your OpenAI API key in appsettings.json.";
            }

            try
            {
                var endpoint = string.IsNullOrWhiteSpace(Options.Endpoint)
                    ? "https://api.openai.com/v1/chat/completions"
                    : Options.Endpoint;

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(new OpenAiChatRequest
                    {
                        Model = string.IsNullOrWhiteSpace(Options.Model) ? "gpt-4o-mini" : Options.Model,
                        Temperature = temperature,
                        Messages = new[]
                        {
                            new OpenAiMessage { Role = "system", Content = "You are a helpful, concise AI assistant embedded in an online computer store website." },
                            new OpenAiMessage { Role = "user", Content = prompt }
                        }
                    })
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    return $"The AI provider returned an error ({(int)response.StatusCode}). Check your API key and model name in appsettings.json. Details: {Truncate(body)}";
                }

                var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
                var text = result?.Choices?.Length > 0 ? result.Choices[0].Message?.Content : null;
                return string.IsNullOrWhiteSpace(text)
                    ? "The AI provider returned an empty response. Please try again."
                    : text.Trim();
            }
            catch (System.Exception)
            {
                return "The AI assistant couldn't be reached just now. Please try again shortly.";
            }
        }

        private static string Truncate(string text, int max = 300) =>
            text.Length <= max ? text : text[..max] + "…";

        private class OpenAiChatRequest
        {
            [JsonPropertyName("model")] public string Model { get; set; } = "";
            [JsonPropertyName("messages")] public OpenAiMessage[] Messages { get; set; } = System.Array.Empty<OpenAiMessage>();
            [JsonPropertyName("temperature")] public double Temperature { get; set; }
        }

        private class OpenAiMessage
        {
            [JsonPropertyName("role")] public string Role { get; set; } = "";
            [JsonPropertyName("content")] public string Content { get; set; } = "";
        }

        private class OpenAiChatResponse
        {
            [JsonPropertyName("choices")] public OpenAiChoice[]? Choices { get; set; }
        }

        private class OpenAiChoice
        {
            [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
        }
    }
}
