using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyFitness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEntraObjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntraObjectId",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_EntraObjectId",
                table: "users",
                column: "EntraObjectId",
                unique: true,
                filter: "\"EntraObjectId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_EntraObjectId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EntraObjectId",
                table: "users");
        }
    }
}
