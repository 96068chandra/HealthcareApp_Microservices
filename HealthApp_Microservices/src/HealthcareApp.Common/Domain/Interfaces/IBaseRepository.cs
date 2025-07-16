using HealthcareApp.Common.Domain.Common;

namespace HealthcareApp.Common.Domain.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> GetByIdAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<IReadOnlyList<T>> GetAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
}
