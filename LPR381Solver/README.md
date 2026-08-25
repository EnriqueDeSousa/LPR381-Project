Primal Simplex + Revised Primal Simplex + Sensitivity Analysis


1. Primal Simplex Algorithm (tableau method, Big-M technique) — shows the canonical form and every tableau iteration.
2. Revised Primal Simplex Algorithm (product form of the inverse / price-out) — shows B⁻¹, the price vector, and priced-out reduced costs at every iteration.
3. Sensitivity Analysis — shadow prices, ranging of basic/non-basic variables, RHS ranging, adding a new activity, adding a new constraint, and duality (builds and solves the dual, checks strong/weak duality).

It reads the input `.txt` file format from the brief, and writes the canonical form + all iterations + the result to `output.txt`.


Project layout
LPR381Solver/
  LPR381Solver.csproj      <- the project file, builds "solve.exe"
  Program.cs                <- the menu you see when you run it
  Models/                   <- data classes: parsed model, standard form, results
  IO/                        <- reading the input file, writing the output text
  Solvers/                   <- Primal Simplex, Revised Simplex, Sensitivity Analysis, Duality
  Utils/                     <- small matrix-math helpers
  sample_input.txt           <- the Knapsack example from the brief
  sample_input_lp.txt        <- a plain LP (no bin/int)

