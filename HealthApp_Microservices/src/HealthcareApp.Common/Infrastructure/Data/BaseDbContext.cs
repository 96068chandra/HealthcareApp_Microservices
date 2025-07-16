using HealthcareApp.Common.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthcareApp.Common.Infrastructure.Data;

public abstract class BaseDbContext : DbContext
{
    public BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = "system"; // This should be replaced with actual user
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedDate = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = "system"; // This should be replaced with actual user
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Configure soft delete filter
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                entityType.GetProperty("IsDeleted")?.SetDefaultValue(false);
                
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "p");
                var deletedCheck = System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, "IsDeleted"),
                    System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(deletedCheck, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
