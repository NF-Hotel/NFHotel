using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Default")!));
builder.Services.AddHttpClient();
builder.Services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        };
    });
    
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapPost("/auth/register", async (RegisterRequest req, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        return Results.BadRequest("First and last name are required");

    await using var conn = await db.OpenConnectionAsync();
    var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
    try
    {
        await conn.ExecuteAsync(
            "INSERT INTO auth.users (email, password_hash, first_name, last_name) VALUES (@Email, @Hash, @FirstName, @LastName)",
            new { req.Email, Hash = hash, FirstName = req.FirstName.Trim(), LastName = req.LastName.Trim() });
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.Conflict("Email already registered");
    }
    return Results.Ok();
}).RequireAuthorization(p => p.RequireRole("admin"));

app.MapPost("/auth/login", async (LoginRequest req, NpgsqlDataSource db, IConfiguration config) =>
{
    await using var conn = await db.OpenConnectionAsync();
    var user = await conn.QuerySingleOrDefaultAsync<UserRow>(
        "SELECT id AS Id, password_hash AS PasswordHash, role AS Role, first_name AS FirstName, last_name AS LastName FROM auth.users WHERE email = @Email",
        new { req.Email });

    if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.Unauthorized();

    return Results.Ok(new { accessToken = CreateToken(config, user.Id, req.Email, user.Role, user.FirstName, user.LastName) });
});

// users without a name set it once after login; renaming after that is admin-only
app.MapPut("/me/name", async (UpdateNameRequest req, HttpContext http, NpgsqlDataSource db, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        return Results.BadRequest("First and last name are required");

    var currentId = long.Parse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var firstName = req.FirstName.Trim();
    var lastName = req.LastName.Trim();
    await using var conn = await db.OpenConnectionAsync();
    var user = await conn.QuerySingleOrDefaultAsync<SelfRow>(
        """
        UPDATE auth.users SET first_name = @FirstName, last_name = @LastName
        WHERE id = @CurrentId AND (first_name = '' OR last_name = '')
        RETURNING email AS Email, role AS Role
        """,
        new { FirstName = firstName, LastName = lastName, CurrentId = currentId });
    if (user is null)
        return Results.Conflict("Name already set");

    // fresh token so the new name shows without logging in again
    return Results.Ok(new { accessToken = CreateToken(config, currentId, user.Email, user.Role, firstName, lastName) });
}).RequireAuthorization();

var admin = app.MapGroup("/admin/users").RequireAuthorization(p => p.RequireRole("admin"));

admin.MapGet("/", async (HttpContext http, NpgsqlDataSource db) =>
{
    var currentId = long.Parse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    await using var conn = await db.OpenConnectionAsync();
    var users = await conn.QueryAsync<AdminUserRow>(
        "SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email, role AS Role FROM auth.users WHERE id <> @CurrentId ORDER BY first_name, last_name",
        new { CurrentId = currentId });
    return Results.Ok(users);
});

admin.MapPut("/{id:long}/email", async (long id, UpdateEmailRequest req, NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    try
    {
        var rows = await conn.ExecuteAsync(
            "UPDATE auth.users SET email = @Email WHERE id = @Id",
            new { req.Email, Id = id });
        return rows == 0 ? Results.NotFound() : Results.Ok();
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.Conflict("Email already registered");
    }
});

admin.MapPut("/{id:long}/name", async (long id, UpdateNameRequest req, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        return Results.BadRequest("First and last name are required");

    await using var conn = await db.OpenConnectionAsync();
    var rows = await conn.ExecuteAsync(
        "UPDATE auth.users SET first_name = @FirstName, last_name = @LastName WHERE id = @Id",
        new { FirstName = req.FirstName.Trim(), LastName = req.LastName.Trim(), Id = id });
    return rows == 0 ? Results.NotFound() : Results.Ok();
});

admin.MapPut("/{id:long}/password", async (long id, UpdatePasswordRequest req, NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var rows = await conn.ExecuteAsync(
        "UPDATE auth.users SET password_hash = @Hash WHERE id = @Id",
        new { Hash = hash, Id = id });
    return rows == 0 ? Results.NotFound() : Results.Ok();
});

admin.MapPut("/{id:long}/role", async (long id, UpdateRoleRequest req, NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    try
    {
        var rows = await conn.ExecuteAsync(
            "UPDATE auth.users SET role = @Role WHERE id = @Id",
            new { req.Role, Id = id });
        return rows == 0 ? Results.NotFound() : Results.Ok();
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
    {
        return Results.BadRequest("Invalid role");
    }
});

admin.MapDelete("/{id:long}", async (long id, HttpContext http, NpgsqlDataSource db) =>
{
    var currentId = long.Parse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    if (id == currentId)
        return Results.BadRequest("Cannot delete your own account");

    await using var conn = await db.OpenConnectionAsync();
    var rows = await conn.ExecuteAsync("DELETE FROM auth.users WHERE id = @Id", new { Id = id });
    return rows == 0 ? Results.NotFound() : Results.Ok();
});

var notes = app.MapGroup("/rooms/{room:int}/notes").RequireAuthorization();

notes.MapGet("/", async (int room, NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    var rows = await conn.QueryAsync<NoteRow>(
        """
        SELECT id AS Id, body AS Body, created_by AS CreatedBy, created_at AS CreatedAt,
               resolved_by AS ResolvedBy, resolved_at AS ResolvedAt
        FROM room_notes WHERE room_number = @Room ORDER BY created_at DESC
        """,
        new { Room = room });
    return Results.Ok(rows);
});

// emails are copied instead of referenced so a note keeps its author/resolver after the account is deleted
notes.MapPost("/", async (int room, CreateNoteRequest req, HttpContext http, NpgsqlDataSource db) =>
{
    if (string.IsNullOrWhiteSpace(req.Body))
        return Results.BadRequest("Note is empty");

    var currentId = long.Parse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    await using var conn = await db.OpenConnectionAsync();
    var rows = await conn.ExecuteAsync(
        "INSERT INTO room_notes (room_number, body, created_by) SELECT @Room, @Body, email FROM auth.users WHERE id = @CurrentId",
        new { Room = room, Body = req.Body.Trim(), CurrentId = currentId });
    // 0 rows = account deleted while its token is still valid
    return rows == 0 ? Results.Unauthorized() : Results.Ok();
});

notes.MapPut("/{id:long}/resolved", async (int room, long id, ResolveNoteRequest req, HttpContext http, NpgsqlDataSource db) =>
{
    var currentId = long.Parse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    await using var conn = await db.OpenConnectionAsync();
    // ticking twice keeps the first fingerprint; only the ticker may untick within 10 minutes, admins anytime
    var rows = await conn.ExecuteAsync(
        """
        UPDATE room_notes SET
            resolved_by = CASE WHEN @Resolved THEN COALESCE(resolved_by, (SELECT email FROM auth.users WHERE id = @CurrentId)) END,
            resolved_at = CASE WHEN @Resolved THEN COALESCE(resolved_at, now()) END
        WHERE id = @Id AND room_number = @Room
          AND EXISTS (SELECT 1 FROM auth.users WHERE id = @CurrentId)
          AND (@Resolved OR @IsAdmin OR (
              resolved_at > now() - interval '10 minutes'
              AND resolved_by = (SELECT email FROM auth.users WHERE id = @CurrentId)))
        """,
        new { req.Resolved, IsAdmin = http.User.IsInRole("admin"), CurrentId = currentId, Id = id, Room = room });
    return rows == 0 ? Results.StatusCode(StatusCodes.Status403Forbidden) : Results.Ok();
});

// the author may delete within 10 minutes of posting, admins anytime
notes.MapDelete("/{id:long}", async (int room, long id, HttpContext http, NpgsqlDataSource db) =>
{
    var currentId = long.Parse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    await using var conn = await db.OpenConnectionAsync();
    var rows = await conn.ExecuteAsync(
        """
        DELETE FROM room_notes
        WHERE id = @Id AND room_number = @Room
          AND EXISTS (SELECT 1 FROM auth.users WHERE id = @CurrentId)
          AND (@IsAdmin OR (
              created_at > now() - interval '10 minutes'
              AND created_by = (SELECT email FROM auth.users WHERE id = @CurrentId)))
        """,
        new { IsAdmin = http.User.IsInRole("admin"), Id = id, Room = room, CurrentId = currentId });
    return rows == 0 ? Results.StatusCode(StatusCodes.Status403Forbidden) : Results.Ok();
});

app.Run();

static string CreateToken(IConfiguration config, long id, string email, string role, string firstName, string lastName)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var token = new JwtSecurityToken(
    issuer: config["Jwt:Issuer"],
    audience: config["Jwt:Audience"],
    claims: [
        new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, email),
        new Claim(JwtRegisteredClaimNames.GivenName, firstName),
        new Claim(JwtRegisteredClaimNames.FamilyName, lastName),
        new Claim(ClaimTypes.Role, role)
    ],
    expires: DateTime.UtcNow.AddHours(8),
    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
}

record LoginRequest(string Email, string Password);
record RegisterRequest(string Email, string Password, string FirstName, string LastName);
record UserRow(long Id, string PasswordHash, string Role, string FirstName, string LastName);
record AdminUserRow(long Id, string FirstName, string LastName, string Email, string Role);
record UpdateEmailRequest(string Email);
record UpdateNameRequest(string FirstName, string LastName);
record SelfRow(string Email, string Role);
record UpdatePasswordRequest(string Password);
record UpdateRoleRequest(string Role);
record NoteRow(long Id, string Body, string CreatedBy, DateTime CreatedAt, string? ResolvedBy, DateTime? ResolvedAt);
record CreateNoteRequest(string Body);
record ResolveNoteRequest(bool Resolved);