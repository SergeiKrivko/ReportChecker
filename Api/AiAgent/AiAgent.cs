using System.Reflection;
using AiAgent.Internals;
using AiAgent.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiAgent : IAiAgent
{
    private readonly ILogger _logger;
    private readonly ChatClient _client;
    private readonly ILlmUsageRepository _llmUsageRepository;
    private readonly LlmUsageType _type;
    private readonly LlmModel _model;
    private readonly Guid _reportId;
    private readonly ChatUsage _usage = new();

    internal AiAgent(ChatClient client, LlmUsageType type, LlmModel model, Guid reportId,
        ILlmUsageRepository llmUsageRepository, ILogger<AiAgent> logger)
    {
        _logger = logger;
        _client = client;
        _llmUsageRepository = llmUsageRepository;

        _type = type;
        _model = model;
        _reportId = reportId;
    }

    public async ValueTask DisposeAsync()
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Disposing AI agent... Total requests: {totalRequests}",
                _usage.TotalRequests);
        await _llmUsageRepository.CreateUsageAsync(new LlmUsage
        {
            ReportId = _reportId,
            ModelId = _model.Id,
            FinishedAt = DateTime.UtcNow,
            Type = _type,

            InputTokens = _usage.InputTokens,
            OutputTokens = _usage.OutputTokens,
            // TotalTokens = (int)(_usage.InputTokens * _model.InputCoefficient +
                                // _usage.OutputTokens * _model.OutputCoefficient),
            TotalTokens = (int)(_usage.TotalMoney * 10000),
            TotalRequests = _usage.TotalRequests,
        });
    }

    private static async Task<string> GetSystemPrompt(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream($"AiAgent.Prompts.{name}.prompt.txt");
        if (stream == null)
            throw new Exception($"Resource '{name}' not found");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<IssueCreateAgent[]?> FindIssues(IssuesRequestAgent param)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(await GetSystemPrompt("FindIssues")),
            ChatMessage.CreateResponseTypeDefinition<IssueCreateAgent[]>(),
            ChatMessage.CreateUserMessage(param.Instructions),
        ];
        var options = new ChatCompletionOptions()
            .SetResponseFormat<IssueCreateAgent>();
        AddChapters(messages, param.Chapters);

        var response = await _client.CompleteChatAsync(messages, options);
        _usage.Add(response);
        var completion = response.Value;
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", completion.ReadAsString());
        return completion.ReadAsJson<IssueCreateAgent[]>();
    }

    public async Task<CommentResponseAgent?> WriteComment(WriteCommentRequestAgent param)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(await GetSystemPrompt("WriteComment")),
            ChatMessage.CreateResponseTypeDefinition<CommentResponseAgent>(),
            ChatMessage.CreateUserMessage(param.Instructions),
            ChatMessage.CreateUserMessage(param.Issue),
        ];
        var options = new ChatCompletionOptions()
            .SetResponseFormat<CommentResponseAgent>();
        if (param.Images.Length > 0 && param.ImageProcessingMode != ImageProcessingMode.Disable)
            messages.Add(ChatMessage.CreateUserMessage(param.Images
                .Select(e =>
                    ChatMessageContentPart.CreateImagePart(new BinaryData(e.Data), e.MimeType,
                        param.ImageProcessingMode switch
                        {
                            ImageProcessingMode.LowDetail => ChatImageDetailLevel.Low,
                            ImageProcessingMode.HighDetail => ChatImageDetailLevel.High,
                            _ => ChatImageDetailLevel.Auto,
                        })).Prepend(ChatMessageContentPart.CreateTextPart(param.Text))));
        else
            messages.Add(ChatMessage.CreateUserMessage(param.Text));

        var response = await _client.CompleteChatAsync(messages, options);
        _usage.Add(response);
        var completion = response.Value;
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", completion.ReadAsString());
        return completion.ReadAsJson<CommentResponseAgent>();
    }

    public async Task<CommentCreateAgent[]?> CheckIssues(IssuesRequestAgent param)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(await GetSystemPrompt("CheckIssues")),
            ChatMessage.CreateResponseTypeDefinition<CommentCreateAgent[]>(),
            ChatMessage.CreateUserMessage(param.Instructions),
        ];
        var options = new ChatCompletionOptions()
            .SetResponseFormat<CommentCreateAgent[]>();
        AddChapters(messages, param.Chapters);

        var response = await _client.CompleteChatAsync(messages, options);
        _usage.Add(response);
        var completion = response.Value;
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", completion.ReadAsString());
        return completion.ReadAsJson<CommentCreateAgent[]>();
    }

    public async Task<CommentCreateAgent[]?> ApplyInstruction(InstructionRequestAgent param)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(await GetSystemPrompt("ApplyInstruction")),
            ChatMessage.CreateResponseTypeDefinition<CommentCreateAgent[]>(),
            ChatMessage.CreateUserMessage(param.Instruction),
        ];
        var options = new ChatCompletionOptions()
            .SetResponseFormat<CommentCreateAgent[]>();
        AddChapters(messages, param.Chapters);

        var response = await _client.CompleteChatAsync(messages, options);
        _usage.Add(response);
        var completion = response.Value;
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", completion.ReadAsString());
        return completion.ReadAsJson<CommentCreateAgent[]>();
    }

    public async Task<IssueCreateAgent[]?> SearchInstruction(InstructionRequestAgent param)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(await GetSystemPrompt("SearchInstruction")),
            ChatMessage.CreateResponseTypeDefinition<IssueCreateAgent[]>(),
            ChatMessage.CreateUserMessage(param.Instruction),
        ];
        var options = new ChatCompletionOptions()
            .SetResponseFormat<IssueCreateAgent[]>();
        AddChapters(messages, param.Chapters);

        var response = await _client.CompleteChatAsync(messages, options);
        _usage.Add(response);
        var completion = response.Value;
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", completion.ReadAsString());
        return completion.ReadAsJson<IssueCreateAgent[]>();
    }

    private static void AddChapters(List<ChatMessage> messages, IEnumerable<ChapterAgent> chapters)
    {
        foreach (var paramChapter in chapters)
        {
            if (paramChapter.Images.Length == 0 || paramChapter.ImageProcessingMode == ImageProcessingMode.Disable)
                messages.Add(ChatMessage.CreateUserMessage(paramChapter));
            else
                messages.Add(ChatMessage.CreateUserMessage(paramChapter.Images
                    .Select(e =>
                        ChatMessageContentPart.CreateImagePart(new BinaryData(e.Data), e.MimeType,
                            paramChapter.ImageProcessingMode switch
                            {
                                ImageProcessingMode.LowDetail => ChatImageDetailLevel.Low,
                                ImageProcessingMode.HighDetail => ChatImageDetailLevel.High,
                                _ => ChatImageDetailLevel.Auto,
                            }))
                    .Prepend(ChatMessageContentPart.CreateTextPart(paramChapter.Text))));
        }
    }
}