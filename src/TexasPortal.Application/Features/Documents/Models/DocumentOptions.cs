namespace TexasPortal.Application.Features.Documents.Models;

public class DocumentOptions
{
    public string CompanyName { get; set; } = "TEXAS TRAVEL AND TOURISM W.L.L";
    public string LogoPath { get; set; } = "static/texas-logo.png";
    public string CrNumber { get; set; } = "CR: 148457";
    public string VatNumber { get; set; } = "";
    public string OfficeDetails { get; set; } = "SHOP NO. 82, BUILDING 363, ROAD 1705, BLOCK 317, DIPLOMATIC AREA";
    public string Phone { get; set; } = "Tel: +973 77990010";
    public string Email { get; set; } = "info@texastravelsbh.com";
    public string Website { get; set; } = "www.texastravelsbh.com";
    public string Note { get; set; } = "This is a computer-generated official document.";
    public DocumentBankDetails BankDetails { get; set; } = new();
    public string PrimaryColor { get; set; } = "#1b2559";
    public string SecondaryColor { get; set; } = "#475569";
}

public class DocumentBankDetails
{
    public string BankName { get; set; } = "KFH BANK Kuwait Finance House B.S.C. (c)";
    public string AccountName { get; set; } = "TEXAS TRAVEL AND TOURISM W.L.L";
    public string AccountNo { get; set; } = "0016475026001";
    public string Iban { get; set; } = "BH29AUBB00016475026001";
    public string Currency { get; set; } = "BHD";
    public string SwiftCode { get; set; } = "AUBBBHBM";
}
