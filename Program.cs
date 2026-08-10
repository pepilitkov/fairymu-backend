using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using FairyMU.Api.Contracts;
using FairyMU.Api.Models;
using FairyMU.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<InMemoryAccountStore>();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<DemoGameDataService>();
builder.Services.AddSingleton<IPasswordHasher<AccountRecord>, PasswordHasher<AccountRecord>>();

var allowedOrigins = builder.Configuration
    .GetSection("FairyMU:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FairyMUFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("public", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("FairyMUFrontend");
app.UseRateLimiter();

static Dictionary<string, string[]> Validate(object request)
{
    var context = new ValidationContext(request);
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(request, context, results, validateAllProperties: true);

    return results
        .SelectMany(
            x => x.MemberNames.DefaultIfEmpty("request"),
            (result, member) => new { member, result.ErrorMessage })
        .GroupBy(x => x.member)
        .ToDictionary(
            g => g.Key,
            g => g.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());
}

var api = app.MapGroup("/api");

api.MapGet("/status", (IConfiguration config) => Results.Ok(new
{
    status = "Online",
    server = config["FairyMU:ServerName"] ?? "FairyMU",
    season = config["FairyMU:Season"] ?? "Season 6 Episode 3",
    apiVersion = "1.0.0",
    mode = "backend-v1-in-memory",
    utc = DateTimeOffset.UtcNow
})).RequireRateLimiting("public");

api.MapGet("/online", (IConfiguration config) => Results.Ok(new
{
    online = config.GetValue<int>("FairyMU:DemoOnlinePlayers"),
    record = 611,
    demo = true
})).RequireRateLimiting("public");

api.MapGet("/rankings", (DemoGameDataService game) =>
    Results.Ok(game.Rankings()))
    .RequireRateLimiting("public");

api.MapGet("/guilds", (DemoGameDataService game) =>
    Results.Ok(game.Guilds()))
    .RequireRateLimiting("public");

api.MapGet("/events", (DemoGameDataService game) =>
    Results.Ok(game.Events()))
    .RequireRateLimiting("public");

api.MapPost("/register", (
    RegisterRequest request,
    InMemoryAccountStore store,
    IPasswordHasher<AccountRecord> passwordHasher) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var normalizedUser = request.Username.Trim();
    var normalizedEmail = request.Email.Trim().ToLowerInvariant();

    var account = new AccountRecord
    {
        Username = normalizedUser,
        Email = normalizedEmail,
        PasswordHash = ""
    };

    account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

    if (!store.TryCreate(account))
        return Results.Conflict(new
        {
            error = "account_exists",
            message = "Username or email is already registered."
        });

    return Results.Created($"/api/account/{account.Id}", new
    {
        account.Id,
        account.Username,
        account.Email,
        account.CreatedAt
    });
}).RequireRateLimiting("auth");

api.MapPost("/login", (
    LoginRequest request,
    InMemoryAccountStore store,
    IPasswordHasher<AccountRecord> passwordHasher,
    SessionService sessions) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var account = store.FindByUsernameOrEmail(request.UsernameOrEmail.Trim());
    if (account is null)
        return Results.Unauthorized();

    var verification = passwordHasher.VerifyHashedPassword(
        account,
        account.PasswordHash,
        request.Password);

    if (verification == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    account.LastLoginAt = DateTimeOffset.UtcNow;
    var token = sessions.Create(account.Id);

    return Results.Ok(new
    {
        accessToken = token,
        tokenType = "Bearer",
        expiresInSeconds = 8 * 60 * 60,
        account = new
        {
            account.Id,
            account.Username,
            account.Email,
            account.WCoins,
            account.Credits
        }
    });
}).RequireRateLimiting("auth");

api.MapPost("/logout", (HttpRequest request, SessionService sessions) =>
{
    sessions.Revoke(request);
    return Results.NoContent();
}).RequireRateLimiting("auth");

api.MapGet("/account", (
    HttpRequest request,
    SessionService sessions,
    InMemoryAccountStore store) =>
{
    var accountId = sessions.Resolve(request);
    if (accountId is null)
        return Results.Unauthorized();

    var account = store.FindById(accountId.Value);
    if (account is null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        account.Id,
        account.Username,
        account.Email,
        account.WCoins,
        account.Credits,
        account.CreatedAt,
        account.LastLoginAt
    });
}).RequireRateLimiting("public");

api.MapGet("/characters", (
    HttpRequest request,
    SessionService sessions,
    DemoGameDataService game) =>
{
    var accountId = sessions.Resolve(request);
    if (accountId is null)
        return Results.Unauthorized();

    return Results.Ok(game.CharactersFor(accountId.Value));
}).RequireRateLimiting("public");

app.MapGet("/", () => Results.Ok(new
{
    project = "FairyMU API",
    version = "1.0.0",
    docs = "See README.md and openapi.yaml"
}));

app.Run();


public partial class Program { }
