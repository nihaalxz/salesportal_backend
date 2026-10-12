using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using TexasPortal.Application.Persistence;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Notifications;

public static class NotificationHelper
{
    public static string SmtpServer { get; set; } = "";
    public static int SmtpPort { get; set; } = 465;
    public static string SenderEmail { get; set; } = "";
    public static string SenderPassword { get; set; } = "";
    public static bool EnableSsl { get; set; } = true;
    public static string AccountsPortalBaseUrl { get; set; } = "https://accountstexas.runasp.net";
    public static string AccountsPortalApiKey { get; set; } = "tx_secure_service_key_999_32chars_long";

    public static async Task<string> GetDocumentNumberAsync(
        ISalesPortalDbContext db,
        string serviceType,
        long serviceId,
        CancellationToken ct)
    {
        var type = serviceType?.ToLowerInvariant() ?? "";
        string? docNo = null;

        switch (type)
        {
            case "visa_application":
            case "visa":
            case "visas":
                docNo = await db.SalesVisaApplications.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "vfs_appointment":
            case "vfs":
                docNo = await db.VfsAppointments.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "holiday_package":
            case "holidays":
            case "holiday":
                docNo = await db.HolidayPackages.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "flight_booking":
            case "flights":
            case "flight":
                docNo = await db.FlightBookings.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "hotel_booking":
            case "hotels":
            case "hotel":
                docNo = await db.HotelBookings.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "transportation":
            case "transport":
                docNo = await db.Transportations.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "other_service":
            case "other-services":
            case "other":
                docNo = await db.OtherServices.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
                break;
            case "receipt":
                docNo = await db.Receipts.AsNoTracking().Where(x => x.Id == serviceId).Select(x => x.DocumentNumber ?? x.ReceiptId).FirstOrDefaultAsync(ct);
                break;
        }

        return !string.IsNullOrWhiteSpace(docNo) ? docNo : $"#{serviceId}";
    }

    public static async Task<(long? HeadId, long? AgentId)> GetHeadAndAgentIdsAsync(
        ISalesPortalDbContext db,
        string serviceType,
        long serviceId,
        CancellationToken ct)
    {
        var type = serviceType?.ToLowerInvariant() ?? "";
        switch (type)
        {
            case "visa_application":
            case "visa":
            case "visas":
                var v = await db.SalesVisaApplications.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (v?.HeadId, v?.AgentId);
            case "vfs_appointment":
            case "vfs":
                var vfs = await db.VfsAppointments.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (vfs?.HeadId, vfs?.AgentId);
            case "holiday_package":
            case "holidays":
            case "holiday":
                var h = await db.HolidayPackages.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (h?.HeadId, h?.AgentId);
            case "flight_booking":
            case "flights":
            case "flight":
                var f = await db.FlightBookings.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (f?.HeadId, f?.AgentId);
            case "hotel_booking":
            case "hotels":
            case "hotel":
                var ht = await db.HotelBookings.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (ht?.HeadId, ht?.AgentId);
            case "transportation":
            case "transport":
                var t = await db.Transportations.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (t?.HeadId, t?.AgentId);
            case "other_service":
            case "other-services":
            case "other":
                var o = await db.OtherServices.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (o?.HeadId, o?.AgentId);
            case "receipt":
                var r = await db.Receipts.AsNoTracking().Where(x => x.Id == serviceId).Select(x => new { x.HeadId, x.AgentId }).FirstOrDefaultAsync(ct);
                return (r?.HeadId, r?.AgentId);
        }
        return (null, null);
    }

    public static async Task<(string? Name, string? Email)> GetAgentFromAccountsApiAsync(long agentId, CancellationToken ct)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(4);
            if (!string.IsNullOrWhiteSpace(AccountsPortalApiKey))
            {
                client.DefaultRequestHeaders.Add("X-OPS-KEY", AccountsPortalApiKey);
            }

            var baseUrl = (string.IsNullOrWhiteSpace(AccountsPortalBaseUrl) ? "https://accountstexas.runasp.net" : AccountsPortalBaseUrl).TrimEnd('/');

            try
            {
                using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts1.CancelAfter(TimeSpan.FromSeconds(3));
                var response = await client.GetAsync($"{baseUrl}/api/public/agents/{agentId}", cts1.Token);
                if (response.IsSuccessStatusCode)
                {
                    var agent = await response.Content.ReadFromJsonAsync<AccountsAgentDto>(cancellationToken: cts1.Token);
                    if (agent != null && !string.IsNullOrWhiteSpace(agent.EmailId))
                    {
                        var name = string.Join(" ", new[] { agent.FirstName, agent.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                        return (name, agent.EmailId);
                    }
                }
            }
            catch
            {
                // Fallback to bulk list
            }

            using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts2.CancelAfter(TimeSpan.FromSeconds(3));
            var listResponse = await client.GetAsync($"{baseUrl}/api/public/agents", cts2.Token);
            if (listResponse.IsSuccessStatusCode)
            {
                var agents = await listResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<AccountsAgentDto>>(cancellationToken: cts2.Token);
                var match = agents?.FirstOrDefault(a => a.Id == agentId);
                if (match != null && !string.IsNullOrWhiteSpace(match.EmailId))
                {
                    var name = string.Join(" ", new[] { match.FirstName, match.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                    return (name, match.EmailId);
                }
            }
        }
        catch
        {
            // Silently fallback on API error
        }
        return (null, null);
    }

    private sealed class AccountsAgentDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? EmailId { get; set; }
        public string? PhoneNo { get; set; }
    }

    public static async Task SendHeadEmailNotificationAsync(
        ISalesPortalDbContext db,
        long? headId,
        long? agentId,
        string serviceType,
        long serviceId,
        string status,
        decimal amount,
        string details,
        CancellationToken ct,
        string? documentNumber = null,
        bool isCreation = false)
    {
        try
        {
            if (!headId.HasValue && !agentId.HasValue)
            {
                var (hId, aId) = await GetHeadAndAgentIdsAsync(db, serviceType, serviceId, ct);
                headId = hId;
                agentId = aId;
            }

            string? recipientEmail = null;
            string? recipientName = null;

            // 1. Check local db.Heads table for Head or Agent entity
            Head? head = null;
            if (headId.HasValue)
            {
                head = await db.Heads.AsNoTracking().FirstOrDefaultAsync(h => h.Id == headId.Value && h.DeletedAt == null, ct);
            }
            if (head == null && agentId.HasValue)
            {
                head = await db.Heads.AsNoTracking().FirstOrDefaultAsync(h => h.Id == agentId.Value && h.DeletedAt == null, ct);
            }

            if (head != null && !string.IsNullOrWhiteSpace(head.Email))
            {
                recipientEmail = head.Email.Trim();
                recipientName = string.Join(" ", new[] { head.FirstName, head.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                if (string.IsNullOrWhiteSpace(recipientName))
                {
                    recipientName = string.Equals(head.Type, "agent", StringComparison.OrdinalIgnoreCase) ? "Valued Agent" : "Valued Customer";
                }
            }

            // 2. If email was not resolved from db.Heads, lookup Agent in AccountsPortal API
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                var targetAgentId = agentId ?? headId;
                if (targetAgentId.HasValue)
                {
                    var (apiAgentName, apiAgentEmail) = await GetAgentFromAccountsApiAsync(targetAgentId.Value, ct);
                    if (!string.IsNullOrWhiteSpace(apiAgentEmail))
                    {
                        recipientEmail = apiAgentEmail.Trim();
                        recipientName = !string.IsNullOrWhiteSpace(apiAgentName) ? apiAgentName : "Valued Agent";
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(recipientEmail)) return;

            var headName = recipientName ?? "Valued Customer / Agent";
            var headEmail = recipientEmail;

            var docNo = documentNumber;
            if (string.IsNullOrWhiteSpace(docNo))
            {
                docNo = await GetDocumentNumberAsync(db, serviceType, serviceId, ct);
            }

            var serviceLabel = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(serviceType.Replace("_", " "));
            var statusText = status.ToUpper();
            var eventSubject = isCreation ? $"[Texas Travels] New {serviceLabel} Record (#{docNo})" : $"[Texas Travels] {serviceLabel} Status Update: {statusText} (#{docNo})";

            var capturedHeadName = headName;
            var capturedHeadEmail = headEmail;
            var capturedDocNo = docNo;
            var capturedStatus = status;
            var capturedAmount = amount;
            var capturedDetails = details;
            var capturedSvcType = serviceType;
            var capturedServiceLabel = serviceLabel;
            var capturedSubject = eventSubject;
            var capturedIsCreation = isCreation;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(SmtpServer) || string.IsNullOrWhiteSpace(SenderEmail)) return;

                    var emailMsg = new MimeMessage();
                    emailMsg.From.Add(new MailboxAddress("Texas Travels", SenderEmail));
                    emailMsg.To.Add(new MailboxAddress(capturedHeadName, capturedHeadEmail));
                    emailMsg.Subject = capturedSubject;

                    var actionClass = capturedStatus.ToLower() == "success" || capturedStatus.ToLower() == "approved" || capturedStatus.ToLower() == "completed" 
                        ? "approved" 
                        : (capturedStatus.ToLower() == "rejected" || capturedStatus.ToLower() == "cancelled" ? "rejected" : "other");

                    var headingText = capturedIsCreation ? $"{capturedServiceLabel} Submitted" : $"{capturedServiceLabel} Status: {capturedStatus.ToUpper()}";
                    var introMessage = capturedIsCreation 
                        ? $"Your {capturedServiceLabel.ToLower()} request (#{capturedDocNo}) has been successfully submitted and is currently being processed." 
                        : $"The status of your {capturedServiceLabel.ToLower()} (#{capturedDocNo}) has been updated to {capturedStatus.ToUpper()}.";

                    var bodyBuilder = new BodyBuilder
                    {
                        HtmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>{capturedServiceLabel} Notification</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6; margin: 0; padding: 20px; color: #1f2937; }}
        .card {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
        .logo {{ font-size: 24px; font-weight: bold; color: #2563eb; text-align: center; margin-bottom: 20px; text-transform: uppercase; letter-spacing: 1px; }}
        .title {{ font-size: 20px; font-weight: bold; margin-bottom: 15px; color: #111827; }}
        .status-badge {{ display: inline-block; padding: 6px 12px; border-radius: 9999px; font-size: 13px; font-weight: bold; text-transform: uppercase; margin-bottom: 20px; }}
        .status-approved {{ background-color: #d1fae5; color: #065f46; }}
        .status-rejected {{ background-color: #fee2e2; color: #991b1b; }}
        .status-other {{ background-color: #f3f4f6; color: #374151; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #e5e7eb; font-size: 14px; }}
        .details-table th {{ color: #6b7280; font-weight: 500; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; color: #9ca3af; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""logo"">Texas Travels</div>
        <div class=""title"">{headingText}</div>
        <div class=""status-badge status-{actionClass}"">{capturedStatus.ToUpper()}</div>
        <p>Dear {capturedHeadName},</p>
        <p>{introMessage}</p>
        
        <table class=""details-table"">
            <tr>
                <th>Service Type</th>
                <td>{capturedServiceLabel}</td>
            </tr>
            <tr>
                <th>Document Number</th>
                <td>{capturedDocNo}</td>
            </tr>
            <tr>
                <th>Amount</th>
                <td>{capturedAmount:N3} BHD</td>
            </tr>
            <tr>
                <th>Details</th>
                <td>{capturedDetails}</td>
            </tr>
        </table>
        
        <div class=""footer"">
            &copy; {DateTime.UtcNow.Year} Texas Travels. All rights reserved.<br/>
            This is an automated notification. Please contact Texas Travels support if you have any questions.
        </div>
    </div>
</body>
</html>"
                    };

                    emailMsg.Body = bodyBuilder.ToMessageBody();

                    using var client = new SmtpClient();
                    var secureSocketOption = EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                    await client.ConnectAsync(SmtpServer, SmtpPort, secureSocketOption, CancellationToken.None);
                    if (!string.IsNullOrWhiteSpace(SenderPassword))
                    {
                        await client.AuthenticateAsync(SenderEmail, SenderPassword, CancellationToken.None);
                    }
                    await client.SendAsync(emailMsg, CancellationToken.None);
                    await client.DisconnectAsync(true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SMTP ERROR] Failed to send Head email notification: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NOTIFICATION ERROR] Failed to prepare Head email notification: {ex.Message}");
        }
    }

    public static async Task CreateStatusChangedNotificationAsync(
        ISalesPortalDbContext db,
        long salesmanId,
        string serviceType,
        long serviceId,
        string status,
        decimal amount,
        string details,
        CancellationToken ct,
        string? documentNumber = null)
    {
        var docNo = documentNumber;
        if (string.IsNullOrWhiteSpace(docNo))
        {
            docNo = await GetDocumentNumberAsync(db, serviceType, serviceId, ct);
        }

        var docDisplay = docNo.StartsWith("#") ? docNo : $"#{docNo}";
        var statusLower = status.ToLowerInvariant();
        var action = statusLower == "success" || statusLower == "approved" || statusLower == "completed" 
            ? "approved" 
            : (statusLower == "document_update_requested" ? "document update requested" : "rejected");
        var serviceLabel = serviceType.Replace("_", " ");
        var title = statusLower == "document_update_requested"
            ? $"{serviceLabel} Document Update Requested"
            : $"{serviceLabel} {action}";
        var message = statusLower == "document_update_requested"
            ? $"Operations team requested a document update for {docDisplay}. Note: {details}"
            : $"Your {serviceLabel.ToLower()} {docDisplay} payment request has been {action} by the accountant (Amount: {amount:N3} BHD).";

        var payload = new
        {
            title = title,
            message = message,
            serviceType = serviceType,
            serviceId = serviceId,
            documentNumber = docNo,
            status = status,
            amount = amount,
            details = details,
            createdAt = DateTime.UtcNow
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Type = "BookingPaymentStatusChanged",
            NotifiableType = "User",
            NotifiableId = salesmanId,
            Data = JsonSerializer.Serialize(payload),
            ReadAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Notifications.Add(notification);
        try { await db.SaveChangesAsync(ct); }
        catch (Exception ex) { Console.WriteLine($"[NOTIFICATION ERROR] Failed to save notification: {ex.Message}"); }

        // Also trigger Head email notification (Agent or Customer)
        await SendHeadEmailNotificationAsync(db, null, null, serviceType, serviceId, status, amount, details, ct, docNo, isCreation: false);

        // Pre-fetch user contact info while the DbContext is still alive (request-scoped).
        // Task.Run runs after the request ends and the DbContext will be disposed by then.
        string? recipientEmail = null;
        string? recipientName = null;
        try
        {
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == salesmanId)
                .Select(u => new { u.Email, u.Name })
                .FirstOrDefaultAsync(ct);
            recipientEmail = user?.Email;
            recipientName = user?.Name;
        }
        catch { /* user lookup failed – skip email */ }

        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            var capturedTitle   = title;
            var capturedMessage = message;
            var capturedDocNo   = docNo;
            var capturedStatus  = status;
            var capturedAmount  = amount;
            var capturedDetails = details;
            var capturedSvcType = serviceType;
            var capturedSvcId   = serviceId;
            var capturedName    = recipientName ?? "";
            var capturedEmail   = recipientEmail;

            // Fire-and-forget on a thread pool thread; DbContext is NOT used here.
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendEmailNotificationAsync(
                        capturedName, capturedEmail,
                        capturedTitle, capturedMessage,
                        capturedSvcType, capturedSvcId,
                        capturedDocNo, capturedStatus,
                        capturedAmount, capturedDetails,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SMTP ERROR] Failed to send notification email: {ex.Message}");
                }
            });
        }
    }

    private static async Task SendEmailNotificationAsync(
        string userName,
        string userEmail,
        string title,
        string message,
        string serviceType,
        long serviceId,
        string docNo,
        string status,
        decimal amount,
        string details,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(SmtpServer) || string.IsNullOrWhiteSpace(SenderEmail))
        {
            return; // Not configured
        }

        var emailMsg = new MimeMessage();
        emailMsg.From.Add(new MailboxAddress("Texas Travels", SenderEmail));
        emailMsg.To.Add(new MailboxAddress(userName, userEmail));
        emailMsg.Subject = $"[Texas Travels] {title}";

        var actionClass = status.ToLower() == "success" || status.ToLower() == "approved" || status.ToLower() == "completed" 
            ? "approved" 
            : (status.ToLower() == "rejected" || status.ToLower() == "cancelled" ? "rejected" : "other");
            
        var statusText = status.ToUpper();

        var serviceLabel = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(serviceType.Replace("_", " "));

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Booking Status Update</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6; margin: 0; padding: 20px; color: #1f2937; }}
        .card {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
        .logo {{ font-size: 24px; font-weight: bold; color: #2563eb; text-align: center; margin-bottom: 20px; text-transform: uppercase; letter-spacing: 1px; }}
        .title {{ font-size: 20px; font-weight: bold; margin-bottom: 15px; color: #111827; }}
        .status-badge {{ display: inline-block; padding: 6px 12px; border-radius: 9999px; font-size: 13px; font-weight: bold; text-transform: uppercase; margin-bottom: 20px; }}
        .status-approved {{ background-color: #d1fae5; color: #065f46; }}
        .status-rejected {{ background-color: #fee2e2; color: #991b1b; }}
        .status-other {{ background-color: #f3f4f6; color: #374151; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #e5e7eb; font-size: 14px; }}
        .details-table th {{ color: #6b7280; font-weight: 500; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; color: #9ca3af; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""logo"">Texas Travels</div>
        <div class=""title"">Booking Status Changed</div>
        <div class=""status-badge status-{actionClass}"">{statusText}</div>
        <p>Dear {userName},</p>
        <p>{message}</p>
        
        <table class=""details-table"">
            <tr>
                <th>Service Type</th>
                <td>{serviceLabel}</td>
            </tr>
            <tr>
                <th>Document Number</th>
                <td>{docNo}</td>
            </tr>
            <tr>
                <th>Amount</th>
                <td>{amount:N3} BHD</td>
            </tr>
            <tr>
                <th>Details</th>
                <td>{details}</td>
            </tr>
        </table>
        
        <div class=""footer"">
            &copy; {DateTime.UtcNow.Year} Texas Travels. All rights reserved.<br/>
            This is an automated notification. Please do not reply directly to this email.
        </div>
    </div>
</body>
</html>"
        };

        emailMsg.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOption = EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(SmtpServer, SmtpPort, secureSocketOption, ct);
        if (!string.IsNullOrWhiteSpace(SenderPassword))
        {
            await client.AuthenticateAsync(SenderEmail, SenderPassword, ct);
        }
        await client.SendAsync(emailMsg, ct);
        await client.DisconnectAsync(true, ct);
    }

    public static async Task SendLeadReminderNotificationAsync(
        ISalesPortalDbContext db,
        Lead lead,
        CancellationToken ct)
    {
        var title = $"Lead Follow-up Reminder: {lead.Name}";
        var message = $"Follow-up reminder due for prospect {lead.Name} ({lead.CountryCode} {lead.MobileNo}).";

        var payload = new
        {
            title = title,
            message = message,
            serviceType = "lead_reminder",
            serviceId = lead.Id,
            status = "Pending",
            details = $"Services: {lead.Services ?? "N/A"} | Source: {lead.LeadSource ?? "N/A"}",
            createdAt = DateTime.UtcNow
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString(),
            Type = "LeadReminderDue",
            NotifiableType = "User",
            NotifiableId = lead.CreatedBy,
            Data = JsonSerializer.Serialize(payload),
            ReadAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        try
        {
            if (string.IsNullOrWhiteSpace(SmtpServer) || string.IsNullOrWhiteSpace(SenderEmail))
                return;

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == lead.CreatedBy, ct);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var emailMsg = new MimeMessage();
            emailMsg.From.Add(new MailboxAddress("Texas Travels", SenderEmail));
            emailMsg.To.Add(new MailboxAddress(user.Name, user.Email));
            emailMsg.Subject = $"[Texas Travels] {title}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Lead Follow-up Reminder</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6; margin: 0; padding: 20px; color: #1f2937; }}
        .card {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
        .logo {{ font-size: 24px; font-weight: bold; color: #16a34a; text-align: center; margin-bottom: 20px; text-transform: uppercase; letter-spacing: 1px; }}
        .title {{ font-size: 20px; font-weight: bold; margin-bottom: 15px; color: #111827; }}
        .reminder-badge {{ display: inline-block; padding: 6px 14px; border-radius: 9999px; font-size: 13px; font-weight: bold; background-color: #fee2e2; color: #991b1b; border: 1px solid #fca5a5; margin-bottom: 20px; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
        .details-table th, .details-table td {{ padding: 10px; text-align: left; border-bottom: 1px solid #e5e7eb; font-size: 14px; }}
        .details-table th {{ color: #6b7280; font-weight: 500; }}
        .footer {{ margin-top: 30px; text-align: center; font-size: 12px; color: #9ca3af; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""logo"">Texas Travels</div>
        <div class=""title"">Lead Follow-up Reminder</div>
        <div class=""reminder-badge"">REMINDER DUE TODAY</div>
        <p>Dear {user.Name},</p>
        <p>This is an automated reminder to follow up with your sales prospect <strong>{lead.Name}</strong>.</p>
        
        <table class=""details-table"">
            <tr>
                <th>Prospect Name</th>
                <td><strong>{lead.Name}</strong></td>
            </tr>
            <tr>
                <th>Contact Mobile</th>
                <td>{lead.CountryCode} {lead.MobileNo}</td>
            </tr>
            <tr>
                <th>Lead Source</th>
                <td>{lead.LeadSource ?? "N/A"}</td>
            </tr>
            <tr>
                <th>Status</th>
                <td>{lead.Status}</td>
            </tr>
            <tr>
                <th>Description / Notes</th>
                <td>{lead.Description ?? "No description provided."}</td>
            </tr>
        </table>
        
        <div class=""footer"">
            &copy; {DateTime.UtcNow.Year} Texas Travels. All rights reserved.<br/>
            This is an automated notification. Please do not reply directly to this email.
        </div>
    </div>
</body>
</html>"
            };

            emailMsg.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var secureSocketOption = EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(SmtpServer, SmtpPort, secureSocketOption, ct);
            if (!string.IsNullOrWhiteSpace(SenderPassword))
            {
                await client.AuthenticateAsync(SenderEmail, SenderPassword, ct);
            }
            await client.SendAsync(emailMsg, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SMTP ERROR] Failed to send lead reminder email: {ex.Message}");
        }
    }
}
