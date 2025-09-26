namespace LibrarySystem.Web.ViewModels;

public class BookCoverViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    // True for card-in-many (Index), false for single item view (Details)
    public bool IsListView { get; set; } = true;
}
