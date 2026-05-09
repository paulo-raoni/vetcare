namespace VetCare.Infrastructure.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationTestsCollection : ICollectionFixture<VetCareWebApplicationFactory>
{
    public const string Name = "VetCare integration tests";
}
