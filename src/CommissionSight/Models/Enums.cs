using System.Text.Json.Serialization;

namespace CommissionSight;

/// <summary>Member status for a period, file-over-file.</summary>
public enum Status
{
    Unknown = 0,
    [JsonStringEnumMemberName("green")] Green,
    [JsonStringEnumMemberName("yellow")] Yellow,
    [JsonStringEnumMemberName("red")] Red,
}

/// <summary>Explicit delta flags attached to a member's status for a period.</summary>
public enum Flag
{
    Unknown = 0,
    [JsonStringEnumMemberName("NEW")] New,
    [JsonStringEnumMemberName("COMMISSION_CHANGED")] CommissionChanged,
    [JsonStringEnumMemberName("DATA_CHANGED")] DataChanged,
    [JsonStringEnumMemberName("DROPPED")] Dropped,
    [JsonStringEnumMemberName("REAPPEARED")] Reappeared,
    [JsonStringEnumMemberName("REAPPEARED_WITH_DELTA")] ReappearedWithDelta,
    [JsonStringEnumMemberName("CHARGEBACK")] Chargeback,
}

/// <summary>Lifecycle of an ingest job.</summary>
public enum JobStatus
{
    Unknown = 0,
    [JsonStringEnumMemberName("queued")] Queued,
    [JsonStringEnumMemberName("processing")] Processing,
    [JsonStringEnumMemberName("completed")] Completed,
    [JsonStringEnumMemberName("failed")] Failed,
}

/// <summary>Statement-quality (data-quality) signal level.</summary>
public enum StabilityLevel
{
    Unknown = 0,
    [JsonStringEnumMemberName("ok")] Ok,
    [JsonStringEnumMemberName("watch")] Watch,
    [JsonStringEnumMemberName("alert")] Alert,
}

/// <summary>How an expected (contracted) commission rate is expressed.</summary>
public enum RateType
{
    Unknown = 0,
    [JsonStringEnumMemberName("percent_of_premium")] PercentOfPremium,
    [JsonStringEnumMemberName("flat_per_member")] FlatPerMember,
}

/// <summary>A saved payment method type.</summary>
public enum PaymentMethodType
{
    Unknown = 0,
    [JsonStringEnumMemberName("card")] Card,
    [JsonStringEnumMemberName("us_bank_account")] UsBankAccount,
}

/// <summary>Role of a user within an account.</summary>
public enum TeamRole
{
    Unknown = 0,
    [JsonStringEnumMemberName("member")] Member,
    [JsonStringEnumMemberName("admin")] Admin,
}

/// <summary>Subscribable webhook event.</summary>
public enum WebhookEvent
{
    Unknown = 0,
    [JsonStringEnumMemberName("job.completed")] JobCompleted,
    [JsonStringEnumMemberName("job.failed")] JobFailed,
}

/// <summary>The product line a carrier writes business in.</summary>
public enum ProductLine
{
    Unknown = 0,
    [JsonStringEnumMemberName("major_medical")] MajorMedical,
    [JsonStringEnumMemberName("medicare")] Medicare,
    [JsonStringEnumMemberName("ancillary")] Ancillary,
}

/// <summary>Account lifecycle status.</summary>
public enum AccountStatus
{
    Unknown = 0,
    [JsonStringEnumMemberName("active")] Active,
    [JsonStringEnumMemberName("pending")] Pending,
    [JsonStringEnumMemberName("suspended")] Suspended,
}
