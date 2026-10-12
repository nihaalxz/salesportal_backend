using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;

namespace TexasPortal.Infrastructure.Persistence;

public class SalesPortalDbContextFactoryWrapper : ISalesPortalDbContextFactory
{
    private readonly IDbContextFactory<SalesPortalDbContext> _factory;

    public SalesPortalDbContextFactoryWrapper(IDbContextFactory<SalesPortalDbContext> factory)
    {
        _factory = factory;
    }

    public ISalesPortalDbContext CreateDbContext()
    {
        return _factory.CreateDbContext();
    }
}
