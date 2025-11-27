using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversalBFF.OobModules.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class V002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProviderConfigJson",
                schema: "sec",
                table: "OAuthProxyTargets",
                newName: "RetrivalUrl");

            migrationBuilder.RenameColumn(
                name: "Flow",
                schema: "sec",
                table: "OAuthProxyTargets",
                newName: "IntrospectorParamsJson");

            migrationBuilder.AddColumn<string>(
                name: "AvailablePortfolios",
                schema: "sec",
                table: "TenantScopes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalParamsJson",
                schema: "sec",
                table: "OAuthProxyTargets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuthUrl",
                schema: "sec",
                table: "OAuthProxyTargets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayIconUrl",
                schema: "sec",
                table: "OAuthProxyTargets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayLabel",
                schema: "sec",
                table: "OAuthProxyTargets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IframeSupported",
                schema: "sec",
                table: "OAuthProxyTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ActiveRefreshTokens",
                schema: "sec",
                columns: table => new
                {
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubjectId = table.Column<long>(type: "bigint", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FollowUpNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveRefreshTokens", x => x.Token);
                    table.ForeignKey(
                        name: "FK_ActiveRefreshTokens_LocalCredentials_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "sec",
                        principalTable: "LocalCredentials",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveRefreshTokens_SubjectId",
                schema: "sec",
                table: "ActiveRefreshTokens",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveRefreshTokens",
                schema: "sec");

            migrationBuilder.DropColumn(
                name: "AvailablePortfolios",
                schema: "sec",
                table: "TenantScopes");

            migrationBuilder.DropColumn(
                name: "AdditionalParamsJson",
                schema: "sec",
                table: "OAuthProxyTargets");

            migrationBuilder.DropColumn(
                name: "AuthUrl",
                schema: "sec",
                table: "OAuthProxyTargets");

            migrationBuilder.DropColumn(
                name: "DisplayIconUrl",
                schema: "sec",
                table: "OAuthProxyTargets");

            migrationBuilder.DropColumn(
                name: "DisplayLabel",
                schema: "sec",
                table: "OAuthProxyTargets");

            migrationBuilder.DropColumn(
                name: "IframeSupported",
                schema: "sec",
                table: "OAuthProxyTargets");

            migrationBuilder.RenameColumn(
                name: "RetrivalUrl",
                schema: "sec",
                table: "OAuthProxyTargets",
                newName: "ProviderConfigJson");

            migrationBuilder.RenameColumn(
                name: "IntrospectorParamsJson",
                schema: "sec",
                table: "OAuthProxyTargets",
                newName: "Flow");
        }
    }
}
