using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Newtonsoft.Json;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.SmartStandards;
using System.Text;
using UniversalBFF.OobModules.UserManagement.Frontend.Contract;

namespace UniversalBFF.OobModules.UserManagement {

  public partial class BffUserService : IOAuthServiceWithDelegation {

    public bool CodeFlowDelegationRequired(
      string clientId, ref string loginHint,
      out string targetAuthorizeUrl, out string targetClientId, out string anonymousSessionId
    ) {

      if(long.TryParse(clientId, out long targetUid)) {
        using (UserManagementDbContext db = new UserManagementDbContext()) {

          OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();
          if (target != null) {
            if (target.ProviderClassName != typeof(LocalCredentialService).FullName && target.AuthUrl != _OurProxyAuthUrl) {

              targetAuthorizeUrl = target.AuthUrl;
              targetClientId = target.ClientId;
              anonymousSessionId = this.CreateSession("=>" + targetUid.ToString(), target.Uid , target.ProviderClassName);

              return true;
            }
          }

        }
      }

      targetAuthorizeUrl = null;
      targetClientId = null;
      anonymousSessionId = null;

      return false;
    }

    public bool TryHandleCodeflowDelegationResult(
      string codeFromDelegate, string sessionId, string thisRedirectUri
    ) {

      if (long.TryParse(sessionId, out long sid)) {

        lock (_AuthFlowSessions) {

          if (_AuthFlowSessions.TryGetValue(sid, out AuthFlowSession session)) {

            if (
              session.LogonNameOrSubject.StartsWith("=>") &&
              long.TryParse(session.LogonNameOrSubject.Substring(2), out long targetUid)
            ) {

              using (UserManagementDbContext db = new UserManagementDbContext()) {

                OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();

                if (target != null) {

                  IOAuthOperationsProvider oAuthOperations = this.InitializeOAuthOperationsProvider(target);

                  if (!oAuthOperations.TryGetAccessTokenViaOAuthCode(
                    codeFromDelegate, target.ClientId, target.ClientSecret, thisRedirectUri,
                    out TokenIssuingResult result
                  )){

                    SecLogger.LogError(0, 77301, $"Returned from CodeFlow-Delegation (over '{target.DisplayLabel}'), but the token could not be retrieved: {result?.error}");
                    return false;
                  }

                  if(oAuthOperations.TryResolveSubjectAndScopes(
                    result.access_token, result.id_token,
                    out string providerResolvedSubject, out string[] scopes, 
                    out Dictionary<string, object> additionalClaims
                  )) {

                    session.TokenResultFromDelegate = result;
                    session.LogonNameOrSubject = $"{providerResolvedSubject}";
                    session.OriginProviderName = oAuthOperations.ProviderInvariantName;
                    session.OriginUid = target.Uid;
                    session.UserDisplayName = session.LogonNameOrSubject;
                    session.UserEmailAddress = "";
                    session.UserImage = null;

                    SecLogger.LogTrace(0, 77302, $"Successfully returned from CodeFlow-Delegation (over '{target.DisplayLabel}') as identity '{_AuthFlowSessions[sid]}' (subject resolved via provider)");
                    return true;
                  }
                  else {

                    try {

                      _NonValidatingJwkIntrospector.IntrospectAccessToken(result.access_token, out bool dummy, out Dictionary<string, object> introspectedClaims);
                      
                      if (
                        introspectedClaims != null &&
                        introspectedClaims.TryGetValue("sub", out object selfExtractedSubjectClaim) &&
                        !String.IsNullOrWhiteSpace(selfExtractedSubjectClaim as string)
                      ) {

                        session.TokenResultFromDelegate = result;
                        session.LogonNameOrSubject = $"{selfExtractedSubjectClaim}";
                        session.OriginProviderName = oAuthOperations.ProviderInvariantName;
                        session.OriginUid = target.Uid;
                        session.UserDisplayName = session.LogonNameOrSubject;
                        session.UserEmailAddress = "";
                        session.UserImage = null;

                        SecLogger.LogTrace(0, 77303, $"Successfully returned from CodeFlow-Delegation (over '{target.DisplayLabel}') as identity '{_AuthFlowSessions[sid]}' (subject resolved via fallback-JWT introspection)");
                        return true;

                      }
                    }
                    catch{
                      //do nothing - fallback failed because asuming token to be a JWK was wrong...
                    }
                    session.TokenResultFromDelegate = result;
                    session.LogonNameOrSubject = $"SHORT_LIVING_IDENTITY_{MD5(result.access_token)}";
                    session.OriginProviderName = oAuthOperations.ProviderInvariantName;
                    session.OriginUid = target.Uid;
                    session.UserDisplayName = $"({oAuthOperations.ProviderDisplayTitle}-User)";
                    session.UserEmailAddress = "";
                    session.UserImage = null;

                    SecLogger.LogTrace(0, 77304, $"Successfully returned from CodeFlow-Delegation (over '{target.DisplayLabel}') as identity '{_AuthFlowSessions[sid]}' (no subject resolvable)");
                    return true;

                    //SecLogger.LogError($"Impossible to map token (comming from oauth delegation) to a local identity because the subject could not be resolved...");
                    //return false;
                  }
                }
              }
            }
          }
        }
      }

      SecLogger.LogError($"Returned from CodeFlow-Delegation, but session or provider identifiers are invalid!");
      return false;
    }

    private static string MD5(string input) {
      using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++) {
          sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString();
      }
    }

    private static LocalJwtIntrospector _NonValidatingJwkIntrospector = new LocalJwtIntrospector((jwk) => true);

    private IOAuthOperationsProvider InitializeOAuthOperationsProvider(OAuthProxyTargetEntity target) {

      //TODO: Cache instances per targetUid?

      IOAuthOperationsProvider oAuthOperations = (
        this.TypeIndexer.GetApplicableTypes<IOAuthOperationsProvider>(true)
        .Where((t) => t.FullName == target.ProviderClassName)
        .Select((t) => (IOAuthOperationsProvider)Activator.CreateInstance(t))
        .FirstOrDefault()
      );

      if (oAuthOperations == null) {
        oAuthOperations = new Security.AccessTokenHandling.OAuth.OobProviders.GenericOAuthOperationsProvider();
      }

      Dictionary<string, string> additionalParams = null;
      if (!string.IsNullOrWhiteSpace(target.AdditionalParamsJson) && target.AdditionalParamsJson.StartsWith("{")) {
        additionalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(target.AdditionalParamsJson);
      }

      oAuthOperations.ApplyCommonConfigurationValues(
        target.DisplayLabel,
        target.AuthUrl,
        target.RetrivalUrl,
        "", //target.IntrospectorParamsJson,
        supportsIframe: true,
        requestIdToken: false,
        additionalParams
      );

      return oAuthOperations;
    }

  }

}
