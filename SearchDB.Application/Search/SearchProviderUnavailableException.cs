namespace SearchDB.Application.Search;

public sealed class SearchProviderUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
