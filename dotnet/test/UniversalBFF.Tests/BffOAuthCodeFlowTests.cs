using Microsoft.VisualStudio.TestTools.UnitTesting;
using Security.AccessTokenHandling.OAuth;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Threading;
using System.Threading.Tasks;
using UniversalBFF.OobModules.UserManagement;

namespace UniversalBFF.Tests {

  [TestClass]
  public class BffOAuthCodeFlowTests {

    /// <summary>
    /// Preserves opaque metadata, including legacy null and empty values.
    /// </summary>
    [TestMethod]
    [DataRow("opaque-challenge", "https://api.example.test/resource")]
    [DataRow(null, null)]
    [DataRow("", "")]
    public void CodeRedemptionPreservesBoundValues(string challenge, string resource) {
      BffUserService service = new BffUserService();
      string code = CreateCode(service, challenge, resource);

      TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "secret", code, out string returnedChallenge, out string returnedResource);

      Assert.IsTrue(string.IsNullOrEmpty(result.error));
      Assert.AreEqual("test-token", result.access_token);
      Assert.AreEqual(challenge, returnedChallenge);
      Assert.AreEqual(resource, returnedResource);
      Assert.AreEqual(0, service.PendingAuthorizationCodeCount);
    }

    /// <summary>
    /// An invalid session cannot create a pending authorization code.
    /// </summary>
    [TestMethod]
    public void InvalidSessionDoesNotCreateCode() {
      BffUserService service = new BffUserService();

      bool success = service.TryValidateSessionIdAndCreateRetrievalCode(
        "client-a", "invalid-session", Array.Empty<string>(), "challenge", "resource", out string code, out string message
      );

      Assert.IsFalse(success);
      Assert.IsNull(code);
      Assert.AreEqual("invalid_session", message);
      Assert.AreEqual(0, service.PendingAuthorizationCodeCount);
    }

    /// <summary>
    /// Codes must have a client even when PKCE remains optional.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void MissingClientDoesNotCreateCode(string clientId) {
      BffUserService service = new BffUserService();

      bool success = service.TryValidateSessionIdAndCreateRetrievalCode(
        clientId, "session", Array.Empty<string>(), null, null, out string code, out string message
      );

      Assert.IsFalse(success);
      Assert.IsNull(code);
      Assert.IsFalse(string.IsNullOrEmpty(message));
      Assert.AreEqual(0, service.PendingAuthorizationCodeCount);
    }

    /// <summary>
    /// A successfully redeemed code cannot be reused.
    /// </summary>
    [TestMethod]
    public void RedeemedCodeCannotBeReused() {
      BffUserService service = new BffUserService();
      string code = CreateCode(service, "challenge", "resource");
      service.RetrieveTokenByCode("client-a", "secret", code, out string firstChallenge, out string firstResource);

      TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "secret", code, out string challenge, out string resource);

      Assert.AreEqual("invalid_grant", result.error);
      Assert.IsNull(challenge);
      Assert.IsNull(resource);
    }

    /// <summary>
    /// A different client cannot redeem or retry a code issued to the original client.
    /// </summary>
    [TestMethod]
    public void WrongClientConsumesCodeWithoutExposingMetadata() {
      BffUserService service = new BffUserService();
      string code = CreateCode(service, "challenge", "resource");

      TokenIssuingResult result = service.RetrieveTokenByCode("client-b", "secret", code, out string challenge, out string resource);
      TokenIssuingResult retry = service.RetrieveTokenByCode("client-a", "secret", code, out string retryChallenge, out string retryResource);

      Assert.AreEqual("invalid_grant", result.error);
      Assert.AreEqual("invalid_grant", retry.error);
      Assert.IsNull(challenge);
      Assert.IsNull(resource);
      Assert.IsNull(retryChallenge);
      Assert.IsNull(retryResource);
    }

    /// <summary>
    /// Confidential-client authentication is retained and failed attempts consume the code.
    /// </summary>
    [TestMethod]
    public void InvalidSecretConsumesCodeWithoutExposingMetadata() {
      BffUserService service = new BffUserService();
      string code = CreateCode(service, "challenge", "resource");

      TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "wrong-secret", code, out string challenge, out string resource);
      TokenIssuingResult retry = service.RetrieveTokenByCode("client-a", "secret", code, out string retryChallenge, out string retryResource);

      Assert.AreEqual("invalid_client", result.error);
      Assert.AreEqual("invalid_grant", retry.error);
      Assert.IsNull(challenge);
      Assert.IsNull(resource);
    }

    /// <summary>
    /// Missing, malformed and unknown codes fail without metadata.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not-a-code")]
    [DataRow("123")]
    public void InvalidCodeIsRejected(string code) {
      BffUserService service = new BffUserService();

      TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "secret", code, out string challenge, out string resource);

      Assert.AreEqual("invalid_grant", result.error);
      Assert.IsNull(challenge);
      Assert.IsNull(resource);
    }

    /// <summary>
    /// Expired codes are rejected and their complete entry is removed.
    /// </summary>
    [TestMethod]
    public void ExpiredCodeIsConsumed() {
      BffUserService service = new BffUserService();
      string code = CreateCode(service, "challenge", "resource");
      service.ExpireAuthorizationCodeForTest(code);

      TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "secret", code, out string challenge, out string resource);

      Assert.AreEqual("invalid_grant", result.error);
      Assert.IsNull(challenge);
      Assert.IsNull(resource);
      Assert.AreEqual(0, service.PendingAuthorizationCodeCount);
    }

    /// <summary>
    /// Creating a new code cleans expired entries but keeps live entries intact.
    /// </summary>
    [TestMethod]
    public void CodeCreationCleansExpiredEntriesOnly() {
      BffUserService service = new BffUserService();
      string expiredCode = CreateCode(service, "old-challenge", "old-resource");
      service.ExpireAuthorizationCodeForTest(expiredCode);
      string liveCode = CreateCode(service, "live-challenge", "live-resource");

      Assert.AreEqual(1, service.PendingAuthorizationCodeCount);
      TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "secret", liveCode, out string challenge, out string resource);
      Assert.IsTrue(string.IsNullOrEmpty(result.error));
      Assert.AreEqual("live-challenge", challenge);
      Assert.AreEqual("live-resource", resource);
    }

    /// <summary>
    /// Concurrent redemption has exactly one successful consumer.
    /// </summary>
    [TestMethod]
    public void ConcurrentRedemptionSucceedsOnlyOnce() {
      BffUserService service = new BffUserService();
      string code = CreateCode(service, "challenge", "resource");
      int successes = 0;

      Parallel.For(0, 16, (index) => {
        TokenIssuingResult result = service.RetrieveTokenByCode("client-a", "secret", code, out string challenge, out string resource);
        if (string.IsNullOrEmpty(result.error)) {
          Interlocked.Increment(ref successes);
        }
      });

      Assert.AreEqual(1, successes);
      Assert.AreEqual(0, service.PendingAuthorizationCodeCount);
    }

    /// <summary>
    /// Discovery retains legacy implicit support without advertising unimplemented grants or registration.
    /// </summary>
    [TestMethod]
    public void CapabilitiesDescribeSupportedFlowsOnly() {
      BffUserService service = new BffUserService();

      AuthorizationServerCapabilities capabilities = service.GetAuthorizationServerCapabilities();

      CollectionAssert.AreEqual(new string[] { "code", "token" }, capabilities.ResponseTypesSupported);
      CollectionAssert.AreEqual(new string[] { "authorization_code", "implicit" }, capabilities.GrantTypesSupported);
      CollectionAssert.AreEqual(new string[] { "S256" }, capabilities.CodeChallengeMethodsSupported);
      CollectionAssert.AreEqual(new string[] { "client_secret_post" }, capabilities.TokenEndpointAuthMethodsSupported);
      Assert.AreEqual(0, capabilities.ScopesSupported.Length);
      Assert.IsFalse(capabilities.SupportsDynamicClientRegistration);
    }

    /// <summary>
    /// Unsupported dynamic registration has an explicit RFC error and never issues a client id.
    /// </summary>
    [TestMethod]
    public void DynamicRegistrationIsExplicitlyRejected() {
      BffUserService service = new BffUserService();

      bool success = service.TryRegisterClient(
        "test-client", new string[] { "https://client.example.test/callback" }, "none",
        out string clientId, out string error, out string description
      );

      Assert.IsFalse(success);
      Assert.IsNull(clientId);
      Assert.AreEqual("invalid_client_metadata", error);
      Assert.IsFalse(string.IsNullOrEmpty(description));
    }

    /// <summary>
    /// Creates a code through the production method using a deterministic test login.
    /// </summary>
    private static string CreateCode(BffUserService service, string challenge, string resource) {
      bool success = service.TryValidateSessionIdAndCreateRetrievalCode(
        "client-a", "session", Array.Empty<string>(), challenge, resource, out string code, out string message
      );
      Assert.IsTrue(success, message);
      Assert.IsFalse(string.IsNullOrEmpty(code));
      return code;
    }

  }

}

namespace UniversalBFF.OobModules.UserManagement {

  // Only the production OAuth partial is linked into this test assembly. These stand-ins replace its DB-dependent siblings.
  public partial class BffUserService {

    /// <summary>
    /// Supplies a token without consulting credentials, discovery or external infrastructure.
    /// </summary>
    public bool TryValidateSessionIdAndCreateToken(
      string apiClientId, string sessionId, string[] selectedScopes, out TokenIssuingResult tokenResult
    ) {
      if (sessionId != "session") {
        tokenResult = TokenIssuingResult.FromError("invalid_session", "Invalid test session.");
        return false;
      }
      tokenResult = new TokenIssuingResult { access_token = "test-token" };
      return true;
    }

    /// <summary>
    /// Supplies confidential-client validation without a database.
    /// </summary>
    public bool TryValidateApiClientSecret(string clientId, string clientSecret) {
      return clientSecret == "secret";
    }

    /// <summary>
    /// Expires an actual pending production entry without sleeping or changing production clocks.
    /// </summary>
    internal void ExpireAuthorizationCodeForTest(string code) {
      long retrievalCode = long.Parse(code);
      lock (_TokensPerRetrievalCode) {
        PendingAuthorizationCode pendingCode = _TokensPerRetrievalCode[retrievalCode];
        _TokensPerRetrievalCode[retrievalCode] = new PendingAuthorizationCode(
          pendingCode.ClientId, pendingCode.TokenResult, pendingCode.CodeChallenge, pendingCode.Resource, DateTime.UtcNow.AddMinutes(-1)
        );
      }
    }

    internal int PendingAuthorizationCodeCount {
      get {
        lock (_TokensPerRetrievalCode) {
          return _TokensPerRetrievalCode.Count;
        }
      }
    }

  }

}
