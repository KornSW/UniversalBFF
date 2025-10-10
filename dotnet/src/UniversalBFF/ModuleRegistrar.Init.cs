using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.SmartStandards;
using System.Text;
using UShell;
using UShell.ServerCommands;
using static System.Formats.Asn1.AsnWriter;

namespace UniversalBFF {

  public abstract partial class ModuleRegistrar {

    protected void EnsurePortfolioIsInitialized() {

      if (_PortfoliosPerName != null) {
        return;
      }
      _PortfoliosPerName = new Dictionary<string, PortfolioDescription>();
      _PortfolioEntries = new List<PortfolioEntry>();

      lock (_PortfoliosPerName) {
        lock (_PortfolioEntries) {

          if (_AutoCreateChooserPortfolio) {
            PortfolioDescription chooserPortfolio = this.CreateChooserPortfolioAndModule(
              out string chooserPortfolioName, out ModuleDescription chooserModule
            );
            _PortfoliosPerName[chooserPortfolioName] = chooserPortfolio;
            this.RegisterModule(chooserModule);
          }

          ProductDefinition[] products = _ProductDefinitionProvider.GetProductDefinitions();

          foreach (ProductDefinition product in products) {

            PortfolioDescription newPortfolio = new PortfolioDescription();

            newPortfolio.ApplicationTitle = product.Title;
            newPortfolio.LogoUrlLight = product.LogoUrlLight;
            newPortfolio.LogoUrlDark = product.LogoUrlDark;
            newPortfolio.LandingWorkspaceName = product.LandingWorkspaceName;

            _PortfoliosPerName[product.TechnicalName] = newPortfolio;

            //prepared entries for the portfolio-chooser (where user selects the product):
            PortfolioEntry newEntry = new PortfolioEntry();

            newEntry.Label = product.Title;
            newEntry.PortfolioUrl = product.TechnicalName + ".portfolio.json";

            newEntry.Tags = new Dictionary<string, string>();
            foreach (var kvp in product.MetaAttributes) {
              //only display User-Visible Attributes (exclude '_'-prefix)!
              if (!kvp.Key.StartsWith("_")) {
                newEntry.Tags[kvp.Key] = kvp.Value;
              }
            }

            //newEntry.LogoUrlLight = product.LogoUrlLight;
            //newEntry.LogoUrlDark = product.LogoUrlDark;

            _PortfolioEntries.Add(newEntry);

            this.ApplySecurityConfigurationToPortfolio(product.TechnicalName, newPortfolio, product.MetaAttributes);

            #region " Application Scopes "

            foreach (IApplicationScopeProvider applicationScopeProvider in _ApplicationScopeProviders) {

              applicationScopeProvider.RegisterScopes(
                product.TechnicalName,
                product.MetaAttributes,
                (ApplicationScopeDefinition scope) => {

                  ApplicationScopeEntry scopeEntry = null;
                  if (newPortfolio.ApplicationScope == null) {
                    newPortfolio.ApplicationScope = Array.Empty<ApplicationScopeEntry>();
                  }
                  else {
                    scopeEntry = newPortfolio.ApplicationScope.Where((s) => s.Name == scope.Name).FirstOrDefault();
                  }
                  if (scopeEntry == null) {
                    scopeEntry = new ApplicationScopeEntry();
                    scopeEntry.Name = scope.Name;
                    scopeEntry.Label = scope.Name;
                    scopeEntry.InitialValue = scope.InitialValue;

                    //for the entry we use the label on purpose, as it is only relevant for the ui
                    if (newEntry.Tags != null && newEntry.Tags.ContainsKey(scope.Label)) {
                      newEntry.Tags[scope.Label] = scope.InitialValue;
                    }

                  }
                  if (!string.IsNullOrWhiteSpace(scope.Label)) {
                    scopeEntry.Label = scope.Label;
                  }

                  //merge
                  MergeScopeValuesAndContraints(
                    scopeEntry,
                    scope.KnownValues,
                    scope.DependentScopeConstraints,
                    scope.DependentScopeNames
                  );

                }
              );

            }

            #endregion

          }

        }//lock entries
      }//lock portfolios

    }

    public PortfolioDescription CreateChooserPortfolioAndModule(
      out string technicalName, out ModuleDescription module   
    ) {

      PortfolioDescription chooserPortfolio = new PortfolioDescription();

      technicalName = "_CHOOSER";
      chooserPortfolio.ApplicationTitle = "Select Product";
      chooserPortfolio.LandingWorkspaceName = "Chooser";

      chooserPortfolio.ModuleDescriptionUrls = new string[] {
        "??" //TODO: wie wirds eingebunden?
      };

      module = new ModuleDescription();
      module.ModuleTitle = chooserPortfolio.ApplicationTitle;
      module.ModuleUid = technicalName;

      //TODO: modul deklarieren

      //...

      Dictionary<string, string> metaAttributes = new  Dictionary<string, string>();
      metaAttributes["_PublicWorkspaces"] = "Chooser";
      metaAttributes["_PublicUsecases"] = "Chooser";

      this.ApplySecurityConfigurationToPortfolio(
        technicalName, chooserPortfolio, metaAttributes
      );

      return chooserPortfolio;
    }

    internal void ApplySecurityConfigurationToPortfolio(
      string productName, PortfolioDescription portfolio, Dictionary<string, string> metaAttributes
    ) {

      #region " Anonymous Access "

      string publicWorkspaces = null;
      string publicUsecases = null;
      string publicCommands = null;

      if (
        metaAttributes != null && (
          metaAttributes.TryGetValue("_PublicWorkspaces", out publicWorkspaces) ||
          metaAttributes.TryGetValue("_PublicUsecases", out publicUsecases) ||
          metaAttributes.TryGetValue("_PublicCommands", out publicCommands)
        )
      ) {

        portfolio.AnonymousAccess = new AnonymousAccessDescription();

        portfolio.AnonymousAccess.RuntimeTagsIfAnonymous = new string[] { "IsAnonymous" };

        if (string.IsNullOrWhiteSpace(publicWorkspaces)) {
          portfolio.AnonymousAccess.AuthIndependentWorkspaces = Array.Empty<string>();
        }
        else {
          portfolio.AnonymousAccess.AuthIndependentWorkspaces = publicWorkspaces.Split(';').Where((v) => v.Trim() != string.Empty).ToArray();
        }

        if (string.IsNullOrWhiteSpace(publicUsecases)) {
          portfolio.AnonymousAccess.AuthIndependentUsecases = Array.Empty<string>();
        }
        else {
          portfolio.AnonymousAccess.AuthIndependentUsecases = publicUsecases.Split(';').Where((v) => v.Trim() != string.Empty).ToArray();
        }

        if (string.IsNullOrWhiteSpace(publicCommands)) {
          portfolio.AnonymousAccess.AuthIndependentCommands = Array.Empty<string>();
        }
        else {
          portfolio.AnonymousAccess.AuthIndependentCommands = publicCommands.Split(';').Where((v) => v.Trim() != string.Empty).ToArray();
        }

      }

      #endregion

      if (_SecurityProvider == null) {
        return;
        //TODO: hier stattdessen einfach fallback auf Filesystem-Provider und cfg aus dem portfolio ziehen!!!
        //      ggf. sogar nach eienr 'fallback-auth.json' suchen, die nur die authentifizierungsdaten enthält
      }

      _SecurityProvider.RegisterAuthTokenSources(
        productName,
        metaAttributes,
        //CALLBACK:
        (string authTokenSourceUid, AuthTokenConfig cfg, bool availableForPrimaryUiLogon)=> {
         
          portfolio.AuthTokenConfigs.Add(authTokenSourceUid, cfg);

          if (portfolio.AuthenticatedAccess == null) {
            portfolio.AuthenticatedAccess = new AuthenticatedAccessDescription {
              PrimaryUiTokenSources = Array.Empty<string>(),
              RuntimeTagsFromTokenScope = new Dictionary<string, string>() //TODO: give a channel for this...
            };
          }
          
          if (availableForPrimaryUiLogon) {
            int oldLength = portfolio.AuthenticatedAccess.PrimaryUiTokenSources.Length;
            string[] newPrimaryUiTokenSources = new string[oldLength + 1];
            Array.Copy(portfolio.AuthenticatedAccess.PrimaryUiTokenSources, newPrimaryUiTokenSources, oldLength);
            newPrimaryUiTokenSources[oldLength] = authTokenSourceUid;
            portfolio.AuthenticatedAccess.PrimaryUiTokenSources = newPrimaryUiTokenSources;
          }

          if (portfolio.AuthTokenConfigs == null) {
            portfolio.AuthTokenConfigs = new Dictionary<string, AuthTokenConfig>();
          }

        } 
      );

      //backward-compatibility (where we had only one 'PrimaryUiTokenSourceUid')
      if (portfolio.AuthenticatedAccess?.PrimaryUiTokenSources != null && portfolio.AuthenticatedAccess.PrimaryUiTokenSources.Length > 0) {
        portfolio.PrimaryUiTokenSourceUid = portfolio.AuthenticatedAccess.PrimaryUiTokenSources[0];
      }

    }

    #region " Merge of Constraints "

    internal static void MergeScopeValuesAndContraints(
      ApplicationScopeEntry scopeEntry, Dictionary<string, string> additionalKnownValues,
      ApplicationScopeValueConstraint[] additionalConstraints, string[] constraintsDependentScopeNames
    ) {

      foreach (var additionalKnownValue in additionalKnownValues) {

        if (scopeEntry.KnownValues.ContainsKey(additionalKnownValue.Key)) {

          //add the label for exisiting entries (if we not yet have one)
          if (scopeEntry.KnownValues[additionalKnownValue.Key] == null) {
            scopeEntry.KnownValues[additionalKnownValue.Key] = additionalKnownValue.Value;
          }

        }
        else {
          scopeEntry.KnownValues.Add(additionalKnownValue.Key, additionalKnownValue.Value);
        }

      }

      if (
        constraintsDependentScopeNames == null || constraintsDependentScopeNames.Length == 0 ||
        additionalConstraints == null || additionalConstraints.Length == 0
      ) {

        //no new constraints to merge

        return;

      }
      else if (
        scopeEntry.DependentScopeNames == null || scopeEntry.DependentScopeNames.Length == 0 ||
        scopeEntry.DependentScopeConstraints == null || scopeEntry.DependentScopeConstraints.Length == 0
      ) {

        //simply set incomming values - no merge needed for the constraints
        scopeEntry.DependentScopeNames = constraintsDependentScopeNames;
        scopeEntry.DependentScopeConstraints = additionalConstraints;

        return;

      }

      string[] combinedNames = scopeEntry.DependentScopeNames.Union(constraintsDependentScopeNames).Distinct().ToArray();
      List<ApplicationScopeValueConstraint> combinedConstraints = new List<ApplicationScopeValueConstraint>();

      Func<string[], string, int> findIndexOf = (
        (string[] array, string target) => {
          for (int i = 0; i < array.Length; i++) {
            if (array[i] == target) {
              return i;
            }
          }
          return -1;
        }
      );

      //we use a anonymous function here, because it allows us to access the 'combinedNames' variable

      Action<ApplicationScopeValueConstraint, string[]> inflate = (
        (ApplicationScopeValueConstraint constraintToMigrate, string[] oldScopeNames) => {

          if (constraintToMigrate.DependentScopeValues == null || constraintToMigrate.DependentScopeValues.Length != oldScopeNames.Length) {
            //ignore this constraint, becauce it is inconsistent (name-array-size != value-array-size)
            return;
          }

          List<string> depValues = new List<string>();

          foreach (string name in combinedNames) {
            int inputIndex = findIndexOf(oldScopeNames, name);
            if (inputIndex < 0) {
              //source specifies no relation to this name -> assume wildcard
              depValues.Add(null);
            }
            else {
              depValues.Add(constraintToMigrate.DependentScopeValues[inputIndex]);
            }
          }

          constraintToMigrate.DependentScopeValues = depValues.ToArray();

        }
      );


      //modify the arrays (exisiting and incomming) to be compliant with the potentially increased number of fields

      foreach (ApplicationScopeValueConstraint alreadyExisitingConstraint in scopeEntry.DependentScopeConstraints) {
        inflate(alreadyExisitingConstraint, scopeEntry.DependentScopeNames);
        combinedConstraints.Add(alreadyExisitingConstraint);
      }
      foreach (ApplicationScopeValueConstraint additionalConstraint in additionalConstraints) {
        inflate(additionalConstraint, constraintsDependentScopeNames);
        combinedConstraints.Add(additionalConstraint);
      }

      //finally overwrite the old fields with the combined arrays
      scopeEntry.DependentScopeNames = combinedNames;
      scopeEntry.DependentScopeConstraints = combinedConstraints.ToArray();
    }

    #endregion

  }

}
