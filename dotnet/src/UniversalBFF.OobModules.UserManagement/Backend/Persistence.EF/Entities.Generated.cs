using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace UniversalBFF.OobModules.UserManagement {

public class ActiveRefreshTokenEntity {

  [Required]
  public String Token { get; set; }

  [Required]
  public Int64 SubjectId { get; set; }

  [Required]
  public DateTime ValidUntil { get; set; }

  [Required]
  public Int32 FollowUpNumber { get; set; }

  [Lookup]
  public virtual LocalCredentialEntity LocalCredential { get; set; }

}

public class LocalCredentialEntity {

  [Required]
  public Int64 SubjectId { get; set; }

  [Required]
  public String EmailAddress { get; set; }

  [Required]
  public String DisplayName { get; set; }

  [Required]
  public Boolean IsValidated { get; set; }

  [Required]
  public DateTime CreationDate { get; set; }

  [Required]
  public Int32 WrongPasswordCount { get; set; }

  /// <summary> *this field is optional </summary>
  public Nullable<DateTime> LockedUntil { get; set; }

  [Required]
  public String PasswordHash { get; set; }

  /// <summary> *this field is optional </summary>
  public Nullable<DateTime> LastLogonDate { get; set; }

  [Referrer]
  public virtual ObservableCollection<ActiveRefreshTokenEntity> ActiveRefreshTokens { get; set; } = new ObservableCollection<ActiveRefreshTokenEntity>();

  [Referrer]
  public virtual ObservableCollection<ApiOAuthClientEntity> OAuthClients { get; set; } = new ObservableCollection<ApiOAuthClientEntity>();

}

/// <summary> ClientCredentialFlow ONLY (non-interactive) </summary>
public class ApiOAuthClientEntity {

  [Required]
  public Int64 ClientId { get; set; }

  [Required]
  public String ClientSecret { get; set; }

  /// <summary> The Identity, which REPRESENTS the external Actor! </summary>
  [Required]
  public Int64 MappedSubjectId { get; set; }

  [Required]
  public String RedirectUrl { get; set; }

  /// <summary> *this field is optional </summary>
  public Nullable<DateTime> ValidUntil { get; set; }

}

public class CachedUserIdentityEntity {

  [Required]
  public Int64 OriginUid { get; set; }

  [Required]
  public String OriginSpecificSubjectId { get; set; }

  [Required]
  public String CachedEmailAddress { get; set; }

  [Required]
  public String CachedDisplayName { get; set; }

  /// <summary> *this field is optional (use null as value) </summary>
  public Byte[] CachedImage { get; set; }

  /// <summary> special socped like ABL:Admin </summary>
  [Required]
  public String PermittedScopes { get; set; }

  [Required]
  public Boolean Disabled { get; set; }

  [Required]
  public DateTime CreationDate { get; set; }

  [Required]
  public DateTime LastLogonDate { get; set; }

  [Principal]
  public virtual OAuthProxyTargetEntity Origin { get; set; }

}

public class OAuthProxyTargetEntity {

  /// <summary> will be used as 'TokenSourceUid' </summary>
  [Required]
  public Int64 Uid { get; set; }

  /// <summary> 'LocalCredential' </summary>
  [Required]
  public String ClientId { get; set; }

  [Required]
  public String ClientSecret { get; set; }

  /// <summary> Can Contain additional defaul-query-params </summary>
  [Required]
  public String AuthUrl { get; set; }

  /// <summary> Can Contain additional defaul-query-params </summary>
  [Required]
  public String RetrivalUrl { get; set; }

  /// <summary> Optional: represents the implizit assignment (filterability) for cached tenants </summary>
  [Required]
  public Int64 TenantUid { get; set; }

  [Required]
  public String AdditionalParamsJson { get; set; }

  [Required]
  public String ProviderClassName { get; set; }

  [Required]
  public String IntrospectorParamsJson { get; set; }

  [Required]
  public String DisplayLabel { get; set; }

  /// <summary> PNG 64x64 (also data:URL possible!) </summary>
  [Required]
  public String DisplayIconUrl { get; set; }

  [Required]
  public Boolean IframeSupported { get; set; }

  [Lookup]
  public virtual TenantScopeEntity TenantScope { get; set; }

}

public class KnownUserLegitimationEntity {

  [Required]
  public Int64 OriginUid { get; set; }

  [Required]
  public String OriginSpecificSubjectId { get; set; }

  [Required]
  public String RoleName { get; set; }

  [Required]
  public Int64 TenantUid { get; set; }

  [Lookup]
  public virtual CachedUserIdentityEntity CachedUserIdentity { get; set; }

  [Principal]
  public virtual RoleEntity Role { get; set; }

}

public class RoleEntity {

  [Required]
  public String RoleName { get; set; }

  [Required]
  public Int64 TenantUid { get; set; }

  [Required]
  public String RoleDescriptiveLabel { get; set; }

  /// <summary> Role:{RoleName} ABL:XXX </summary>
  [Required]
  public String PermittedScopes { get; set; }

  [Required]
  public Boolean IsDefaultRoleForNewUsers { get; set; }

  [Dependent]
  public virtual ObservableCollection<KnownUserLegitimationEntity> Users { get; set; } = new ObservableCollection<KnownUserLegitimationEntity>();

  [Lookup]
  public virtual TenantScopeEntity TenantScope { get; set; }

}

public class TenantScopeEntity {

  [Required]
  public Int64 TenantUid { get; set; }

  [Required]
  public String DisplayLabel { get; set; }

  /// <summary> Tenant:{TenantUid} </summary>
  [Required]
  public String PermittedScopes { get; set; }

  /// <summary> Semicolon-sep. List of Portfolio-IDs </summary>
  [Required]
  public String AvailablePortfolios { get; set; }

  [Referrer]
  public virtual ObservableCollection<RoleEntity> Roles { get; set; } = new ObservableCollection<RoleEntity>();

}

}
