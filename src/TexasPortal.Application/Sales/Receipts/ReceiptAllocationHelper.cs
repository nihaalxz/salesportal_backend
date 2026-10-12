using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Receipts;

public static class ReceiptAllocationHelper
{
    public static async Task AllocateReceiptFundsAsync(Receipt receipt, decimal amountToAllocate, ISalesPortalDbContext db)
    {
        if (amountToAllocate <= 0) return;

        // If service is "deposit" or "wallet_deposit", we do nothing here since the wallet was already credited upon receipt approval.
        if (receipt.Service.Equals("wallet_deposit", StringComparison.OrdinalIgnoreCase) || receipt.Service.Equals("deposit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // If specific booking allocation is requested
        if (!string.IsNullOrEmpty(receipt.BookingId) && long.TryParse(receipt.BookingId, out var specificBookingId))
        {
            await AllocateToSpecificBookingAsync(receipt.Service, specificBookingId, amountToAllocate, db);
            return;
        }

        // If FIFO booking allocation (no specific booking ID)
        if (receipt.HeadId.HasValue)
        {
            await AllocateFifoAsync(receipt.Service, receipt.HeadId.Value, amountToAllocate, db);
        }
    }

    private static async Task<decimal> AllocateToSpecificBookingAsync(string service, long bookingId, decimal amount, ISalesPortalDbContext db)
    {
        switch (service.ToLowerInvariant())
        {
            case "flights":
                var flight = await db.FlightBookings.FindAsync(bookingId);
                if (flight != null && flight.PaymentStatus != "paid" && flight.PaymentStatus != "success")
                {
                    if (amount >= flight.PayableAmount)
                    {
                        flight.PaymentStatus = "paid";
                        return amount - flight.PayableAmount;
                    }
                    return 0;
                }
                break;

            case "hotels":
                var hotel = await db.HotelBookings.FindAsync(bookingId);
                if (hotel != null && hotel.PaymentStatus != "paid" && hotel.PaymentStatus != "success")
                {
                    if (amount >= hotel.PayableAmount)
                    {
                        hotel.PaymentStatus = "paid";
                        return amount - hotel.PayableAmount;
                    }
                    return 0;
                }
                break;

            case "visas":
                var visa = await db.SalesVisaApplications.FindAsync(bookingId);
                if (visa != null && visa.PaymentStatus != "paid" && visa.PaymentStatus != "success")
                {
                    if (amount >= visa.PayableAmount)
                    {
                        visa.PaymentStatus = "paid";
                        return amount - visa.PayableAmount;
                    }
                    return 0;
                }
                break;

            case "vfs":
                var vfs = await db.VfsAppointments.FindAsync(bookingId);
                if (vfs != null && vfs.PaymentStatus != "paid" && vfs.PaymentStatus != "success")
                {
                    if (amount >= vfs.PayableAmount)
                    {
                        vfs.PaymentStatus = "paid";
                        return amount - vfs.PayableAmount;
                    }
                    return 0;
                }
                break;

            case "holidays":
                var holiday = await db.HolidayPackages.FindAsync(bookingId);
                if (holiday != null && holiday.PaymentStatus != "paid" && holiday.PaymentStatus != "success")
                {
                    if (amount >= holiday.PayableAmount)
                    {
                        holiday.PaymentStatus = "paid";
                        return amount - holiday.PayableAmount;
                    }
                    return 0;
                }
                break;

            case "transportations":
                var transport = await db.Transportations.FindAsync(bookingId);
                if (transport != null && transport.PaymentStatus != "paid" && transport.PaymentStatus != "success")
                {
                    if (amount >= transport.PayableAmount)
                    {
                        transport.PaymentStatus = "paid";
                        return amount - transport.PayableAmount;
                    }
                    return 0;
                }
                break;

            case "others":
                var other = await db.OtherServices.FindAsync(bookingId);
                if (other != null && other.PaymentStatus != "paid" && other.PaymentStatus != "success")
                {
                    if (amount >= other.PayableAmount)
                    {
                        other.PaymentStatus = "paid";
                        return amount - other.PayableAmount;
                    }
                    return 0;
                }
                break;
        }
        return amount;
    }

    private static async Task<decimal> AllocateFifoAsync(string service, long headId, decimal amount, ISalesPortalDbContext db)
    {
        var remaining = amount;

        switch (service.ToLowerInvariant())
        {
            case "flights":
                var flights = await db.FlightBookings
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success")
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var f in flights)
                {
                    if (remaining >= f.PayableAmount)
                    {
                        remaining -= f.PayableAmount;
                        f.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;

            case "hotels":
                var hotels = await db.HotelBookings
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success")
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var h in hotels)
                {
                    if (remaining >= h.PayableAmount)
                    {
                        remaining -= h.PayableAmount;
                        h.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;

            case "visas":
                var visas = await db.SalesVisaApplications
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success")
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var v in visas)
                {
                    if (remaining >= v.PayableAmount)
                    {
                        remaining -= v.PayableAmount;
                        v.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;

            case "vfs":
                var vfs = await db.VfsAppointments
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success" && x.DeletedAt == null)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var v in vfs)
                {
                    if (remaining >= v.PayableAmount)
                    {
                        remaining -= v.PayableAmount;
                        v.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;

            case "holidays":
                var holidays = await db.HolidayPackages
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success" && x.DeletedAt == null)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var h in holidays)
                {
                    if (remaining >= h.PayableAmount)
                    {
                        remaining -= h.PayableAmount;
                        h.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;

            case "transportations":
                var transports = await db.Transportations
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success" && x.DeletedAt == null)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var t in transports)
                {
                    if (remaining >= t.PayableAmount)
                    {
                        remaining -= t.PayableAmount;
                        t.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;

            case "others":
                var others = await db.OtherServices
                    .Where(x => x.HeadId == headId && x.PaymentStatus != "paid" && x.PaymentStatus != "success" && x.DeletedAt == null)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();
                foreach (var o in others)
                {
                    if (remaining >= o.PayableAmount)
                    {
                        remaining -= o.PayableAmount;
                        o.PaymentStatus = "paid";
                    }
                    else
                    {
                        remaining = 0;
                        break;
                    }
                }
                break;
        }

        return remaining;
    }
}
