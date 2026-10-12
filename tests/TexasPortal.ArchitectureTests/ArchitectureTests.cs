using Xunit;
using System.Reflection;
using System.Linq;

namespace TexasPortal.ArchitectureTests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNot_Reference_Other_Layers()
    {
        var assembly = typeof(TexasPortal.Domain.AssemblyMarker).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

        Assert.DoesNotContain("TexasPortal.Application", referencedAssemblies);
        Assert.DoesNotContain("TexasPortal.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("TexasPortal.Api", referencedAssemblies);
    }

    [Fact]
    public void Application_ShouldNot_Reference_Infrastructure_Or_Api()
    {
        var assembly = typeof(TexasPortal.Application.AssemblyMarker).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

        Assert.DoesNotContain("TexasPortal.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("TexasPortal.Api", referencedAssemblies);
    }

    [Fact]
    public void Contracts_ShouldNot_Reference_Other_Layers()
    {
        var assembly = typeof(TexasPortal.Contracts.AssemblyMarker).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

        Assert.DoesNotContain("TexasPortal.Domain", referencedAssemblies);
        Assert.DoesNotContain("TexasPortal.Application", referencedAssemblies);
        Assert.DoesNotContain("TexasPortal.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("TexasPortal.Api", referencedAssemblies);
    }

    [Fact]
    public void ApiControllers_ShouldNot_Reference_Infrastructure_Types_Or_DbContext()
    {
        var apiAssembly = typeof(TexasPortal.Api.AssemblyMarker).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.BaseType != null && t.Name.EndsWith("Controller"))
            .ToList();

        foreach (var controller in controllerTypes)
        {
            var constructors = controller.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                foreach (var param in parameters)
                {
                    var paramTypeName = param.ParameterType.FullName ?? "";
                    
                    // Controllers cannot inject Infrastructure types
                    Assert.False(paramTypeName.Contains("TexasPortal.Infrastructure"),
                        $"Controller {controller.Name} injects an Infrastructure type: {paramTypeName}");

                    // Controllers cannot inject EF DbContext or entities
                    Assert.False(paramTypeName.Contains("DbContext") || paramTypeName.Contains("SalesPortalDbContext"),
                        $"Controller {controller.Name} injects a DbContext: {paramTypeName}");
                }
            }

            // Also check fields and properties
            var members = controller.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var member in members)
            {
                var memberType = member.MemberType;
                var declaredType = member.DeclaringType?.FullName ?? "";

                Assert.False(declaredType.Contains("Microsoft.EntityFrameworkCore"),
                    $"Controller {controller.Name} references EF Core type directly.");
            }
        }
    }

    [Fact]
    public void Contracts_ShouldNot_Expose_Domain_Entities()
    {
        var contractsAssembly = typeof(TexasPortal.Contracts.AssemblyMarker).Assembly;
        var contractTypes = contractsAssembly.GetTypes().ToList();

        foreach (var type in contractTypes)
        {
            // Verify properties do not return domain entities
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                var propTypeName = prop.PropertyType.FullName ?? "";
                Assert.False(propTypeName.Contains("TexasPortal.Domain.Entities"),
                    $"Contract DTO {type.Name} exposes Domain Entity in property {prop.Name}");
            }
        }
    }
}
