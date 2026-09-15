using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESDEMO.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915100000_AddOutboxLeases")]
    public partial class AddOutboxLeases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LeaseId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_LeaseExpiresAt_NextAttemptAt_OccurredAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "LeaseExpiresAt", "NextAttemptAt", "OccurredAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_LeaseExpiresAt_NextAttemptAt_OccurredAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(name: "LeaseId", table: "OutboxMessages");
            migrationBuilder.DropColumn(name: "LeaseExpiresAt", table: "OutboxMessages");
        }
    }
}


