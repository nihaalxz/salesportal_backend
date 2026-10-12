using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Leads;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;
using Xunit;

namespace TexasPortal.IntegrationTests;

[Collection("SalesApi")]
public class LeadsWriteTests : IClassFixture<SalesApiTestFactory>, IAsyncLifetime
{
    private readonly SalesApiTestFactory _factory;
    private readonly HttpClient _client;
    private long _salesmanId;
    private long _otherSalesmanId;
    private long _leadId;

    public LeadsWriteTests(SalesApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();

        // Seed Salesman A
        var salesman = new User
        {
            Name = "Salesman A",
            Email = "salesman.leads.a@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(salesman);

        // Seed Salesman B (Other)
        var otherSalesman = new User
        {
            Name = "Salesman B",
            Email = "salesman.leads.b@texasportal.com",
            UserType = "Salesman",
            IsActive = true,
            Password = "hashedpassword"
        };
        db.UsersDbSet.Add(otherSalesman);
        await db.SaveChangesAsync();

        _salesmanId = salesman.Id;
        _otherSalesmanId = otherSalesman.Id;

        // Seed Lead owned by Salesman A
        var lead = new Lead
        {
            CreatedBy = _salesmanId,
            Name = "Active Inquirer",
            CountryCode = "+973",
            MobileNo = "33445566",
            Services = "[\"Flights\",\"Hotel Booking\"]",
            LeadSource = "Facebook",
            Status = "Pending",
            ReminderActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeadsDbSet.Add(lead);
        await db.SaveChangesAsync();
        _leadId = lead.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void SetAuthHeaders(HttpRequestMessage request, string role = "Salesman", long? customUserId = null)
    {
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-UserId", (customUserId ?? _salesmanId).ToString());
        request.Headers.Add("X-Test-Role", role);
    }

    [Fact]
    public async Task AnonymousRequest_Returns_401()
    {
        var requestPayload = new CreateLeadRequest(
            Name: "Jane Doe",
            CountryCode: "+973",
            MobileNo: "33332222",
            Services: ["Flights"],
            LeadSource: "WhatsApp",
            Description: "Flight to Muscat",
            ReminderDate: null
        );

        var response = await _client.PostAsJsonAsync("/api/sales/leads", requestPayload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongRoleRequest_Returns_403()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/leads")
        {
            Content = JsonContent.Create(new CreateLeadRequest(
                Name: "Jane Doe",
                CountryCode: "+973",
                MobileNo: "33332222",
                Services: ["Flights"],
                LeadSource: "WhatsApp",
                Description: "Flight to Muscat",
                ReminderDate: null
            ))
        };
        SetAuthHeaders(request, role: "Accountant");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateLead_Succeeds_And_Sets_Pending_Status()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/leads")
        {
            Content = JsonContent.Create(new CreateLeadRequest(
                Name: "Sarah Connor",
                CountryCode: "+973",
                MobileNo: "999000111",
                Services: ["Flights", "Hotel Booking"],
                LeadSource: "Instagram",
                Description: "Looking for family package deals",
                ReminderDate: DateTime.UtcNow.AddDays(3)
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lead = await response.Content.ReadFromJsonAsync<LeadDetailResponse>();
        Assert.NotNull(lead);
        Assert.Equal("Sarah Connor", lead.Name);
        Assert.Equal("Pending", lead.Status); // Force pending default
        Assert.True(lead.ReminderActive);

        // Verify database entry
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var dbRecord = await db.LeadsDbSet.FirstOrDefaultAsync(l => l.Id == lead.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal(_salesmanId, dbRecord.CreatedBy);
    }

    [Fact]
    public async Task CreateLead_ValidationErrors_Returns_422()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/leads")
        {
            Content = JsonContent.Create(new CreateLeadRequest(
                Name: "", // invalid empty
                CountryCode: "+973",
                MobileNo: "33333333333333333333333333333333333333333", // too long
                Services: [], // invalid empty services list
                LeadSource: "",
                Description: null,
                ReminderDate: null
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLead_Succeeds()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/sales/leads/{_leadId}")
        {
            Content = JsonContent.Create(new UpdateLeadRequest(
                Name: "Active Inquirer Updated",
                Status: "Followed Up", // Pending, Completed, Followed Up
                CountryCode: "+973",
                MobileNo: "33445566",
                Services: ["Flights"],
                LeadSource: "Facebook",
                Description: "Updated inquirer",
                ReminderDate: null
            ))
        };
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<LeadDetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Active Inquirer Updated", updated.Name);
        Assert.Equal("Followed Up", updated.Status);
    }

    [Fact]
    public async Task UpdateLead_Of_OtherSalesman_Returns_404()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/sales/leads/{_leadId}")
        {
            Content = JsonContent.Create(new UpdateLeadRequest(
                Name: "Hack Lead Name",
                Status: "Completed",
                CountryCode: "+973",
                MobileNo: "33445566",
                Services: ["Flights"],
                LeadSource: "Facebook",
                Description: "Description",
                ReminderDate: null
            ))
        };
        // Authenticated as Salesman B
        SetAuthHeaders(request, customUserId: _otherSalesmanId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLead_Succeeds_And_SoftDeletes()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/sales/leads/{_leadId}");
        SetAuthHeaders(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Check if soft deleted in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesPortalDbContext>();
        var dbRecord = await db.LeadsDbSet.FirstOrDefaultAsync(l => l.Id == _leadId);
        Assert.NotNull(dbRecord);
        Assert.NotNull(dbRecord.DeletedAt);
    }
}
