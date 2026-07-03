using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprovalFlow.Intake.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "intake");

            migrationBuilder.CreateTable(
                name: "SubmittedInvoices",
                schema: "intake",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Vendor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    VendorKnown = table.Column<bool>(type: "boolean", nullable: false),
                    Submitter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceiptPresent = table.Column<bool>(type: "boolean", nullable: false),
                    Attendees = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LineItemsJson = table.Column<string>(type: "text", nullable: false),
                    DedupKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDuplicate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedInvoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedInvoices_DedupKey",
                schema: "intake",
                table: "SubmittedInvoices",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedInvoices_TrackingId",
                schema: "intake",
                table: "SubmittedInvoices",
                column: "TrackingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmittedInvoices",
                schema: "intake");
        }
    }
}
