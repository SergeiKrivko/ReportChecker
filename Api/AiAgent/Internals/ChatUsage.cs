using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace AiAgent.Internals;

internal class ChatUsage
{
    public int InputTokens { get; private set; } = 0;
    public int OutputTokens { get; private set; } = 0;
    public int TotalTokens { get; private set; } = 0;
    public int TotalRequests { get; private set; } = 0;
    public decimal TotalMoney { get; private set; } = 0;

#pragma warning disable SCME0001

    public void Add(ChatTokenUsage usage)
    {
        InputTokens += usage.InputTokenCount;
        OutputTokens += usage.OutputTokenCount;
        TotalTokens += usage.TotalTokenCount;
        TotalRequests += 1;

        var money = usage.Patch.GetDecimal("$.cost_rub"u8);
        TotalMoney += money;
    }

#pragma warning restore SCME0001
}