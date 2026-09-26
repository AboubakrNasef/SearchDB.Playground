namespace SearchDB.Application.Search;

public sealed record SearchRequest(
    SearchEntity Entity,
    string Query,
    int Page,
    int PageSize,
    SearchFilter Filters);
