namespace CommissionSight;

/// <summary>A paginated list response: <c>{ data, pagination }</c>.</summary>
public sealed record Page<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public Pagination? Pagination { get; init; }
}

/// <summary>Pagination metadata. Offset-based endpoints set <see cref="Offset"/>;
/// cursor-based endpoints set <see cref="NextCursor"/>.</summary>
public sealed record Pagination
{
    public int Limit { get; init; }
    public int? Offset { get; init; }
    public long? NextCursor { get; init; }
    public bool HasMore { get; init; }
}

/// <summary>A bare list response: <c>{ data }</c> (no pagination).</summary>
public sealed record DataList<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
}

/// <summary>A carrier.</summary>
public sealed record CarrierSummary
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
}

/// <summary>A year/month period reference.</summary>
public sealed record PeriodRef
{
    public int Year { get; init; }
    public int Month { get; init; }
}
