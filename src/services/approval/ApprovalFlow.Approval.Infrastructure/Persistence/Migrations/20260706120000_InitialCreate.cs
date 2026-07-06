using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprovalFlow.Approval.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "approval");

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Route = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Recommendation = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CitedRulesJson = table.Column<string>(type: "text", nullable: false),
                    FraudDetected = table.Column<bool>(type: "boolean", nullable: false),
                    FraudReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingApprovals",
                schema: "approval",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AgentRecommendation = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CitedRulesJson = table.Column<string>(type: "text", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingApprovals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_TrackingId",
                schema: "approval",
                table: "WorkflowInstances",
                column: "TrackingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingApprovals_TrackingId",
                schema: "approval",
                table: "PendingApprovals",
                column: "TrackingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingApprovals",
                schema: "approval");

            migrationBuilder.DropTable(
                name: "WorkflowInstances",
                schema: "approval");
        }
    }
}
