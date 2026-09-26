using Domain;

namespace SearchDB.Application.Search;

public sealed record SearchFilter(
    string? Category = null,
    bool? Active = null,
    OrderStatus? Status = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null);
