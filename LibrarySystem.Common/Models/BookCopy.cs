using LibrarySystem.Common.Enums;

namespace LibrarySystem.Common.Models;

public class BookCopy
{
    public int Id { get; set; }
    public string ISBN { get; set; } = default!;
    public BookCopyStatus Status { get; set; }
    public string? Location { get; set; }
    public bool IsHidden { get; set; }
    public DateTime AddedDate { get; set; }
}
