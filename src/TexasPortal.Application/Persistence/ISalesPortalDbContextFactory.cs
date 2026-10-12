namespace TexasPortal.Application.Persistence;

public interface ISalesPortalDbContextFactory
{
    ISalesPortalDbContext CreateDbContext();
}
