# Archived contributions

These files are earlier/parallel work from teammates that predates the merge into the single
`LPR Project` console app (see `../LPR Project/README.md` → "About the merge" for the full story).
They're kept here for reference and are **not** part of the buildable project — they target
`.NET Framework 4.7.2` + WinForms, which can't share a project with the `.NET 8` console app.

- `Home.cs` / `Home.Designer.cs` / `Home.resx`, `BranchAndBoundSimplex.cs` — WinForms UI.
- `LinearProgram.cs`, `SimplexSolver.cs`, `BranchAndBoundSolver.cs` — the original LP model/solver
  classes behind that UI. The branching logic in `BranchAndBoundSolver.cs` is what
  `LPR Project/Solvers/BranchAndBound.cs` is based on (ported to the shared `LPModel` types).
- `ModelFileReader.cs`, `CuttingPlaneModel.cs` — a separate, self-contained cutting-plane engine
  with its own `LpModel` model classes. `LPR Project/Solvers/CuttingPlane.cs` implements the same
  algorithm (Gomory cuts) against the shared `LPModel`/`StandardForm` types instead.
- `Form1_IncompleteWinFormsPrototype.cs` — an early WinForms form stub (no `.Designer.cs`/`.resx`,
  so it never compiled on its own).
- `Properties/`, `App.config`, `LPR Project.csproj`, `LPR Project.sln` — the old-style project files
  for the WinForms build.

To resurrect a WinForms front-end later: `InputParser`, `StandardForm`, and all five solvers in
`LPR Project/Solvers/` have no WinForms dependency, so a new WinForms project could reference/copy
them directly rather than rebuilding the math again.
