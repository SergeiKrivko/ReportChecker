using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using OpenAI.Chat;

namespace AiAgent.Internals;

public static class ChatMessageExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    extension(ChatMessage)
    {
        public static UserChatMessage CreateUserMessage(object schema)
        {
            var json = JsonSerializer.Serialize(schema, JsonSerializerOptions);
            return new UserChatMessage(json);
        }

        public static SystemChatMessage CreateResponseTypeDefinition<T>()
        {
            return CreateResponseTypeDefinition(typeof(T));
        }

        public static SystemChatMessage CreateResponseTypeDefinition(Type type)
        {
            return new SystemChatMessage($"Final response must be presented as a json schema {type.Name}. Description in the OpenAPI format:\n\n```json\n{JsonSerializer.Serialize(type.ToSchema(), JsonSerializerOptions)}\n```");
        }
    }
}