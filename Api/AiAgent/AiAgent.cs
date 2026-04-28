using System.Reflection;
using System.Text.Json;
using AiAgent.Models;
using Avalux.OpenAi.Client;
using Avalux.OpenAi.Client.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiAgent : IAiAgent
{
    private readonly ILogger _logger;
    private readonly OpenAiClient<string> _client;
    private readonly ILlmUsageRepository _llmUsageRepository;
    private readonly LlmUsageType _type;
    private readonly LlmModel _model;
    private readonly Guid _reportId;

    internal AiAgent(OpenAiClient<string> client, LlmUsageType type, LlmModel model, Guid reportId,
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
                _client.Usage.TotalRequests);
        await _llmUsageRepository.CreateUsageAsync(new LlmUsage
        {
            ReportId = _reportId,
            ModelId = _model.Id,
            FinishedAt = DateTime.UtcNow,
            Type = _type,

            InputTokens = _client.Usage.InputTokens,
            OutputTokens = _client.Usage.OutputTokens,
            TotalTokens = (int)(_client.Usage.InputTokens * _model.InputCoefficient +
                                _client.Usage.OutputTokens * _model.OutputCoefficient),
            TotalRequests = _client.Usage.TotalRequests,
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
        var request = new ChatRequest()
            .AddSystemPrompt(await GetSystemPrompt("FindIssues"))
            .SetResponseType<IEnumerable<IssueCreateAgent>>()
            .AddUserMessage(param.Instructions);
        foreach (var paramChapter in param.Chapters)
        {
            Console.WriteLine(string.Join("; ", paramChapter.Images.Select(e => e.MimeType)));
            if (paramChapter.Images.Length == 0 || paramChapter.ImageProcessingMode == ImageProcessingMode.Disable)
                request.AddUserMessage(paramChapter);
            else
                request.AddUserMessage(paramChapter.Images
                    .Select(e =>
                        ChatMessageContentPart.CreateImagePart(new BinaryData(e.Data), e.MimeType,
                            paramChapter.ImageProcessingMode switch
                            {
                                ImageProcessingMode.LowDetail => ChatImageDetailLevel.Low,
                                ImageProcessingMode.HighDetail => ChatImageDetailLevel.High,
                                _ => ChatImageDetailLevel.Auto,
                            }))
                    .Prepend(ChatMessageContentPart.CreateTextPart(paramChapter.Text)));
        }

        var response = await _client.CompleteAsync(request);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", response.ReadAsString());
        return response.ReadAsJson<IssueCreateAgent[]>();
    }

    public async Task<CommentResponseAgent?> WriteComment(WriteCommentRequestAgent param)
    {
        var request = new ChatRequest()
            .AddSystemPrompt(await GetSystemPrompt("WriteComment"))
            .SetResponseType<CommentResponseAgent>()
            .AddUserMessage(param.Instructions)
            .AddUserMessage(param.Issue);
        if (param.Images.Length > 0 && param.ImageProcessingMode != ImageProcessingMode.Disable)
            request = request.AddUserMessage(param.Images
                .Select(e =>
                    ChatMessageContentPart.CreateImagePart(new BinaryData(e.Data), e.MimeType,
                        param.ImageProcessingMode switch
                        {
                            ImageProcessingMode.LowDetail => ChatImageDetailLevel.Low,
                            ImageProcessingMode.HighDetail => ChatImageDetailLevel.High,
                            _ => ChatImageDetailLevel.Auto,
                        })).Prepend(ChatMessageContentPart.CreateTextPart(param.Text)));
        else
            request = request.AddUserMessage(param.Text);

        var response = await _client.CompleteAsync(request);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", response.ReadAsString());
        return response.ReadAsJson<CommentResponseAgent>();
    }

    public async Task<CommentCreateAgent[]?> CheckIssues(IssuesRequestAgent param)
    {
        var request = new ChatRequest()
            .AddSystemPrompt(await GetSystemPrompt("CheckIssues"))
            .SetResponseType<IEnumerable<CommentCreateAgent>>()
            .AddUserMessage(param.Instructions);
        foreach (var paramChapter in param.Chapters)
        {
            request.AddUserMessage(paramChapter);
        }

        var response = await _client.CompleteAsync(request);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", response.ReadAsString());
        return response.ReadAsJson<CommentCreateAgent[]>();
    }

    public async Task<CommentCreateAgent[]?> ApplyInstruction(InstructionRequestAgent param)
    {
        var request = new ChatRequest()
            .AddSystemPrompt(await GetSystemPrompt("ApplyInstruction"))
            .SetResponseType<CommentCreateAgent[]>()
            .AddUserMessage(param.Instruction);
        foreach (var paramChapter in param.Chapters)
        {
            request.AddUserMessage(paramChapter);
        }

        var response = await _client.CompleteAsync(request);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", response.ReadAsString());
        return response.ReadAsJson<CommentCreateAgent[]>();
    }

    public async Task<IssueCreateAgent[]?> SearchInstruction(InstructionRequestAgent param)
    {
        var request = new ChatRequest()
            .AddSystemPrompt(await GetSystemPrompt("SearchInstruction"))
            .SetResponseType<IssueCreateAgent[]>()
            .AddUserMessage(param.Instruction);
        foreach (var paramChapter in param.Chapters)
        {
            request.AddUserMessage(paramChapter);
        }

        var response = await _client.CompleteAsync(request);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Agent response: {response}", response.ReadAsString());
        return response.ReadAsJson<IssueCreateAgent[]>();
    }
}