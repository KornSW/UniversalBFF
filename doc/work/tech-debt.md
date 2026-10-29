# UniversalBFF Technical Debt

Known technical and design debt: deviation, why it is not fixed yet, and the proposed resolution.

| Deviation | Why not fixed yet | Proposed resolution |
|---|---|---|
| The root `readme.md` is lower case; the repository convention expects `README.md`. | Renaming only the case is easy to get wrong on case-insensitive file systems and affects external links. | Rename via `git mv readme.md README.md` in a dedicated change. |
| The repository root has no `LICENSE`, `AGENTS.md`, `CLAUDE.md` or `.editorconfig`. | Out of scope of the documentation restructuring. | Add them according to the `smart-solution-and-project-structure` skill. |
