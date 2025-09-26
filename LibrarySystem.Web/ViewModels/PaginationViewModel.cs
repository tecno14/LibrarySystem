namespace LibrarySystem.Web.ViewModels;

public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    // Optional route values like search filters
    public Dictionary<string, string?> RouteValues { get; set; } = [];
}
