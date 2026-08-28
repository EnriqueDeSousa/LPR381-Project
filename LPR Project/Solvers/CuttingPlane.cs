using System;
using System.Collections.Generic;
using System.Linq;
using LPR381Solver.Errors;
using LPR381Solver.Models;

namespace LPR381Solver.Solvers
{
    /// <summary>Final answer from the cutting-plane algorithm.</summary>
    public class CuttingPlaneResult
    {
        public SolveStatus Status;
        public double ObjectiveValue;
        public double[] OriginalSolution = Array.Empty<double>();
        public int CutsAdded;
        public List<string> Log { get; } = new();
        public string Message = "";
    }

    /// <summary>
    /// Gomory fractional cutting-plane method for models with "int"/"bin" sign restrictions.
    /// Solves the LP relaxation with the shared PrimalSimplex, then repeatedly reads a
    /// fractional row for an integer-restricted variable straight off the optimal tableau,
    /// derives a Gomory cut from it, appends it as a new row/column to the tableau, and
    /// restores feasibility with a single dual-simplex pivot -- exactly the classical
    /// dual-simplex formulation of Gomory cuts, kept self-contained here so it can sit
    /// alongside Primal/Revised Simplex without changing either of them.
    /// </summary>
    public static class CuttingPlane
    {
        private const double Eps = 1e-9;
        private const double IntTolerance = 1e-6;
        private const int MaxCuts = 50;

        public static CuttingPlaneResult Solve(LPModel model)
        {
            var integerCols = new List<int>();
            var sf0 = StandardForm.Build(model);
            foreach (var map in sf0.Mappings)
            {
                var restriction = model.SignRestrictions.Length > map.OriginalIndex
                    ? model.SignRestrictions[map.OriginalIndex]
                    : SignRestriction.Positive;
                if ((restriction == SignRestriction.Integer || restriction == SignRestriction.Binary)
                    && map.Type == VarMapType.Direct)
                {
                    integerCols.Add(map.Col);
                }
            }

            if (integerCols.Count == 0)
                throw new AlgorithmNotSupportedException(
                    "Cutting Plane needs at least one 'int' or 'bin' variable in the sign-restriction line " +
                    "-- for a pure LP, use Primal Simplex or Revised Primal Simplex instead.");

            var result = new CuttingPlaneResult();
            var relax = PrimalSimplex.Solve(sf0);

            if (relax.Status != SolveStatus.Optimal)
            {
                result.Status = relax.Status;
                result.Message = relax.Message;
                return result;
            }

            // Seed our working tableau from the LAST snapshot PrimalSimplex recorded: unlike
            // SimplexResult.FinalTableau (which only stores the constraint rows), the snapshot
            // also carries the fully priced-out objective/reduced-cost row we need for the
            // dual-simplex ratio test below.
            var lastSnap = relax.Iterations[^1];
            int m = lastSnap.Basis.Length;              // number of constraint rows
            int ncols = sf0.NumCols;                    // number of structural/slack/artificial columns
            var T = (double[,])lastSnap.Tableau.Clone(); // (m+1) x (ncols+1): last row = objective, last col = RHS
            var basis = (int[])lastSnap.Basis.Clone();
            var colNames = new List<string>(sf0.ColNames);

            int cutsAdded = 0;
            while (true)
            {
                // ---- find a basic integer-restricted variable with a fractional value ----
                int cutRow = -1;
                double bestFrac = IntTolerance;
                for (int i = 0; i < m; i++)
                {
                    if (!integerCols.Contains(basis[i])) continue;
                    double val = T[i, ncols];
                    double frac = Frac(val);
                    double distanceFromInteger = Math.Min(frac, 1.0 - frac);
                    if (distanceFromInteger > bestFrac)
                    {
                        bestFrac = distanceFromInteger;
                        cutRow = i;
                    }
                }

                if (cutRow == -1)
                {
                    result.Log.Add(cutsAdded == 0
                        ? "LP relaxation was already integer-feasible; no cuts were needed."
                        : $"All integer-restricted variables are now integral after {cutsAdded} cut(s).");
                    break;
                }

                if (cutsAdded >= MaxCuts)
                {
                    result.Log.Add($"Reached the cut limit ({MaxCuts}) without full integrality; stopping.");
                    break;
                }

                // ---- build the Gomory cut from row `cutRow` ----
                // Source-row identity:  x_B[cutRow] + sum_j a_ij x_j = b_i
                // Gomory cut:           sum_j frac(a_ij) x_j >= frac(b_i)
                // In dual-simplex form (new slack s >= 0, RHS made negative for the pivot below):
                //   -sum_j frac(a_ij) x_j + s = -frac(b_i)
                var newRow = new double[ncols + 1];
                for (int j = 0; j < ncols; j++)
                    newRow[j] = -Frac(T[cutRow, j]);
                double cutRhs = -Frac(T[cutRow, ncols]);

                cutsAdded++;
                colNames.Add($"g{cutsAdded}");

                // ---- expand the tableau: +1 column (new slack), +1 row (the cut) ----
                int newNcols = ncols + 1;
                int newM = m + 1;
                var newT = new double[newM + 1, newNcols + 1]; // +1 row for objective, +1 col for RHS

                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < ncols; j++) newT[i, j] = T[i, j];
                    newT[i, ncols] = 0.0;                 // new slack column, zero in old rows
                    newT[i, newNcols] = T[i, ncols];       // RHS shifts right by one column
                }
                for (int j = 0; j < ncols; j++) newT[m, j] = newRow[j];
                newT[m, ncols] = 1.0;                      // new slack's own column
                newT[m, newNcols] = cutRhs;

                // objective row (was row m in the old tableau) moves down to row newM
                for (int j = 0; j < ncols; j++) newT[newM, j] = T[m, j];
                newT[newM, ncols] = 0.0;                    // slack has zero objective coefficient
                newT[newM, newNcols] = T[m, ncols];

                var newBasis = new int[newM];
                Array.Copy(basis, newBasis, m);
                newBasis[m] = ncols; // the new slack starts in the basis (at a negative value)

                // ---- one dual-simplex pivot to restore primal feasibility on the cut row ----
                int leaving = m; // the cut row is the only infeasible one
                int entering = -1;
                double bestRatio = double.PositiveInfinity;
                for (int j = 0; j < newNcols; j++)
                {
                    if (newT[leaving, j] < -Eps)
                    {
                        double ratio = Math.Abs(newT[newM, j] / newT[leaving, j]);
                        if (ratio < bestRatio - 1e-12 || (Math.Abs(ratio - bestRatio) < 1e-9 &&
                                                           (entering == -1 || j < entering)))
                        {
                            bestRatio = ratio;
                            entering = j;
                        }
                    }
                }

                if (entering == -1)
                {
                    result.Status = SolveStatus.Infeasible;
                    result.Message = "A Gomory cut proved the integer program infeasible " +
                                      "(no entering column found during the dual-simplex pivot).";
                    result.Log.Add($"Cut {cutsAdded}: {result.Message}");
                    return result;
                }

                double pivotVal = newT[leaving, entering];
                for (int j = 0; j <= newNcols; j++) newT[leaving, j] /= pivotVal;
                for (int i = 0; i <= newM; i++)
                {
                    if (i == leaving) continue;
                    double factor = newT[i, entering];
                    if (Math.Abs(factor) < 1e-14) continue;
                    for (int j = 0; j <= newNcols; j++) newT[i, j] -= factor * newT[leaving, j];
                }
                newBasis[leaving] = entering;

                result.Log.Add($"Cut {cutsAdded}: added from row of '{colNames[basis[cutRow]]}' " +
                                $"(fractional value {T[cutRow, ncols]:F3}); pivoted in '{colNames[entering]}'.");

                T = newT;
                basis = newBasis;
                ncols = newNcols;
                m = newM;
            }

            // ---- extract the solution: ignore any Gomory-cut slack columns we added ----
            var xStandard = new double[sf0.NumCols];
            for (int i = 0; i < m; i++)
                if (basis[i] < sf0.NumCols)
                    xStandard[basis[i]] = T[i, ncols];

            double zVal = T[m, ncols];
            result.Status = SolveStatus.Optimal;
            result.ObjectiveValue = sf0.OriginalWasMin ? -zVal : zVal;
            result.OriginalSolution = sf0.RecoverOriginalValues(xStandard);
            result.CutsAdded = cutsAdded;
            return result;
        }

        private static double Frac(double x)
        {
            double f = x - Math.Floor(x);
            // Guard against floating noise landing just below 0 or just above 1.
            if (f < 0) f += 1.0;
            if (f > 1.0 - 1e-12) f = 0.0;
            return f;
        }
    }
}
