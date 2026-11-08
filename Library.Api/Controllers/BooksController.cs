using Library.Api.Contracts;
using Library.Api.Data;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(LibraryDbContext db) : ControllerBase
{
    private IQueryable<BookResponse> Responses(IQueryable<Book> books) => books.AsNoTracking()
        .Select(book => new BookResponse(book.Id, book.Title, book.Author, book.Isbn, book.TotalCopies,
            book.TotalCopies - db.Loans.Count(loan => loan.BookId == book.Id && loan.ReturnedAt == null)));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookResponse>>> GetAll(CancellationToken ct) =>
        Ok(await Responses(db.Books.OrderBy(book => book.Id)).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookResponse>> GetById(int id, CancellationToken ct)
    {
        var book = await Responses(db.Books.Where(book => book.Id == id)).SingleOrDefaultAsync(ct);
        return book is null ? Problem(statusCode: 404, title: "Book not found.") : Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<BookResponse>> Create(BookRequest request, CancellationToken ct)
    {
        if (await db.Books.AnyAsync(book => book.Isbn == request.Isbn, ct))
            return Problem(statusCode: 409, title: "ISBN is already registered.");

        var book = new Book
        {
            Title = request.Title.Trim(), Author = request.Author.Trim(),
            Isbn = request.Isbn, TotalCopies = request.TotalCopies
        };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn, book.TotalCopies, book.TotalCopies));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BookResponse>> Update(int id, BookRequest request, CancellationToken ct)
    {
        // SQLite write transactions serialize stock changes and borrowing.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var book = await db.Books.FindAsync([id], ct);
        if (book is null)
            return Problem(statusCode: 404, title: "Book not found.");
        if (await db.Books.AnyAsync(other => other.Id != id && other.Isbn == request.Isbn, ct))
            return Problem(statusCode: 409, title: "ISBN is already registered.");

        var activeLoans = await db.Loans.CountAsync(loan => loan.BookId == id && loan.ReturnedAt == null, ct);
        if (request.TotalCopies < activeLoans)
            return Problem(statusCode: 409, title: "Total copies cannot be fewer than active loans.");

        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Isbn = request.Isbn;
        book.TotalCopies = request.TotalCopies;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new BookResponse(book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.TotalCopies - activeLoans));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var book = await db.Books.FindAsync([id], ct);
        if (book is null)
            return Problem(statusCode: 404, title: "Book not found.");
        if (await db.Loans.AnyAsync(loan => loan.BookId == id, ct))
            return Problem(statusCode: 409, title: "Books with loan history cannot be deleted.");

        db.Books.Remove(book);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
