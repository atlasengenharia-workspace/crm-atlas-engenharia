using CrmAtlas.ApplicationCore;

namespace CrmAtlas.UnitTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ApplicationCore_DoesNotReferenceOuterLayers()
    {
        var referencedAssemblies = AssemblyReference.Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("CrmAtlas.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("CrmAtlas.Web", referencedAssemblies);
    }
}
