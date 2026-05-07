using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Specifications;

public sealed class OwnerListSpec : Specification<Owner>
{
    public OwnerListSpec(int page, int pageSize, bool applyPaging = true)
    {
        ApplyOrderBy(o => o.FullName);
        if (applyPaging)
        {
            ApplyPaging((page - 1) * pageSize, pageSize);
        }
    }
}
