using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using Security.Domain.Entities;

namespace Security.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<FormDefinition>()
            .HasIndex(f => f.MenuKey)
            .IsUnique();
        builder.Entity<FormSubmission>()
            .HasIndex(fs => new { fs.MenuKey, fs.RecordKey });
    }
}
