using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TexasPortal.Application.Common.Options;
using TexasPortal.Application.Features.Invoices.Builders;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.UnitTests.Pdf;

public class VisaInvoiceBuilderTests
{
    private static SalesPortalDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<SalesPortalDbContext>()
            .UseInMemoryDatabase($"VisaInvoiceBuilder_{Guid.NewGuid():N}")
            .Options;
        return new SalesPortalDbContext(options);
    }

    [Fact]
    public async Task BuildAsync_IncludesVisaCountryNameInDescriptionAndRemarks()
    {
        await using var db = CreateInMemoryDb();

        var detail = new VisaApplicationDetailResponse
        {
            Id = 30L,
            DocumentNumber = "TXSVISA000030",
            UserId = 1L,
            CountryId = 5,
            CountryName = "United Arab Emirates",
            VisaPackId = 2,
            VisaPackName = "30 Days Tourist Visa",
            ClientType = "customer",
            HeadName = "Test Client",
            PayableAmount = 45.000m,
            Applicants = new List<VisaApplicantResponse>
            {
                new(
                    Id: 100L,
                    Prefix: "Mr",
                    FName: "John",
                    LName: "Doe",
                    Dob: null,
                    Nationality: "Bahraini",
                    CareOf: null,
                    ContactNo: "33001122",
                    EmailId: "john@example.com",
                    PassportNo: "P123456",
                    IqamaHotelNo: null,
                    PayableAmount: 45.000m,
                    ExpectedSupplierCost: 30.000m,
                    Profit: 15.000m,
                    IsInhouse: true,
                    PaymentMethod: "cash",
                    PaymentStatus: "paid",
                    PaymentProof: null,
                    PaymentReference: null,
                    BankId: null,
                    BankName: null,
                    PassportCopy: null,
                    BankDoc: null,
                    GovtFeeReference: null)
            }
        };

        var opsProxy = StubProxy<IOperationsReadService>.Create((method, _) =>
            method.Name == nameof(IOperationsReadService.GetVisaApplicationByIdAsync)
                ? Task.FromResult<VisaApplicationDetailResponse?>(detail)
                : throw new NotSupportedException(method.Name));

        var agentsProxy = StubProxy<IAgentsApiClient>.Create((method, _) =>
            method.Name == nameof(IAgentsApiClient.GetBookingApproverAsync)
                ? Task.FromResult<string?>("Master Accountant")
                : throw new NotSupportedException(method.Name));

        var builder = new VisaInvoiceBuilder(opsProxy, db, agentsProxy, Options.Create(new InvoiceOptions()));
        var model = await builder.BuildAsync(30L, userId: 0L, CancellationToken.None);

        Assert.NotNull(model);
        Assert.Single(model!.Items);
        Assert.Equal("United Arab Emirates - 30 Days Tourist Visa - John Doe (P123456)", model.Items[0].Description);
        Assert.Equal("Visa Application (United Arab Emirates) - Test Client", model.RemarksNote);
    }

    private class StubProxy<T> : DispatchProxy where T : class
    {
        private Func<MethodInfo, object?[]?, object?> _handler = null!;

        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            var proxy = Create<T, StubProxy<T>>();
            ((StubProxy<T>)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => _handler(targetMethod!, args);
    }
}
