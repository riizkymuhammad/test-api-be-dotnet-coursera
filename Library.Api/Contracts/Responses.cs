using Library.Api.Models;

namespace Library.Api.Contracts;

public sealed record BookResponse(
    int Id, string Title, string Author, string Isbn, int TotalCopies, int AvailableCopies);

public sealed record LoanResponse(
    int Id, int UserId, int BookId, DateTimeOffset BorrowedAt,
    DateOnly DueDate, DateTimeOffset? ReturnedAt, bool IsOverdue)
{
    public static LoanResponse From(Loan loan) => new(
        loan.Id, loan.UserId, loan.BookId, loan.BorrowedAt, loan.DueDate, loan.ReturnedAt,
        loan.ReturnedAt is null && loan.DueDate < DateOnly.FromDateTime(DateTime.UtcNow));
}
