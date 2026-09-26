namespace SearchDB.Application.Search;

public static class SearchRequestValidator
{
    public static IReadOnlyList<SearchValidationError> Validate(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<SearchValidationError>();
        var query = request.Query ?? string.Empty;

        if (query.Length > 200)
            errors.Add(new("query", "Query must be 200 characters or fewer."));
        if (request.Page < 1)
            errors.Add(new("page", "Page must be at least 1."));
        if (request.PageSize is < 1 or > 100)
            errors.Add(new("pageSize", "Page size must be between 1 and 100."));
        if (request.Filters is null)
            errors.Add(new("filters", "Filters are required."));
        else
        {
            var filters = request.Filters;
            if (filters.CreatedFrom.HasValue && filters.CreatedTo.HasValue && filters.CreatedFrom > filters.CreatedTo)
                errors.Add(new("createdFrom", "Created-from must be earlier than or equal to created-to."));

            if (request.Entity == SearchEntity.Products && (filters.Status.HasValue || filters.CreatedFrom.HasValue || filters.CreatedTo.HasValue))
                errors.Add(new(filters.Status.HasValue ? "status" : "createdFrom", "Order-only filters cannot be used for product searches."));
            if (request.Entity == SearchEntity.Orders && (filters.Category is not null || filters.Active.HasValue))
                errors.Add(new(filters.Category is not null ? "category" : "active", "Product-only filters cannot be used for order searches."));
        }

        if (!Enum.IsDefined(request.Entity))
            errors.Add(new("entity", "Entity must be products or orders."));

        return errors;
    }
}
