using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using OpenAI.Chat;

namespace AiAgent.Internals;

public static class ChatCompletionExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    extension(ChatCompletion completion)
    {
        public string ReadAsString()
        {
            var builder = new StringBuilder();
            foreach (var part in completion.Content)
            {
                if (!string.IsNullOrWhiteSpace(part.Text))
                    builder.AppendLine(part.Text);
            }

            return builder.ToString();
        }

        public T ReadAsJson<T>()
        {
            var content = completion.Content.First(e => !string.IsNullOrEmpty(e.Text)).Text;
            if (content.Contains("```json"))
            {
                content = content.Substring(
                    content.IndexOf("```json", StringComparison.InvariantCulture) + "```json".Length);
                content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
            }

            return JsonSerializer.Deserialize<T>(content, JsonSerializerOptions) ??
                   throw new Exception("Invalid LLM response");
        }

        public string ReadAsCode()
        {
            var content = completion.Content.First(e => !string.IsNullOrEmpty(e.Text)).Text;
            if (content.Contains("```"))
            {
                content = content.Substring(
                    content.IndexOf("```", StringComparison.InvariantCulture) + "```".Length);
                content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
            }

            return content;
        }

        public string ReadAsCode(string language)
        {
            var content = completion.Content.First(e => !string.IsNullOrEmpty(e.Text)).Text;
            if (content.Contains($"```{language}"))
            {
                content = content.Substring(
                    content.IndexOf($"```{language}", StringComparison.InvariantCulture) + $"```{language}".Length);
                content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
            }

            return content;
        }
    }
}