using System.Linq.Expressions;

namespace VetCare.Application.Abstractions.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    Expression<Func<T, object>>? OrderBy { get; }

    Expression<Func<T, object>>? OrderByDescending { get; }

    bool IsPaginated { get; }

    int Skip { get; }

    int Take { get; }
}
