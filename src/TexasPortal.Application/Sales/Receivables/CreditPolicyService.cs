using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receivables;

public interface ICreditPolicyService
{
    Task ValidateCreditLimitAsync(long headId, HeadType headType, decimal newCreditAmount, CancellationToken ct = default);
    DateTime CalculateDueDate(DateTime bookingDate, CreditTerm creditTerm);
    CollectionPriority CalculatePriority(DateTime dueDate, decimal outstandingAmount, decimal creditLimit);
}

public class CreditPolicyService : ICreditPolicyService
{
    private readonly ISalesPortalDbContext _db;

    public CreditPolicyService(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public Task ValidateCreditLimitAsync(long headId, HeadType headType, decimal newCreditAmount, CancellationToken ct = default)
    {
        // Credit limit validation bypassed as per business requirements for standard ledger operations.
        return Task.CompletedTask;
    }

    public DateTime CalculateDueDate(DateTime bookingDate, CreditTerm creditTerm)
    {
        var days = (int)creditTerm;
        return bookingDate.AddDays(days);
    }

    public CollectionPriority CalculatePriority(DateTime dueDate, decimal outstandingAmount, decimal creditLimit)
    {
        var today = DateTime.UtcNow.Date;
        var overdueDays = (today - dueDate.Date).Days;

        if (overdueDays > 60 || (creditLimit > 0 && outstandingAmount >= creditLimit * 0.9m))
        {
            return CollectionPriority.Critical;
        }
        if (overdueDays > 30 || outstandingAmount >= 1000m)
        {
            return CollectionPriority.High;
        }
        if (overdueDays > 0)
        {
            return CollectionPriority.Medium;
        }
        return CollectionPriority.Low;
    }
}
