using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ksse.Users;

internal sealed class UsersDbContext : IdentityDbContext
{
    public const string Id = "users";

    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Id);
        base.OnModelCreating(modelBuilder);
    }
}
