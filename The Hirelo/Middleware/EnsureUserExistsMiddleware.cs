using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.Enums;
using The_Hirelo.Models;

namespace The_Hirelo.Middleware;

public class EnsureUserExistsMiddleware
{
    private readonly RequestDelegate _next;

    public EnsureUserExistsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst("email")?.Value;

            if (!string.IsNullOrWhiteSpace(sub))
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<HireloDbContext>();

                var existing = await db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.CognitoSub == sub);

                if (existing == null)
                {
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        CognitoSub = sub,
                        Email = email ?? $"{sub}@cognito.local",
                        Role = UserRole.Candidate,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }
                else if (!string.IsNullOrWhiteSpace(email) && existing.Email != email)
                {
                    var tracked = await db.Users.FirstOrDefaultAsync(u => u.Id == existing.Id);
                    if (tracked != null)
                    {
                        tracked.Email = email;
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        await _next(context);
    }
}
