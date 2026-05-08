using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace AiAgent.Internals;

public static class CompletionOptionsExtension
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    extension(ChatCompletionOptions options)
    {
        public ChatCompletionOptions SetResponseFormat<T>()
        {
            return options.SetResponseFormat(typeof(T));
        }

        public ChatCompletionOptions SetResponseFormat(Type type)
        {
            // options.ResponseFormat =
            //     ChatResponseFormat.CreateJsonSchemaFormat(type.Name,
            //         BinaryData.FromObjectAsJson(type.ToSchema(), JsonSerializerOptions));
            return options;
        }

#pragma warning disable OPENAI001

        public ChatCompletionOptions DisableReasoning()
        {
            options.ReasoningEffortLevel = ChatReasoningEffortLevel.None;
            return options;
        }

#pragma warning restore OPENAI001
    }
}