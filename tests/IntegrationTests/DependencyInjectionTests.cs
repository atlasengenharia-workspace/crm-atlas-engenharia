using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.Infrastructure;
using CrmAtlas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrmAtlas.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersAValidServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddInfrastructure(configuration);

        Assert.Same(services, result);
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true });
    }

    [Fact]
    public async Task DbContext_PersistsMigratedDomainEntity()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseInMemoryDatabase($"atlas-{Guid.NewGuid()}")
            .Options;

        await using var context = new AtlasDbContext(options);
        context.Clientes.Add(new Cliente
        {
            CnpjCpf = "12345678901",
            RazaoSocial = "Cliente de teste"
        });

        await context.SaveChangesAsync();

        var cliente = await context.Clientes.SingleAsync();
        Assert.Equal("Cliente de teste", cliente.RazaoSocial);
    }

    [Fact]
    public void DbContext_PreservesLegacyTableNames()
    {
        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseNpgsql("Host=localhost;Database=atlas_model;Username=atlas;Password=not-used")
            .Options;

        using var context = new AtlasDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Cliente));

        Assert.NotNull(entity);
        Assert.Equal("clientes", entity.GetTableName());
        Assert.Equal("cnpj_cpf", entity.FindProperty(nameof(Cliente.CnpjCpf))?.GetColumnName());
    }
}
