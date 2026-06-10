using System.Net;
using System.Text.Json;

namespace CommissionSight;

/// <summary>
/// Thrown when the API returns a non-success status. Mirrors the RFC-9457
/// <c>problem+json</c> body the API emits: <see cref="Title"/> and
/// <see cref="Detail"/> come from that body when present.
/// </summary>
public sealed class CommissionSightApiException : Exception
{
    public CommissionSightApiException(HttpStatusCode statusCode, string message, string? detail, JsonElement? body)
        : base(message)
    {
        StatusCode = statusCode;
        Title = message;
        Detail = detail;
        Body = body;
    }

    /// <summary>The HTTP status code returned by the API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The numeric HTTP status code, for convenience.</summary>
    public int Status => (int)StatusCode;

    /// <summary>The <c>title</c> field of the problem body (or the status reason).</summary>
    public string Title { get; }

    /// <summary>The <c>detail</c> field of the problem body, when present.</summary>
    public string? Detail { get; }

    /// <summary>
    /// A machine-readable error code from the problem body's <c>code</c> field
    /// (e.g. <c>period_exists</c>, <c>ai_paused</c>), when present.
    /// </summary>
    public string? Code =>
        Body is { ValueKind: JsonValueKind.Object } b && b.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String
            ? c.GetString()
            : null;

    /// <summary>The raw parsed problem body, when the response carried JSON.</summary>
    public JsonElement? Body { get; }
}
