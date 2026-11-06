# UniversalBFF Technical Debt

Known technical and design debt: deviation, why it is not fixed yet, and the proposed resolution.

| Deviation | Why not fixed yet | Proposed resolution |
|---|---|---|
| The root `readme.md` is lower case; the repository convention expects `README.md`. | Renaming only the case is easy to get wrong on case-insensitive file systems and affects external links. | Rename via `git mv readme.md README.md` in a dedicated change. |
| The User Management module resolves relative datastore URLs of its `module.json` in the C# provider. | The shell resolves datastore URLs against the document, not against the module (UShell DD-07). | Remove `ResolveRelativeDatastoreUrls` once the shell resolves them against the `module.json` URL. |
| The User Management module converts `JToken`s of its `module.json` into plain objects (`ModuleDescriptionNormalizer`). | Newtonsoft deserialization vs. System.Text.Json serving - NO GO, see [issue](issue-moduledescription-serialization.md). | Solve centrally, then remove the normalizer. |
| `FUSE-fx.RepositoryContract` 3.0.2 is pinned in the hosts and AspNetCore wrappers. | `SmartStandards.Logging.Centralized` 3.4.4 still references 1.0.2. | Remove the pin once Logging.Centralized references 3.x; general topic: module isolation ([future directions](../epics/epic-future-directions.md)). |
| Local passwords are stored as unsalted SHA-256. | Out of scope so far. | Salted, slow hash with a versioned format (see epic I3, UI field transformers). |
| The User Management bundle contains a generic `EntityTableWidget`. | CommonComponents' `EntityTableModule` is neither exported nor a built-in widget class (UShell DD-04). | Promote to a built-in widget class; switch the use cases' `widgetClass` and drop the bundle. |
| The web build is embedded only in Debug builds (`webapp-files` is git-ignored). | CI does not build the web project yet. | Build the web project in CI and embed it in every configuration (epic I5). Without it, the module logs an error and registers no UI. |
| The repository root has no `LICENSE`, `AGENTS.md`, `CLAUDE.md` or `.editorconfig`. | Out of scope of the documentation restructuring. | Add them according to the `smart-solution-and-project-structure` skill. |
