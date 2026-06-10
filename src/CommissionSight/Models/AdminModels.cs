using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommissionSight;

/// <summary>An <c>{ id, name, slug }</c> reference (created account/carrier; carrier rename).</summary>
public sealed record NamedEntity
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Slug { get; init; }
}

/// <summary>One account in the admin account list.</summary>
public sealed record AdminAccountListItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Status { get; init; }
    public long? CustomRateCents { get; init; }
    public bool? Provisioned { get; init; }
}

/// <summary>Platform-wide admin metrics.</summary>
public sealed record AdminMetrics
{
    public int TotalJobs { get; init; }
    public int JobsLast24h { get; init; }
    public IReadOnlyDictionary<string, int> ByStatus { get; init; } = new Dictionary<string, int>();
    public int Failures { get; init; }
    public int Accounts { get; init; }
    public IReadOnlyDictionary<string, int> AccountsByStatus { get; init; } = new Dictionary<string, int>();
    public int PendingAccounts { get; init; }
    public int Users { get; init; }
    public IReadOnlyDictionary<string, int> Webhooks { get; init; } = new Dictionary<string, int>();
    public int WebhooksPending { get; init; }
    public int WebhooksFailed { get; init; }
    public IReadOnlyList<AdminRecentJob> RecentJobs { get; init; } = [];
    public IReadOnlyList<AdminRecentAccount> RecentAccounts { get; init; } = [];
}

public sealed record AdminRecentJob
{
    public required string Id { get; init; }
    public required string CarrierId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public required string Status { get; init; }
    public string? Error { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record AdminRecentAccount
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>An admin job-list item (a job plus its owning account).</summary>
public sealed record AdminJobListItem : JobSummary
{
    public required string AccountId { get; init; }
    public bool? RescoreSuggested { get; init; }
}

/// <summary>The full job record on the admin job-detail view.</summary>
public sealed record AdminJobInfo : JobSummary
{
    public required string AccountId { get; init; }
    public string? IdempotencyKey { get; init; }
}

public sealed record AdminJobFile
{
    public required string OriginalFilename { get; init; }
    public long ByteSize { get; init; }
    public required string ChecksumSha256 { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}

public sealed record AdminJobAccount
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
}

/// <summary>Admin job-detail response.</summary>
public sealed record AdminJobDetail
{
    public required AdminJobInfo Job { get; init; }
    public string? CarrierName { get; init; }
    public AdminJobAccount? Account { get; init; }
    public AdminJobFile? File { get; init; }
    public long? DurationMs { get; init; }
    public bool? RescoreSuggested { get; init; }
}

public sealed record AdminLogEvent
{
    public required string Id { get; init; }
    public DateTimeOffset Ts { get; init; }
    public required string Level { get; init; }
    public required string Source { get; init; }
    public required string Message { get; init; }
    public string? Detail { get; init; }
}

public sealed record AdminAlert
{
    public required string Id { get; init; }
    public required string Severity { get; init; }
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public DateTimeOffset Ts { get; init; }
}

public sealed record AdminLogs
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<AdminLogEvent> Events { get; init; } = [];
    public IReadOnlyList<AdminAlert> Alerts { get; init; } = [];
    public Pagination? Pagination { get; init; }
}

/// <summary>Latest-period rollup on the admin account dashboard.</summary>
public sealed record AdminAccountRollup
{
    public int MemberCount { get; init; }
    public int Green { get; init; }
    public int Yellow { get; init; }
    public int Red { get; init; }
    [JsonPropertyName("new")] public int New { get; init; }
    public int Reappeared { get; init; }
    public double AttritionRate { get; init; }
    public decimal CommissionAtRisk { get; init; }
    public decimal CommissionOwed { get; init; }
    public int ChargebackCount { get; init; }
    public decimal ChargebackAmount { get; init; }
}

public sealed record AdminAccountCounts
{
    public int Files { get; init; }
    public int Jobs { get; init; }
    public int Users { get; init; }
}

public sealed record AdminAccountSummary
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Status { get; init; }
}

/// <summary>Per-account admin dashboard: counts + the latest period's summed rollup.</summary>
public sealed record AdminAccountOverview
{
    public required AdminAccountSummary Account { get; init; }
    public required AdminAccountCounts Counts { get; init; }
    public string? LatestPeriod { get; init; }
    public AdminAccountRollup? Rollup { get; init; }
}

public sealed record AdminAccountFile
{
    public required string Id { get; init; }
    public required string CarrierId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public required string OriginalFilename { get; init; }
    public long ByteSize { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? SupersededAt { get; init; }
    public DateTimeOffset? R2PurgedAt { get; init; }
}

public sealed record AdminAccountJob
{
    public required string Id { get; init; }
    public required string CarrierId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public required string Status { get; init; }
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, double>? Stats { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public sealed record AdminAccountUser
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>One scheduled-maintenance (cron) pass — admin System tab.</summary>
public sealed record AdminCronRun
{
    public required string Id { get; init; }
    public DateTimeOffset RanAt { get; init; }
    public int Requeued { get; init; }
    public int WebhooksRedelivered { get; init; }
    public int FilesPurged { get; init; }
    public int JobsCanceled { get; init; }
    public int Billed { get; init; }
    public long DurationMs { get; init; }
}

public sealed record AdminCronTask
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}

public sealed record AdminSystemActivity
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<AdminCronTask> Tasks { get; init; } = [];
    public IReadOnlyList<AdminCronRun> Runs { get; init; } = [];
}

public sealed record AdminRevenueAccounts
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Pending { get; init; }
    public int Suspended { get; init; }
}

public sealed record AdminRevenuePlatform
{
    public required AdminRevenueAccounts Accounts { get; init; }
    public int Users { get; init; }
    public int ApiKeys { get; init; }
    public int Carriers { get; init; }

    /// <summary>Distinct monitored members across the platform.</summary>
    public int Heartbeats { get; init; }
}

public sealed record AdminRevenueByType
{
    public int Accounts { get; init; }
    public long ProjectedCents { get; init; }
    public long LastBilledCents { get; init; }
}

public sealed record AdminRevenueRevenue
{
    public long ProjectedMonthlyCents { get; init; }
    public long AvgCostPerMemberCents { get; init; }
    public long LastCycleBilledCents { get; init; }
    public int AccountsWithPaymentMethod { get; init; }
    public IReadOnlyDictionary<string, AdminRevenueByType> ByPaymentType { get; init; } =
        new Dictionary<string, AdminRevenueByType>();
}

/// <summary>Stripe A/R block. When <see cref="Configured"/> is false, the amount fields are null.</summary>
public sealed record AdminRevenueAr
{
    public bool Configured { get; init; }
    public int? Invoices { get; init; }
    public long? BilledCents { get; init; }
    public long? PaidCents { get; init; }
    public long? OutstandingCents { get; init; }
    public long? FailedCents { get; init; }
    public bool? RecentOnly { get; init; }
}

public sealed record AdminRevenueCarrier
{
    public required string CarrierId { get; init; }
    public required string Name { get; init; }
    public int Accounts { get; init; }
    public int Heartbeats { get; init; }
}

/// <summary>Platform billing/revenue summary (admin). Amounts are in cents.</summary>
public sealed record AdminRevenueSummary
{
    public DateTimeOffset GeneratedAt { get; init; }
    public required AdminRevenuePlatform Platform { get; init; }
    public required AdminRevenueRevenue Revenue { get; init; }
    public required AdminRevenueAr Ar { get; init; }
    public IReadOnlyList<AdminRevenueCarrier> Carriers { get; init; } = [];
}

/// <summary>Result of provisioning an account's data store.</summary>
public sealed record AdminProvisionResult
{
    public bool Provisioned { get; init; }
    public bool? AlreadyProvisioned { get; init; }
    public bool? CreatedDatabase { get; init; }
    public int? MigrationsApplied { get; init; }
    public string? Error { get; init; }
}

/// <summary>An account's billing record (admin view).</summary>
public sealed record AdminAccountBilling
{
    public required string AccountId { get; init; }
    public string? ContactName { get; init; }
    public string? CompanyName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public long? CustomRateCents { get; init; }
    public bool SurchargeEnabled { get; init; }
    public PaymentMethodType? PaymentMethodType { get; init; }
    public CardInfo? Card { get; init; }
    public string? LastBilledPeriod { get; init; }
    public long? LastBilledAmountCents { get; init; }
}

/// <summary>A platform user (admin view).</summary>
public sealed record AdminUser
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public TeamRole Role { get; init; }
    public string? AccountId { get; init; }
    public string? AccountName { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>An API token's metadata (never the secret).</summary>
public sealed record AdminTokenInfo
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public bool Revoked { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>A carrier config row (admin view).</summary>
public sealed record CarrierConfigEntry
{
    public required string Id { get; init; }
    public int Version { get; init; }
    public required string FileType { get; init; }
    public string? AccountId { get; init; }
    public bool IsActive { get; init; }
    public JsonElement Config { get; init; }
}

// ─── Small admin action results ───────────────────────────────────────────────

public sealed record AdminBillingRateResult
{
    public required string AccountId { get; init; }
    public long? CustomRateCents { get; init; }
}

public sealed record AdminAiSettingsResult
{
    public required string AccountId { get; init; }
    public long? CapCents { get; init; }
    public bool? Passthrough { get; init; }
}

public sealed record AdminSurchargeResult
{
    public required string AccountId { get; init; }
    public bool SurchargeEnabled { get; init; }
}

public sealed record AdminApproveResult
{
    public required string Id { get; init; }
    public required string Status { get; init; }
    public int Notified { get; init; }
    public AdminProvisionResult? Provision { get; init; }
}

public sealed record AdminPurgeFilesResult
{
    public required string AccountId { get; init; }
    public int Purged { get; init; }
}

public sealed record AdminIssuedToken
{
    public required string TokenId { get; init; }
    public required string Token { get; init; }
    public required string Label { get; init; }
}

public sealed record AdminRevokeTokenResult
{
    public required string TokenId { get; init; }
    public bool Revoked { get; init; }
}

public sealed record AdminStoreCredentialsResult
{
    public required string AccountId { get; init; }
    public bool Stored { get; init; }
}

public sealed record AdminUpdateUserResult
{
    public required string Id { get; init; }
    public bool Updated { get; init; }
}

public sealed record AdminDeleteUserResult
{
    public required string Id { get; init; }
    public bool Deleted { get; init; }
}

public sealed record AdminUpdateConfigResult
{
    public required string Id { get; init; }
    public int Version { get; init; }
    public bool Updated { get; init; }
}

public sealed record AdminAllowlistResult
{
    public required string Email { get; init; }
}

public sealed record AdminRescoreJobResult
{
    public required string JobId { get; init; }
    public required string Status { get; init; }
    public required string Mode { get; init; }
}
