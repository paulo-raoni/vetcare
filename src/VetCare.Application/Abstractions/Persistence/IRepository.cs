using VetCare.Application.Abstractions.Specifications;

namespace VetCare.Application.Abstractions.Persistence;

public interface IRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsyncNoTracking(Guid id, CancellationToken cancellationToken = default);

    Task<T?> SingleOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    void Add(T entity);

    void Update(T entity);

    void Remove(T entity);
}
