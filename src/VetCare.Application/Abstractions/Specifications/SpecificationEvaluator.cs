using Microsoft.EntityFrameworkCore;

namespace VetCare.Application.Abstractions.Specifications;

public static class SpecificationEvaluator<T>
    where T : class
{
    public static IQueryable<T> ApplySpecification(IQueryable<T> source, ISpecification<T> spec)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(spec);

        var query = source;

        if (spec.Criteria is not null)
        {
            query = query.Where(spec.Criteria);
        }

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy is not null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending is not null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        if (spec.IsPaginated)
        {
            query = query.Skip(spec.Skip).Take(spec.Take);
        }

        return query;
    }
}
