using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.UnitTests.Sales
{
    public class OperationsHeadSearchTests
    {
        private static SalesPortalDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
                .UseInMemoryDatabase(databaseName: $"OperationsHeadSearch_{Guid.NewGuid():N}")
                .Options;
            return new SalesPortalDbContext(options);
        }

        private static OperationsReadService CreateService(SalesPortalDbContext db)
            => new(db, null!, null!, null!, null!);

        [Theory]
        [InlineData("Mohammed Ali Khan", 1L)]
        [InlineData("mohammed   ali   khan", 1L)]
        [InlineData("Mohammed Khan", 1L)]
        [InlineData("Abdul Rahman Al Doseri", 2L)]
        [InlineData("Bahraini", 2L)]
        public async Task GetHeadsAsync_SupportsSpacesAcrossFirstMiddleAndLastNames(string search, long expectedId)
        {
            await using var db = CreateInMemoryDbContext();
            db.HeadsDbSet.AddRange(
                new Head
                {
                    Id = 1L,
                    FirstName = "Mohammed Ali",
                    LastName = "Khan",
                    Email = "mak@example.com",
                    MobileNo = "33001122",
                    Nationality = "Indian",
                    Address = "Manama",
                    Type = "customer",
                    CreatedAt = DateTime.UtcNow
                },
                new Head
                {
                    Id = 2L,
                    FirstName = "Abdul",
                    LastName = "Rahman Al Doseri",
                    Email = "abd@example.com",
                    MobileNo = "33998877",
                    Nationality = "Bahraini",
                    Address = "Riffa",
                    Type = "customer",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                });
            await db.SaveChangesAsync(CancellationToken.None);

            var service = CreateService(db);
            var result = await service.GetHeadsAsync(1, 50, search, type: null, salesmanId: null, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(expectedId, result.Items[0].Id);
        }
    }
}
