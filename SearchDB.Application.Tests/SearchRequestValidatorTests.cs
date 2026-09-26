using SearchDB.Application.Search;
using Domain;
using Xunit;

namespace SearchDB.Application.Tests;

public class SearchRequestValidatorTests
{
    private static SearchRequest Request(SearchEntity entity = SearchEntity.Products, string query = "", int page = 1, int pageSize = 20, SearchFilter? filters = null) =>
        new(entity, query, page, pageSize, filters ?? new SearchFilter());

    [Fact]
    public void Empty_query_is_valid_for_browse_mode()
    {
        Assert.Empty(SearchRequestValidator.Validate(Request()));
    }

    [Fact]
    public void Query_over_200_characters_is_rejected()
    {
        var errors = SearchRequestValidator.Validate(Request(query: new string('x', 201)));

        Assert.Contains(errors, error => error.Field == "query");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Invalid_page_values_are_rejected(int page, int pageSize)
    {
        Assert.NotEmpty(SearchRequestValidator.Validate(Request(page: page, pageSize: pageSize)));
    }

    [Fact]
    public void Date_range_and_entity_filter_mismatch_are_rejected()
    {
        var invertedRange = new SearchFilter(CreatedFrom: DateTimeOffset.Parse("2026-02-01T00:00:00Z"), CreatedTo: DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var productStatus = new SearchFilter(Status: OrderStatus.Pending);

        Assert.Contains(SearchRequestValidator.Validate(Request(SearchEntity.Orders, filters: invertedRange)), error => error.Field == "createdFrom");
        Assert.Contains(SearchRequestValidator.Validate(Request(SearchEntity.Products, filters: productStatus)), error => error.Field == "status");
    }
}
