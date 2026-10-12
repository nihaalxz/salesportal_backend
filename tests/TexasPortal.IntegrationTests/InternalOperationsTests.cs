using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class InternalOperationsTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _visaId;
    private long _vfsId;
    private long _holidayId;

    public InternalOperationsTests(SalesApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Seed Salesman
        var salesman = new User
        {
            Name = "Salesman C",
            Email = "http.c@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesman);
        await db.SaveChangesAsync();

        // Seed Head
        var head = new Head
        {
            CreatedBy = salesman.Id,
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@example.com",
            MobileNo = "973123456",
            Nationality = "Bahraini",
            Address = "Riffa",
            Status = "active"
        };
        db.HeadsDbSet.Add(head);
        await db.SaveChangesAsync();

        // Seed Visa Booking
        var visa = new SalesVisaApplication
        {
            UserId = salesman.Id,
            CountryId = 1,
            VisaPackId = 10,
            VisaPackName = "Schengen Visa",
            ClientType = "Inhouse",
            HeadId = head.Id,
            ProcessingStatus = "pending",
            PaymentStatus = "paid",
            PayableAmount = 250m,
            ExpectedSupplierCost = 150m,
            Profit = 100m,
            CreatedAt = DateTime.UtcNow
        };
        db.SalesVisaApplicationsDbSet.Add(visa);

        // Seed VFS Appointment
        var vfs = new VfsAppointment
        {
            CreatedBy = salesman.Id,
            HeadId = head.Id,
            ClientType = "Inhouse",
            Prefix = "Mr.",
            Name = "John VFS",
            Supplier = "Supplier A",
            Country = "Germany",
            VisaType = "Tourist",
            PayableAmount = 80m,
            ExpectedSupplierCost = 50m,
            Profit = 30m,
            PaymentMode = "Cash",
            PaymentStatus = "paid",
            ProcessingStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };
        db.VfsAppointmentsDbSet.Add(vfs);

        // Seed Holiday Package
        var holiday = new HolidayPackage
        {
            CreatedBy = salesman.Id,
            HeadId = head.Id,
            ClientType = "Inhouse",
            Prefix = "Mr.",
            PassengerName = "Alice Holiday",
            Supplier = "Holiday Supplier",
            Country = "Switzerland",
            Days = 7,
            PayableAmount = 1500m,
            ExpectedSupplierCost = 1000m,
            Profit = 500m,
            PaymentMode = "Bank Transfer",
            PaymentStatus = "paid",
            ProcessingStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };
        db.HolidayPackagesDbSet.Add(holiday);

        await db.SaveChangesAsync();

        _visaId = visa.Id;
        _vfsId = vfs.Id;
        _holidayId = holiday.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetVisas_NoApiKey_Returns_401()
    {
        var response = await _client.GetAsync("/api/internal/operations/visas");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVisas_WrongApiKey_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/operations/visas");
        request.Headers.Add("X-OPS-KEY", "malicious_key_123");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVisas_ValidApiKey_Returns_Visas()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/operations/visas?page=1&pageSize=10");
        request.Headers.Add("X-OPS-KEY", "tx_ops_secure_888");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<PagedResponse<VisaApplicationSummaryResponse>>();
        Assert.NotNull(data);
        Assert.True(data.TotalCount >= 1);
        var visaItem = data.Items[0];
        Assert.Equal("Schengen Visa", visaItem.VisaPackName);
        Assert.Equal("Charlie Brown", visaItem.HeadName);
    }

    [Fact]
    public async Task GetVisaDetails_ValidApiKey_Returns_Details()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/operations/visas/{_visaId}");
        request.Headers.Add("X-OPS-KEY", "tx_ops_secure_888");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<VisaApplicationDetailResponse>();
        Assert.NotNull(details);
        Assert.Equal("Schengen Visa", details.VisaPackName);
        Assert.Equal("Charlie Brown", details.HeadName);
    }

    [Fact]
    public async Task GetVfsAppointments_ValidApiKey_Returns_Appointments()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/operations/vfs-appointments?page=1&pageSize=10");
        request.Headers.Add("X-OPS-KEY", "tx_ops_secure_888");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<PagedResponse<VfsAppointmentSummaryResponse>>();
        Assert.NotNull(data);
        Assert.True(data.TotalCount >= 1);
    }

    [Fact]
    public async Task GetVfsAppointmentDetails_ValidApiKey_Returns_Details()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/operations/vfs-appointments/{_vfsId}");
        request.Headers.Add("X-OPS-KEY", "tx_ops_secure_888");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<VfsAppointmentDetailResponse>();
        Assert.NotNull(details);
        Assert.Equal("Germany", details.Country);
    }

    [Fact]
    public async Task GetHolidayPackages_ValidApiKey_Returns_Packages()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/operations/holiday-packages?page=1&pageSize=10");
        request.Headers.Add("X-OPS-KEY", "tx_ops_secure_888");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<PagedResponse<HolidayPackageSummaryResponse>>();
        Assert.NotNull(data);
        Assert.True(data.TotalCount >= 1);
    }

    [Fact]
    public async Task GetHolidayPackageDetails_ValidApiKey_Returns_Details()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/operations/holiday-packages/{_holidayId}");
        request.Headers.Add("X-OPS-KEY", "tx_ops_secure_888");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<HolidayPackageDetailResponse>();
        Assert.NotNull(details);
        Assert.Equal("Switzerland", details.Country);
    }
}
