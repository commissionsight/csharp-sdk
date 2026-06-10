# CommissionSight .NET SDK

Official .NET client for the [CommissionSight](https://commissionsight.com) API — commission-statement
intelligence for insurance agencies. Ingest carrier statements, score every member month-over-month
(green / yellow / red), surface the commission you're owed, trace chargebacks, and more.

- **Modern & async** — `Task`-based, `CancellationToken` everywhere, `IAsyncDisposable`-friendly `HttpClient` usage.
- **Minimal dependencies** — zero third-party runtime packages; just `System.Net.Http` + `System.Text.Json` from the framework.
- **Multi-targeted** — `net9.0` and `net10.0`.
- **Complete** — covers 100% of the API surface (parity with the TypeScript `@commissionsight/sdk`).

## Install

```bash
dotnet add package CommissionSight
```

## Quick start

```csharp
using CommissionSight;

using var client = new CommissionSightClient(new CommissionSightClientOptions
{
    BaseUrl = "https://api.commissionsight.com/v1",
    Token = Environment.GetEnvironmentVariable("COMMISSIONSIGHT_TOKEN"),
});

// Who am I?
var me = await client.MeAsync();
Console.WriteLine($"{me.Name} ({me.Status})");

// Upload a monthly statement (async ingest → returns the queued job).
using var upload = FileUpload.FromFile("humana-2026-04.csv");
var job = await client.UploadFileAsync(upload, carrierId: "carrier_123", periodYear: 2026, periodMonth: 4,
    idempotencyKey: Guid.NewGuid().ToString());

// Read the scored results once the job completes.
var results = await client.GetJobResultsAsync(job.JobId, status: "yellow");
foreach (var row in results.Data)
{
    Console.WriteLine($"{row.MemberName}: {row.Status} [{string.Join(", ", row.Flags)}] owed {row.CommissionOwed:C}");
}
```

## Authentication

Pass a bearer token (an account API token, or a web session token) via `CommissionSightClientOptions.Token`,
or set it later with `client.SetToken("...")`. The only endpoint that needs no token is `HealthAsync()`.

## Dependency injection / `IHttpClientFactory`

Supply your own `HttpClient` so the client integrates with `IHttpClientFactory` and your DI container:

```csharp
services.AddHttpClient("commissionsight");
services.AddSingleton(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("commissionsight");
    return new CommissionSightClient(
        new CommissionSightClientOptions { BaseUrl = "https://api.commissionsight.com/v1", Token = "..." },
        http);
});
```

When you pass your own `HttpClient`, the client does **not** dispose it (the factory owns its lifetime).
The parameterless-`HttpClient` constructor creates and owns one, disposed with the client.

## Error handling

Non-success responses throw `CommissionSightApiException`, which surfaces the API's RFC-9457
`problem+json` body:

```csharp
try
{
    await client.UploadFileAsync(upload, "carrier_123", 2026, 4);
}
catch (CommissionSightApiException ex) when (ex.Code == "period_exists")
{
    // A statement already exists for this carrier+period — replace it:
    await client.UploadFileAsync(upload, "carrier_123", 2026, 4, replace: true);
}
catch (CommissionSightApiException ex)
{
    Console.Error.WriteLine($"{ex.Status} {ex.Title}: {ex.Detail}");
}
```

`ex.StatusCode`, `ex.Status` (int), `ex.Title`, `ex.Detail`, `ex.Code`, and the raw `ex.Body`
(`JsonElement?`) are all available.

## Pagination

List endpoints return `Page<T>` with a `Pagination` block (`Limit`, `Offset` or `NextCursor`, `HasMore`):

```csharp
var page = await client.ListFilesAsync(limit: 50);
while (true)
{
    foreach (var f in page.Data) Console.WriteLine(f.OriginalFilename);
    if (page.Pagination is not { HasMore: true, NextCursor: { } cursor }) break;
    page = await client.ListFilesAsync(limit: 50, cursor: cursor);
}
```

## Reports & analytics

```csharp
var rollup = await client.RollupAsync("2026-04");
Console.WriteLine($"At risk: {rollup.Totals.CommissionAtRisk:C}, owed: {rollup.Totals.CommissionOwed:C}");

var series  = await client.AttritionSeriesAsync(months: 12);
var quality = await client.DataQualityAsync("2026-04");   // incomplete / wrong-period file detection
var cbs     = await client.ListChargebacksAsync("2026-04"); // each traced to its original payout
var journey = await client.GetMemberJourneyAsync("member_ref_id"); // full per-member audit history
```

## Admin

Back-office endpoints (require an allowlisted admin session token) live under `client.Admin`:

```csharp
var accounts = await client.Admin.ListAccountsAsync(status: "pending");
await client.Admin.ApproveAccountAsync(accounts.Data[0].Id);
var metrics = await client.Admin.MetricsAsync();
```

## Types

- Enums (`Status`, `Flag`, `JobStatus`, `StabilityLevel`, `RateType`, …) serialize to/from their wire
  strings. **Unrecognized values deserialize to `Unknown`** rather than throwing, so a newer server
  never breaks an older SDK.
- Timestamps surface as `DateTimeOffset` whether the API sends an epoch (ms) number or an ISO string.
- Monetary dollar amounts are `decimal`; rates/fractions are `double`; cents are `long`.
- Opaque/free-form payloads (raw configs, member rows, deltas) surface as `System.Text.Json.JsonElement`.

## Building from source

```bash
dotnet build -c Release
dotnet test  -c Release
dotnet pack  src/CommissionSight/CommissionSight.csproj -c Release -o ./artifacts
```

## License

MIT © CommissionSight. See [LICENSE](LICENSE).
