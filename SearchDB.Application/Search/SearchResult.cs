namespace SearchDB.Application.Search;

public sealed record SearchResult(
    SearchEntity Entity,
    IReadOnlyList<SearchResultItem> Items,
    long? TotalCount,
    int Page,
    int PageSize,
    double DurationMilliseconds);

public sealed record SearchResultItem(
    string Id,
    string PrimaryText,
    string SecondaryText,
    string? Category,
    string? Status,
    decimal? Amount,
    DateTimeOffset? CreatedAt,
    double Score);

public sealed record SearchValidationError(string Field, string Message);
