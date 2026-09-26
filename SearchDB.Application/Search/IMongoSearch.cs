namespace SearchDB.Application.Search;

public interface IMongoSearch
{
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
