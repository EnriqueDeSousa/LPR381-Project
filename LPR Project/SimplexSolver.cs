using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BranchAndBoundSimplex
{
    public enum SimplexStatus
    {
        Optimal,
        Infeasible,
        Unbounded
    }

    public class SimplexResult
    {
        public SimplexStatus Status { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; } = Array.Empty<double>();
        public List<string> Log { get; } = new List<string>();
    }

    public static class SimplexSolver
    {
        private const double Epsilon = 1e-9;
        private const double BigM = 1_000_000.0;
        private const int MaxIterations = 500;

        public static SimplexResult Solve(LinearProgram lp, bool verbose = false)
        {
            int n = lp.NumVariables;
            int m = lp.Constraints.Count;
            var result = new SimplexResult();

            // 1) Normalize rows so RHS >= 0 (flip sign + relation if needed).
            var relations = new Relation[m];
            var rhs = new double[m];
            var rowCoeffs = new double[m][];
            for (int i = 0; i < m; i++)
            {
                var c = lp.Constraints[i];
                double b = c.Rhs;
                var coeffs = (double[])c.Coefficients.Clone();
                var rel = c.Relation;
                if (b < 0)
                {
                    b = -b;
                    for (int j = 0; j < n; j++) coeffs[j] = -coeffs[j];
                    rel = rel switch
                    {
                        Relation.LE => Relation.GE,
                        Relation.GE => Relation.LE,
                        _ => Relation.EQ
                    };
                }
                relations[i] = rel;
                rhs[i] = b;
                rowCoeffs[i] = coeffs;
            }

            // 2) Work out how many slack / surplus / artificial columns we need.
            int slackCount = relations.Count(r => r == Relation.LE);
            int surplusCount = relations.Count(r => r == Relation.GE);
            int artificialCount = relations.Count(r => r == Relation.GE || r == Relation.EQ);

            int slackStart = n;
            int surplusStart = slackStart + slackCount;
            int artificialStart = surplusStart + surplusCount;
            int totalVars = artificialStart + artificialCount;

            var tableau = new double[m + 1, totalVars + 1]; // +1 for RHS column
            var basis = new int[m];

            int slackIdx = 0, surplusIdx = 0, artificialIdx = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    tableau[i, j] = rowCoeffs[i][j];

                switch (relations[i])
                {
                    case Relation.LE:
                        {
                            int col = slackStart + slackIdx++;
                            tableau[i, col] = 1.0;
                            basis[i] = col;
                            break;
                        }
                    case Relation.GE:
                        {
                            int surplusCol = surplusStart + surplusIdx++;
                            tableau[i, surplusCol] = -1.0;
                            int artCol = artificialStart + artificialIdx++;
                            tableau[i, artCol] = 1.0;
                            basis[i] = artCol;
                            break;
                        }
                    case Relation.EQ:
                        {
                            int artCol = artificialStart + artificialIdx++;
                            tableau[i, artCol] = 1.0;
                            basis[i] = artCol;
                            break;
                        }
                }
                tableau[i, totalVars] = rhs[i];
            }

            // 3) Cost vector for a MINIMIZATION problem (we minimize -c^T x,
            // which is equivalent to maximizing c^T x). Artificials get Big-M.
            var cost = new double[totalVars];
            for (int j = 0; j < n; j++) cost[j] = -lp.ObjectiveCoefficients[j];
            for (int j = artificialStart; j < totalVars; j++) cost[j] = BigM;

            // 4) Build the initial objective row: row[j] = cost[j] - z[j].
            RecomputeObjectiveRow(tableau, cost, basis, m, totalVars);

            if (verbose)
            {
                result.Log.Add("Initial tableau (Big-M method, minimizing -objective):");
                result.Log.Add(FormatTableau(tableau, basis, lp, slackStart, surplusStart, artificialStart, totalVars));
            }

            int iteration = 0;
            while (iteration++ < MaxIterations)
            {
                // Choose entering column: most negative reduced cost.
                int enter = -1;
                double best = -Epsilon;
                for (int j = 0; j < totalVars; j++)
                {
                    if (tableau[m, j] < best)
                    {
                        best = tableau[m, j];
                        enter = j;
                    }
                }

                if (enter == -1)
                    break; // optimal for the Big-M problem

                // Ratio test to choose the leaving row.
                int leave = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (tableau[i, enter] > Epsilon)
                    {
                        double ratio = tableau[i, totalVars] / tableau[i, enter];
                        if (ratio < bestRatio - Epsilon)
                        {
                            bestRatio = ratio;
                            leave = i;
                        }
                    }
                }

                if (leave == -1)
                {
                    result.Status = SimplexStatus.Unbounded;
                    if (verbose) result.Log.Add("No positive ratio found -> problem is unbounded.");
                    return result;
                }

                if (verbose)
                {
                    string enterName = ColumnName(enter, lp, slackStart, surplusStart, artificialStart);
                    string leaveName = ColumnName(basis[leave], lp, slackStart, surplusStart, artificialStart);
                    result.Log.Add($"Pivot {iteration}: {enterName} enters, {leaveName} leaves (row {leave}).");
                }

                Pivot(tableau, m, totalVars, leave, enter);
                basis[leave] = enter;

                if (verbose)
                    result.Log.Add(FormatTableau(tableau, basis, lp, slackStart, surplusStart, artificialStart, totalVars));
            }

            for (int i = 0; i < m; i++)
            {
                if (basis[i] >= artificialStart && tableau[i, totalVars] > 1e-6)
                {
                    result.Status = SimplexStatus.Infeasible;
                    if (verbose) result.Log.Add("An artificial variable remains basic and positive -> infeasible.");
                    return result;
                }
            }

            var values = new double[n];
            for (int i = 0; i < m; i++)
            {
                if (basis[i] < n)
                    values[basis[i]] = tableau[i, totalVars];
            }

            double objective = 0.0;
            for (int j = 0; j < n; j++) objective += lp.ObjectiveCoefficients[j] * values[j];

            result.Status = SimplexStatus.Optimal;
            result.VariableValues = values;
            result.ObjectiveValue = objective;
            return result;
        }

        private static void RecomputeObjectiveRow(double[,] tableau, double[] cost, int[] basis, int m, int totalVars)
        {
            for (int j = 0; j <= totalVars; j++)
            {
                double z = 0.0;
                for (int i = 0; i < m; i++)
                    z += cost[basis[i]] * tableau[i, j];
                double costJ = j < cost.Length ? cost[j] : 0.0;
                tableau[m, j] = costJ - z;
            }
        }

        private static void Pivot(double[,] tableau, int m, int totalVars, int pivotRow, int pivotCol)
        {
            double pivotVal = tableau[pivotRow, pivotCol];
            for (int j = 0; j <= totalVars; j++)
                tableau[pivotRow, j] /= pivotVal;

            for (int i = 0; i <= m; i++)
            {
                if (i == pivotRow) continue;
                double factor = tableau[i, pivotCol];
                if (Math.Abs(factor) < Epsilon) continue;
                for (int j = 0; j <= totalVars; j++)
                    tableau[i, j] -= factor * tableau[pivotRow, j];
            }
        }

        private static string ColumnName(int col, LinearProgram lp, int slackStart, int surplusStart, int artificialStart)
        {
            if (col < lp.NumVariables) return lp.VariableNames[col];
            if (col < surplusStart) return $"s{col - slackStart + 1}";
            if (col < artificialStart) return $"e{col - surplusStart + 1}";
            return $"a{col - artificialStart + 1}";
        }

        private static string FormatTableau(double[,] tableau, int[] basis, LinearProgram lp, int slackStart, int surplusStart, int artificialStart, int totalVars)
        {
            var sb = new StringBuilder();
            int m = basis.Length;
            var headers = new List<string> { "Basis" };
            for (int j = 0; j < totalVars; j++)
                headers.Add(ColumnName(j, lp, slackStart, surplusStart, artificialStart));
            headers.Add("RHS");
            sb.AppendLine(string.Join("\t", headers));

            for (int i = 0; i < m; i++)
            {
                var row = new List<string> { ColumnName(basis[i], lp, slackStart, surplusStart, artificialStart) };
                for (int j = 0; j <= totalVars; j++)
                    row.Add(tableau[i, j].ToString("F2"));
                sb.AppendLine(string.Join("\t", row));
            }

            var objRow = new List<string> { "z-row" };
            for (int j = 0; j <= totalVars; j++)
                objRow.Add(tableau[m, j].ToString("F2"));
            sb.AppendLine(string.Join("\t", objRow));

            return sb.ToString();
        }
    }
}