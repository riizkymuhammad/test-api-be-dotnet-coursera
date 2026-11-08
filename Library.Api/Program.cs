using Library.Api.Data;
using Library.Api.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Library") ?? "Data Source=library.db;Foreign Keys=True"));

var app = builder.Build();

// Log the final status, including errors handled by the exception middleware.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    name = "Library Management API",
    documentation = "/openapi/v1.json",
    resources = new[] { "/api/users", "/api/books", "/api/loans" }
}));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// A self-contained course project: create the SQLite schema on first launch.
// For subsequent schema changes in a deployed app, use EF Core migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

// Expose the entry point to the integration test host.
public partial class Program;
