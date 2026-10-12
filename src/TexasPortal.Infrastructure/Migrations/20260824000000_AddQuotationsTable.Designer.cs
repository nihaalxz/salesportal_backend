using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TexasPortal.Infrastructure.Persistence;

#nullable disable

namespace TexasPortal.Infrastructure.Migrations
{
    [DbContext(typeof(SalesPortalDbContext))]
    [Migration("20260824000000_AddQuotationsTable")]
    partial class AddQuotationsTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0");
#pragma warning restore 612, 618
        }
    }
}
