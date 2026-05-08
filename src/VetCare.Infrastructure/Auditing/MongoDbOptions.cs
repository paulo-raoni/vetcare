namespace VetCare.Infrastructure.Auditing;

public sealed class MongoDbOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    public string DatabaseName { get; set; } = "vetcare";

    public string AuditCollectionName { get; set; } = "audit_log";
}
