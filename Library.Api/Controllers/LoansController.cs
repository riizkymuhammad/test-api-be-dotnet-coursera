using Library.Api.Contracts;
using Library.Api.Data;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/loans")]
public sealed class LoansController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanResponse>>> GetAll(CancellationToken ct)
    {
        var loans = await db.Loans.AsNoTracking().OrderBy(loan => loan.Id).ToListAsync(ct);
        return Ok(loans.Select(LoanResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoanResponse>> GetById(int id, CancellationToken ct)
    {
        var loan = await db.Loans.AsNoTracking().SingleOrDefaultAsync(loan => loan.Id == id, ct);
        return loan is null ? Problem(statusCode: 404, title: "Loan not found.") : Ok(LoanResponse.From(loan));
    }

    [HttpPost]
    public async Task<ActionResult<LoanResponse>> Create(LoanRequest request, CancellationToken ct)
    {
        // Keep the stock check and insert atomic, even for simultaneous requests.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.Users.AnyAsync(user => user.Id == request.UserId, ct))
            return Problem(statusCode: 404, title: "User not found.");

        var book = await db.Books.FindAsync([request.BookId], ct);
        if (book is null)
            return Problem(statusCode: 404, title: "Book not found.");
        if (await db.Loans.AnyAsync(loan => loan.UserId == request.UserId &&
                loan.BookId == request.BookId && loan.ReturnedAt == null, ct))
            return Problem(statusCode: 409, title: "User already has an active loan for this book.");

        var activeLoans = await db.Loans.CountAsync(loan => loan.BookId == book.Id && loan.ReturnedAt == null, ct);
        if (activeLoans >= book.TotalCopies)
            return Problem(statusCode: 409, title: "No copies are available.");

        var now = DateTimeOffset.UtcNow;
        var loan = new Loan
        {
            UserId = request.UserId, BookId = request.BookId, BorrowedAt = now,
            DueDate = DateOnly.FromDateTime(now.UtcDateTime).AddDays(request.DurationDays)
        };
        db.Loans.Add(loan);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, LoanResponse.From(loan));
    }

    [HttpPut("{id:int}/return")]
    public async Task<ActionResult<LoanResponse>> Return(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var loan = await db.Loans.FindAsync([id], ct);
        if (loan is null)
            return Problem(statusCode: 404, title: "Loan not found.");
        if (loan.ReturnedAt is not null)
            return Problem(statusCode: 409, title: "This loan has already been returned.");

        loan.ReturnedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(LoanResponse.From(loan));
    }
}
