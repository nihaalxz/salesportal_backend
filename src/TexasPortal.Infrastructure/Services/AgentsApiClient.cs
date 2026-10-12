using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Heads;
using TexasPortal.Contracts.Sales.Receipts;

using Microsoft.Extensions.Caching.Memory;

namespace TexasPortal.Infrastructure.Services;

public sealed class AgentsApiClient : IAgentsApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public AgentsApiClient(IHttpClientFactory httpClientFactory, IConfiguration config, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _cache = cache;
    }

    private string BaseUrl => _config["AccountsPortal:BaseUrl"] ?? "https://accountstexas.runasp.net";
    private string ApiKey => _config["AccountsPortal:ApiKey"] ?? "tx_secure_service_key_999_32chars_long";

    public async Task<List<HeadLookupResponse>> GetAgentsAsync(long salesmanId, string? search, int limit, CancellationToken ct)
    {
        var cacheKey = $"agents_raw:{salesmanId}";
        if (!_cache.TryGetValue(cacheKey, out List<AccountsAgentResponse>? rawAgents) || rawAgents == null)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var url = salesmanId > 0 ? $"{BaseUrl}/api/public/agents?salesmanId={salesmanId}" : $"{BaseUrl}/api/public/agents";
                var response = await client.GetAsync(url, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    rawAgents = await response.Content.ReadFromJsonAsync<List<AccountsAgentResponse>>(cancellationToken: cts.Token);
                    if (rawAgents != null)
                    {
                        _cache.Set(cacheKey, rawAgents, TimeSpan.FromSeconds(15));
                    }
                }
            }
            catch (Exception)
            {
                rawAgents = new List<AccountsAgentResponse>();
            }
        }

        var agents = rawAgents ?? new List<AccountsAgentResponse>();
        var list = agents.Select(a => new HeadLookupResponse
        {
            Id = a.Id,
            FirstName = a.FirstName,
            LastName = a.LastName,
            Email = a.EmailId ?? string.Empty,
            MobileNo = a.PhoneNo ?? string.Empty,
            Wallet = a.Wallet
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            list = list.Where(a => 
                (a.FirstName != null && a.FirstName.ToLower().Contains(term)) || 
                (a.LastName != null && a.LastName.ToLower().Contains(term)) || 
                (a.Email != null && a.Email.ToLower().Contains(term)) || 
                (a.MobileNo != null && a.MobileNo.Contains(term))
            );
        }

        return list.Take(limit).ToList();
    }

    public async Task<HeadLookupResponse?> GetAgentByIdAsync(long agentId, CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var response = await client.GetAsync($"{BaseUrl}/api/public/agents/{agentId}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();

            var agent = await response.Content.ReadFromJsonAsync<AccountsAgentResponse>(cancellationToken: ct);
            if (agent == null) return null;

            return new HeadLookupResponse
            {
                Id = agent.Id,
                FirstName = agent.FirstName,
                LastName = agent.LastName,
                Email = agent.EmailId ?? string.Empty,
                MobileNo = agent.PhoneNo ?? string.Empty,
                Wallet = agent.Wallet
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<BankResponse>> GetBanksAsync(CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var response = await client.GetAsync($"{BaseUrl}/api/public/banks", ct);
            response.EnsureSuccessStatusCode();

            var banks = await response.Content.ReadFromJsonAsync<List<BankResponse>>(cancellationToken: ct);
            return banks ?? new List<BankResponse>();
        }
        catch (Exception)
        {
            return new List<BankResponse>();
        }
    }

    public async Task<List<SupplierResponse>> GetSuppliersAsync(CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var response = await client.GetAsync($"{BaseUrl}/api/public/suppliers", ct);
            response.EnsureSuccessStatusCode();

            var suppliers = await response.Content.ReadFromJsonAsync<List<SupplierResponse>>(cancellationToken: ct);
            return suppliers ?? new List<SupplierResponse>();
        }
        catch (Exception)
        {
            return new List<SupplierResponse>();
        }
    }

    public async Task<List<VisaCountryMasterResponse>> GetVisaCountriesAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var response = await client.GetAsync($"{BaseUrl}/api/visa-master/countries?includeInactive=false", ct);
            response.EnsureSuccessStatusCode();

            var countries = await response.Content.ReadFromJsonAsync<List<VisaCountryMasterResponse>>(cancellationToken: ct);
            return countries ?? new List<VisaCountryMasterResponse>();
        }
        catch (Exception)
        {
            return new List<VisaCountryMasterResponse>();
        }
    }

    public async Task<List<VisaPackageMasterResponse>> GetVisaPackagesAsync(long? countryId = null, CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var url = $"{BaseUrl}/api/visa-master/packages?includeInactive=false";
            if (countryId.HasValue)
            {
                url += $"&countryId={countryId.Value}";
            }

            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var packages = await response.Content.ReadFromJsonAsync<List<VisaPackageMasterResponse>>(cancellationToken: ct);
            return packages ?? new List<VisaPackageMasterResponse>();
        }
        catch (Exception)
        {
            return new List<VisaPackageMasterResponse>();
        }
    }

    public async Task CreditAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/api/public/agents/{agentId}/credit", new { Amount = amount, Description = description }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DebitAgentWalletAsync(long agentId, decimal amount, string description, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/api/public/agents/{agentId}/debit", new { Amount = amount, Description = description }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResponse<HeadLedgerEntryResponse>> GetAgentLedgerAsync(long agentId, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var response = await client.GetAsync($"{BaseUrl}/api/public/agents/{agentId}/ledger?page={page}&pageSize={pageSize}", ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AgentLedgerPagedResponse>(cancellationToken: ct);
            if (result == null)
                return PagedResponse<HeadLedgerEntryResponse>.Create([], 0, page, pageSize);

            var items = result.Items.Select(t => new HeadLedgerEntryResponse
            {
                Id = t.Id,
                Type = t.Type,
                Description = t.Description,
                Debit = t.Debit,
                Credit = t.Credit,
                Balance = t.Credit - t.Debit,
                CreatedAt = t.CreatedAt
            }).ToList();

            return PagedResponse<HeadLedgerEntryResponse>.Create(items, result.TotalCount, result.Page, result.PageSize);
        }
        catch (Exception)
        {
            return PagedResponse<HeadLedgerEntryResponse>.Create([], 0, page, pageSize);
        }
    }

    public async Task CreateGovtSupplierPaymentAsync(
        long supplierId,
        decimal amount,
        string? referenceNumber,
        string? externalReference,
        string? serviceType,
        string? description,
        long? createdByUserId = null,
        CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var body = new
            {
                SupplierId = supplierId,
                Amount = amount,
                ReferenceNumber = referenceNumber,
                ExternalReference = externalReference,
                ServiceType = serviceType,
                Description = description,
                CreatedByUserId = createdByUserId
            };

            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/public/supplier-payments/govt-payment", body, ct);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Silently fallback if accounts portal API is unreachable
        }
    }
    public async Task SyncGovtSupplierPaymentsAsync(
        string externalReference,
        string serviceType,
        long? createdByUserId,
        List<GovtFeeSyncItem> fees,
        CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var body = new
            {
                ExternalReference = externalReference,
                ServiceType = serviceType,
                CreatedByUserId = createdByUserId,
                Payments = fees.Where(f => f.Amount > 0).Select(f => new
                {
                    SupplierId = (f.SupplierId.HasValue && f.SupplierId.Value > 0) ? f.SupplierId.Value : 0,
                    SupplierName = f.SupplierName,
                    Amount = f.Amount,
                    ReferenceNumber = f.TransactionNo,
                    Description = string.IsNullOrWhiteSpace(f.GovtFeeDescription) ? $"Government Fee for {serviceType} {externalReference}" : f.GovtFeeDescription,
                    RowKey = f.RowKey
                }).ToList()
            };

            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/public/supplier-payments/sync-govt-payments", body, ct);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Silently fallback if accounts portal API is unreachable, ensuring operations submission succeeds
        }
    }

    public async Task<BookingSupplierPaymentSummary?> GetBookingSupplierPaymentsAsync(string serviceType, string externalReference, CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var url = $"{BaseUrl}/api/public/supplier-payments/booking/{Uri.EscapeDataString(serviceType ?? "")}/{Uri.EscapeDataString(externalReference ?? "")}";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<BookingSupplierPaymentSummary>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool HasReturn, decimal? ReturnAmount, bool HasPurchaseReturn, decimal? PurchaseReturnAmount)> CheckSalesReturnAsync(string serviceType, string serviceId, CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

            var url = $"{BaseUrl}/api/public/sales-returns/check?serviceType={Uri.EscapeDataString(serviceType ?? "")}&serviceId={Uri.EscapeDataString(serviceId ?? "")}";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, false, null);

            var res = await response.Content.ReadFromJsonAsync<SalesReturnCheckResponse>(cancellationToken: ct);
            return (res?.HasSalesReturn ?? false, res?.SalesReturnAmount, res?.HasPurchaseReturn ?? false, res?.PurchaseReturnAmount);
        }
        catch
        {
            return (false, null, false, null);
        }
    }

    private sealed class SalesReturnCheckResponse
    {
        public bool HasSalesReturn { get; set; }
        public decimal? SalesReturnAmount { get; set; }
        public bool HasPurchaseReturn { get; set; }
        public decimal? PurchaseReturnAmount { get; set; }
    }

    private sealed class AgentLedgerPagedResponse
    {
        public List<AgentLedgerItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class AgentLedgerItem
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    private sealed class AccountsAgentResponse
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? EmailId { get; set; }
        public string? PhoneNo { get; set; }
        public string? Nationality { get; set; }
        public string? Address { get; set; }
        public decimal Wallet { get; set; }
        public long? SalesmanId { get; set; }
    }

    public async Task<string?> GetBookingApproverAsync(string serviceType, string bookingId, string? docNumber = null, CancellationToken ct = default)
    {
        var cacheKey = $"approver:{serviceType}:{bookingId}:{docNumber}";
        if (_cache.TryGetValue(cacheKey, out string? cachedApprover))
        {
            return cachedApprover;
        }

        var candidateBaseUrls = new List<string>();
        var configured = _config["AccountsPortal:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) candidateBaseUrls.Add(configured.TrimEnd('/'));
        if (!candidateBaseUrls.Contains("http://localhost:5028")) candidateBaseUrls.Add("http://localhost:5028");
        if (!candidateBaseUrls.Contains("https://accountstexas.runasp.net")) candidateBaseUrls.Add("https://accountstexas.runasp.net");

        foreach (var baseUrl in candidateBaseUrls)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("X-OPS-KEY", ApiKey);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var url = $"{baseUrl}/api/public/approver?serviceType={Uri.EscapeDataString(serviceType)}&bookingId={Uri.EscapeDataString(bookingId)}";
                if (!string.IsNullOrWhiteSpace(docNumber))
                {
                    url += $"&docNumber={Uri.EscapeDataString(docNumber)}";
                }

                var response = await client.GetAsync(url, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApproverResponseDto>(cancellationToken: cts.Token);
                    var accountantName = result?.AccountantName;
                    if (!string.IsNullOrWhiteSpace(accountantName))
                    {
                        _cache.Set(cacheKey, accountantName, TimeSpan.FromMinutes(2));
                        return accountantName;
                    }
                }
            }
            catch (Exception)
            {
                // Try next base URL
            }
        }

        return null;
    }

    private sealed record ApproverResponseDto(string? AccountantName);

}