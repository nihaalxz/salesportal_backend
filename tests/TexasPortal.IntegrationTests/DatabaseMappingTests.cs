using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

public class DatabaseMappingTests
{
    private readonly SalesPortalDbContext _dbContext;

    public DatabaseMappingTests()
    {
        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseSqlServer("Server=localhost;Database=Test_Db;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;")
            .Options;

        _dbContext = new SalesPortalDbContext(options);
    }

    [Fact]
    public void DbContextModel_ShouldBuildSuccessfully()
    {
        var model = _dbContext.Model;
        Assert.NotNull(model);
    }

    [Theory]
    [InlineData(typeof(User), "users")]
    [InlineData(typeof(Head), "heads")]
    [InlineData(typeof(HeadLedger), "head_ledgers")]
    [InlineData(typeof(Receipt), "receipts")]
    [InlineData(typeof(FlightBooking), "flight_bookings")]
    [InlineData(typeof(FlightPassenger), "flight_passengers")]
    [InlineData(typeof(HotelBooking), "hotel_bookings")]
    [InlineData(typeof(SalesVisaApplication), "sales_visa_applications")]
    [InlineData(typeof(SalesVisaApplicant), "sales_visa_applicants")]
    [InlineData(typeof(VfsAppointment), "vfs_appointments")]
    [InlineData(typeof(HolidayPackage), "holiday_packages")]
    [InlineData(typeof(Transportation), "transportations")]
    [InlineData(typeof(OtherService), "other_services")]
    [InlineData(typeof(CashInHand), "cash_in_hands")]
    [InlineData(typeof(Deposit), "deposits")]
    [InlineData(typeof(StaffMonthlyRecord), "staff_monthly_records")]
    [InlineData(typeof(Lead), "leads")]
    [InlineData(typeof(Dispute), "disputes")]
    [InlineData(typeof(SalesVisaGovtFee), "sales_visa_govt_fees")]
    [InlineData(typeof(VfsAppointmentGovtFee), "vfs_appointment_govt_fees")]
    [InlineData(typeof(HolidayPackageGovtFee), "holiday_package_govt_fees")]
    [InlineData(typeof(HolidayPackageDocument), "holiday_package_documents")]
    [InlineData(typeof(Notification), "notifications")]
    public void Entity_ShouldBeMappedToCorrectTable(Type entityType, string expectedTableName)
    {
        var mappedEntityType = _dbContext.Model.FindEntityType(entityType);
        Assert.NotNull(mappedEntityType);
        Assert.Equal(expectedTableName, mappedEntityType.GetTableName());
    }

    [Theory]
    [InlineData(typeof(User), "ProfitGenerated", 15, 3)]
    [InlineData(typeof(Head), "Wallet", 15, 3)]
    [InlineData(typeof(HeadLedger), "Debit", 15, 3)]
    [InlineData(typeof(HeadLedger), "Credit", 15, 3)]
    [InlineData(typeof(Receipt), "Amount", 15, 3)]
    [InlineData(typeof(FlightBooking), "PayableAmount", 15, 3)]
    public void FinancialProperties_ShouldHaveCorrectPrecisionAndScale(Type entityType, string propertyName, int expectedPrecision, int expectedScale)
    {
        var mappedEntityType = _dbContext.Model.FindEntityType(entityType);
        Assert.NotNull(mappedEntityType);
        
        var property = mappedEntityType.FindProperty(propertyName);
        Assert.NotNull(property);

        Assert.Equal(expectedScale, property.GetScale());
        Assert.Equal(expectedPrecision, property.GetPrecision());
    }

    [Fact]
    public void DatabaseModel_ShouldNotContainUnexpectedShadowForeignKeys()
    {
        var model = _dbContext.Model;
        var entityTypes = model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var shadowProperties = entityType.GetProperties()
                .Where(p => p.IsShadowProperty() && p.Name != "TempId"); // TempId is standard fallback in some EF setups

            foreach (var property in shadowProperties)
            {
                // We assert that no foreign keys are represented as shadow properties.
                // Foreign keys must be explicitly declared as class properties on the entities.
                Assert.False(property.IsForeignKey(), 
                    $"Entity '{entityType.Name}' contains an unexpected shadow foreign key property: '{property.Name}'");
            }
        }
    }

    [Fact]
    public void PrimaryKeys_ShouldBeExplicitlyMapped()
    {
        var entityTypes = _dbContext.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.Single(primaryKey.Properties); // All our tables have single PKs
        }
    }
}
