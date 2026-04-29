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

    public void Add(ClientResult<ChatCompletion> result)
    {
        InputTokens += result.Value.Usage.InputTokenCount;
        OutputTokens += result.Value.Usage.OutputTokenCount;
        TotalTokens += result.Value.Usage.TotalTokenCount;
        TotalRequests += 1;

        Console.WriteLine(result.GetRawResponse().Content);
        var document = JsonDocument.Parse(result.GetRawResponse().Content);
        var money = document.RootElement
            .GetProperty("usage"u8)
            .GetProperty("cost_rub"u8)
            .GetDecimal();
        Console.WriteLine($"{money} RUB");
        TotalMoney += money;
    }
}