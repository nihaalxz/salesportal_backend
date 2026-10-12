using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    public partial class AddSupplierAndDescriptionToHolidayGovtFees : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'supplier_id')
                BEGIN
                    ALTER TABLE holiday_package_govt_fees ADD supplier_id bigint NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'supplier_name')
                BEGIN
                    ALTER TABLE holiday_package_govt_fees ADD supplier_name nvarchar(255) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'description')
                BEGIN
                    ALTER TABLE holiday_package_govt_fees ADD description nvarchar(500) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'supplier_id')
                BEGIN
                    ALTER TABLE holiday_package_govt_fees DROP COLUMN supplier_id;
                END

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'supplier_name')
                BEGIN
                    ALTER TABLE holiday_package_govt_fees DROP COLUMN supplier_name;
                END

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('holiday_package_govt_fees') AND name = 'description')
                BEGIN
                    ALTER TABLE holiday_package_govt_fees DROP COLUMN description;
                END
            ");
        }
    }
}
