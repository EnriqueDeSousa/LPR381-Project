using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR_Project
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
        public double[] VariableValues { get; set; }
        public List<string> Log { get; private set; }

        public SimplexResult()
        {
            VariableValues = new double[0];
            Log = new List<string>();
        }
    }

    public static class SimplexSolver
    {
        private const double Epsilon = 1e-9;
        private const double BigM = 1000000.0;
        private const int MaxIterations = 500;

        public static SimplexResult Solve(LinearProgram lp, bool verbose)
        {
            int n = lp.NumVariables;
            int m = lp.Constraints.Count;
            SimplexResult result = new SimplexResult();

            Relation[] relations = new Relation[m];
            double[] rhs = new double[m];
            double[][] rowCoeffs = new double[m][];
            for (int i = 0; i < m; i++)
            {
                Constraint c = lp.Constraints[i];
                double b = c.Rhs;
                double[] coeffs = (double[])c.Coefficients.Clone();
                Relation rel = c.Relation;
                if (b < 0)
                {
                    b = -b;
                    for (int j = 0; j < n; j++) coeffs[j] = -coeffs[j];

                    if (rel == Relation.LE) rel = Relation.GE;
                    else if (rel == Relation.GE) rel = Relation.LE;
                    else rel = Relation.EQ;
                }
                relations[i] = rel;
                rhs[i] = b;
                rowCoeffs[i] = coeffs;
            }

            int slackCount = relations.Count(r => r == Relation.LE);
            int surplusCount = relations.Count(r => r == Relation.GE);
            int artificialCount = relations.Count(r => r == Relation.GE || r == Relation.EQ);

            int slackStart = n;
            int surplusStart = slackStart + slackCount;
            int artificialStart = surplusStart + surplusCount;
            int totalVars = artificialStart + artificialCount;

            double[,] tableau = new double[m + 1, totalVars + 1];
            int[] basis = new int[m];

            int slackIdx = 0, surplusIdx = 0, artificialIdx = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    tableau[i, j] = rowCoeffs[i][j];

                if (relations[i] == Relation.LE)
                {
                    int col = slackStart + slackIdx++;
                    tableau[i, col] = 1.0;
                    basis[i] = col;
                }
                else if (relations[i] == Relation.GE)
                {
                    int surplusCol = surplusStart + surplusIdx++;
                    tableau[i, surplusCol] = -1.0;
                    int artCol = artificialStart + artificialIdx++;
                    tableau[i, artCol] = 1.0;
                    basis[i] = artCol;
                }
                else // EQ
                {
                    int artCol = artificialStart + artificialIdx++;
                    tableau[i, artCol] = 1.0;
                    basis[i] = artCol;
                }
                tableau[i, totalVars] = rhs[i];
            }

            double[] cost = new double[totalVars];
            for (int j = 0; j < n; j++) cost[j] = -lp.ObjectiveCoefficients[j];
            for (int j = artificialStart; j < totalVars; j++) cost[j] = BigM;

            RecomputeObjectiveRow(tableau, cost, basis, m, totalVars);

            if (verbose)
            {
                result.Log.Add("Initial tableau (Big-M method, minimizing -objective):");
                result.Log.Add(FormatTableau(tableau, basis, lp, slackStart, surplusStart, artificialStart, totalVars));
            }

            int iteration = 0;
            while (iteration++ < MaxIterations)
            {
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
                    break;

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
                    result.Log.Add("Pivot " + iteration + ": " + enterName + " enters, " + leaveName + " leaves (row " + leave + ").");
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

            double[] values = new double[n];
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
            if (col < surplusStart) return "s" + (col - slackStart + 1);
            if (col < artificialStart) return "e" + (col - surplusStart + 1);
            return "a" + (col - artificialStart + 1);
        }

        private static string FormatTableau(double[,] tableau, int[] basis, LinearProgram lp, int slackStart, int surplusStart, int artificialStart, int totalVars)
        {
            StringBuilder sb = new StringBuilder();
            int m = basis.Length;
            List<string> headers = new List<string>();
            headers.Add("Basis");
            for (int j = 0; j < totalVars; j++)
                headers.Add(ColumnName(j, lp, slackStart, surplusStart, artificialStart));
            headers.Add("RHS");
            sb.AppendLine(string.Join("\t", headers));

            for (int i = 0; i < m; i++)
            {
                List<string> row = new List<string>();
                row.Add(ColumnName(basis[i], lp, slackStart, surplusStart, artificialStart));
                for (int j = 0; j <= totalVars; j++)
                    row.Add(tableau[i, j].ToString("F2"));
                sb.AppendLine(string.Join("\t", row));
            }

            List<string> objRow = new List<string>();
            objRow.Add("z-row");
            for (int j = 0; j <= totalVars; j++)
                objRow.Add(tableau[m, j].ToString("F2"));
            sb.AppendLine(string.Join("\t", objRow));

            return sb.ToString();
        }
    }
}