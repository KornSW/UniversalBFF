using Logging.SmartStandards;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UniversalBFF.OobModules.UserManagement {

  public class LocalCredentialService :
    IOAuthOperationsProvider, IAccessTokenIntrospector, IAccessTokenIssuer, ILocalCredentialManagementService
  {

    #region " Matadata & Config "

    private const string _DefaultIconUrl = "data:image/png;base64,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";

    private Dictionary<string, string> Configuration { get; } = new Dictionary<string, string>();

    public void SetConfigurationValue(string key, string value) {
      if (value == null) {
        value = string.Empty;
      }
      this.Configuration[key] = value;
    }
    public bool TryGetConfigurationValue(string key, out string value) {
      return this.Configuration.TryGetValue(key, out value);
    }

    public string ProviderInvariantName {
      get {
        return "ubff-local";
      }
    }

    public string ProviderDisplayTitle {
      get {
        return "uBFF User Management (self-contained)";
      }
    }

    public string ProviderIconUrl {
      get {
        string configured;
        if (this.Configuration.TryGetValue("provider_icon_url", out configured) && !String.IsNullOrWhiteSpace(configured)) {
          return configured;
        }
        return _DefaultIconUrl;
      }
    }

    /// <summary></summary>
    /// <param name="capabilityName">
    /// Wellknown capabilities are:
    ///   "introspection"
    ///   "refresh_token"
    ///   "id_token"
    ///   "darkmode_url_param"
    ///   "iframe_allowed"
    /// </param>
    public bool HasCapability(string capabilityName) {
      return _SupportedCapabilities.Contains(capabilityName);
    }
    private static string[] _SupportedCapabilities = {
      "introspection", "id_token"
    };

    #endregion

    #region " HttpClient (Lazy) "

    //DUMMY (needed by interface)
    public Func<IOAuthOperationsProvider, HttpClient> HttpClientFactory { get; set; }

    #endregion

    internal bool TryAuthenticate(
      string login, string password, out string message
    ) {

      using (UserManagementDbContext db = new UserManagementDbContext()) {

        LocalCredentialEntity lcl = null;
        if (Int64.TryParse(login, out Int64 numericLogin)) {
          lcl = db.LocalCredentials.Where(c => c.SubjectId == numericLogin).FirstOrDefault();
        }
        else {
          lcl = db.LocalCredentials.Where(c => c.EmailAddress.ToLower() == login.ToLower()).FirstOrDefault();
        }

        if (lcl == null) {
          message = "Wrong credentials";
          SecLogger.LogWarning(2079222383703567401L, 73901, "TryAuthenticate failed: " + message);
          return false;
        }
        else if (!lcl.IsValidated) {
          message = "User has not jet been validated...";
          SecLogger.LogInformation(2079222383703567402L, 73904, "TryAuthenticate failed: " + message);
          return false;
        }
        else if (lcl.LockedUntil.HasValue && lcl.LockedUntil.Value > DateTime.Now) {
          message = $"User is locked until {lcl.LockedUntil.Value.AddMinutes(1):dd.MM HH:mm}, please come back later!";
          SecLogger.LogWarning(2079222383703567403L, 73903, "TryAuthenticate failed: " + message);
          return false;
        }
        else if (lcl.PasswordHash != GetPasswordHash(password)) {

          lcl.WrongPasswordCount = lcl.WrongPasswordCount + 1;

          const int allowedFails = 3;

          if (lcl.WrongPasswordCount > allowedFails) {
            lcl.LockedUntil = DateTime.Now.AddMinutes(5 ^ (lcl.WrongPasswordCount - allowedFails));
            message = $"Wrong credentials! The user will now be locked until {lcl.LockedUntil.Value.AddMinutes(1):dd.MM HH:mm}, please come back later!";
            SecLogger.LogWarning(2079222383703567404L, 73902, "TryAuthenticate failed: " + message);
          }
          else {
            message = "Wrong credentials";
            SecLogger.LogWarning(2079222383703567405L, 73902, "TryAuthenticate failed: " + message);
          }

          db.SaveChanges();
          return false;
        }
        else {

          lcl.WrongPasswordCount = 0;
          lcl.LockedUntil = null;

          SecLogger.LogInformation(2079222383703567403L, 73900, "TryAuthenticate succeeded for local Credential '{login}'", login);

          message = null;
          db.SaveChanges();
          return true;
        }

      }

    }

    internal string GetPasswordHash(string password) {
      using (SHA256 sha = SHA256.Create()) {
        byte[] hashedBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
      }
    }
   

    //#region " DEMO-CONFIG "

    //// In a real world scenario these would be stored in a secure database!

    //internal static readonly string _MyOAuthClientId = "11aa22bb33cc";
    //private static readonly string _MyOAuthClientSecret = "wow!";

    //private static readonly byte[] _MyTotallySecretDemoJwtKey = Encoding.ASCII.GetBytes("TheBigAndMightyFoo");

    //#endregion




    //private IAccessTokenIssuer _JwtIssuer = new LocalJwtIssuer(_MyTotallySecretDemoJwtKey, 5, true);
    //private IAccessTokenIntrospector _JwtIntropector = new LocalJwtIntrospector(_MyTotallySecretDemoJwtKey);



    /*


     wir graifen auch auf die andere tabelle zu um zu prüfen, dass die client-ids (=UID) wiederum auf auf uns zeigen!!,
     dann  sind sie automatisch gültig und wir kommen auch aus das clientsecret (ist gleichzeitig client und server config

     -> ACHTUNG: auch wir packen erstmal nicht die ganzen scopes rein - vllt lediglich die tenantId...






     */

    public bool TryRequestAccessToken(out TokenIssuingResult accessToken) {
      return TryRequestAccessToken(new Dictionary<string, object>(), out accessToken);
    }

    public bool TryRequestAccessToken(Dictionary<string, object> claimsToRequest, out TokenIssuingResult result) {
      throw new NotImplementedException();
    }

    public void IntrospectAccessToken(string rawToken, out bool isActive, out Dictionary<string, object> claims) {
      throw new NotImplementedException();
    }

    // Der LocalCredentialService ist nicht nur "Server", sondern wird intern auch wie ein Client benutzt...
    // Nur deshlab implementiert er auch IOAuthOperationsProvider, denn der BffUserService entscheidet im
    // Rahmen seiner Proxy-Funktio, ob er weiterleitet an Google und Co. oder eben, an den den LocalCredentialService,
    // der über dieses Interface am allerbesten über seine eigene Verwendung auskunft geben kann.
    // Teilweise können wir uns dadurch unnötige Roundtrips sparen...

    public string GenerateEntryUrlForOAuthCodeGrant(string clientId, string redirectUri, bool requestRefreshToken, bool requestIdToken, string state, string[] scopes, Dictionary<string, object> additionalQueryParams = null) {
      //da wir uns die unnötige roundtrips sparen, sollte von ihm hier gar niemand eine url anfragen...
      throw new NotImplementedException();
    }

    public string GenerateEntryUrlForOAuthImplicitGrant(string clientId, string redirectUri, bool requestRefreshToken, bool requestIdToken, string state, string[] scopes, Dictionary<string, object> additionalQueryParams = null) {
      throw new NotImplementedException();
    }

    public bool TryGetTokenFromRedirectedUrl(string finalUrlFromAuthFlow, string clientId, string clientSecret, out TokenIssuingResult result) {
      throw new NotImplementedException();
    }

    public bool TryGetCodeFromRedirectedUrl(string finalUrlFromAuthFlow, out string code, out string finalUrlWithoutQuery) {
      throw new NotImplementedException();
    }

    public bool TryGetAccessTokenViaOAuthCode(string code, string clientId, string clientSecret, string redirectUriAgain, out TokenIssuingResult result, Dictionary<string, object> additionalQueryParams = null) {
      throw new NotImplementedException();
    }

    public bool TryGetAccessTokenViaOAuthClientCredentials(string clientId, string clientSecret, out TokenIssuingResult result, Dictionary<string, object> additionalQueryParams = null) {
      throw new NotImplementedException();
    }

    public bool TryGetAccessTokenViaOAuthRefreshToken(string refreshToken, string clientId, string clientSecret, out TokenIssuingResult result, Dictionary<string, object> additionalQueryParams = null) {
      throw new NotImplementedException();
    }

    public bool TryResolveSubjectAndScopes(string accessToken, out string subject, out string[] scopes, out Dictionary<string, object> additionalClaims) {
      throw new NotImplementedException();
    }

    public bool TryResolveSubjectAndScopes(string accessToken, string idToken, out string subject, out string[] scopes, out Dictionary<string, object> additionalClaims) {
      throw new NotImplementedException();
    }

    public bool TryValidateToken(string accessToken, out bool isActive, out DateTime? validUntil, out string invalidReason) {
      throw new NotImplementedException();
    }

    #region " ILocalCredentialManagementService "

    public void LocalCredentialTest() {
     

    }

    #endregion

  }

}
