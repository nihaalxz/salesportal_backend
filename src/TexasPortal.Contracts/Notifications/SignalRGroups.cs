namespace TexasPortal.Contracts.Notifications;

public static class SignalRGroups
{
    public const string Accounts = "role:accounts";
    public const string Sales = "role:sales";
    public const string Operations = "role:operations";
    public const string GlobalAll = "global:all";

    public static string User(string userId) => $"user:{userId}";
    public static string Role(string roleName) => $"role:{roleName.ToLowerInvariant()}";
    public static string CustomerLedger(long headId) => $"ledger:customer:{headId}";
    public static string SupplierLedger(long supplierId) => $"ledger:supplier:{supplierId}";
    public static string AgentLedger(long agentId) => $"ledger:agent:{agentId}";
    public static string Wallet(long headId) => $"wallet:{headId}";
    public static string CustomerWallet(long headId) => $"wallet:customer:{headId}";
    public static string AgentWallet(long agentId) => $"wallet:agent:{agentId}";
    public static string SupplierWallet(long supplierId) => $"wallet:supplier:{supplierId}";
    public static string CustomerOutstanding(long headId) => $"outstanding:customer:{headId}";
    public static string AgentOutstanding(long agentId) => $"outstanding:agent:{agentId}";
    public static string SupplierOutstanding(long supplierId) => $"outstanding:supplier:{supplierId}";
    public const string ReceiptApproval = "receipt:approval";
    public const string ReceiptList = "receipt:list";
    public static string CustomerReceipt(long headId) => $"receipt:customer:{headId}";
    public static string AgentReceipt(long agentId) => $"receipt:agent:{agentId}";
    public static string SupplierReceipt(long supplierId) => $"receipt:supplier:{supplierId}";
    public const string OperationsVisa = "operations:visa";
    public const string OperationsVfs = "operations:vfs";
    public const string OperationsHoliday = "operations:holiday";
    public const string OperationsTransport = "operations:transport";
    public const string OperationsOtherServices = "operations:otherservices";
    public static string OperationsUser(long userId) => $"operations:user:{userId}";
    public const string DashboardSales = "dashboard:sales";
    public const string DashboardFinance = "dashboard:finance";
    public const string DashboardWallet = "dashboard:wallet";
    public const string DashboardSummary = "dashboard:summary";
    public const string DashboardReceipts = "dashboard:receipts";
    public const string DashboardOperations = "dashboard:operations";
    public static string DashboardUser(long userId) => $"dashboard:user:{userId}";
    public static string Booking(long bookingId) => $"booking:{bookingId}";
    public static string Dashboard(string portal) => $"dashboard:{portal.ToLowerInvariant()}";
}
