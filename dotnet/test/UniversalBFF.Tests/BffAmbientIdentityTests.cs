using Microsoft.VisualStudio.TestTools.UnitTesting;
using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using UniversalBFF.OobModules.UserManagement;

namespace UniversalBFF.Tests {

  /// <summary>
  /// Tests for the resolution of the ambient ("current") identity via the AccessTokenManager
  /// and for the authorization helpers used by the IPortfolioSecurityProvider implementation.
  /// </summary>
  [TestClass]
  public class BffAmbientIdentityTests {

    private const string _ValidToken = "valid-token";
    private const string _InactiveToken = "inactive-token";
    private const string _Subject = "4711";
    private const string _Scopes = "Tenant:1111111111111111111 Role:Administrator ABL:UserManagement";

    private string _TokenSourceId;

    /// <summary>
    /// Deterministic introspector which accepts only '_ValidToken'.
    /// </summary>
    private class FakeIntrospector : IAccessTokenIntrospector {

      public void IntrospectAccessToken(string rawToken, out bool isActive, out Dictionary<string, object> claims) {
        claims = new Dictionary<string, object>();
        if (rawToken == _ValidToken) {
          isActive = true;
          claims["sub"] = _Subject;
          claims["scope"] = _Scopes;
          claims["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        }
        else {
          isActive = false;
        }
      }

    }

    /// <summary>
    /// Registers an isolated token source (unique id per test) with the fake introspector.
    /// </summary>
    [TestInitialize]
    public void Initialize() {
      _TokenSourceId = Guid.NewGuid().ToString();
      AccessTokenManager.ConfigureTokenSupply((cfg) => {
        cfg.ApplyCustomTokenSource(_TokenSourceId, null, new FakeIntrospector());
      });
    }

    /// <summary>
    /// Clears the ambient token of the test token source.
    /// </summary>
    [TestCleanup]
    public void Cleanup() {
      AccessTokenManager.SetCurrentToken(_TokenSourceId, null);
    }

    [TestMethod]
    public void TryGetCurrentIdentity_WithoutAmbientToken_FailsClosed() {
      BffUserService service = new BffUserService();
      service.RegisterKnownTokenSourceId(_TokenSourceId);

      bool found = service.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes);

      Assert.IsFalse(found);
      Assert.IsNull(tokenSourceId);
      Assert.IsNull(subject);
      Assert.AreEqual(0, scopes.Length);
    }

    [TestMethod]
    public void TryGetCurrentIdentity_WithAmbientToken_ReturnsSubjectAndScopes() {
      BffUserService service = new BffUserService();
      service.RegisterKnownTokenSourceId(_TokenSourceId);
      AccessTokenManager.SetCurrentToken(_TokenSourceId, _ValidToken);

      bool found = service.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes);

      Assert.IsTrue(found);
      Assert.AreEqual(_TokenSourceId, tokenSourceId);
      Assert.AreEqual(_Subject, subject);
      CollectionAssert.Contains(scopes, "Tenant:1111111111111111111");
      CollectionAssert.Contains(scopes, "ABL:UserManagement");
    }

    [TestMethod]
    public void TryGetCurrentIdentity_TokenOfUnknownSource_IsIgnored() {
      BffUserService service = new BffUserService();
      AccessTokenManager.SetCurrentToken(_TokenSourceId, _ValidToken);

      bool found = service.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes);

      Assert.IsFalse(found);
    }

    [TestMethod]
    public void SetCurrentToken_InactiveToken_IsRejectedAndNoIdentityIsAvailable() {
      BffUserService service = new BffUserService();
      service.RegisterKnownTokenSourceId(_TokenSourceId);

      Assert.ThrowsExactly<ArgumentException>(() => AccessTokenManager.SetCurrentToken(_TokenSourceId, _InactiveToken));

      Assert.IsFalse(service.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes));
    }

    [TestMethod]
    public void TryGetCurrentIdentity_AfterClearingToken_FailsClosed() {
      BffUserService service = new BffUserService();
      service.RegisterKnownTokenSourceId(_TokenSourceId);
      AccessTokenManager.SetCurrentToken(_TokenSourceId, _ValidToken);
      AccessTokenManager.SetCurrentToken(_TokenSourceId, null);

      Assert.IsFalse(service.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes));
    }

    [TestMethod]
    public void ContainsScope_MatchesNameAndValueExactly() {
      string[] scopes = new string[] { "Tenant:1111111111111111111", "Role:Administrator" };

      Assert.IsTrue(BffUserService.ContainsScope(scopes, "Tenant", "1111111111111111111"));
      Assert.IsFalse(BffUserService.ContainsScope(scopes, "Tenant", "2222222222222222222"));
      Assert.IsFalse(BffUserService.ContainsScope(scopes, "tenant", "1111111111111111111"));
      Assert.IsFalse(BffUserService.ContainsScope(scopes, "Role", "Admin"));
    }

    [TestMethod]
    public void ContainsScope_WithoutValue_MatchesAnyValueOfTheName() {
      string[] scopes = new string[] { "Tenant:1111111111111111111", "DefaultTenant" };

      Assert.IsTrue(BffUserService.ContainsScope(scopes, "Tenant", null));
      Assert.IsTrue(BffUserService.ContainsScope(scopes, "DefaultTenant", ""));
      Assert.IsFalse(BffUserService.ContainsScope(scopes, "Role", null));
    }

    [TestMethod]
    public void ContainsScope_InvalidInput_FailsClosed() {
      Assert.IsFalse(BffUserService.ContainsScope(null, "Tenant", "1"));
      Assert.IsFalse(BffUserService.ContainsScope(new string[] { "Tenant:1" }, null, "1"));
      Assert.IsFalse(BffUserService.ContainsScope(new string[] { "Tenant:1" }, " ", "1"));
    }

    [TestMethod]
    public void GetTenantUidsFromScopes_ExtractsDistinctNumericTenantUids() {
      string[] scopes = new string[] { "Tenant:1", "Tenant:2", "Tenant:1", "Tenant:abc", "Role:User", "DefaultTenant" };

      long[] tenantUids = BffUserService.GetTenantUidsFromScopes(scopes);

      CollectionAssert.AreEquivalent(new long[] { 1L, 2L }, tenantUids);
      Assert.AreEqual(0, BffUserService.GetTenantUidsFromScopes(null).Length);
    }

    [TestMethod]
    public void IsProductAvailable_EvaluatesWildcardAndList() {
      Assert.IsTrue(BffUserService.IsProductAvailable("*", "ProductA"));
      Assert.IsTrue(BffUserService.IsProductAvailable("ProductA;ProductB", "productb"));
      Assert.IsTrue(BffUserService.IsProductAvailable(" ProductA ; ProductB ", "ProductB"));
      Assert.IsFalse(BffUserService.IsProductAvailable("ProductA;ProductB", "ProductC"));
      Assert.IsFalse(BffUserService.IsProductAvailable("ProductAB", "ProductA"));
      Assert.IsFalse(BffUserService.IsProductAvailable("", "ProductA"));
      Assert.IsFalse(BffUserService.IsProductAvailable("*", ""));
    }

    [TestMethod]
    public void CanAccessProduct_RequiresAtLeastOneLegitimatedTenantWithTheProduct() {
      Assert.IsTrue(BffUserService.CanAccessProduct(new string[] { "ProductB", "ProductA;ProductC" }, "ProductA"));
      Assert.IsFalse(BffUserService.CanAccessProduct(new string[] { "ProductB" }, "ProductA"));
      Assert.IsFalse(BffUserService.CanAccessProduct(new string[] { }, "ProductA"));
      Assert.IsFalse(BffUserService.CanAccessProduct(null, "ProductA"));
    }

  }

}
