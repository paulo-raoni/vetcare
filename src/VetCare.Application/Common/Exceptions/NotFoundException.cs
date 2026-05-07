namespace VetCare.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string resource, Guid id)
        : base($"{resource} with id '{id}' was not found.")
    {
        Resource = resource;
        ResourceId = id;
    }

    public string Resource { get; }

    public Guid ResourceId { get; }
}
