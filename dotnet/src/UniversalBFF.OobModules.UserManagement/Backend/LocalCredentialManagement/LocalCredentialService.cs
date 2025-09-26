using Logging.SmartStandards;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UniversalBFF.OobModules.UserManagement.Frontend.Contract {

  public class LocalCredentialService :
    IOAuthOperationsProvider, IAccessTokenIntrospector, IAccessTokenIssuer
  {
    public Dictionary<string, string> Configuration => throw new NotImplementedException();

    public string ProviderInvariantName => throw new NotImplementedException();

    public string ProviderDisplayTitle => throw new NotImplementedException();

    public string ProviderIconUrl => throw new NotImplementedException();

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


    #region " IOAuthOperationsProvider "

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

    public bool HasCapability(string capabilityName) {
      throw new NotImplementedException();
    }

    #endregion

  }

}
