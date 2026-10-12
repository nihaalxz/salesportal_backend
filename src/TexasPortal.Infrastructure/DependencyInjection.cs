using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Bookings.OtherServices.CreateOtherServiceBooking;
using TexasPortal.Application.Sales.Bookings.Hotels.CreateHotelBooking;
using TexasPortal.Application.Sales.Bookings.Flights.CreateFlightBooking;
using TexasPortal.Application.Sales.Bookings.Visas.CreateVisaBooking;
using TexasPortal.Application.Sales.Bookings.Vfs.CreateVfsAppointment;
using TexasPortal.Application.Sales.Bookings.Holidays.CreateHolidayPackage;
using TexasPortal.Application.Sales.Bookings.Transports.CreateTransportation;
using TexasPortal.Application.Sales.Heads.CreateHead;
using TexasPortal.Application.Sales.Heads.UpdateHead;
using TexasPortal.Application.Sales.Leads.CreateLead;
using TexasPortal.Application.Sales.Leads.UpdateLead;
using TexasPortal.Application.Sales.Leads.ArchiveLead;
using TexasPortal.Application.Sales.Receipts.CreateReceipt;
using TexasPortal.Application.Sales.Receipts.UpdateReceiptStatus;
using TexasPortal.Application.Sales.Disputes.ResolveDispute;
using TexasPortal.Application.Sales.Disputes.CreateDispute;
using TexasPortal.Application.Auth;
using TexasPortal.Infrastructure.Persistence;
using TexasPortal.Infrastructure.Services;

namespace TexasPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=db67449.public.databaseasp.net; Database=db67449; User Id=db67449; Password=n#7YD2o%5?Qy; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

        services.AddDbContextFactory<SalesPortalDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: System.TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }, ServiceLifetime.Scoped);

        services.AddScoped<SalesPortalDbContext>(provider =>
            provider.GetRequiredService<IDbContextFactory<SalesPortalDbContext>>().CreateDbContext());

        services.AddScoped<ISalesPortalDbContext>(provider => provider.GetRequiredService<SalesPortalDbContext>());
        services.AddScoped<ISalesPortalDbContextFactory, SalesPortalDbContextFactoryWrapper>();
        services.AddScoped<ITransactionalExecutor, TransactionalExecutor>();
        
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<TexasPortal.Domain.Common.IDocumentNumberGenerator, DocumentNumberGenerator>();
        services.AddScoped<IAgentsApiClient, AgentsApiClient>();
        services.AddScoped<TexasPortal.Application.Sales.Services.ISupplierCostReconciliationService, TexasPortal.Application.Sales.Services.SupplierCostReconciliationService>();
        services.AddScoped<TexasPortal.Application.Sales.Profit.ISalesmanProfitService, TexasPortal.Application.Sales.Profit.SalesmanProfitService>();
        services.AddScoped<IOperationalAssignmentClient, OperationalAssignmentClient>();
        services.AddScoped<ISalesReadService, SalesReadService>();
        services.AddScoped<IOperationsReadService, OperationsReadService>();
        services.AddScoped<IOperationsWriteService, OperationsWriteService>();
        services.AddScoped<ISalesEntriesNotificationService, TexasPortal.Infrastructure.Notifications.SalesEntriesNotificationService>();
        services.AddScoped<ILedgerNotificationService, TexasPortal.Infrastructure.Notifications.LedgerNotificationService>();
        services.AddScoped<IWalletNotificationService, TexasPortal.Infrastructure.Notifications.WalletNotificationService>();
        services.AddScoped<IOutstandingNotificationService, TexasPortal.Infrastructure.Notifications.OutstandingNotificationService>();
        services.AddScoped<IReceiptNotificationService, TexasPortal.Infrastructure.Notifications.ReceiptNotificationService>();
        services.AddScoped<IOperationalQueueNotificationService, TexasPortal.Infrastructure.Notifications.OperationalQueueNotificationService>();
        services.AddScoped<IDashboardRefreshCoordinator, TexasPortal.Infrastructure.Notifications.DashboardRefreshCoordinator>();
        services.AddScoped<TexasPortal.Application.Sales.Receivables.ICreditPolicyService, TexasPortal.Application.Sales.Receivables.CreditPolicyService>();
        services.AddScoped<TexasPortal.Application.Sales.Receivables.IReceivableService, TexasPortal.Application.Sales.Receivables.ReceivableService>();
        services.AddScoped<TexasPortal.Application.Sales.Receivables.IReceiptService, TexasPortal.Application.Sales.Receivables.ReceiptService>();
        services.AddScoped<TexasPortal.Application.Sales.Receivables.IApprovalService, TexasPortal.Application.Sales.Receivables.ApprovalService>();
        services.AddScoped<TexasPortal.Application.Sales.Receivables.IStatementService, TexasPortal.Application.Sales.Receivables.StatementService>();
        services.AddScoped<TexasPortal.Application.Sales.Receivables.IReportingService, TexasPortal.Application.Sales.Receivables.ReportingService>();

        services.AddScoped<TexasPortal.Domain.Services.IPaymentAllocationEngine, TexasPortal.Domain.Services.PaymentAllocationEngine>();
        services.AddScoped<TexasPortal.Application.Sales.Accounts.ICustomerAccountQueryService, TexasPortal.Application.Sales.Accounts.CustomerAccountQueryService>();
        services.AddScoped<TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService, TexasPortal.Application.Sales.Accounts.CustomerAccountCommandService>();
        services.AddScoped<TexasPortal.Application.Sales.Accounts.ICustomerAccountService, TexasPortal.Application.Sales.Accounts.CustomerAccountCommandService>();
        services.AddScoped<TexasPortal.Application.Sales.Accounts.ICustomerAccountProjectionService, TexasPortal.Application.Sales.Accounts.CustomerAccountProjectionService>();

        services.AddScoped<TexasPortal.Application.Sales.Receipts.Approval.IAutomaticReceiptAllocationService, TexasPortal.Application.Sales.Receipts.Approval.AutomaticReceiptAllocationService>();
        services.AddScoped<TexasPortal.Application.Sales.Receipts.Approval.ReceiptApprovalOrchestrator>();

        services.AddScoped<CreateOtherServiceBookingHandler>();
        services.AddScoped<CreateHotelBookingHandler>();
        services.AddScoped<CreateFlightBookingHandler>();
        services.AddScoped<CreateVisaBookingHandler>();
        services.AddScoped<CreateVfsAppointmentHandler>();
        services.AddScoped<CreateHolidayPackageHandler>();
        services.AddScoped<CreateTransportationHandler>();
        services.AddScoped<TexasPortal.Application.Sales.Quotations.CreateQuotation.CreateQuotationHandler>();
        services.AddScoped<CreateHeadHandler>();
        services.AddScoped<UpdateHeadHandler>();
        services.AddScoped<CreateLeadHandler>();
        services.AddScoped<UpdateLeadHandler>();
        services.AddScoped<ArchiveLeadHandler>();
        services.AddScoped<CreateReceiptHandler>();
        services.AddScoped<UpdateReceiptStatusHandler>();
        services.AddScoped<ResolveDisputeHandler>();
        services.AddScoped<CreateDisputeHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<CreateUserHandler>();

        // Invoice & Document Framework Services
        services.Configure<TexasPortal.Application.Common.Options.InvoiceOptions>(configuration.GetSection(TexasPortal.Application.Common.Options.InvoiceOptions.SectionName));
        services.Configure<TexasPortal.Application.Features.Documents.Models.DocumentOptions>(configuration.GetSection("DocumentOptions"));

        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.VisaInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.FlightInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.HotelInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.HolidayInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.TransportInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.VfsInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.OtherServiceInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Builders.IInvoiceBuilder, TexasPortal.Application.Features.Invoices.Builders.ReceiptInvoiceBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Factory.IInvoiceBuilderFactory, TexasPortal.Application.Features.Invoices.Factory.InvoiceBuilderFactory>();
        services.AddScoped<TexasPortal.Application.Features.Invoices.Services.IInvoicePdfService, TexasPortal.Infrastructure.Pdf.InvoicePdfService>();

        // Centralized Enterprise Document Framework
        services.AddScoped<TexasPortal.Application.Features.Documents.Builders.IDocumentBuilder, TexasPortal.Application.Features.Documents.Builders.InvoiceDocumentBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Documents.Builders.IDocumentBuilder, TexasPortal.Application.Features.Documents.Builders.ListReportDocumentBuilder>();
        services.AddScoped<TexasPortal.Application.Features.Documents.Factory.IDocumentBuilderFactory, TexasPortal.Application.Features.Documents.Factory.DocumentBuilderFactory>();
        services.AddScoped<TexasPortal.Application.Features.Documents.Services.IDocumentPdfService, TexasPortal.Infrastructure.Pdf.DocumentPdfService>();

        return services;
    }
}

