namespace TexasPortal.Domain.Enums;

public enum ProfitType
{
    Normal = 0,
    Rejection = 1,
    Inhouse = 2,
    Adjustment = 3
}

public enum SupplierCostSource
{
    Estimated = 0,
    Approved = 1,
    Reconciled = 2,
    Rejection = 3
}

public enum SalesmanProfitAdjustmentType
{
    SalesReturn = 0,
    RejectionProfit = 1,
    ManualAdjustment = 2,
    Correction = 3
}
