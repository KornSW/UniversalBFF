using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversalBFF.OobModules.UserManagement.Migrations
{
    /// <inheritdoc />
    public partial class V001_Initital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sec");

            migrationBuilder.CreateTable(
                name: "LocalCredentials",
                schema: "sec",
                columns: table => new
                {
                    SubjectId = table.Column<long>(type: "bigint", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsValidated = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WrongPasswordCount = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastLogonDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalCredentials", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "TenantScopes",
                schema: "sec",
                columns: table => new
                {
                    TenantUid = table.Column<long>(type: "bigint", nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermittedScopes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantScopes", x => x.TenantUid);
                });

            migrationBuilder.CreateTable(
                name: "ApiOAuthClients",
                schema: "sec",
                columns: table => new
                {
                    ClientId = table.Column<long>(type: "bigint", nullable: false),
                    ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MappedSubjectId = table.Column<long>(type: "bigint", nullable: false),
                    RedirectUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiOAuthClients", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_ApiOAuthClients_LocalCredentials_MappedSubjectId",
                        column: x => x.MappedSubjectId,
                        principalSchema: "sec",
                        principalTable: "LocalCredentials",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OAuthProxyTargets",
                schema: "sec",
                columns: table => new
                {
                    Uid = table.Column<long>(type: "bigint", nullable: false),
                    ProviderClassName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Flow = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantUid = table.Column<long>(type: "bigint", nullable: false),
                    ProviderConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthProxyTargets", x => x.Uid);
                    table.ForeignKey(
                        name: "FK_OAuthProxyTargets_TenantScopes_TenantUid",
                        column: x => x.TenantUid,
                        principalSchema: "sec",
                        principalTable: "TenantScopes",
                        principalColumn: "TenantUid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "sec",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantUid = table.Column<long>(type: "bigint", nullable: false),
                    RoleDescriptiveLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermittedScopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefaultRoleForNewUsers = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => new { x.RoleName, x.TenantUid });
                    table.ForeignKey(
                        name: "FK_Roles_TenantScopes_TenantUid",
                        column: x => x.TenantUid,
                        principalSchema: "sec",
                        principalTable: "TenantScopes",
                        principalColumn: "TenantUid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CachedUserIdentities",
                schema: "sec",
                columns: table => new
                {
                    OriginUid = table.Column<long>(type: "bigint", nullable: false),
                    OriginSpecificSubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CachedEmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CachedDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CachedImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PermittedScopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLogonDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedUserIdentities", x => new { x.OriginUid, x.OriginSpecificSubjectId });
                    table.ForeignKey(
                        name: "FK_CachedUserIdentities_OAuthProxyTargets_OriginUid",
                        column: x => x.OriginUid,
                        principalSchema: "sec",
                        principalTable: "OAuthProxyTargets",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnownUserLegitimations",
                schema: "sec",
                columns: table => new
                {
                    OriginUid = table.Column<long>(type: "bigint", nullable: false),
                    OriginSpecificSubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantUid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownUserLegitimations", x => new { x.OriginUid, x.OriginSpecificSubjectId, x.RoleName, x.TenantUid });
                    table.ForeignKey(
                        name: "FK_KnownUserLegitimations_CachedUserIdentities_OriginUid_OriginSpecificSubjectId",
                        columns: x => new { x.OriginUid, x.OriginSpecificSubjectId },
                        principalSchema: "sec",
                        principalTable: "CachedUserIdentities",
                        principalColumns: new[] { "OriginUid", "OriginSpecificSubjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnownUserLegitimations_Roles_RoleName_TenantUid",
                        columns: x => new { x.RoleName, x.TenantUid },
                        principalSchema: "sec",
                        principalTable: "Roles",
                        principalColumns: new[] { "RoleName", "TenantUid" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiOAuthClients_MappedSubjectId",
                schema: "sec",
                table: "ApiOAuthClients",
                column: "MappedSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KnownUserLegitimations_RoleName_TenantUid",
                schema: "sec",
                table: "KnownUserLegitimations",
                columns: new[] { "RoleName", "TenantUid" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthProxyTargets_TenantUid",
                schema: "sec",
                table: "OAuthProxyTargets",
                column: "TenantUid");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantUid",
                schema: "sec",
                table: "Roles",
                column: "TenantUid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiOAuthClients",
                schema: "sec");

            migrationBuilder.DropTable(
                name: "KnownUserLegitimations",
                schema: "sec");

            migrationBuilder.DropTable(
                name: "LocalCredentials",
                schema: "sec");

            migrationBuilder.DropTable(
                name: "CachedUserIdentities",
                schema: "sec");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "sec");

            migrationBuilder.DropTable(
                name: "OAuthProxyTargets",
                schema: "sec");

            migrationBuilder.DropTable(
                name: "TenantScopes",
                schema: "sec");
        }
    }
}
