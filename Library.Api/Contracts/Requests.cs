using System.ComponentModel.DataAnnotations;

namespace Library.Api.Contracts;

public sealed class UserRequest
{
    private string _fullName = "";
    private string _email = "";

    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get => _fullName; set => _fullName = value?.Trim() ?? ""; }

    [Required, EmailAddress, StringLength(254)]
    public string Email { get => _email; set => _email = value?.Trim() ?? ""; }
}

public sealed class BookRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(150)]
    public string Author { get; set; } = "";

    [Required, RegularExpression(@"^(?:[0-9]{10}|[0-9]{13})$",
        ErrorMessage = "ISBN must contain exactly 10 or 13 digits.")]
    public string Isbn { get; set; } = "";

    [Range(1, 10000)]
    public int TotalCopies { get; set; }
}

public sealed class LoanRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int BookId { get; set; }

    [Range(1, 30)]
    public int DurationDays { get; set; } = 14;
}
