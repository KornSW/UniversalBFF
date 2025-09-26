
namespace UniversalBFF {

  public interface ITenancyProvider {

    string[] GetAllTenantNames();

    string[] GetAllTenantNamesAssignedToProduct(string productTecchnicalName);

    bool IsTenantAssignedToProduct(string tenantName, string productTecchnicalName);

  }

}
