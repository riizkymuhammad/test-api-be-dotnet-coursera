using Library.Api.Contracts;
using Library.Api.Data;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll(CancellationToken ct) =>
        Ok(await db.Users.AsNoTracking().OrderBy(user => user.Id).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetById(int id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, ct);
        return user is null ? Problem(statusCode: 404, title: "User not found.") : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create(UserRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, ct))
            return Problem(statusCode: 409, title: "Email is already registered.");

        var user = new User { FullName = request.FullName.Trim(), Email = email };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<User>> Update(int id, UserRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Problem(statusCode: 404, title: "User not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(other => other.Id != id && other.Email == email, ct))
            return Problem(statusCode: 409, title: "Email is already registered.");

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await db.SaveChangesAsync(ct);
        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return Problem(statusCode: 404, title: "User not found.");
        if (await db.Loans.AnyAsync(loan => loan.UserId == id, ct))
            return Problem(statusCode: 409, title: "Users with loan history cannot be deleted.");

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
