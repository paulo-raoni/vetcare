using FluentAssertions;
using VetCare.Domain;

namespace VetCare.Domain.UnitTests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void Marker_assembly_is_reachable()
    {
        typeof(AssemblyMarker).Assembly.GetName().Name.Should().Be("VetCare.Domain");
    }
}
