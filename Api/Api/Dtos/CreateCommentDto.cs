using System.Text.Json.Serialization;
using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CreateCommentDto
{
    public string? Content { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IssueStatus? Status { get; init; }
}