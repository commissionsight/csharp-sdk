using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommissionSight;

// ─── Jobs & files ───────────────────────────────────────────────────────────

/// <summary>An ingest job.</summary>
public record JobSummary
{
    public required string Id { get; init; }
    public required string CarrierId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public required string FileId { get; init; }
    public string? R2Key { get; init; }
    public JobStatus Status { get; init; }
    public IReadOnlyDictionary<string, double>? Stats { get; init; }
    public string? Error { get; init; }
    public string? WebhookUrl { get; init; }

    /// <summary>Rows rejected on ingest; download the detail via <c>DownloadExceptionsAsync</c>.</summary>
    public int? ExceptionRowCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>An uploaded statement file's metadata.</summary>
public sealed record FileSummary
{
    public required string Id { get; init; }
    public required string AccountId { get; init; }
    public required string CarrierId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public required string OriginalFilename { get; init; }
    public long ByteSize { get; init; }
    public required string ChecksumSha256 { get; init; }
    public DateTimeOffset UploadedAt { get; init; }

    /// <summary>True when this period's scoring is stale because an earlier baseline was
    /// uploaded out of order. Call <c>RescoreFileAsync</c> to refresh it.</summary>
    public bool? RescoreSuggested { get; init; }

    /// <summary>When set, the raw bytes were purged from object storage (retention).</summary>
    public DateTimeOffset? R2PurgedAt { get; init; }
}

/// <summary>Result of <c>UploadFileAsync</c>.</summary>
public sealed record UploadResult
{
    public required string JobId { get; init; }
    public required string FileId { get; init; }
    public required string Status { get; init; }
    public string? Mode { get; init; }
}

/// <summary>Result of file operations that enqueue a job (rescore, retract).</summary>
public sealed record JobRef
{
    public required string JobId { get; init; }
    public required string FileId { get; init; }
    public required string Status { get; init; }
    public required string Mode { get; init; }
}

/// <summary>Result of <c>PurgeFileAsync</c>.</summary>
public sealed record PurgeResult
{
    public required string FileId { get; init; }
    public bool Purged { get; init; }
}

/// <summary>Result of <c>RetryJobAsync</c>.</summary>
public sealed record JobStatusRef
{
    public required string JobId { get; init; }
    public required string Status { get; init; }
}

// ─── Results ────────────────────────────────────────────────────────────────

/// <summary>One scored member row in a job's results grid.</summary>
public sealed record ResultRow
{
    /// <summary>Internal member id — stable across periods; use it for <c>GetMemberJourneyAsync</c>.</summary>
    public required string MemberRefId { get; init; }

    /// <summary>Internal policy id (member-scoped); use it for <c>GetPolicyJourneyAsync</c>.</summary>
    public string? PolicyRefId { get; init; }

    public Status Status { get; init; }
    public IReadOnlyList<Flag> Flags { get; init; } = [];
    public decimal? CommissionAmount { get; init; }
    public decimal? PrevCommissionAmount { get; init; }

    /// <summary>Expected-vs-actual shortfall for this member (recoverable), in dollars.</summary>
    public decimal CommissionOwed { get; init; }

    public string? ComparedAgainstPeriod { get; init; }
    public string? MemberExternalId { get; init; }
    public string? MemberName { get; init; }
    public string? Email { get; init; }
    public string? PlanName { get; init; }
    public string? PolicyNumber { get; init; }
    public decimal? PremiumAmount { get; init; }
}

/// <summary>A job's results grid (a <see cref="Page{T}"/> of <see cref="ResultRow"/> plus the period).</summary>
public sealed record JobResultsPage
{
    public IReadOnlyList<ResultRow> Data { get; init; } = [];
    public Pagination? Pagination { get; init; }
    public PeriodRef? Period { get; init; }
}

// ─── Carriers & configs ───────────────────────────────────────────────────────

/// <summary>Result of creating a carrier config.</summary>
public sealed record CreateConfigResult
{
    public required string Id { get; init; }
    public int Version { get; init; }
}

/// <summary>Mapped/preview result of a config dry-run (<c>TestConfigAsync</c>) or inference preview.</summary>
public sealed record ConfigPreview
{
    public int Mapped { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<JsonElement> Rows { get; init; } = [];
}

/// <summary>One column the inference engine matched to a canonical field.</summary>
public sealed record MappedColumn
{
    public required string Header { get; init; }
    public required string Target { get; init; }
    public double Score { get; init; }
}

/// <summary>A draft config inferred from a sample file.</summary>
public sealed record InferredConfig
{
    public JsonElement Config { get; init; }
    public double Confidence { get; init; }
    public int HeaderRow { get; init; }
    public IReadOnlyList<string> Sheets { get; init; } = [];
    public IReadOnlyList<MappedColumn> Mapped { get; init; } = [];
    public IReadOnlyList<string> Unmapped { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
    public ConfigPreview? Preview { get; init; }
}

// ─── Members & journeys ───────────────────────────────────────────────────────

/// <summary>One policy line within a journey period.</summary>
public sealed record JourneyPolicy
{
    public required string PolicyRefId { get; init; }
    public string? PolicyNumber { get; init; }
    public string? PlanName { get; init; }
    public string? PlanCode { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal? PremiumAmount { get; init; }
    public string? EffectiveDate { get; init; }
    public string? RenewalDate { get; init; }
}

/// <summary>One field-level change entering a journey period.</summary>
public sealed record JourneyDelta
{
    public required string Field { get; init; }
    public string? PrevValue { get; init; }
    public string? CurrValue { get; init; }
}

/// <summary>The source file behind a journey period.</summary>
public sealed record JourneyFile
{
    public required string FileId { get; init; }
    public string? FileName { get; init; }
    public DateTimeOffset? UploadedAt { get; init; }
}

/// <summary>One period in a member/policy audit journey.</summary>
public sealed record JourneyPeriod
{
    public required string Period { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public Status? Status { get; init; }
    public IReadOnlyList<Flag> Flags { get; init; } = [];

    /// <summary>True when present on this period's statement; false for a drop-off period.</summary>
    public bool Present { get; init; }

    public decimal? CommissionAmount { get; init; }
    public decimal? PrevCommissionAmount { get; init; }
    public decimal? PremiumAmount { get; init; }
    public IReadOnlyList<JourneyPolicy> Policies { get; init; } = [];
    public JourneyFile? File { get; init; }
    public IReadOnlyList<JourneyDelta> Deltas { get; init; } = [];
    public bool FirstSeen { get; init; }
}

/// <summary>The member summary block of a journey.</summary>
public sealed record JourneyMember
{
    public string? MemberExternalId { get; init; }
    public string? MemberName { get; init; }
    public string? Email { get; init; }
    public required string CarrierId { get; init; }
}

/// <summary>The full audit history of a member (or a single policy).</summary>
public sealed record Journey
{
    public required string MemberRefId { get; init; }
    public string? PolicyRefId { get; init; }
    public required JourneyMember Member { get; init; }
    public string? FirstPeriod { get; init; }
    public string? LatestPeriod { get; init; }
    public int PeriodCount { get; init; }
    public IReadOnlyList<JourneyPeriod> Periods { get; init; } = [];
}

// ─── Team & audit ─────────────────────────────────────────────────────────────

/// <summary>An account team member.</summary>
public sealed record TeamMember
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public TeamRole Role { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Result of inviting a teammate.</summary>
public sealed record InviteResult
{
    public required string Status { get; init; }
    public required string Email { get; init; }
    public string? UserId { get; init; }
}

/// <summary>One audit-trail event. <see cref="Action"/> is a dotted verb (e.g. <c>api.upload_statement</c>).</summary>
public sealed record AuditEvent
{
    public required string Id { get; init; }
    public required string Actor { get; init; }
    public required string Action { get; init; }
    public string? Target { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? Meta { get; init; }
    public string? Ip { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

// ─── Comparisons & reports ────────────────────────────────────────────────────

/// <summary>One row in a period comparison.</summary>
public sealed record ComparisonRow
{
    public required string MemberRefId { get; init; }
    public Status Status { get; init; }
    public IReadOnlyList<Flag> Flags { get; init; } = [];
    public decimal? CommissionAmount { get; init; }
    public decimal? PrevCommissionAmount { get; init; }
    public string? ComparedAgainstPeriod { get; init; }
    public string? MemberName { get; init; }
    public string? MemberExternalId { get; init; }
    public string? PolicyNumber { get; init; }
}

/// <summary>Status mix summary of a comparison.</summary>
public sealed record ComparisonSummary
{
    public int Green { get; init; }
    public int Yellow { get; init; }
    public int Red { get; init; }
    [JsonPropertyName("new")] public int New { get; init; }
    public int Reappeared { get; init; }
    public int Total { get; init; }
}

/// <summary>Result of <c>CompareAsync</c>.</summary>
public sealed record ComparisonResult
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required ComparisonSummary Summary { get; init; }
    public IReadOnlyList<ComparisonRow> Data { get; init; } = [];
}

/// <summary>Period rollup totals (green/yellow/red plus money).</summary>
public sealed record RollupTotals
{
    public int MemberCount { get; init; }
    public int Green { get; init; }
    public int Yellow { get; init; }
    public int Red { get; init; }
    [JsonPropertyName("new")] public int New { get; init; }
    public int Reappeared { get; init; }

    /// <summary>Commission at risk vs the prior period (MoM shortfall), in dollars.</summary>
    public decimal CommissionAtRisk { get; init; }

    /// <summary>Of <see cref="CommissionAtRisk"/>: prior commission of dropped members.</summary>
    public decimal CommissionDropped { get; init; }

    /// <summary>Of <see cref="CommissionAtRisk"/>: the decrease for members paid less.</summary>
    public decimal CommissionReduced { get; init; }

    /// <summary>Still-present members paid less than the prior period.</summary>
    public int ReducedCount { get; init; }

    /// <summary>Expected-vs-actual commission owed (recoverable), in dollars.</summary>
    public decimal CommissionOwed { get; init; }

    /// <summary>Records the owed figure could be computed for.</summary>
    public int OwedEvaluated { get; init; }

    /// <summary>All records considered for owed (coverage denominator).</summary>
    public int OwedTotal { get; init; }

    /// <summary>Members with a chargeback this period.</summary>
    public int ChargebackCount { get; init; }

    /// <summary>Total commission clawed back this period (positive magnitude).</summary>
    public decimal ChargebackAmount { get; init; }
}

/// <summary>Result of <c>RollupAsync</c>.</summary>
public sealed record RollupResult
{
    public string? Period { get; init; }
    public required RollupTotals Totals { get; init; }
    public IReadOnlyList<JsonElement> ByCarrier { get; init; } = [];
}

/// <summary>The original payout a chargeback reverses.</summary>
public sealed record OriginalPayout
{
    public required string Period { get; init; }
    public decimal Amount { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
}

/// <summary>One chargeback (negative-commission record) enriched with the original payout.</summary>
public sealed record ChargebackRow
{
    public required string MemberRefId { get; init; }
    public required string PolicyRefId { get; init; }
    public string? MemberExternalId { get; init; }
    public string? PolicyNumber { get; init; }
    public string? PlanName { get; init; }

    /// <summary>Amount clawed back this period (positive magnitude).</summary>
    public decimal ChargebackAmount { get; init; }

    /// <summary>Whether the carrier ever paid this policy out.</summary>
    public bool PaidOut { get; init; }

    /// <summary>The original payout: when/where the carrier first paid, and how much.</summary>
    public OriginalPayout? OriginalPayout { get; init; }

    /// <summary>Whether the chargeback exactly reverses the original payout.</summary>
    public bool FullyReversed { get; init; }
}

/// <summary>Result of <c>ListChargebacksAsync</c>.</summary>
public sealed record ChargebacksResult
{
    public string? Period { get; init; }
    public IReadOnlyList<ChargebackRow> Data { get; init; } = [];
    public Pagination? Pagination { get; init; }
}

/// <summary>Result of <c>AttritionAsync</c> (point-in-time).</summary>
public sealed record AttritionResult
{
    public double AttritionRate { get; init; }
    public IReadOnlyList<JsonElement> ByCarrier { get; init; } = [];
}

/// <summary>One point in an attrition trend series.</summary>
public sealed record AttritionPoint
{
    public required string Period { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public int Red { get; init; }
    public int MemberCount { get; init; }
    public double AttritionRate { get; init; }

    /// <summary>Commission at risk this period (MoM shortfall), in dollars.</summary>
    public decimal CommissionAtRisk { get; init; }
}

/// <summary>Result of <c>AttritionSeriesAsync</c>.</summary>
public sealed record AttritionSeriesResult
{
    public IReadOnlyList<AttritionPoint> Data { get; init; } = [];
}

/// <summary>Statement-quality signal for one carrier in a period.</summary>
public sealed record DataQualitySignal
{
    public required string CarrierId { get; init; }
    public string? CarrierName { get; init; }
    public StabilityLevel Level { get; init; }
    public required string Reason { get; init; }
    public double DroppedRate { get; init; }
    public double NewRate { get; init; }
    public double ChurnOverlap { get; init; }
    public int Red { get; init; }
    public int NewMembers { get; init; }
    public int Reappeared { get; init; }
    public int Present { get; init; }
}

/// <summary>Result of <c>DataQualityAsync</c>.</summary>
public sealed record DataQualityReport
{
    public string? Period { get; init; }
    public StabilityLevel Overall { get; init; }
    public IReadOnlyList<DataQualitySignal> Carriers { get; init; } = [];
}

// ─── Expected rates ───────────────────────────────────────────────────────────

/// <summary>An expected (contracted) commission rate — an input to the "owed" model.</summary>
public record ExpectedCommissionRate
{
    public required string Id { get; init; }
    public required string CarrierId { get; init; }

    /// <summary>null = the carrier-wide default; a value = a per-plan override.</summary>
    public string? PlanCode { get; init; }

    public RateType RateType { get; init; }

    /// <summary>Fraction for percent_of_premium (0.20 = 20%); dollars for flat_per_member.</summary>
    public double RateValue { get; init; }
}

/// <summary>Result of <c>UpsertExpectedRateAsync</c> — the rate plus how many periods were re-scored.</summary>
public sealed record UpsertedRate : ExpectedCommissionRate
{
    public int RescoredPeriods { get; init; }
}

// ─── Webhooks ─────────────────────────────────────────────────────────────────

/// <summary>A webhook subscription.</summary>
public record Webhook
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public IReadOnlyList<string> Events { get; init; } = [];
}

/// <summary>A newly created webhook — the signing <see cref="Secret"/> is returned ONCE.</summary>
public sealed record CreatedWebhook : Webhook
{
    public required string Secret { get; init; }
}

// ─── Session / service ────────────────────────────────────────────────────────

/// <summary>The account behind the current token.</summary>
public sealed record AccountInfo
{
    public required string AccountId { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
}

/// <summary>One workspace within an account.</summary>
public sealed record WorkspaceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsDefault { get; init; }
}

/// <summary>The account's workspaces. When <see cref="Enabled"/> is true, uploads must target one.</summary>
public sealed record WorkspacesInfo
{
    public bool Enabled { get; init; }
    public IReadOnlyList<WorkspaceInfo> Workspaces { get; init; } = [];
}

/// <summary>Liveness probe result.</summary>
public sealed record HealthInfo
{
    public required string Status { get; init; }
    public required string Service { get; init; }
    public required string Environment { get; init; }
}

// ─── Billing ──────────────────────────────────────────────────────────────────

/// <summary>Editable billing contact details.</summary>
public record BillingDetails
{
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
}

/// <summary>A stored card summary.</summary>
public sealed record CardInfo
{
    public required string Brand { get; init; }
    public required string Last4 { get; init; }
}

/// <summary>The account's billing profile (details + payment method).</summary>
public sealed record BillingProfile : BillingDetails
{
    public CardInfo? Card { get; init; }
    public PaymentMethodType? PaymentMethod { get; init; }
    public bool? StripeEnabled { get; init; }
}

/// <summary>A preview of the next invoice.</summary>
public sealed record BillingPreview
{
    public string? Period { get; init; }
    public int Members { get; init; }
    public long PricePerMemberCents { get; init; }
    public long AmountCents { get; init; }
    public PaymentMethodType Method { get; init; }
    public long FeeCents { get; init; }
    public long TotalCents { get; init; }
    public long AchSavingsCents { get; init; }
    public bool? Surcharge { get; init; }
    public string? DueDate { get; init; }
    public bool? Custom { get; init; }
    public string? LastBilledPeriod { get; init; }
}

/// <summary>Result of <c>CreateSetupIntentAsync</c>.</summary>
public sealed record SetupIntentResult
{
    public required string ClientSecret { get; init; }
    public string? PublishableKey { get; init; }
    public required string CustomerId { get; init; }
}

/// <summary>Result of <c>SavePaymentMethodAsync</c>.</summary>
public sealed record SavedPaymentMethod
{
    public required string Method { get; init; }
    public string? Brand { get; init; }
    public string? Last4 { get; init; }
}
