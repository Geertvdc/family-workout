using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyFitness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupOwnershipAndInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Set existing groups to have the first admin member as owner
            // This handles the cruijsjes group by setting Geert (first admin) as owner
            migrationBuilder.Sql(@"
                UPDATE groups g
                SET ""OwnerId"" = (
                    SELECT gm.""UserId""
                    FROM group_memberships gm
                    WHERE gm.""GroupId"" = g.""Id"" AND gm.""Role"" = 'Admin'
                    ORDER BY gm.""JoinedAt""
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1 FROM group_memberships gm 
                    WHERE gm.""GroupId"" = g.""Id"" AND gm.""Role"" = 'Admin'
                );
            ");

            migrationBuilder.CreateTable(
                name: "group_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_invites_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_groups_OwnerId",
                table: "groups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_group_invites_GroupId",
                table: "group_invites",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_invites_Token",
                table: "group_invites",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_groups_users_OwnerId",
                table: "groups",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_groups_users_OwnerId",
                table: "groups");

            migrationBuilder.DropTable(
                name: "group_invites");

            migrationBuilder.DropIndex(
                name: "IX_groups_OwnerId",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "groups");
        }
    }
}
