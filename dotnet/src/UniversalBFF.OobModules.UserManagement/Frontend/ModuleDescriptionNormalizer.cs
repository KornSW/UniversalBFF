using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UShell;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// 'ModuleDescription.FromEmbeddedResFile' deserializes with Newtonsoft, so all untyped values
  /// (UnitOfWork defaults, provider arguments, ...) become 'JToken's. The UniversalBFF serves the
  /// 'module.json' via System.Text.Json, which cannot serialize 'JToken's (they would end up as
  /// nested empty arrays). Therefore all untyped values are converted into plain CLR objects.
  /// WORKAROUND: should be solved centrally (UniversalBFF core or UShell.PortfolioDescription).
  /// </summary>
  internal static class ModuleDescriptionNormalizer {

    public static void ConvertJTokensToPlainObjects(ModuleDescription moduleDesc) {

      foreach (UsecaseDescription usecase in moduleDesc.Usecases ?? Enumerable.Empty<UsecaseDescription>()) {
        NormalizeDynamicParamObject(usecase.UnitOfWorkDefaults);
      }

      foreach (StaticUsecaseAssignment assignment in moduleDesc.StaticUsecaseAssignments ?? Enumerable.Empty<StaticUsecaseAssignment>()) {
        NormalizeDynamicParamObject(assignment.InitUnitOfWork);
      }

      foreach (CommandDescription command in moduleDesc.Commands ?? Enumerable.Empty<CommandDescription>()) {
        NormalizeDynamicParamObject(command.InitUnitOfWork);
      }

      foreach (DatastoreDescription datastore in moduleDesc.Datastores ?? Enumerable.Empty<DatastoreDescription>()) {
        datastore.ProviderArguments = ToPlainObject(datastore.ProviderArguments);
      }

      foreach (DatasourceDescription datasource in moduleDesc.Datasources ?? Enumerable.Empty<DatasourceDescription>()) {
        foreach (IDynamicParamObject providerArguments in datasource.ProviderArguments ?? Enumerable.Empty<IDynamicParamObject>()) {
          NormalizeDynamicParamObject(providerArguments);
        }
      }

    }

    private static void NormalizeDynamicParamObject(IDynamicParamObject dynamicParamObject) {
      if (dynamicParamObject == null) {
        return;
      }
      foreach (string key in dynamicParamObject.Keys.ToArray()) {
        dynamicParamObject[key] = ToPlainObject(dynamicParamObject[key]);
      }
    }

    public static object ToPlainObject(object value) {

      JToken token = value as JToken;
      if (token == null) {
        return value;
      }

      if (token is JObject jObject) {
        Dictionary<string, object> result = new Dictionary<string, object>();
        foreach (JProperty property in jObject.Properties()) {
          result[property.Name] = ToPlainObject(property.Value);
        }
        return result;
      }

      if (token is JArray jArray) {
        return jArray.Select((item) => ToPlainObject(item)).ToList();
      }

      if (token is JValue jValue) {
        return jValue.Value;
      }

      return token.ToString();
    }

  }

}
