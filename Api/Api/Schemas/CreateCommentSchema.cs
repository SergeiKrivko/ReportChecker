using System.Text.Json.Serialization;
using ReportChecker.Models;

namespace ReportChecker.Api.Schemas;

public class CreateCommentSchema
{
    public string? Content { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IssueStatus? Status { get; init; }
}