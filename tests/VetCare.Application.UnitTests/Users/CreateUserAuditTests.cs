using System.Text.Json;
using FluentAssertions;
using VetCare.Application.Users.Commands.CreateUser;
using VetCare.Domain.Users;

namespace VetCare.Application.UnitTests.Users;

public sealed class CreateUserAuditTests
{
    [Fact]
    public void Serialized_payload_does_not_leak_password()
    {
        const string password = "Sup3rS3cret!Password";
        var command = new CreateUserCommand("user@vetcare.test", password, UserRole.Vet);

        var json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().NotContain(password);
        json.Should().NotContain("password", "the Password property must be excluded from audit serialization");
    }
}
