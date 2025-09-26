namespace LibrarySystem.BLL.Constants;

// Define a constants for the cache keys.
public static class CacheKeys
{
    public const string MasterBookList = "books:master";

    public static string BookById(int id) => $"books:id:{id}";
    public static string CopiesByIsbn(string isbn) => $"books:copies:{isbn}";
}
