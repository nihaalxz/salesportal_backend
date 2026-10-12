using System;

namespace TexasPortal.Contracts.Sales.Receipts;

public sealed record CreateReceiptRequest(
    long? HeadId,
    string ClientType, // customer, agent
    string? AgentName,
    string Narration,
    string Service, // flights, hotels, visas, vfs, holidays, transportations, others, deposit, wallet_deposit
    long? BookingId,
    decimal Amount,
    string PaymentMode, // cash, online, wallet
    long? BankId,
    string? BankName,
    string? PaymentProof = null
);

public sealed record UpdateReceiptStatusRequest(
    string Status // success
);

public class ReceiptSummaryResponse
{
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string ReceiptId { get; init; } = null!; // e.g. INV1023
    public string? HeadName { get; init; }
    public string ClientType { get; init; } = null!;
    public string? AgentName { get; init; }
    public string Service { get; init; } = null!;
    public decimal Amount { get; init; }
    public string PaymentMode { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public long? AgentId { get; init; }
    public string? SalesmanName { get; init; }
}

public sealed class ReceiptAllocationDetailResponse
{
    public long AllocationId { get; init; }
    public long ReceivableId { get; init; }
    public string? BookingNumber { get; init; }
    public string? BookingModule { get; init; }
    public decimal AllocatedAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ReceiptDetailResponse : ReceiptSummaryResponse
{
    public string Narration { get; init; } = null!;
    public long? HeadId { get; init; }
    public long? BookingId { get; init; }
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string? PaymentProof { get; init; }
    public string? PaymentReference { get; init; }
    public System.Collections.Generic.List<ReceiptAllocationDetailResponse> Allocations { get; init; } = new();
}

public sealed class BookingOptionResponse
{
    public long Id { get; init; }
    public string Label { get; init; } = null!;
    public decimal Amount { get; init; }
}

public sealed record BankResponse(long Id, string BankName, string AccountNumber);

public sealed class SupplierResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public string Code { get; init; } = null!;
    public System.Collections.Generic.List<string> Categories { get; init; } = new();
}
