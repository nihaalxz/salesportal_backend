using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Common.Interfaces;

public interface IOperationalAssignmentClient
{
    Task<IReadOnlyDictionary<long, OperationalAssignmentSummary>> GetActiveAssignmentsAsync(
        string entityType, IEnumerable<long> entityIds, CancellationToken ct = default);
}

public sealed record OperationalAssignmentSummary(
    long EntityId,
    string AssignedOperatorName,
    DateTime AssignedAt,
    OperationalAssignmentStatus Status
);
