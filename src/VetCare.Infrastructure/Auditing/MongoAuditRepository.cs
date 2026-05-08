using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using VetCare.Application.Abstractions.Auditing;
using VetCare.Application.Auditing;

namespace VetCare.Infrastructure.Auditing;

public sealed class MongoAuditRepository : IAuditRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoAuditRepository(IMongoDatabase database, IOptions<MongoDbOptions> options)
    {
        _collection = database.GetCollection<BsonDocument>(options.Value.AuditCollectionName);
    }

    public async Task SaveAsync(AuditEntry entry, CancellationToken ct)
    {
        var doc = new BsonDocument
        {
            ["_id"] = entry.Id.ToString(),
            ["tenantId"] = ToBsonValue(entry.TenantId),
            ["userId"] = ToBsonValue(entry.UserId),
            ["action"] = entry.Action,
            ["entityType"] = entry.EntityType is null ? BsonNull.Value : (BsonValue)entry.EntityType,
            ["entityId"] = ToBsonValue(entry.EntityId),
            ["payload"] = SerializePayload(entry.Payload),
            ["occurredAt"] = entry.OccurredAt,
        };

        await _collection.InsertOneAsync(doc, options: null, cancellationToken: ct);
    }

    private static BsonValue ToBsonValue(Guid? value) =>
        value.HasValue ? value.Value.ToString() : BsonNull.Value;

    private static BsonValue SerializePayload(object? payload)
    {
        if (payload is null)
        {
            return BsonNull.Value;
        }

        var json = JsonSerializer.Serialize(payload, payload.GetType(), SerializerOptions);
        return BsonDocument.Parse(json);
    }
}
