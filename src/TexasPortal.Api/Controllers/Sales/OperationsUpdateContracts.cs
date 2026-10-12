using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TexasPortal.Api.Controllers.Sales;

public class GovtFeeInput : TexasPortal.Contracts.Sales.Bookings.GovtFeeInputDto
{
    public IFormFile? PaymentProof { get; set; }
}

public class ProcessVisaRequest
{
    public string? VisaAppId { get; set; }
    public long ProcessedBy { get; set; }
    public List<GovtFeeInput> GovtFees { get; set; } = new();
}

public class EditGovRefRequest
{
    [Required]
    public string VisaAppId { get; set; } = null!;
    public long ProcessedBy { get; set; }
}

public class ApproveVisaRequest
{
    public string? DescNote { get; set; }
    public long ProcessedBy { get; set; }
    public IFormFile? VisaDocument { get; set; }
}

public class RejectVisaRequest
{
    public string? DescNote { get; set; }
    public string? RefundNote { get; set; }
    public decimal RejectionFee { get; set; }
    public decimal? PayableAmount { get; set; }
    public long ProcessedBy { get; set; }
    public IFormFile? RejectionDocument { get; set; }
}

public class RequestDocUpdateRequest
{
    public string? RequestNote { get; set; }
    public string? Request_Note { get; set; }
    public long ProcessedBy { get; set; }
}

// ─── VFS Contracts ───

public class ProcessVfsRequest
{
    public long ProcessedBy { get; set; }
    public List<GovtFeeInput> GovtFees { get; set; } = new();
}

public class UpdateVfsScheduleRequest
{
    [Required]
    public DateTime AppointmentDate { get; set; }
    [Required]
    public string AppointmentTime { get; set; } = null!; // HH:mm
    public string? Description { get; set; }
    public long ProcessedBy { get; set; }
}

public class UpdateVfsRefRequest
{
    public long ProcessedBy { get; set; }
}

public class UpdateVfsStatusRequest
{
    [Required]
    public string Status { get; set; } = null!; // completed or cancelled
    public long ProcessedBy { get; set; }
    public IFormFile? VfsDoc { get; set; } // Required for Completed
    public IFormFile? VerifiedReceipt { get; set; } // Required for Completed
    public IFormFile? RejectedReceipt { get; set; } // Optional for Cancelled
}

// ─── Holiday Contracts ───

public class ProcessHolidayRequest
{
    public long ProcessedBy { get; set; }
    public List<GovtFeeInput> GovtFees { get; set; } = new();
}

public class UpdateHolidayRefRequest
{
    public long ProcessedBy { get; set; }
}

public class UpdateHolidayStatusRequest
{
    [Required]
    public string Status { get; set; } = null!; // completed or cancelled
    public long ProcessedBy { get; set; }
}

public class HolidayDocUploadItem
{
    [Required]
    public string ServiceName { get; set; } = null!;
    [Required]
    public IFormFile File { get; set; } = null!;
}

public class UploadHolidayDocsRequest
{
    public List<HolidayDocUploadItem> Documents { get; set; } = new();
}

public record SyncGovtFeeUpdateRequest(
    string? ServiceType,
    long EntityId,
    decimal Amount,
    long? SupplierId,
    string? SupplierName,
    string? Description,
    string? ExternalReference,
    long? AccountsPaymentId = null,
    string? AccountsPaymentNumber = null,
    string? Status = null,
    string? RowKey = null,
    long PaymentId = 0,
    string? ReferenceNumber = null
);

public record SyncSupplierCostRequest(
    string? ServiceType,
    long EntityId,
    decimal Amount,
    long? SupplierId,
    string? SupplierName,
    string? Description,
    string? ExternalReference
);

public record BatchDocNumberLookupItem(string? ServiceType, string? ExternalReference);
public record BatchDocNumberResponseItem(string? ServiceType, string? ExternalReference, string? DocumentNumber);
public record UpdateBookingProcessingStatusInternalRequest(string? Status, bool? IsDealClosed = null, bool? OperationClosed = null, bool? IsOperationClosed = null);
public record UpdateBookingOperationClosedRequest(bool? OperationClosed = null, bool? IsOperationClosed = null);