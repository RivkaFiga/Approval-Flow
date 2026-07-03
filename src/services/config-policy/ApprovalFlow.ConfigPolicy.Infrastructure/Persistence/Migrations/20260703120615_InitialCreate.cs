using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprovalFlow.ConfigPolicy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "config_policy");

            migrationBuilder.CreateTable(
                name: "PolicyDocuments",
                schema: "config_policy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Markdown = table.Column<string>(type: "text", nullable: false),
                    AutonomyCeilingUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AutonomyMinConfidence = table.Column<double>(type: "double precision", nullable: false),
                    BaseCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FxRates",
                schema: "config_policy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RateToBaseCurrency = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FxRates_PolicyDocuments_PolicyDocumentId",
                        column: x => x.PolicyDocumentId,
                        principalSchema: "config_policy",
                        principalTable: "PolicyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnownVendors",
                schema: "config_policy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnownVendors_PolicyDocuments_PolicyDocumentId",
                        column: x => x.PolicyDocumentId,
                        principalSchema: "config_policy",
                        principalTable: "PolicyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_PolicyDocumentId",
                schema: "config_policy",
                table: "FxRates",
                column: "PolicyDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnownVendors_PolicyDocumentId",
                schema: "config_policy",
                table: "KnownVendors",
                column: "PolicyDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_Active",
                schema: "config_policy",
                table: "PolicyDocuments",
                column: "IsActive",
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FxRates",
                schema: "config_policy");

            migrationBuilder.DropTable(
                name: "KnownVendors",
                schema: "config_policy");

            migrationBuilder.DropTable(
                name: "PolicyDocuments",
                schema: "config_policy");
        }
    }
}
