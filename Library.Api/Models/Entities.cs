namespace Library.Api.Models;

public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Isbn { get; set; } = "";
    public int TotalCopies { get; set; }
}

public sealed class Loan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public DateTimeOffset BorrowedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
}
