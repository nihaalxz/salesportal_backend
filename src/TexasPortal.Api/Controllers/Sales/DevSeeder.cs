using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TexasPortal.Domain.Entities;
using TexasPortal.Infrastructure.Persistence;

namespace TexasPortal.Api.Controllers.Sales;

public static class DevSeeder
{
    public static async Task SeedAsync(SalesPortalDbContext db)
    {
        // Safety guard: only seed if the connection string points to localhost or localdb
        var connStr = db.Database.GetConnectionString() ?? "";
        var isLocal = connStr.Contains("localhost") ||
                      connStr.Contains("(localdb)") ||
                      connStr.Contains("127.0.0.1") ||
                      connStr.Contains("SQLEXPRESS");

        var forceSeed = Environment.GetEnvironmentVariable("FORCE_SEED") == "true";
        if (!isLocal && !forceSeed)
        {
            throw new InvalidOperationException("DevSeeder is blocked from running on non-local databases to prevent data loss. Set FORCE_SEED=true to bypass.");
        }

        // Wipe all leaf tables first, then parents
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [idempotency_records]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [disputes]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [notifications]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [head_ledgers]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [flight_passengers]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [flight_bookings]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [hotel_bookings]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [sales_visa_govt_fees]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [sales_visa_applicants]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [sales_visa_applications]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [vfs_appointment_govt_fees]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [vfs_appointments]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [holiday_package_govt_fees]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [holiday_package_documents]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [holiday_packages]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [transportations]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [other_services]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [cash_in_hands]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [deposits]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [leads]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [receipts]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [heads]");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [users]");

        // ── Users (IDs 1, 2, 3) ──────────────────────────────────────────────
        // IMPORTANT: SET IDENTITY_INSERT and SaveChangesAsync MUST share the same
        // connection. An explicit transaction achieves this. SqlServerRetryingExecutionStrategy
        // requires wrapping manual transactions in CreateExecutionStrategy().ExecuteAsync.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [users] ON");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Texas@2024!");

            db.UsersDbSet.Add(new User
            {
                Id = 1,
                Name = "John Salesman",
                Email = "john.sales@texastravelsbh.com",
                UserType = "Salesman",
                IsActive = true,
                Password = hashedPassword,
                MonthlyTarget = 5000,
                ProfitTarget = 1000
            });
            db.UsersDbSet.Add(new User
            {
                Id = 2,
                Name = "Sarah Salesman",
                Email = "sarah.sales@texastravelsbh.com",
                UserType = "Salesman",
                IsActive = true,
                Password = hashedPassword,
                MonthlyTarget = 6000,
                ProfitTarget = 1200
            });
            db.UsersDbSet.Add(new User
            {
                Id = 3,
                Name = "Guest User",
                Email = "guest@texastravelsbh.com",
                UserType = "Guest",
                IsActive = true,
                Password = hashedPassword
            });

            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [users] OFF");
            await tx.CommitAsync();
        });

        // ── Heads (IDs 1, 2) ─────────────────────────────────────────────────
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [heads] ON");

            db.HeadsDbSet.Add(new Head
            {
                Id = 1,
                CreatedBy = 1,   // John
                FirstName = "Alice",
                LastName = "A_Head",
                MobileNo = "97311111111",
                Email = "alice.a@example.com",
                Nationality = "Bahraini",
                Address = "Manama Block 312",
                Wallet = 200.000m,
                Status = "active"
            });
            db.HeadsDbSet.Add(new Head
            {
                Id = 2,
                CreatedBy = 2,   // Sarah
                FirstName = "Bob",
                LastName = "B_Head",
                MobileNo = "97322222222",
                Email = "bob.b@example.com",
                Nationality = "Saudi",
                Address = "Khobar Main St",
                Wallet = 50.000m,
                Status = "active"
            });

            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [heads] OFF");
            await tx.CommitAsync();
        });

        // ── Head Ledgers ──────────────────────────────────────────────────────
        db.HeadLedgersDbSet.Add(new HeadLedger
        {
            HeadId = 1,
            ServiceType = "Flight",
            ServiceId = 1,
            Debit = 250.750m,
            Credit = 0.000m,
            Description = "Flight Ticket Booking TX-FL-90210",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
        db.HeadLedgersDbSet.Add(new HeadLedger
        {
            HeadId = 1,
            ServiceType = "Receipt",
            ServiceId = 1,
            Debit = 0.000m,
            Credit = 100.000m,
            Description = "Deposit payment for flights",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        });
        db.HeadLedgersDbSet.Add(new HeadLedger
        {
            HeadId = 2,
            ServiceType = "Hotel",
            ServiceId = 1,
            Debit = 480.000m,
            Credit = 0.000m,
            Description = "Hotel Booking TX-HT-44810",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });

        // ── Cash In Hand ──────────────────────────────────────────────────────
        db.CashInHandsDbSet.Add(new CashInHand { UserId = 1, Amount = 350.250m });
        db.CashInHandsDbSet.Add(new CashInHand { UserId = 2, Amount = 120.500m });

        // ── Bookings ─────────────────────────────────────────────────────────
        // John: 1 Flight + 1 Hotel.  Payable: 250.75 + 100 = 350.75.  Profit: 25.
        db.FlightBookingsDbSet.Add(new FlightBooking
        {
            UserId = 1,
            HeadId = 1,
            FromLocation = "BAH",
            ToLocation = "DXB",
            PaymentMethod = "cash",
            PaymentStatus = "pending",
            ProcessingStatus = "confirmed",
            PayableAmount = 250.750m,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        db.HotelBookingsDbSet.Add(new HotelBooking
        {
            UserId = 1,
            HeadId = 1,
            GuestName = "Alice",
            Country = "UAE",
            Days = 2,
            CheckInDate = DateTime.UtcNow.AddDays(5),
            PaymentMethod = "cash",
            PaymentStatus = "paid",
            ProcessingStatus = "confirmed",
            OrderStatus = "confirmed",
            PayableAmount = 100.000m,
            Profit = 25.000m,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        // Sarah: 1 Hotel.  Payable: 480.00.  Profit: 80.
        db.HotelBookingsDbSet.Add(new HotelBooking
        {
            UserId = 2,
            HeadId = 2,
            GuestName = "Bob",
            Country = "Bahrain",
            Days = 3,
            CheckInDate = DateTime.UtcNow.AddDays(2),
            PaymentMethod = "bank",
            PaymentStatus = "partial",
            ProcessingStatus = "confirmed",
            OrderStatus = "pending",
            PayableAmount = 480.000m,
            Profit = 80.000m,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        // ── Leads ─────────────────────────────────────────────────────────────
        db.LeadsDbSet.Add(new Lead
        {
            CreatedBy = 1,
            Name = "John's Active Lead",
            CountryCode = "+973",
            MobileNo = "999888777",
            Status = "new",
            CreatedAt = DateTime.UtcNow
        });
        db.LeadsDbSet.Add(new Lead
        {
            CreatedBy = 2,
            Name = "Sarah's Active Lead",
            CountryCode = "+973",
            MobileNo = "888777666",
            Status = "qualified",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
