using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
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
            if (target.AuthUrl != _OurProxyAuthUrl) {

              targetAuthorizeUrl = target.AuthUrl;
              targetClientId = target.ClientId;
              anonymousSessionId = this.CreateSessionId("=>" + targetUid.ToString());

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

        lock (_LoginsPerSessionId) {

          if (_LoginsPerSessionId.TryGetValue(sid, out string originalClientIdAsLogonName)) { 

            if(
              originalClientIdAsLogonName.StartsWith("=>") &&
              long.TryParse(originalClientIdAsLogonName.Substring(2), out long targetUid)
            ) {

              using (UserManagementDbContext db = new UserManagementDbContext()) {

                OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();

                if (target != null) { 
                

                  IOAuthOperationsProvider oAuthOperations = this.TypeIndexer.GetApplicableTypes<IOAuthOperationsProvider>(true)
                    .Where((t) => t.FullName == target.ProviderClassName)
                    .Select((t) => (IOAuthOperationsProvider)Activator.CreateInstance(t))
                    .FirstOrDefault();

                  if(!oAuthOperations.TryGetAccessTokenViaOAuthCode(
                    codeFromDelegate, target.ClientId, target.ClientSecret, thisRedirectUri,
                    out TokenIssuingResult result
                  )){
                    return false;
                  }

                  if(oAuthOperations.TryResolveSubjectAndScopes(
                    result.access_token, result.id_token,
                    out string resolvedSubject, out string[] scopes, 
                    out Dictionary<string, object> additionalClaims
                  )) {

                    _LoginsPerSessionId[sid] = resolvedSubject;
                    return true;
                  }
                }
              }
            }
          }
        }
      }

      return false;
    }

  }

}
