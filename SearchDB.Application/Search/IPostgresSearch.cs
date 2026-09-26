namespace SearchDB.Application.Search;

public interface IPostgresSearch
{
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
