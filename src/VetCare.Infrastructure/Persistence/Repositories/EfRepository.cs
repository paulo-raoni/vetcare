using Microsoft.EntityFrameworkCore;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;

namespace VetCare.Infrastructure.Persistence.Repositories;

public abstract class EfRepository<T> : IRepository<T>
    where T : class
{
    protected EfRepository(VetCareDbContext db)
    {
        Db = db;
    }

    protected VetCareDbContext Db { get; }

    protected DbSet<T> Set => Db.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Set.FindAsync(new object[] { id }, cancellationToken).AsTask();

    public Task<T?> SingleOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => SpecificationEvaluator<T>.ApplySpecification(Set, specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => await SpecificationEvaluator<T>.ApplySpecification(Set, specification).ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = Set;
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return query.CountAsync(cancellationToken);
    }

    public void Add(T entity) => Set.Add(entity);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
