using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary> EntityFramework DbContext (based on schema version '1.0.0') </summary>
  public partial class UserManagementDbContext : DbContext{

    public const String SchemaVersion = "1.0.0";

    public DbSet<ActiveRefreshTokenEntity> ActiveRefreshTokens { get; set; }

    public DbSet<LocalCredentialEntity> LocalCredentials { get; set; }

    /// <summary> ApiOAuthClient: ClientCredentialFlow ONLY (non-interactive) </summary>
    public DbSet<ApiOAuthClientEntity> ApiOAuthClients { get; set; }

    public DbSet<CachedUserIdentityEntity> CachedUserIdentities { get; set; }

    public DbSet<OAuthProxyTargetEntity> OAuthProxyTargets { get; set; }

    public DbSet<KnownUserLegitimationEntity> KnownUserLegitimations { get; set; }

    public DbSet<RoleEntity> Roles { get; set; }

    public DbSet<TenantScopeEntity> TenantScopes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      base.OnModelCreating(modelBuilder);

#region Mapping

      modelBuilder.HasDefaultSchema("sec");

      //////////////////////////////////////////////////////////////////////////////////////
      // ActiveRefreshToken
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgActiveRefreshToken = modelBuilder.Entity<ActiveRefreshTokenEntity>();
      cfgActiveRefreshToken.ToTable("ActiveRefreshTokens");
      cfgActiveRefreshToken.HasKey((e) => e.Token);
      cfgActiveRefreshToken.Property((e) => e.Token).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      // LOOKUP: >>> LocalCredential
      cfgActiveRefreshToken
        .HasOne((lcl) => lcl.LocalCredential )
        .WithMany((rem) => rem.ActiveRefreshTokens )
        .HasForeignKey(nameof(ActiveRefreshTokenEntity.SubjectId))
        .OnDelete(DeleteBehavior.Restrict);

      //////////////////////////////////////////////////////////////////////////////////////
      // LocalCredential
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgLocalCredential = modelBuilder.Entity<LocalCredentialEntity>();
      cfgLocalCredential.ToTable("LocalCredentials");
      cfgLocalCredential.HasKey((e) => e.SubjectId);
      cfgLocalCredential.Property((e) => e.SubjectId).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      // REFERRER: <<< ApiOAuthClient
      cfgLocalCredential
        .HasMany((lcl) => lcl.OAuthClients )
        .WithOne()
        .HasForeignKey(nameof(ApiOAuthClientEntity.MappedSubjectId))
        .OnDelete(DeleteBehavior.Restrict);

      //////////////////////////////////////////////////////////////////////////////////////
      // ApiOAuthClient
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgApiOAuthClient = modelBuilder.Entity<ApiOAuthClientEntity>();
      cfgApiOAuthClient.ToTable("ApiOAuthClients");
      cfgApiOAuthClient.HasKey((e) => e.ClientId);
      cfgApiOAuthClient.Property((e) => e.ClientId).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      //////////////////////////////////////////////////////////////////////////////////////
      // CachedUserIdentity
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgCachedUserIdentity = modelBuilder.Entity<CachedUserIdentityEntity>();
      cfgCachedUserIdentity.ToTable("CachedUserIdentities");
      cfgCachedUserIdentity.HasKey((e) => new {e.OriginUid, e.OriginSpecificSubjectId});
      cfgCachedUserIdentity.Property((e) => e.OriginUid).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);
      cfgCachedUserIdentity.Property((e) => e.OriginSpecificSubjectId).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      // PRINCIPAL: >>> OAuthProxyTarget
      cfgCachedUserIdentity
        .HasOne((lcl) => lcl.Origin )
        .WithMany()
        .HasForeignKey(nameof(CachedUserIdentityEntity.OriginUid))
        .OnDelete(DeleteBehavior.Cascade);

      //////////////////////////////////////////////////////////////////////////////////////
      // OAuthProxyTarget
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgOAuthProxyTarget = modelBuilder.Entity<OAuthProxyTargetEntity>();
      cfgOAuthProxyTarget.ToTable("OAuthProxyTargets");
      cfgOAuthProxyTarget.HasKey((e) => e.Uid);
      cfgOAuthProxyTarget.Property((e) => e.Uid).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      // LOOKUP: >>> TenantScope
      cfgOAuthProxyTarget
        .HasOne((lcl) => lcl.TenantScope )
        .WithMany()
        .HasForeignKey(nameof(OAuthProxyTargetEntity.TenantUid))
        .OnDelete(DeleteBehavior.Restrict);

      //////////////////////////////////////////////////////////////////////////////////////
      // KnownUserLegitimation
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgKnownUserLegitimation = modelBuilder.Entity<KnownUserLegitimationEntity>();
      cfgKnownUserLegitimation.ToTable("KnownUserLegitimations");
      cfgKnownUserLegitimation.HasKey((e) => new {e.OriginUid, e.OriginSpecificSubjectId, e.RoleName, e.TenantUid});
      cfgKnownUserLegitimation.Property((e) => e.OriginUid).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);
      cfgKnownUserLegitimation.Property((e) => e.OriginSpecificSubjectId).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);
      cfgKnownUserLegitimation.Property((e) => e.RoleName).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);
      cfgKnownUserLegitimation.Property((e) => e.TenantUid).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      // LOOKUP: >>> CachedUserIdentity
      cfgKnownUserLegitimation
        .HasOne((lcl) => lcl.CachedUserIdentity )
        .WithMany()
        .HasForeignKey(nameof(KnownUserLegitimationEntity.OriginUid), nameof(KnownUserLegitimationEntity.OriginSpecificSubjectId))
        .OnDelete(DeleteBehavior.Restrict);

      // PRINCIPAL: >>> Role
      cfgKnownUserLegitimation
        .HasOne((lcl) => lcl.Role )
        .WithMany((rem) => rem.Users )
        .HasForeignKey(nameof(KnownUserLegitimationEntity.RoleName), nameof(KnownUserLegitimationEntity.TenantUid))
        .OnDelete(DeleteBehavior.Cascade);

      //////////////////////////////////////////////////////////////////////////////////////
      // Role
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgRole = modelBuilder.Entity<RoleEntity>();
      cfgRole.ToTable("Roles");
      cfgRole.HasKey((e) => new {e.RoleName, e.TenantUid});
      cfgRole.Property((e) => e.RoleName).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);
      cfgRole.Property((e) => e.TenantUid).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

      // LOOKUP: >>> TenantScope
      cfgRole
        .HasOne((lcl) => lcl.TenantScope )
        .WithMany((rem) => rem.Roles )
        .HasForeignKey(nameof(RoleEntity.TenantUid))
        .OnDelete(DeleteBehavior.Restrict);

      //////////////////////////////////////////////////////////////////////////////////////
      // TenantScope
      //////////////////////////////////////////////////////////////////////////////////////

      var cfgTenantScope = modelBuilder.Entity<TenantScopeEntity>();
      cfgTenantScope.ToTable("TenantScopes");
      cfgTenantScope.HasKey((e) => e.TenantUid);
      cfgTenantScope.Property((e) => e.TenantUid).ValueGeneratedNever().HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.None);

#endregion

      this.OnModelCreatingCustom(modelBuilder);
    }

    partial void OnModelCreatingCustom(ModelBuilder modelBuilder);

    protected override void OnConfiguring(DbContextOptionsBuilder options) {

      //reqires separate nuget-package Microsoft.EntityFrameworkCore.SqlServer
      options.UseSqlServer(_ConnectionString);

      //reqires separate nuget-package Microsoft.EntityFrameworkCore.Proxies
      options.UseLazyLoadingProxies();

      this.OnConfiguringCustom(options);
    }

    partial void OnConfiguringCustom(DbContextOptionsBuilder options);

    public static void Migrate() {
      if (!_Migrated) {
        UserManagementDbContext c = new UserManagementDbContext();
        c.Database.Migrate();
        _Migrated = true;
        c.Dispose();
      }
    }

   private static bool _Migrated = false;

    private static String _ConnectionString = "Server=(localdb)\\MsSqlLocalDb;Database=UserManagementDbContext;Integrated Security=True;Persist Security Info=True;MultipleActiveResultSets=True;";
    public static String ConnectionString {
      set{ _ConnectionString = value;  }
    }

  }

}
