using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;
using ReportChecker.Models;

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

#pragma warning disable SCME0001, OPENAI001

        public ChatCompletionOptions DisableReasoning()
        {
            options.Patch.Set("$.reasoning.enabled"u8, false);
            return options;
        }

        public ChatCompletionOptions SetReasoningEffort(LlmReasoningEffort reasoningEffort)
        {
            if (reasoningEffort != LlmReasoningEffort.Default)
                options.Patch.Set("$.reasoning.effort"u8, reasoningEffort.ToString().ToLower());
            if (reasoningEffort == LlmReasoningEffort.None)
                options.Patch.Set("$.reasoning.enabled"u8, false);
            return options;
        }

        public ChatCompletionOptions SelectProviderByPrice()
        {
            options.Patch.Set("$.provider.sort"u8, "price");
            return options;
        }

#pragma warning restore SCME0001, OPENAI001
    }
}