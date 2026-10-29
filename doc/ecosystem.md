# UniversalBFF Ecosystem

This document lists the dependencies and consumers that matter for understanding and classifying UniversalBFF. As an open-source repository, it names only open-source dependencies and consumers.

## Outgoing Dependencies

| Dependency | Packages | Role in UniversalBFF |
|---|---|---|
| U-Shell | `UShell.FrontendBundle`, `UShell.FrontendBundle.Mvc`, `UShell.PortfolioDescription`, `UShell.PortfolioHosting.Mvc`, `UShell.ServerCommands.Contract` | Frontend shell that is hosted by the BFF; portfolio/product description and hosting contracts. |
| UJMW | `UJMW.DynamicController`, `UJMW.DynamicClient` | Dynamic exposure of module backend service contracts as HTTP endpoints. |
| SmartStandards AuthTokenHandling | `SmartStandards.AuthTokenHandling`, `SmartStandards.AuthTokenHandling.MvcSupport` | OAuth/token contracts, token issuing/introspection, and the ambient `AccessTokenManager` used as the source of the current identity. |
| SmartStandards Logging | `SmartStandards.Logging`, `SmartStandards.Logging.AspSupport`, `SmartStandards.Logging.Centralized` | Logging via `DevLogger` / `SecLogger`. |
| SmartStandards (other) | `SmartStandards.Essentials`, `SmartStandards.CyclicTriggering.AspNetCore`, `SmartStandards.WebApiDependencies.net10.0` | Shared base functionality and ASP.NET Core integration. |
| SmartAmbience | `SmartAmbience`, `SmartAmbience.MvcSupport` | Ambient context propagation for requests. |
| ComponentDiscovery | `ComponentDiscovery` | Type indexing and instance discovery of module providers and security providers. |
| Snowflake44 | `Snowflake44` | Generation of 64-bit unique identifiers. |
| FUSE-fx | `FUSE-fx.EfSupport`, `FUSE-fx.EntityAnnotations`, `FUSE-fx.ModelDescription`, `FUSE-fx.RepositoryContract` | Entity/repository contracts used by out-of-box modules. |

Framework-level dependencies such as ASP.NET Core, Entity Framework Core, Newtonsoft.Json, Swashbuckle, WebView2 and MSTest are not listed individually.

## Incoming Dependencies (Consumers)

No open-source consumers are documented yet. Applications use UniversalBFF as a host for U-Shell based frontends together with their own module providers.
