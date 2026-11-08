using Library.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Book>().HasIndex(book => book.Isbn).IsUnique();
        modelBuilder.Entity<Book>().ToTable(table =>
            table.HasCheckConstraint("CK_Book_TotalCopies", "TotalCopies >= 1 AND TotalCopies <= 10000"));
        modelBuilder.Entity<Loan>().HasOne<User>().WithMany()
            .HasForeignKey(loan => loan.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Loan>().HasOne<Book>().WithMany()
            .HasForeignKey(loan => loan.BookId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Loan>().HasIndex(loan => new { loan.BookId, loan.ReturnedAt });
        // A member can borrow multiple books, but only one active copy of the same book.
        modelBuilder.Entity<Loan>().HasIndex(loan => new { loan.UserId, loan.BookId })
            .IsUnique().HasFilter("ReturnedAt IS NULL");
    }
}
