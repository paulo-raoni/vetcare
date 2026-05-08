using VetCare.Domain.Primitives;

namespace VetCare.Domain.Vaccinations;

public sealed class Vaccination : AggregateRoot, ITenantEntity
{
    public const int VaccineNameMaxLength = 120;
    public const int BatchNumberMaxLength = 64;

    private Vaccination()
    {
        VaccineName = string.Empty;
        BatchNumber = string.Empty;
    }

    public Vaccination(
        Guid tenantId,
        Guid petId,
        string vaccineName,
        DateTime administeredAt,
        DateTime? nextDueAt,
        string batchNumber)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));
        }

        if (petId == Guid.Empty)
        {
            throw new ArgumentException("PetId must not be empty.", nameof(petId));
        }

        if (string.IsNullOrWhiteSpace(vaccineName))
        {
            throw new ArgumentException("Vaccine name must not be empty.", nameof(vaccineName));
        }

        if (administeredAt > DateTime.UtcNow)
        {
            throw new ArgumentException("AdministeredAt must not be in the future.", nameof(administeredAt));
        }

        if (nextDueAt is not null && nextDueAt.Value < administeredAt)
        {
            throw new ArgumentException("NextDueAt must be on or after AdministeredAt.", nameof(nextDueAt));
        }

        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            throw new ArgumentException("Batch number must not be empty.", nameof(batchNumber));
        }

        TenantId = tenantId;
        PetId = petId;
        VaccineName = vaccineName.Trim();
        AdministeredAt = administeredAt;
        NextDueAt = nextDueAt;
        BatchNumber = batchNumber.Trim();

        AddDomainEvent(new VaccinationRecordedEvent(Id, TenantId, PetId, VaccineName, AdministeredAt, NextDueAt));
    }

    public Guid TenantId { get; private set; }

    public Guid PetId { get; private set; }

    public string VaccineName { get; private set; }

    public DateTime AdministeredAt { get; private set; }

    public DateTime? NextDueAt { get; private set; }

    public string BatchNumber { get; private set; }

    public void Update(string vaccineName, DateTime administeredAt, DateTime? nextDueAt, string batchNumber)
    {
        if (string.IsNullOrWhiteSpace(vaccineName))
        {
            throw new ArgumentException("Vaccine name must not be empty.", nameof(vaccineName));
        }

        if (administeredAt > DateTime.UtcNow)
        {
            throw new ArgumentException("AdministeredAt must not be in the future.", nameof(administeredAt));
        }

        if (nextDueAt is not null && nextDueAt.Value < administeredAt)
        {
            throw new ArgumentException("NextDueAt must be on or after AdministeredAt.", nameof(nextDueAt));
        }

        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            throw new ArgumentException("Batch number must not be empty.", nameof(batchNumber));
        }

        VaccineName = vaccineName.Trim();
        AdministeredAt = administeredAt;
        NextDueAt = nextDueAt;
        BatchNumber = batchNumber.Trim();
        Touch();
    }
}
