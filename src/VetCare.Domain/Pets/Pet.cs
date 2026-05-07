using VetCare.Domain.Primitives;

namespace VetCare.Domain.Pets;

public sealed class Pet : AggregateRoot, ITenantEntity
{
    private Pet()
    {
        Name = string.Empty;
        Breed = string.Empty;
    }

    public Pet(
        Guid tenantId,
        Guid ownerId,
        string name,
        Species species,
        string breed,
        DateOnly dateOfBirth,
        string? photoUrl = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("OwnerId must not be empty.", nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pet name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(breed))
        {
            throw new ArgumentException("Pet breed must not be empty.", nameof(breed));
        }

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Pet date of birth must not be in the future.", nameof(dateOfBirth));
        }

        TenantId = tenantId;
        OwnerId = ownerId;
        Name = name.Trim();
        Species = species;
        Breed = breed.Trim();
        DateOfBirth = dateOfBirth;
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();

        AddDomainEvent(new PetRegisteredEvent(Id, TenantId, OwnerId, Name, Species));
    }

    public Guid TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; }

    public Species Species { get; private set; }

    public string Breed { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public string? PhotoUrl { get; private set; }

    public void UpdatePhoto(string? photoUrl)
    {
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        Touch();
    }

    public void UpdateProfile(string name, Species species, string breed, DateOnly dateOfBirth, string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pet name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(breed))
        {
            throw new ArgumentException("Pet breed must not be empty.", nameof(breed));
        }

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Pet date of birth must not be in the future.", nameof(dateOfBirth));
        }

        Name = name.Trim();
        Species = species;
        Breed = breed.Trim();
        DateOfBirth = dateOfBirth;
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        Touch();
    }
}
