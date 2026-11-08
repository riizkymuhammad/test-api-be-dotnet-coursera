using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Library.Api.Contracts;
using Library.Api.Data;
using Library.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Library.Api.Tests;

public sealed class LibraryApiTests : IDisposable
{
    private readonly TestApiFactory _factory = new();
    private readonly HttpClient _client;

    public LibraryApiTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task Users_support_full_crud_and_return_location()
    {
        var created = await _client.PostAsJsonAsync("/api/users", new { fullName = "Budi Santoso", email = "BUDI@example.com" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<User>())!;
        Assert.Equal("budi@example.com", user.Email);
        Assert.Equal($"/api/users/{user.Id}", created.Headers.Location!.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(created.Headers.Location)).StatusCode);
        Assert.Single((await _client.GetFromJsonAsync<User[]>("/api/users"))!);

        var updated = await _client.PutAsJsonAsync($"/api/users/{user.Id}",
            new { fullName = "Budi Updated", email = "updated@example.com" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Budi Updated", (await updated.Content.ReadFromJsonAsync<User>())!.FullName);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/users/{user.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/users/{user.Id}")).StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<User[]>("/api/users"))!);
    }

    [Theory]
    [InlineData("", "valid@example.com")]
    [InlineData("   ", "valid@example.com")]
    [InlineData("A", "valid@example.com")]
    [InlineData(" A ", "valid@example.com")]
    [InlineData("Valid Name", "invalid-email")]
    [InlineData("Valid Name", "")]
    public async Task Invalid_users_are_rejected_without_being_saved(string fullName, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { fullName, email });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.TryGetProperty("errors", out _));
        Assert.Empty((await _client.GetFromJsonAsync<User[]>("/api/users"))!);
    }

    [Fact]
    public async Task Duplicate_email_is_case_insensitive_for_create_and_update()
    {
        await CreateUser("first@example.com");
        var duplicate = await _client.PostAsJsonAsync("/api/users",
            new { fullName = "Another Member", email = "FIRST@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var second = await CreateUser("second@example.com");
        var update = await _client.PutAsJsonAsync($"/api/users/{second.Id}",
            new { fullName = "Another Member", email = "FIRST@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        Assert.Equal("second@example.com", (await _client.GetFromJsonAsync<User>($"/api/users/{second.Id}"))!.Email);
    }

    [Fact]
    public async Task Invalid_user_update_preserves_existing_data()
    {
        var user = await CreateUser();
        var update = await _client.PutAsJsonAsync($"/api/users/{user.Id}", new { fullName = "", email = "broken" });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        var stored = (await _client.GetFromJsonAsync<User>($"/api/users/{user.Id}"))!;
        Assert.Equal(user.FullName, stored.FullName);
        Assert.Equal(user.Email, stored.Email);
        Assert.Equal(user.JoinedAt, stored.JoinedAt);
    }

    [Fact]
    public async Task Missing_resources_return_404_for_reads_updates_and_deletes()
    {
        foreach (var resource in new[] { "users", "books", "loans" })
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/{resource}/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync("/api/users/999",
            new { fullName = "Valid Name", email = "valid@example.com" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync("/api/books/999", BookInput())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/users/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/books/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsync("/api/loans/999/return", null)).StatusCode);
    }

    [Fact]
    public async Task Books_support_full_crud()
    {
        var book = await CreateBook();
        Assert.Single((await _client.GetFromJsonAsync<BookResponse[]>("/api/books"))!);
        Assert.Equal(book, await _client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}"));
        var updated = await _client.PutAsJsonAsync($"/api/books/{book.Id}",
            new { title = "Updated Book", author = "Updated Author", isbn = "9780132350884", totalCopies = 3 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var result = (await updated.Content.ReadFromJsonAsync<BookResponse>())!;
        Assert.Equal("Updated Book", result.Title);
        Assert.Equal(3, result.AvailableCopies);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/books/{book.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/books/{book.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("", "Author", "9780132350884", 1)]
    [InlineData("Title", " ", "9780132350884", 1)]
    [InlineData("Title", "Author", "invalid-isbn", 1)]
    [InlineData("Title", "Author", "9780132350884", 0)]
    [InlineData("Title", "Author", "9780132350884", -1)]
    public async Task Invalid_books_are_rejected(string title, string author, string isbn, int totalCopies)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new { title, author, isbn, totalCopies });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<BookResponse[]>("/api/books"))!);
    }

    [Fact]
    public async Task Duplicate_isbn_is_rejected_for_create_and_update()
    {
        await CreateBook();
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/books", BookInput())).StatusCode);
        var second = await CreateBook(isbn: "9780201633610");
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PutAsJsonAsync($"/api/books/{second.Id}", BookInput())).StatusCode);
    }

    [Fact]
    public async Task Borrowing_and_returning_update_availability_and_preserve_history()
    {
        var user = await CreateUser();
        var book = await CreateBook();
        var created = await _client.PostAsJsonAsync("/api/loans", new { userId = user.Id, bookId = book.Id });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var loan = (await created.Content.ReadFromJsonAsync<LoanResponse>())!;
        Assert.Equal($"/api/loans/{loan.Id}", created.Headers.Location!.AbsolutePath);
        Assert.Equal(DateOnly.FromDateTime(loan.BorrowedAt.UtcDateTime).AddDays(14), loan.DueDate);
        Assert.False(loan.IsOverdue);
        Assert.Equal(0, (await _client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}"))!.AvailableCopies);

        var returned = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.NotNull((await returned.Content.ReadFromJsonAsync<LoanResponse>())!.ReturnedAt);
        Assert.Equal(1, (await _client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}"))!.AvailableCopies);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PutAsync($"/api/loans/{loan.Id}/return", null)).StatusCode);
        Assert.Single((await _client.GetFromJsonAsync<LoanResponse[]>("/api/loans"))!);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/loans",
            new { userId = user.Id, bookId = book.Id })).StatusCode);
    }

    [Fact]
    public async Task Borrowing_rejects_duplicate_active_loans_and_exhausted_stock()
    {
        var first = await CreateUser();
        var second = await CreateUser("second@example.com");
        var book = await CreateBook();
        await Borrow(first.Id, book.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/loans",
            new { userId = first.Id, bookId = book.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/loans",
            new { userId = second.Id, bookId = book.Id })).StatusCode);
    }

    [Theory]
    [InlineData(0, 1, 14)]
    [InlineData(1, 0, 14)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 31)]
    public async Task Invalid_loan_requests_return_400(int userId, int bookId, int durationDays)
    {
        var response = await _client.PostAsJsonAsync("/api/loans", new { userId, bookId, durationDays });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Loans_require_existing_user_and_book()
    {
        var user = await CreateUser();
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/loans",
            new { userId = 999, bookId = 999 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/loans",
            new { userId = user.Id, bookId = 999 })).StatusCode);
    }

    [Fact]
    public async Task Loan_history_prevents_deleting_users_and_books_even_after_return()
    {
        var user = await CreateUser();
        var book = await CreateBook();
        var loan = await Borrow(user.Id, book.Id);
        foreach (var beforeReturn in new[] { true, false })
        {
            if (!beforeReturn)
                Assert.Equal(HttpStatusCode.OK, (await _client.PutAsync($"/api/loans/{loan.Id}/return", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/users/{user.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/books/{book.Id}")).StatusCode);
        }
    }

    [Fact]
    public async Task Stock_cannot_be_reduced_below_active_loans()
    {
        var first = await CreateUser();
        var second = await CreateUser("second@example.com");
        var book = await CreateBook(copies: 2);
        await Borrow(first.Id, book.Id);
        await Borrow(second.Id, book.Id);
        var response = await _client.PutAsJsonAsync($"/api/books/{book.Id}", BookInput(copies: 1));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, (await _client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}"))!.TotalCopies);
    }

    [Fact]
    public async Task Concurrent_borrowing_never_overbooks_the_last_copy()
    {
        var first = await CreateUser();
        var second = await CreateUser("second@example.com");
        var book = await CreateBook();
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/loans", new { userId = first.Id, bookId = book.Id }),
            _client.PostAsJsonAsync("/api/loans", new { userId = second.Id, bookId = book.Id }));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(0, (await _client.GetFromJsonAsync<BookResponse>($"/api/books/{book.Id}"))!.AvailableCopies);
        Assert.Single((await _client.GetFromJsonAsync<LoanResponse[]>("/api/loans"))!);
    }

    [Fact]
    public async Task Overdue_status_only_applies_to_active_loans()
    {
        var user = await CreateUser();
        var book = await CreateBook();
        var loan = await Borrow(user.Id, book.Id);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            var stored = (await db.Loans.FindAsync(loan.Id))!;
            stored.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            await db.SaveChangesAsync();
        }
        Assert.True((await _client.GetFromJsonAsync<LoanResponse>($"/api/loans/{loan.Id}"))!.IsOverdue);
        var returned = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        Assert.False((await returned.Content.ReadFromJsonAsync<LoanResponse>())!.IsOverdue);
    }

    [Fact]
    public async Task Malformed_json_returns_validation_problem_and_request_id()
    {
        var response = await _client.PostAsync("/api/users", new StringContent("{broken", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Request-ID"));
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/missing-route", HttpStatusCode.NotFound)]
    [InlineData("/api/users/999", HttpStatusCode.NotFound)]
    public async Task Middleware_adds_request_ids_to_success_and_error_responses(string path, HttpStatusCode status)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(status, response.StatusCode);
        var requestId = Assert.Single(response.Headers.GetValues("X-Request-ID"));
        Assert.False(string.IsNullOrWhiteSpace(requestId));
        if (status == HttpStatusCode.NotFound)
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(requestId, problem.GetProperty("traceId").GetString());
        }
    }

    [Fact]
    public async Task Unexpected_database_errors_are_safe_problem_responses()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE Users");
        }

        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Sqlite", body);
        Assert.DoesNotContain("no such table", body);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("title").GetString());
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Request-ID")),
            problem.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Sqlite_data_survives_a_new_database_context()
    {
        var user = await CreateUser();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var stored = await db.Users.AsNoTracking().SingleAsync(member => member.Id == user.Id);
        Assert.Equal(user.Email, stored.Email);
        Assert.Equal(user.FullName, stored.FullName);
    }

    [Fact]
    public async Task Openapi_document_describes_users_books_and_loans()
    {
        var document = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = document.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/users", out _));
        Assert.True(paths.TryGetProperty("/api/books", out _));
        Assert.True(paths.TryGetProperty("/api/loans", out _));
    }

    private async Task<User> CreateUser(string email = "member@example.com")
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { fullName = "Library Member", email });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<User>())!;
    }

    private static object BookInput(int copies = 1, string isbn = "9780132350884") =>
        new { title = "Clean Code", author = "Robert C. Martin", isbn, totalCopies = copies };

    private async Task<BookResponse> CreateBook(int copies = 1, string isbn = "9780132350884")
    {
        var response = await _client.PostAsJsonAsync("/api/books", BookInput(copies, isbn));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BookResponse>())!;
    }

    private async Task<LoanResponse> Borrow(int userId, int bookId)
    {
        var response = await _client.PostAsJsonAsync("/api/loans", new { userId, bookId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>())!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
