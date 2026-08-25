using System;
using System.Linq;
using LPR381Solver.Models;

namespace LPR381Solver.Solvers
{
    /// <summary>
    /// Classic tableau Primal Simplex method using the Big-M technique for &gt;= and = constraints.
    /// Dantzig's rule (most positive reduced cost) is used to choose the entering variable, with
    /// Bland's rule (smallest index) as a tie-breaker to guard against cycling.
    /// </summary>
    public static class PrimalSimplex
    {
        private const double Eps = 1e-9;
        private const int MaxIterations = 500;

        public static SimplexResult Solve(StandardForm sf)
        {
            int m = sf.NumRows;
            int ncols = sf.NumCols;
            var basis = (int[])sf.InitialBasis.Clone();

            // Augmented tableau: m rows x (ncols + 1) columns, last column is RHS.
            var T = new double[m, ncols + 1];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < ncols; j++) T[i, j] = sf.A[i, j];
                T[i, ncols] = sf.b[i];
            }

            var result = new SimplexResult { StandardForm = sf };
            int iteration = 0;

            while (true)
            {
                var cB = basis.Select(bi => sf.c[bi]).ToArray();
                var z = new double[ncols];
                for (int j = 0; j < ncols; j++)
                {
                    double sum = 0;
                    for (int i = 0; i < m; i++) sum += cB[i] * T[i, j];
                    z[j] = sum;
                }
                double zVal = 0;
                for (int i = 0; i < m; i++) zVal += cB[i] * T[i, ncols];

                var reduced = new double[ncols];
                for (int j = 0; j < ncols; j++) reduced[j] = sf.c[j] - z[j];

                // Snapshot BEFORE pivoting, so the printed tableau includes its own objective row.
                var snap = BuildSnapshot(T, basis, reduced, zVal, iteration, m, ncols);
                result.Iterations.Add(snap);

                // Entering variable: most positive reduced cost (Dantzig), ties -> smallest index (Bland).
                int entering = -1;
                double best = Eps;
                for (int j = 0; j < ncols; j++)
                {
                    if (reduced[j] > best + 1e-12)
                    {
                        best = reduced[j];
                        entering = j;
                    }
                }

                if (entering == -1)
                {
                    // Optimal.
                    result.Status = SolveStatus.Optimal;
                    FillFinalResult(result, sf, T, basis, zVal, m, ncols);
                    return result;
                }

                // Ratio test.
                int leaving = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (T[i, entering] > Eps)
                    {
                        double ratio = T[i, ncols] / T[i, entering];
                        if (ratio < bestRatio - 1e-12 ||
                            (Math.Abs(ratio - bestRatio) < 1e-9 && (leaving == -1 || basis[i] < basis[leaving])))
                        {
                            bestRatio = ratio;
                            leaving = i;
                        }
                    }
                }

                if (leaving == -1)
                {
                    result.Status = SolveStatus.Unbounded;
                    result.Message = "The problem is unbounded: no limiting ratio was found on the entering column " +
                                      $"'{sf.ColNames[entering]}'.";
                    return result;
                }

                snap.EnteringCol = entering;
                snap.LeavingRow = leaving;

                // Pivot (Gauss-Jordan).
                double pivotVal = T[leaving, entering];
                for (int j = 0; j <= ncols; j++) T[leaving, j] /= pivotVal;
                for (int i = 0; i < m; i++)
                {
                    if (i == leaving) continue;
                    double factor = T[i, entering];
                    if (Math.Abs(factor) < 1e-14) continue;
                    for (int j = 0; j <= ncols; j++) T[i, j] -= factor * T[leaving, j];
                }
                basis[leaving] = entering;
                iteration++;

                if (iteration > MaxIterations)
                {
                    result.Status = SolveStatus.Unbounded;
                    result.Message = "Exceeded the maximum number of iterations (possible cycling). Aborting.";
                    return result;
                }
            }
        }

        private static TableauSnapshot BuildSnapshot(double[,] T, int[] basis, double[] reduced, double zVal,
                                                       int iteration, int m, int ncols)
        {
            var full = new double[m + 1, ncols + 1];
            for (int i = 0; i < m; i++)
                for (int j = 0; j <= ncols; j++)
                    full[i, j] = T[i, j];
            for (int j = 0; j < ncols; j++) full[m, j] = reduced[j];
            full[m, ncols] = zVal;

            return new TableauSnapshot
            {
                IterationNumber = iteration,
                Tableau = full,
                Basis = (int[])basis.Clone(),
                Note = iteration == 0 ? "Initial tableau (canonical form)" : $"Iteration {iteration}"
            };
        }

        private static void FillFinalResult(SimplexResult result, StandardForm sf, double[,] T, int[] basis,
                                             double zVal, int m, int ncols)
        {
            // Infeasibility check: an artificial variable left in the basis with a positive value
            // means no feasible solution exists to the ORIGINAL problem (Big-M penalty could not
            // drive it out).
            for (int i = 0; i < m; i++)
            {
                if (sf.IsArtificial[basis[i]] && T[i, ncols] > 1e-6)
                {
                    result.Status = SolveStatus.Infeasible;
                    result.Message = $"Artificial variable '{sf.ColNames[basis[i]]}' remains in the basis " +
                                      $"at value {T[i, ncols]:F3} -- the model is infeasible.";
                    return;
                }
            }

            var xStandard = new double[ncols];
            for (int i = 0; i < m; i++) xStandard[basis[i]] = T[i, ncols];

            result.StandardSolution = xStandard;
            result.OriginalSolution = sf.RecoverOriginalValues(xStandard);
            result.FinalBasis = (int[])basis.Clone();
            var finalTab = new double[m + 1, ncols + 1];
            for (int i = 0; i < m; i++)
                for (int j = 0; j <= ncols; j++)
                    finalTab[i, j] = T[i, j];
            result.FinalTableau = finalTab;
            result.ObjectiveValue = sf.OriginalWasMin ? -zVal : zVal;
        }
    }
}
