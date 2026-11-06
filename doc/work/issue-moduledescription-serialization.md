# Issue: Module Descriptions Lose Nested Values When Served (NO GO)

Status: **open - classified as NO GO** (must be solved centrally; the module-local compensation is a stopgap only).
Found while moving the User Management `module.json` into the web project (epic finding B-3, [epic](../epics/epic-usermanagement-module-mvp.md)).

## Symptom

The User Management module registers its embedded `module.json` via `ModuleDescription.FromEmbeddedResFile(...)`.
Requesting `GET {BaseUrl}oob-usrmgmt/module.json` returned every **untyped** nested value as nested empty arrays.

Source (`src/module.json`, excerpt):

```json
"unitOfWorkDefaults": {
  "entityName": "CachedUserIdentity",
  "layoutDescription": {
    "entityLayouts": [
      { "entityName": "CachedUserIdentity", "displayLabel": "User", "tableFields": ["CachedDisplayName", "..."] }
    ]
  }
}
```

Delivered to the shell:

```json
"unitOfWorkDefaults": {
  "entityName": "CachedUserIdentity",
  "layoutDescription": {
    "entityLayouts": [[[[]],[[]],[[]],[[]],[[]],[[[],[],[],[],[]]],[[[[[]],[[]]], "..."]]]]
  }
}
```

The data is not lost with an error - it is **silently replaced by structurally plausible garbage**. The shell does not
complain; the widget simply gets no layout. Scalar values on the first level (`entityName`) survive, which makes the
defect easy to overlook.

## Root Cause

Two JSON stacks meet without an agreed boundary:

| Step | Component | Serializer | Effect |
|---|---|---|---|
| 1. read | `UShell.PortfolioDescription` - `ModuleDescription.FromEmbeddedResFile` / `PortfolioDescription.FromEmbeddedResFile` / `PortfolioExtensions` | **Newtonsoft.Json** (`JsonConvert.DeserializeObject<ModuleDescription>`) | Every property typed `object` / `Dictionary<string, object>` receives `JObject` / `JArray` / `JValue` instances. |
| 2. hold | `ModuleRegistrar` (UniversalBFF core) | - | Keeps the `ModuleDescription` instance as is. |
| 3. write | `PortfolioController.GetModule` (`UShell.PortfolioHosting.Mvc`) returns `this.Ok(moduleDesc)` | **System.Text.Json** (ASP.NET Core default, `services.AddControllers()` in `UniversalBFF.AspNetCore/Programm.cs`, no `AddNewtonsoftJson`) | STJ does not know `JToken`; it treats `JObject`/`JArray` as `IEnumerable` and writes nested arrays of (empty) children. |

Affected contract members (all untyped):

- `UsecaseDescription.UnitOfWorkDefaults`, `StaticUsecaseAssignment.InitUnitOfWork`, `CommandDescription.InitUnitOfWork` (`IDynamicParamObject : Dictionary<string, object>`)
- `DatastoreDescription.ProviderArguments` (`object`)
- `DatasourceDescription.ProviderArguments` (`List<IDynamicParamObject>`)
- the same pattern in `PortfolioDescription` wherever `object`/`IDynamicParamObject` is used (`mapDynamic` arguments etc.)

## Why NO GO

- The API that the hosting target state prescribes (`ushell-hosting` M-2: "the provider registers the embedded `module.json`") produces wrong output in the main delivery path (UniversalBFF). Every module that follows the documented best practice is affected.
- The failure is silent (no exception, no log entry), the output is valid JSON.
- Whether it works depends on an implicit host setting (the MVC serializer), i.e. on something neither the module nor the contract library controls.
- The previous C#-built module descriptions worked only by accident: their nested values were plain CLR dictionaries/arrays.

## Current Compensation (stopgap)

`dotnet/src/UniversalBFF.OobModules.UserManagement/Frontend/ModuleDescriptionNormalizer.cs` converts all `JToken`s of
the loaded description into plain CLR objects (`Dictionary<string, object>`, `List<object>`, primitives) before
registration. To be removed as soon as the central fix exists.

## Options for the Central Fix

| Option | Where | Assessment |
|---|---|---|
| A. The description endpoints serialize explicitly with Newtonsoft (e.g. `ContentResult` with `JsonConvert.SerializeObject`) | `UShell.PortfolioHosting.Mvc` (`PortfolioController`) | **Recommended.** Read and write use the same stack inside the library that owns the contract; independent of the host's MVC serializer. Consistent with the landscape preference (Newtonsoft preferred). |
| B. `FromEmbeddedResFile` and friends normalize `JToken`s into plain CLR objects | `UShell.PortfolioDescription` | Good complement to A (descriptions held in memory become serializer-neutral, e.g. for C# code that inspects them). Alone it does not cover descriptions deserialized elsewhere. |
| C. `AddNewtonsoftJson()` for all controllers | UniversalBFF core | Fixes the symptom here, but changes the serialization of every controller (incl. UJMW-adjacent and OAuth endpoints) and leaves other hosts of `PortfolioHosting.Mvc` broken. Not recommended. |

Whichever option is chosen, add a test that round-trips a `module.json` with nested `unitOfWorkDefaults` and
`providerArguments` through the hosting endpoint and compares the result.
