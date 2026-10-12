using System;

namespace TexasPortal.Domain.Entities;

public class HolidayPackageDocument
{
    public long Id { get; set; }
    public long HolidayPackageId { get; set; }
    public string DocumentPath { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public HolidayPackage HolidayPackage { get; set; } = null!;
}
