using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookShopping1.Migrations
{
    /// <inheritdoc />
    public partial class newtableforcustomer5553 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "CustomIdentityUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "CustomIdentityUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
