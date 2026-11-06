# User Management - U-Shell Module (Web Project)

Web part of the UniversalBFF out-of-box module `UniversalBFF.OobModules.UserManagement`.
It is meant to be the **reference example for a configuration-first U-Shell module** (see
[`doc/epics/epic-usermanagement-module-mvp.md`](../../doc/epics/epic-usermanagement-module-mvp.md)).

## What lives here

| File | Role |
|---|---|
| `src/module.json` | The module description: workspaces, use cases, menu commands, datastore, table layouts. **The only place that describes the UI.** |
| `src/widgets/EntityTableWidget.tsx` | Generic, configuration-driven widget (thin module entry). Exposed via module federation as `./EntityTableWidget`. |
| `src/widgets/EntityTableWidgetConfig.ts` | The UnitOfWork contract of that widget (= its configuration in `module.json`). |
| `src/pages/EntityTablePage.tsx` | Host-agnostic page around the CommonComponents `EntityTable`. |
| `src/bootstrap.tsx` | Stand-alone info page for local debugging only (never loaded by the shell). |

The bundle contains **no entity-specific code**. Adding a new table view means adding a use case to `module.json`.

## Build

```sh
npm install
npm run build
```

Output (canonical module layout, see `ushell-hosting`):

```text
dist/module.json   <- copied from src/module.json
dist/ui/**         <- remoteEntry.js (federation scope 'oobUsrMgmt') + chunks
```

A **Debug** build of `dotnet/src/UniversalBFF.OobModules.UserManagement` copies `dist/` into
`Frontend/webapp-files/` and embeds it. The module provider then registers the embedded
`module.json` and mounts `ui/` as `{BaseUrl}oob-usrmgmt/ui/`.

## Conventions

- `react`, `react-dom`, `ushell-modulebase`, `ushell-common-components` are shared singletons.
  `ushell-common-components` has no own fallback (`import: false`): it is always provided by the shell.
- `publicPath` stays `_PUBLIC_PATH_` (replaced by UniversalBFF) until decision D-12.
- URLs inside `module.json` are relative to the module (`ui/remoteEntry.js`, `api/v1/store/`).
  Host-bound values (datastore URL resolution, token source) are supplemented by the C# module provider.
- Entity names in `module.json` are the names of the designed model (`UserManagement.Model.json`), e.g. `Role`, `LocalCredential`.
