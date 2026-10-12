namespace TexasPortal.Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,
    Online = 2,
    Credit = 3
}

public enum AllocationType
{
    FullyFromBalance = 1,
    SplitBalanceAndCredit = 2,
    FullyCredit = 3
}

public enum FinancialSourceType
{
    Booking = 1,
    Refund = 2,
    Adjustment = 3,
    Migration = 4,
    Manual = 5
}

public enum CustomerAccountTransactionType
{
    BookingCharge = 1,
    Deposit = 2,
    ReceiptSettlement = 3,
    Refund = 4,
    CreditNote = 5,
    ManualAdjustment = 6,
    OpeningBalance = 7,
    WriteOff = 8,
    Transfer = 9
}

public enum BalanceTransactionType
{
    Deposit = 1,
    Booking = 2,
    Refund = 3,
    Adjustment = 4,
    Transfer = 5
}

public enum AccountHolderType
{
    Customer = 1,
    Agent = 2,
    Supplier = 3,
    Corporate = 4
}
