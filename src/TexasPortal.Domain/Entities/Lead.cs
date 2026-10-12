using System;
using TexasPortal.Domain.Common;

namespace TexasPortal.Domain.Entities;

public class Lead : ICreatorOwned
{
    public long Id { get; set; }
    public long CreatedBy { get; set; }
    long? ICreatorOwned.CreatedBy => CreatedBy;
    public string Name { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public string MobileNo { get; set; } = null!;
    public string? Services { get; set; } // JSON or serialized array
    public string? LeadSource { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? ReminderDate { get; set; }
    public bool ReminderActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User Creator { get; set; } = null!;
}
