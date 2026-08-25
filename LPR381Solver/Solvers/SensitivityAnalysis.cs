using System;
using System.Collections.Generic;
using System.Linq;
using LPR381Solver.Models;

namespace LPR381Solver.Solvers
{
    public class RangeResult
    {
        public double Lower;
        public double Upper;
        public bool LowerIsInfinite;
        public bool UpperIsInfinite;
        public override string ToString()
        {
            string lo = LowerIsInfinite ? "-infinity" : Lower.ToString("F3");
            string hi = UpperIsInfinite ? "+infinity" : Upper.ToString("F3");
            return $"[{lo}, {hi}]";
        }
    }

    /// <summary>
    /// All sensitivity-analysis operations act on the FINAL (optimal) tableau produced by
    /// <see cref="PrimalSimplex"/>. Everything is derived from two facts about a simplex tableau
    /// at optimality:
    ///   1) The sub-matrix of the final tableau sitting under the columns that were the identity
    ///      matrix in the very first tableau (i.e. the initial basis columns) IS B^-1.
    ///   2) Reduced costs (c_j - z_j) for every column can be recomputed at any time from cB and
    ///      the current tableau.
    /// </summary>
    public static class SensitivityAnalysis
    {
        private const double Eps = 1e-9;

        // ---------- shared building blocks ----------

        public static double[] ReducedCosts(SimplexResult r, StandardForm sf)
        {
            int m = sf.NumRows, ncols = sf.NumCols;
            var cB = r.FinalBasis.Select(bi => sf.c[bi]).ToArray();
            var reduced = new double[ncols];
            for (int j = 0; j < ncols; j++)
            {
                double z = 0;
                for (int i = 0; i < m; i++) z += cB[i] * r.FinalTableau[i, j];
                reduced[j] = sf.c[j] - z;
            }
            return reduced;
        }

        /// <summary>B^-1, recovered from the columns of the final tableau that were the identity
        /// matrix in the initial tableau (see class summary).</summary>
        public static double[,] BInverse(SimplexResult r, StandardForm sf)
        {
            int m = sf.NumRows;
            var Binv = new double[m, m];
            for (int col = 0; col < m; col++)
            {
                int initialCol = sf.InitialBasis[col]; // the column that was e_col at iteration 0
                for (int row = 0; row < m; row++)
                    Binv[row, col] = r.FinalTableau[row, initialCol];
            }
            return Binv;
        }

        /// <summary>Shadow prices y = cB * B^-1, one value per original constraint row.</summary>
        public static double[] ShadowPrices(SimplexResult r, StandardForm sf)
        {
            int m = sf.NumRows;
            var Binv = BInverse(r, sf);
            var cB = r.FinalBasis.Select(bi => sf.c[bi]).ToArray();
            var y = new double[m];
            for (int col = 0; col < m; col++)
            {
                double sum = 0;
                for (int i = 0; i < m; i++) sum += cB[i] * Binv[i, col];
                y[col] = sum;
            }
            return y;
        }

        private static int RowOfBasicVar(SimplexResult r, int col)
        {
            for (int i = 0; i < r.FinalBasis.Length; i++)
                if (r.FinalBasis[i] == col) return i;
            return -1;
        }

        // ---------- 1) range of a non-basic variable's objective coefficient ----------

        public static RangeResult RangeNonBasic(SimplexResult r, StandardForm sf, int col)
        {
            var reduced = ReducedCosts(r, sf);
            // Non-basic, max problem: currently reduced[col] <= 0. c_col may rise until reduced=0;
            // it may fall without limit (further from entering).
            double upper = sf.c[col] - reduced[col];
            return new RangeResult { LowerIsInfinite = true, Upper = upper, UpperIsInfinite = false };
        }

        // ---------- 2) range of a basic variable's objective coefficient ----------

        public static RangeResult RangeBasic(SimplexResult r, StandardForm sf, int col)
        {
            int row = RowOfBasicVar(r, col);
            if (row == -1) throw new InvalidOperationException($"Column {sf.ColNames[col]} is not in the final basis.");

            var reduced = ReducedCosts(r, sf);
            double lower = double.NegativeInfinity, upper = double.PositiveInfinity;

            for (int k = 0; k < sf.NumCols; k++)
            {
                if (k == col) continue;
                if (r.FinalBasis.Contains(k)) continue; // only non-basic columns constrain the range
                double t = r.FinalTableau[row, k];
                if (t > Eps)
                {
                    double candidate = reduced[k] / t;
                    if (candidate > lower) lower = candidate;
                }
                else if (t < -Eps)
                {
                    double candidate = reduced[k] / t;
                    if (candidate < upper) upper = candidate;
                }
            }

            var result = new RangeResult();
            if (double.IsNegativeInfinity(lower)) result.LowerIsInfinite = true; else result.Lower = sf.c[col] + lower;
            if (double.IsPositiveInfinity(upper)) result.UpperIsInfinite = true; else result.Upper = sf.c[col] + upper;
            return result;
        }

        /// <summary>Dispatches to <see cref="RangeNonBasic"/> or <see cref="RangeBasic"/> automatically.</summary>
        public static RangeResult RangeOfVariable(SimplexResult r, StandardForm sf, int col) =>
            r.FinalBasis.Contains(col) ? RangeBasic(r, sf, col) : RangeNonBasic(r, sf, col);

        // ---------- 3) range of a constraint's RHS ----------

        public static RangeResult RangeRhs(SimplexResult r, StandardForm sf, int constraintRow)
        {
            var Binv = BInverse(r, sf);
            int m = sf.NumRows;
            var alpha = new double[m];
            for (int i = 0; i < m; i++) alpha[i] = Binv[i, constraintRow];

            var xB = new double[m];
            for (int i = 0; i < m; i++) xB[i] = r.FinalTableau[i, sf.NumCols];

            double lower = double.NegativeInfinity, upper = double.PositiveInfinity;
            for (int i = 0; i < m; i++)
            {
                if (alpha[i] > Eps)
                {
                    double candidate = -xB[i] / alpha[i];
                    if (candidate > lower) lower = candidate;
                }
                else if (alpha[i] < -Eps)
                {
                    double candidate = -xB[i] / alpha[i];
                    if (candidate < upper) upper = candidate;
                }
            }

            var result = new RangeResult();
            double b0 = sf.b[constraintRow];
            if (double.IsNegativeInfinity(lower)) result.LowerIsInfinite = true; else result.Lower = b0 + lower;
            if (double.IsPositiveInfinity(upper)) result.UpperIsInfinite = true; else result.Upper = b0 + upper;
            return result;
        }

        public static (double[] newXB, double newObjective, bool stillFeasible) ApplyRhsChange(
            SimplexResult r, StandardForm sf, int constraintRow, double newRhs)
        {
            double delta = newRhs - sf.b[constraintRow];
            var Binv = BInverse(r, sf);
            int m = sf.NumRows;
            var alpha = new double[m];
            for (int i = 0; i < m; i++) alpha[i] = Binv[i, constraintRow];

            var xB = new double[m];
            bool feasible = true;
            for (int i = 0; i < m; i++)
            {
                xB[i] = r.FinalTableau[i, sf.NumCols] + delta * alpha[i];
                if (xB[i] < -1e-6) feasible = false;
            }

            var y = ShadowPrices(r, sf);
            double newObj = r.ObjectiveValue + delta * y[constraintRow] * (sf.OriginalWasMin ? -1 : 1);
            return (xB, newObj, feasible);
        }

        // ---------- 4) add a new activity (decision variable) to an optimal solution ----------

        public static (double reducedCost, bool wouldImproveSolution) EvaluateNewActivity(
            SimplexResult r, StandardForm sf, double objCoeff, double[] constraintColumn)
        {
            var y = ShadowPrices(r, sf);
            double yA = 0;
            for (int i = 0; i < sf.NumRows; i++) yA += y[i] * constraintColumn[i];
            double reduced = objCoeff - yA;
            return (reduced, reduced > Eps);
        }

        // ---------- 5) add a new constraint to an optimal solution ----------

        public static (double lhsValue, bool satisfied) EvaluateNewConstraint(
            SimplexResult r, double[] newRowCoefficients, Relation relation, double rhs)
        {
            double lhs = 0;
            for (int j = 0; j < newRowCoefficients.Length; j++) lhs += newRowCoefficients[j] * r.OriginalSolution[j];
            bool ok = relation switch
            {
                Relation.LessOrEqual => lhs <= rhs + 1e-6,
                Relation.GreaterOrEqual => lhs >= rhs - 1e-6,
                _ => Math.Abs(lhs - rhs) < 1e-6
            };
            return (lhs, ok);
        }
    }
}
