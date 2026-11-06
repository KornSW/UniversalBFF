using System;
using System.Collections.Generic;
using System.Data.Fuse;
using System.Data.Fuse.Convenience;
using System.Data.ModelDescription;
using System.Linq;
using System.Reflection;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Decorator for the repositories exposed by the 'ManagementStore'. It keeps the pure
  /// 'IRepository' contract towards the frontend and is the single place for store-specific behavior:
  ///  * entities are returned as detached plain copies (no lazy-loading proxies, no navigations)
  ///  * secret fields are WRITE-ONLY: they are masked on read, and a masked (or empty) value
  ///    written back keeps the persisted value
  /// </summary>
  internal class ManagementStoreRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {

    /// <summary>
    /// The value, which is delivered instead of a (non-empty) secret.
    /// </summary>
    public const string SecretMask = "********";

    private IRepository<TEntity, TKey> _Inner;
    private Func<TEntity, TKey> _KeyExtractor;
    private PropertyInfo[] _SecretProperties;
    private HashSet<string> _UnwritableSecretFieldNames;

    /// <param name="inner"></param>
    /// <param name="schemaRoot"></param>
    /// <param name="secretFieldNames">Fields, which are never delivered to the client.</param>
    /// <param name="unwritableSecretFieldNames">
    /// Secret fields, which cannot be set by the client at all (only their persisted value can be kept).
    /// </param>
    public ManagementStoreRepository(
      IRepository<TEntity, TKey> inner, SchemaRoot schemaRoot,
      string[] secretFieldNames = null, string[] unwritableSecretFieldNames = null
    ) {
      _Inner = inner;
      _KeyExtractor = ConversionHelper.GetKeyExtractor<TEntity, TKey>(schemaRoot);

      string[] secrets = secretFieldNames ?? Array.Empty<string>();
      _SecretProperties = _ScalarProperties.Where((p) => secrets.Contains(p.Name)).ToArray();

      _UnwritableSecretFieldNames = new HashSet<string>(
        unwritableSecretFieldNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase
      );
    }

    public string GetOriginIdentity() {
      return _Inner.GetOriginIdentity();
    }

    public RepositoryCapabilities GetCapabilities() {
      return _Inner.GetCapabilities();
    }

    #region " Reading "

    public EntityRef<TKey>[] GetEntityRefs(ExpressionTree filter, string[] sortedBy, int limit = 500, int skip = 0) {
      return _Inner.GetEntityRefs(filter, sortedBy, limit, skip);
    }

    public EntityRef<TKey>[] GetEntityRefsBySearchExpression(string searchExpression, string[] sortedBy, int limit = 500, int skip = 0) {
      return _Inner.GetEntityRefsBySearchExpression(searchExpression, sortedBy, limit, skip);
    }

    public EntityRef<TKey>[] GetEntityRefsByKey(TKey[] keysToLoad) {
      return _Inner.GetEntityRefsByKey(keysToLoad);
    }

    public TEntity[] GetEntities(ExpressionTree filter, string[] sortedBy, int limit = 500, int skip = 0) {
      return this.ToClientCopies(_Inner.GetEntities(filter, sortedBy, limit, skip));
    }

    public TEntity[] GetEntitiesBySearchExpression(string searchExpression, string[] sortedBy, int limit = 500, int skip = 0) {
      return this.ToClientCopies(_Inner.GetEntitiesBySearchExpression(searchExpression, sortedBy, limit, skip));
    }

    public TEntity[] GetEntitiesByKey(TKey[] keysToLoad) {
      return this.ToClientCopies(_Inner.GetEntitiesByKey(keysToLoad));
    }

    public Dictionary<string, object>[] GetEntityFields(ExpressionTree filter, string[] includedFieldNames, string[] sortedBy, int limit = 500, int skip = 0) {
      return this.MaskSecrets(_Inner.GetEntityFields(filter, includedFieldNames, sortedBy, limit, skip));
    }

    public Dictionary<string, object>[] GetEntityFieldsBySearchExpression(string searchExpression, string[] includedFieldNames, string[] sortedBy, int limit = 500, int skip = 0) {
      return this.MaskSecrets(_Inner.GetEntityFieldsBySearchExpression(searchExpression, includedFieldNames, sortedBy, limit, skip));
    }

    public Dictionary<string, object>[] GetEntityFieldsByKey(TKey[] keysToLoad, string[] includedFieldNames) {
      return this.MaskSecrets(_Inner.GetEntityFieldsByKey(keysToLoad, includedFieldNames));
    }

    public int CountAll() {
      return _Inner.CountAll();
    }

    public int Count(ExpressionTree filter) {
      //the frontend sends no filter when nothing is filtered, but the 'EfRepository' requires one
      if (filter == null) {
        return _Inner.CountAll();
      }
      return _Inner.Count(filter);
    }

    public int CountBySearchExpression(string searchExpression) {
      return _Inner.CountBySearchExpression(searchExpression);
    }

    public bool ContainsKey(TKey key) {
      return _Inner.ContainsKey(key);
    }

    #endregion

    #region " Writing "

    public Dictionary<string, object> AddOrUpdateEntityFields(Dictionary<string, object> fields) {
      return this.MaskSecrets(_Inner.AddOrUpdateEntityFields(this.RemoveKeptSecrets(fields)));
    }

    public TEntity AddOrUpdateEntity(TEntity entity) {
      this.RestoreKeptSecrets(entity);
      return this.ToClientCopy(_Inner.AddOrUpdateEntity(entity));
    }

    public Dictionary<string, object> TryUpdateEntityFields(Dictionary<string, object> fields) {
      return this.MaskSecrets(_Inner.TryUpdateEntityFields(this.RemoveKeptSecrets(fields)));
    }

    public TEntity TryUpdateEntity(TEntity entity) {
      this.RestoreKeptSecrets(entity);
      return this.ToClientCopy(_Inner.TryUpdateEntity(entity));
    }

    public TKey TryAddEntity(TEntity entity) {
      this.RestoreKeptSecrets(entity);
      return _Inner.TryAddEntity(entity);
    }

    public TKey[] MassupdateByKeys(TKey[] keysToUpdate, Dictionary<string, object> fields) {
      return _Inner.MassupdateByKeys(keysToUpdate, this.RemoveKeptSecrets(fields));
    }

    public TKey[] Massupdate(ExpressionTree filter, Dictionary<string, object> fields) {
      return _Inner.Massupdate(filter, this.RemoveKeptSecrets(fields));
    }

    public TKey[] MassupdateBySearchExpression(string searchExpression, Dictionary<string, object> fields) {
      return _Inner.MassupdateBySearchExpression(searchExpression, this.RemoveKeptSecrets(fields));
    }

    public TKey[] TryDeleteEntities(TKey[] keysToDelete) {
      return _Inner.TryDeleteEntities(keysToDelete);
    }

    public bool TryUpdateKey(TKey currentKey, TKey newKey) {
      return _Inner.TryUpdateKey(currentKey, newKey);
    }

    #endregion

    #region " Secret Handling "

    private static bool IsKeepValue(object value) {
      string stringValue = value as string;
      return (value == null || stringValue == "" || stringValue == SecretMask);
    }

    private void AssertSecretIsWritable(string fieldName) {
      if (_UnwritableSecretFieldNames.Contains(fieldName)) {
        throw new InvalidOperationException(
          $"The field '{fieldName}' cannot be set via this store (only its persisted value can be kept)."
        );
      }
    }

    /// <summary>
    /// For each secret field carrying the mask (or no value): use the persisted value.
    /// </summary>
    private void RestoreKeptSecrets(TEntity entity) {
      if (entity == null || _SecretProperties.Length == 0) {
        return;
      }

      TEntity persisted = null;
      bool persistedLoaded = false;

      foreach (PropertyInfo secretProperty in _SecretProperties) {
        if (!IsKeepValue(secretProperty.GetValue(entity))) {
          this.AssertSecretIsWritable(secretProperty.Name);
          continue;
        }
        if (!persistedLoaded) {
          persisted = _Inner.GetEntitiesByKey(new TKey[] { _KeyExtractor.Invoke(entity) }).FirstOrDefault();
          persistedLoaded = true;
        }
        if (persisted == null) {
          //new record: there is nothing to keep
          secretProperty.SetValue(entity, null);
        }
        else {
          secretProperty.SetValue(entity, secretProperty.GetValue(persisted));
        }
      }
    }

    /// <summary>
    /// Removes each secret field carrying the mask (or no value), so that the persisted value is kept.
    /// </summary>
    private Dictionary<string, object> RemoveKeptSecrets(Dictionary<string, object> fields) {
      if (fields == null || _SecretProperties.Length == 0) {
        return fields;
      }
      Dictionary<string, object> result = new Dictionary<string, object>(fields);
      foreach (PropertyInfo secretProperty in _SecretProperties) {
        string fieldName = result.Keys.FirstOrDefault(
          (k) => k.Equals(secretProperty.Name, StringComparison.OrdinalIgnoreCase)
        );
        if (fieldName == null) {
          continue;
        }
        if (IsKeepValue(result[fieldName])) {
          result.Remove(fieldName);
        }
        else {
          this.AssertSecretIsWritable(secretProperty.Name);
        }
      }
      return result;
    }

    private Dictionary<string, object> MaskSecrets(Dictionary<string, object> fields) {
      if (fields == null || _SecretProperties.Length == 0) {
        return fields;
      }
      foreach (PropertyInfo secretProperty in _SecretProperties) {
        string fieldName = fields.Keys.FirstOrDefault(
          (k) => k.Equals(secretProperty.Name, StringComparison.OrdinalIgnoreCase)
        );
        if (fieldName != null && !IsKeepValue(fields[fieldName])) {
          fields[fieldName] = SecretMask;
        }
      }
      return fields;
    }

    private Dictionary<string, object>[] MaskSecrets(Dictionary<string, object>[] records) {
      if (records == null) {
        return null;
      }
      foreach (Dictionary<string, object> record in records) {
        this.MaskSecrets(record);
      }
      return records;
    }

    #endregion

    #region " Client Copies "

    /// <summary>
    /// The scalar properties of the entity (navigation properties are excluded).
    /// </summary>
    private static readonly PropertyInfo[] _ScalarProperties = typeof(TEntity).GetProperties(
      BindingFlags.Public | BindingFlags.Instance
    ).Where(
      (p) => p.CanRead && p.CanWrite && IsScalarType(p.PropertyType)
    ).ToArray();

    private static bool IsScalarType(Type type) {
      Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
      if (underlyingType.IsPrimitive || underlyingType.IsEnum) {
        return true;
      }
      return (
        underlyingType == typeof(string) || underlyingType == typeof(decimal) ||
        underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) ||
        underlyingType == typeof(TimeSpan) || underlyingType == typeof(Guid) ||
        underlyingType == typeof(byte[])
      );
    }

    /// <summary>
    /// Returns a plain (non-proxy) copy containing only the scalar fields (with masked secrets).
    /// The entities are serialized after their (short living) DbContext was disposed, so the
    /// lazy-loading proxies of the navigation properties must not reach the serializer.
    /// </summary>
    private TEntity ToClientCopy(TEntity entity) {
      if (entity == null) {
        return null;
      }
      TEntity copy = Activator.CreateInstance<TEntity>();
      foreach (PropertyInfo property in _ScalarProperties) {
        property.SetValue(copy, property.GetValue(entity));
      }
      foreach (PropertyInfo secretProperty in _SecretProperties) {
        if (!IsKeepValue(secretProperty.GetValue(copy))) {
          secretProperty.SetValue(copy, SecretMask);
        }
      }
      return copy;
    }

    private TEntity[] ToClientCopies(TEntity[] entities) {
      if (entities == null) {
        return null;
      }
      return entities.Select((e) => this.ToClientCopy(e)).ToArray();
    }

    #endregion

  }

}
