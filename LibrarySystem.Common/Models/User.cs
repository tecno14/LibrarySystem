namespace LibrarySystem.Common.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
}
