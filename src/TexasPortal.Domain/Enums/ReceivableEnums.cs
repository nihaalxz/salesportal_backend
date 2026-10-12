namespace TexasPortal.Domain.Enums;

public enum BookingModule
{
    Visa,
    Vfs,
    Holiday,
    Hotel,
    Transport,
    Flight,
    OtherService
}

public enum ReceivableStatus
{
    Open,
    PartiallyPaid,
    Closed,
    Cancelled
}

public enum ReceivablePaymentStatus
{
    Pending,
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Cancelled
}

public enum HeadType
{
    Customer,
    Agent
}

public enum CreditTransactionType
{
    CreditSale,
    ReceiptPayment,
    CreditNote,
    WriteOff,
    Adjustment
}

public enum CollectionPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum CreditTerm
{
    Immediate = 0,
    Days7 = 7,
    Days15 = 15,
    Days30 = 30,
    Days45 = 45,
    Days60 = 60
}

/// <summary>
/// Discriminates receipt intent for business logic.
/// Replaces string-based Service checks throughout the approval pipeline.
/// </summary>
public enum ReceiptType
{
    General = 0,         // flights, hotels, visas, vfs, holidays, transportations, others
    WalletDeposit = 1,   // wallet_deposit / deposit
    CreditSettlement = 2 // credit_settlement
}
