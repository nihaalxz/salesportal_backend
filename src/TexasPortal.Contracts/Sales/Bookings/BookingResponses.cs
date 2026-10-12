using System;

namespace TexasPortal.Contracts.Sales.Bookings;

// --- Shared ---
public sealed class FlightBookingSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string FromLocation { get; init; } = null!;
    public string ToLocation { get; init; } = null!;
    public DateTime? TravelDate { get; init; }
    public string? AirlineCompany { get; init; }
    public string? TicketPnr { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public int PassengerCount { get; init; }
    public string? PassengerNames { get; set; }
    public DateTime? CreatedAt { get; init; }
    public string? AssignedOperatorName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public OperationalAssignmentStatus? AssignmentStatus { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class HotelBookingSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string GuestName { get; init; } = null!;
    public string? HotelName { get; init; }
    public string Country { get; init; } = null!;
    public int Days { get; init; }
    public DateTime CheckInDate { get; init; }
    public string? PaymentMethod { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public string OrderStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? AssignedOperatorName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public OperationalAssignmentStatus? AssignmentStatus { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class VisaApplicationSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string? VisaPackName { get; init; }
    public string ClientType { get; init; } = null!;
    public string? PaymentMethod { get; init; }
    public string ProcessingStatus { get; init; } = null!;
    public string PaymentStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public int ApplicantCount { get; init; }
    public string? ApplicantNames { get; set; }
    public DateTime? CreatedAt { get; init; }
    public string? VisaDocument { get; init; }
    public string? AssignedOperatorName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public OperationalAssignmentStatus? AssignmentStatus { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class VfsAppointmentSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string Name { get; init; } = null!;
    public string Country { get; init; } = null!;
    public string VisaType { get; init; } = null!;
    public string? PaymentMethod { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public DateTime? AppointmentDate { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? AssignedOperatorName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public OperationalAssignmentStatus? AssignmentStatus { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class HolidayPackageSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string PassengerName { get; init; } = null!;
    public string Country { get; init; } = null!;
    public int Days { get; init; }
    public string? PaymentMethod { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? AssignedOperatorName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public OperationalAssignmentStatus? AssignmentStatus { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class TransportationSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string? Name { get; init; }
    public string? Pickup { get; init; }
    public string? Drop { get; init; }
    public DateTime? DropDate { get; init; }
    public string? PaymentMethod { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? AssignedOperatorName { get; init; }
    public DateTime? AssignedAt { get; init; }
    public OperationalAssignmentStatus? AssignmentStatus { get; init; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class OtherServiceSummaryResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public string? HeadName { get; init; }
    public string? SalesmanName { get; init; }
    public string? ServiceName { get; init; }
    public string? Name { get; init; }
    public string? PaymentMethod { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? AssignedOperatorName { get; init; }
    public DateTime? AssignedAt { get; init; }
    public OperationalAssignmentStatus? AssignmentStatus { get; init; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Supplier { get; set; }
}

public sealed class OtherServiceDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long CreatedBy { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string? ClientType { get; init; }
    public string? Prefix { get; init; }
    public string? Name { get; init; }
    public string? CareOf { get; init; }
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public bool IsInhouse { get; init; }
    public string? SupplierName { get; init; }
    public long? SupplierId { get; set; }
    public string? ServiceName { get; set; }
    public string? Narration { get; set; }
    public System.Collections.Generic.List<OtherServiceApplicantResponse> Applicants { get; set; } = new();
    public decimal PayableAmount { get; set; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public long? BankId { get; set; }
    public string? BankName { get; set; }
    public string? PaymentProof { get; init; }
    public string PaymentStatus { get; set; } = null!;
    public string ProcessingStatus { get; set; } = null!;
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public System.Collections.Generic.List<HotelGuestResponse> Guests { get; set; } = new();
}

public sealed class FlightBookingDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long UserId { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string? ClientType { get; init; }
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public string? CareOf { get; init; }
    public string? Supplier { get; init; }
    public bool IsInhouse { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public decimal ExpectedSupplierCost { get; init; }
    public decimal Profit { get; init; }
    public string FromLocation { get; init; } = null!;
    public string ToLocation { get; init; } = null!;
    public DateTime? TravelDate { get; init; }
    public string? AirlineCompany { get; init; }
    public string? TicketPnr { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public string? PaymentProof { get; init; }
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public System.Collections.Generic.List<FlightPassengerResponse> Passengers { get; init; } = new();
}

public sealed class HotelBookingDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public string? HotelName { get; init; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long UserId { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string? ClientType { get; init; }
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public bool IsInhouse { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public decimal ExpectedSupplierCost { get; init; }
    public decimal PayableAmount { get; init; }
    public decimal Profit { get; init; }
    public string? Prefix { get; init; }
    public string GuestName { get; init; } = null!;
    public string? CareOf { get; init; }
    public string Country { get; init; } = null!;
    public int Days { get; init; }
    public DateTime CheckInDate { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public string OrderStatus { get; init; } = null!;
    public string? PaymentProof { get; init; }
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public System.Collections.Generic.List<HotelGuestResponse> Guests { get; init; } = new();
}

public sealed class GovtFeeDetailResponse
{
    public long Id { get; init; }
    public string? RowKey { get; init; }
    public long? AccountsPaymentId { get; init; }
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public string? PaymentReference { get; init; }
    public string Status { get; init; } = "success";
    public string? PaymentProof { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public class GovtFeeInputDto
{
    public string? RowKey { get; set; }
    public long? AccountsPaymentId { get; set; }
    public decimal Amount { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? TransactionNo { get; set; }
    public string? GovtFeeDescription { get; set; }
    public string? ExistingPaymentProof { get; set; }
}

public sealed class VisaApplicationDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long UserId { get; init; }
    public int CountryId { get; init; }
    public string? CountryName { get; set; }
    public int VisaPackId { get; init; }
    public string? VisaPackName { get; init; }
    public string ClientType { get; init; } = null!;
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string ProcessingStatus { get; init; } = null!;
    public string PaymentStatus { get; init; } = null!;
    public string? VisaDocument { get; init; }
    public string? ApprovalNote { get; init; }
    public string? RejectionDocument { get; init; }
    public string? RejectionNote { get; init; }
    public string? RefundNote { get; init; }
    public string? DocumentRequestNote { get; init; }
    public DateTime? DocumentRequestDate { get; init; }
    public decimal PayableAmount { get; init; }
    public decimal ExpectedSupplierCost { get; init; }
    public decimal Profit { get; init; }
    public bool IsInhouse { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public decimal RejectionFee { get; init; }
    public string? PaymentMethod { get; init; }
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string? PaymentProof { get; init; }
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? AssignedOperatorName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public OperationalAssignmentStatus? AssignmentStatus { get; set; }
    public System.Collections.Generic.List<VisaApplicantResponse> Applicants { get; init; } = new();
    public System.Collections.Generic.List<GovtFeeDetailResponse> GovtFees { get; set; } = new();
}

public sealed class VfsAppointmentDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long CreatedBy { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string? ClientType { get; init; }
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public string Prefix { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Supplier { get; init; } = null!;
    public bool IsInhouse { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public string Country { get; init; } = null!;
    public string VisaType { get; init; } = null!;
    public decimal PayableAmount { get; init; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public string PaymentMode { get; init; } = null!;
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public DateTime? AppointmentDate { get; init; }
    public TimeSpan? AppointmentTime { get; init; }
    public string? VfsReceipt { get; init; }
    public string? PassportCopy { get; init; }
    public string? PaymentProof { get; init; }
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public System.Collections.Generic.List<VfsApplicantResponse> Applicants { get; init; } = new();
    public System.Collections.Generic.List<GovtFeeDetailResponse> GovtFees { get; set; } = new();
}

public sealed class HolidayPackageDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long CreatedBy { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string? ClientType { get; init; }
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public string Prefix { get; init; } = null!;
    public string PassengerName { get; init; } = null!;
    public string Supplier { get; init; } = null!;
    public bool IsInhouse { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public string Country { get; init; } = null!;
    public int Days { get; init; }
    public decimal PayableAmount { get; init; }
    public decimal ExpectedSupplierCost { get; set; }
    public decimal Profit { get; set; }
    public string PaymentMode { get; init; } = null!;
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public string? PaymentProof { get; init; }
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public System.Collections.Generic.List<HolidayPassengerResponse> Passengers { get; init; } = new();
    public List<HolidayPackageDocumentResponse> Documents { get; set; } = new();
    public System.Collections.Generic.List<GovtFeeDetailResponse> GovtFees { get; set; } = new();
}

public sealed class HolidayPackageDocumentResponse
{
    public long Id { get; init; }
    public string DocumentPath { get; init; } = null!;
    public string DocumentType { get; init; } = null!;
    public DateTime? CreatedAt { get; init; }
}

public sealed class TransportationDetailResponse
{
    public bool IsDealClosed { get; set; }
    public bool OperationClosed { get; set; }
    public long Id { get; init; }
    public string? DocumentNumber { get; init; }
    public long CreatedBy { get; init; }
    public long? HeadId { get; init; }
    public string? HeadName { get; init; }
    public string? ClientType { get; init; }
    public string? Agent { get; init; }
    public long? AgentId { get; init; }
    public string? Prefix { get; init; }
    public string? Name { get; init; }
    public string? CareOf { get; init; }
    public string? Supplier { get; init; }
    public bool IsInhouse { get; init; }
    public long? SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public string? Pickup { get; init; }
    public string? Drop { get; init; }
    public DateTime? DropDate { get; init; }
    public TimeSpan? DropTime { get; init; }
    public decimal PayableAmount { get; init; }
    public decimal ExpectedSupplierCost { get; init; }
    public decimal Profit { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public long? BankId { get; init; }
    public string? BankName { get; init; }
    public string? Vehicle { get; init; }
    public string? PaymentProof { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string ProcessingStatus { get; init; } = null!;
    public bool HasSalesReturn { get; set; }
    public decimal? SalesReturnAmount { get; set; }
    public bool HasPurchaseReturn { get; set; }
    public decimal? PurchaseReturnAmount { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public System.Collections.Generic.List<TransportPassengerResponse> Passengers { get; init; } = new();
}
