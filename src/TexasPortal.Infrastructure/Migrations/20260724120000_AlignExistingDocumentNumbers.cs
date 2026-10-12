using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    public partial class AlignExistingDocumentNumbers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Part A: Realign existing document numbers to match current database IDs
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sales_visa_applications')
                    UPDATE sales_visa_applications SET document_number = 'TXSVISA' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'flight_bookings')
                    UPDATE flight_bookings SET document_number = 'TXSFLT' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'hotel_bookings')
                    UPDATE hotel_bookings SET document_number = 'TXSHOTL' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'holiday_packages')
                    UPDATE holiday_packages SET document_number = 'TXSHOL' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'transportations')
                    UPDATE transportations SET document_number = 'TXSTRANS' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'vfs_appointments')
                    UPDATE vfs_appointments SET document_number = 'TXSVFS' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'other_services')
                    UPDATE other_services SET document_number = 'TXSOTHR' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'receipts')
                    UPDATE receipts SET document_number = 'TXSRCPT' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'deposits')
                    UPDATE deposits SET document_number = 'TXSDEP' + FORMAT(id, 'D6');

                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'disputes')
                    UPDATE disputes SET document_number = 'TXSDISP' + FORMAT(id, 'D6');

                -- Part B: Restart SQL Sequences to continue seamlessly after MAX(id)
                DECLARE @nextId BIGINT;

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_SalesVisaDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM sales_visa_applications;
                    EXEC('ALTER SEQUENCE dbo.Seq_SalesVisaDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_FlightDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM flight_bookings;
                    EXEC('ALTER SEQUENCE dbo.Seq_FlightDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_HotelDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM hotel_bookings;
                    EXEC('ALTER SEQUENCE dbo.Seq_HotelDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_HolidayDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM holiday_packages;
                    EXEC('ALTER SEQUENCE dbo.Seq_HolidayDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_TransportDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM transportations;
                    EXEC('ALTER SEQUENCE dbo.Seq_TransportDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_VfsDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM vfs_appointments;
                    EXEC('ALTER SEQUENCE dbo.Seq_VfsDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_OtherServiceDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM other_services;
                    EXEC('ALTER SEQUENCE dbo.Seq_OtherServiceDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_ReceiptDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM receipts;
                    EXEC('ALTER SEQUENCE dbo.Seq_ReceiptDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_DepositDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM deposits;
                    EXEC('ALTER SEQUENCE dbo.Seq_DepositDocumentNo RESTART WITH ' + @nextId);
                END

                IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_DisputeDocumentNo')
                BEGIN
                    SELECT @nextId = ISNULL(MAX(id), 0) + 1 FROM disputes;
                    EXEC('ALTER SEQUENCE dbo.Seq_DisputeDocumentNo RESTART WITH ' + @nextId);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

