using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Infrastructure.Services;

public sealed class OperationalAssignmentClient : IOperationalAssignmentClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OperationalAssignmentClient> _logger;
    private readonly string _baseUrl;

    public OperationalAssignmentClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<OperationalAssignmentClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _baseUrl = config["OperationalPortal:BaseUrl"]?.TrimEnd('/') ?? "https://operationtexas.runasp.net";
    }

    public async Task<IReadOnlyDictionary<long, OperationalAssignmentSummary>> GetActiveAssignmentsAsync(
        string entityType, IEnumerable<long> entityIds, CancellationToken ct = default)
    {
        var idList = entityIds.Distinct().Where(id => id > 0).ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<long, OperationalAssignmentSummary>();
        }

        // Generate cache key for this batch
        var cacheKey = $"OpAssign_{entityType.ToLower()}_{string.Join(",", idList.OrderBy(x => x))}";
        if (_cache.TryGetValue<IReadOnlyDictionary<long, OperationalAssignmentSummary>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var result = new Dictionary<long, OperationalAssignmentSummary>();

        try
        {
            // Max 100 IDs per request as per spec
            var batches = idList.Chunk(100);

            foreach (var batch in batches)
            {
                var client = _httpClientFactory.CreateClient("OperationalAssignmentClient");
                client.Timeout = TimeSpan.FromMilliseconds(1500); // 1.5 second fallback timeout

                var idsQuery = string.Join(",", batch);
                var requestUrl = $"{_baseUrl}/api/internal/assignments/batch?entityType={Uri.EscapeDataString(entityType)}&ids={idsQuery}";

                using var response = await client.GetAsync(requestUrl, ct);

                if (response.IsSuccessStatusCode)
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var items = await response.Content.ReadFromJsonAsync<List<RawAssignmentResponse>>(options, cancellationToken: ct);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            if (item.EntityId > 0 && !string.IsNullOrWhiteSpace(item.AssignedToUserName))
                            {
                                var summary = new OperationalAssignmentSummary(
                                    item.EntityId,
                                    item.AssignedToUserName,
                                    item.AssignedAt ?? DateTime.UtcNow,
                                    OperationalAssignmentStatus.Assigned
                                );
                                result[item.EntityId] = summary;
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("[OperationalAssignmentClient] Operational portal returned HTTP {StatusCode} for entityType={EntityType}", response.StatusCode, entityType);
                }
            }

            // Cache successful / degraded result for 10 seconds
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(10));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("[OperationalAssignmentClient] Timeout querying operational assignments for entityType={EntityType}. Falling back silently.", entityType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OperationalAssignmentClient] Failed to query operational assignments for entityType={EntityType}. Falling back silently.", entityType);
        }

        return result;
    }

    private sealed record RawAssignmentResponse(
        [property: JsonPropertyName("entityId")] long EntityId,
        [property: JsonPropertyName("assignedToUserName")] string? AssignedToUserName,
        [property: JsonPropertyName("assignedAt")] DateTime? AssignedAt,
        [property: JsonPropertyName("status")] string? Status
    );
}
