namespace CommissionSight;

/// <summary>Configuration for a <see cref="CommissionSightClient"/>.</summary>
public sealed class CommissionSightClientOptions
{
    /// <summary>
    /// Base URL of the API, e.g. <c>https://api.commissionsight.com/v1</c>.
    /// A trailing slash is trimmed automatically.
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// The bearer token (an account API token or a web session token). Optional —
    /// the only unauthenticated endpoint is <see cref="CommissionSightClient.HealthAsync"/>.
    /// Can also be set later via <see cref="CommissionSightClient.SetToken"/>.
    /// </summary>
    public string? Token { get; init; }
}
