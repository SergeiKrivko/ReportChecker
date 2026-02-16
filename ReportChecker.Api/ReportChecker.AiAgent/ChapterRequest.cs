using ReportChecker.Models;

namespace AiAgent;

internal record ChapterRequest(
    string Name,
    string? OldText,
    string NewText,
    string Difference,
    IAiAgentClient.IssueRead[] Issues);