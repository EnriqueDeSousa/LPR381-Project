# LPR381 — LP/IP Solver (WinForms)

A single-window WinForms app that loads an LP/IP model and solves it with:

1. **Primal Simplex** (tableau method, Big-M) — canonical form and every tableau iteration.
2. **Revised Primal Simplex** (product form of the inverse) — B⁻¹, price vector, and priced-out
   reduced costs at every iteration.
3. **Sensitivity Analysis** — shadow prices, ranging of basic/non-basic variables, RHS ranging,
   adding a new activity, adding a new constraint.
4. **Duality** — builds and solves the dual, checks strong/weak duality.
5. **Branch & Bound** — exact solver for models with `int`/`bin` variables.
6. **Cutting Plane** (Gomory) — alternative exact solver for `int`/`bin` models, via dual-simplex cuts.

Each algorithm has its own tab; a model loaded on the **Model** tab is shared by all of them.

## Building & running

```
cd "LPR Project"
dotnet run
```

This targets `net8.0-windows` (WinForms), so it builds and runs on Windows. `LPR Project.sln` opens
the same project in Visual Studio.

## Using it

- **Model tab** — paste/edit the model text (same `.txt` format as the brief: an objective line, one
  constraint per line, a sign-restriction line) or click **Open File...** to load one from disk, then
  **Parse / Load Model**. A sample LP is pre-filled.
- **Primal Simplex / Revised Simplex / Canonical Form** — one **Solve**/**Show** button each, with a
  **Save Output...** button to write the result to a `.txt` file.
- **Sensitivity && Duality** — click **1) Solve (Primal Simplex)** first, then use any of the panels on
  the left (shadow prices, variable/RHS ranging, adding an activity or constraint, duality). Results
  accumulate in the log on the right; **Clear Log** resets it. Coefficient lists (for a new activity's
  constraint column, or a new constraint's row) are entered space-separated.
- **Branch & Bound / Cutting Plane** — one **Solve** button each; needs `int`/`bin` variables in the
  loaded model (declared on the sign-restriction line).

## Project layout

```
LPR Project/
  LPR381Solver.csproj   <- WinForms project file (net8.0-windows)
  Program.cs             <- WinForms entry point (Application.Run(new MainForm()))
  Forms/
    MainForm.cs           <- the whole UI: one tab per algorithm, sharing one loaded model
  Models/                 <- parsed model, standard form, results
  IO/                     <- reading/parsing the input file or pasted text, formatting results
  Solvers/                <- Primal Simplex, Revised Simplex, Sensitivity Analysis,
                             Duality, Branch & Bound, Cutting Plane
  Errors/                 <- SolverException hierarchy + ErrorHandler, used across the app
                             for consistent "what went wrong" messages/dialogs
  Utils/                  <- small matrix-math helpers
  sample_input.txt        <- the knapsack (all-binary) example from the brief
  sample_input_lp.txt     <- a plain LP (no bin/int)
```

Everything shares the same pipeline: `InputParser` → `LPModel` → `StandardForm` → a solver in
`Solvers/`. The UI in `Forms/MainForm.cs` is a thin layer on top of that — every button click just
calls into `Solvers/`/`IO/` and displays the returned string/result, so the underlying engine has no
UI dependency at all.

## History: from console app to WinForms

This project went through two rounds of consolidation:

1. It started as several people's separate, incompatible implementations merged through GitHub
   (different `.csproj` targets, a `Program.cs` with two unresolved `Main` methods, a WinForms app,
   a console app, and a standalone cutting-plane engine all tangled together). That was resolved by
   standardising on one shared `LPModel`/`StandardForm`/`PrimalSimplex` pipeline, porting the missing
   algorithms (Branch & Bound, Cutting Plane) onto it, and running everything through a console menu.
   See `../Legacy-OtherContributions/README.md` for what was archived from that stage.
2. The console menu has since been replaced with this WinForms front end (`Forms/MainForm.cs`) —
   `Program.cs` now just starts `Application.Run(new MainForm())`. No solver/IO code changed to make
   this move; only the "how do I ask for input and show output" layer did, since `Solvers/`/`IO/` never
   had a UI dependency to begin with.
