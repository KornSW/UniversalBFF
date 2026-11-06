using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Fuse;
using System.Data.Fuse.Ef;
using System.Data.Fuse.Ef.InstanceManagement;
using System.Data.ModelDescription;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Exposes the administrable part of the user management persistence as FUSE store
  /// (one 'ISchemaProvider' + one 'IRepository' per entity), which is consumed by the
  /// 'fuse' datastore declared in the 'module.json' (route pattern 'route'):
  ///   {moduleScopingKey}/api/v1/store/GetSchemaRoot
  ///   {moduleScopingKey}/api/v1/store/{EntityName}/{IRepository-Method}
  /// The schema is the designed model ('UserManagement.Model.json', the same source the
  /// entity classes are generated from), so the entity names are the model names (without
  /// the 'Entity' suffix of the generated classes) and keys/relations are complete.
  /// </summary>
  internal static class ManagementStore {

    internal const string EndpointAlias = "store";

    private const string ModelResourceName = "UniversalBFF.OobModules.UserManagement.UserManagement.Model.json";
    private const string EntityClassNameSuffix = "Entity";

    /// <summary>
    /// The (model-)names of all entities, which are exposed by the store.
    /// NOTE: 'ActiveRefreshToken' is deliberately NOT exposed (contains tokens).
    /// </summary>
    private static readonly HashSet<string> _ExposedEntityNames = new HashSet<string>();

    public static void RegisterTo(IBackendServiceRegistrar registrar, string moduleScopingKey) {

      RegisterRepository<TenantScopeEntity, Int64>(registrar, moduleScopingKey);
      RegisterRepository<RoleEntity, CompositeKey2<String, Int64>>(registrar, moduleScopingKey);
      RegisterRepository<CachedUserIdentityEntity, CompositeKey2<Int64, String>>(registrar, moduleScopingKey);
      RegisterRepository<KnownUserLegitimationEntity, CompositeKey4<Int64, String, String, Int64>>(registrar, moduleScopingKey);

      //OPEN DECISION (epic I3): how passwords are set via the pure IRepository contract
      RegisterRepository<LocalCredentialEntity, Int64>(registrar, moduleScopingKey,
        secretFieldNames: new string[] { nameof(LocalCredentialEntity.PasswordHash) },
        unwritableSecretFieldNames: new string[] { nameof(LocalCredentialEntity.PasswordHash) }
      );
      RegisterRepository<OAuthProxyTargetEntity, Int64>(registrar, moduleScopingKey,
        secretFieldNames: new string[] { nameof(OAuthProxyTargetEntity.ClientSecret) }
      );
      RegisterRepository<ApiOAuthClientEntity, Int64>(registrar, moduleScopingKey,
        secretFieldNames: new string[] { nameof(ApiOAuthClientEntity.ClientSecret) }
      );

      registrar.RegisterUjmwServiceEndpoint<ISchemaProvider>(
        moduleScopingKey, EndpointAlias, () => new ExposedSchemaProvider()
      );

    }

    private static void RegisterRepository<TEntity, TKey>(
      IBackendServiceRegistrar registrar, string moduleScopingKey,
      string[] secretFieldNames = null, string[] unwritableSecretFieldNames = null
    ) where TEntity : class {

      string entityName = GetModelEntityName(typeof(TEntity));

      lock (_ExposedEntityNames) {
        _ExposedEntityNames.Add(entityName);
      }

      registrar.RegisterUjmwServiceEndpoint<IRepository<TEntity, TKey>>(
        moduleScopingKey, $"{EndpointAlias}/{entityName}",
        () => new ManagementStoreRepository<TEntity, TKey>(
          new EfRepository<TEntity, TKey>(CreateContextInstanceProvider(), GetClassBasedSchemaRoot()),
          GetClassBasedSchemaRoot(), secretFieldNames, unwritableSecretFieldNames
        )
      );

    }

    private static IDbContextInstanceProvider CreateContextInstanceProvider() {
      return new ShortLivingDbContextInstanceProvider<UserManagementDbContext>(
        () => new UserManagementDbContext()
      );
    }

    private static string GetModelEntityName(Type entityType) {
      string className = entityType.Name;
      if (className.EndsWith(EntityClassNameSuffix)) {
        return className.Substring(0, className.Length - EntityClassNameSuffix.Length);
      }
      return className;
    }

    #region " Schema "

    private static object _SchemaSyncRoot = new object();
    private static string _RawModelJson = null;
    private static SchemaRoot _ClassBasedSchemaRoot = null;
    private static SchemaRoot _ExposedSchemaRoot = null;

    private static SchemaRoot LoadModelSchemaRoot() {
      lock (_SchemaSyncRoot) {
        if (_RawModelJson == null) {
          using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ModelResourceName)) {
            if (stream == null) {
              throw new Exception($"Embedded resource '{ModelResourceName}' not found.");
            }
            using (StreamReader reader = new StreamReader(stream)) {
              _RawModelJson = reader.ReadToEnd();
            }
          }
        }
        //always a new instance (the callers are modifying it)
        return JsonConvert.DeserializeObject<SchemaRoot>(_RawModelJson);
      }
    }

    /// <summary>
    /// The model schema with the entity names of the generated classes ('{E}Entity'),
    /// as required by the 'EfRepository' (which resolves its schema by 'typeof(TEntity).Name').
    /// </summary>
    private static SchemaRoot GetClassBasedSchemaRoot() {
      lock (_SchemaSyncRoot) {
        if (_ClassBasedSchemaRoot == null) {
          SchemaRoot schemaRoot = LoadModelSchemaRoot();
          foreach (EntitySchema entity in schemaRoot.Entities) {
            entity.Name = entity.Name + EntityClassNameSuffix;
            if (!string.IsNullOrWhiteSpace(entity.InheritedEntityName)) {
              entity.InheritedEntityName = entity.InheritedEntityName + EntityClassNameSuffix;
            }
          }
          foreach (RelationSchema relation in schemaRoot.Relations) {
            relation.PrimaryEntityName = relation.PrimaryEntityName + EntityClassNameSuffix;
            relation.ForeignEntityName = relation.ForeignEntityName + EntityClassNameSuffix;
          }
          _ClassBasedSchemaRoot = schemaRoot;
        }
        return _ClassBasedSchemaRoot;
      }
    }

    /// <summary>
    /// The model schema, reduced to the exposed entities (and the relations between them).
    /// </summary>
    private static SchemaRoot GetExposedSchemaRoot() {
      lock (_SchemaSyncRoot) {
        if (_ExposedSchemaRoot == null) {

          string[] exposedEntityNames;
          lock (_ExposedEntityNames) {
            exposedEntityNames = _ExposedEntityNames.ToArray();
          }

          SchemaRoot schemaRoot = LoadModelSchemaRoot();

          schemaRoot.Entities = schemaRoot.Entities.Where(
            (e) => exposedEntityNames.Contains(e.Name)
          ).ToArray();

          schemaRoot.Relations = schemaRoot.Relations.Where(
            (r) => exposedEntityNames.Contains(r.PrimaryEntityName) && exposedEntityNames.Contains(r.ForeignEntityName)
          ).ToArray();

          _ExposedSchemaRoot = schemaRoot;
        }
        return _ExposedSchemaRoot;
      }
    }

    private class ExposedSchemaProvider : ISchemaProvider {

      public SchemaRoot GetSchemaRoot() {
        return GetExposedSchemaRoot();
      }

    }

    #endregion

  }

}
