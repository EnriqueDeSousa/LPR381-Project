using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LpModel;

namespace CuttingPlane
{
    /// <summary>
    /// The result of solving an LpModel: the value of every original decision
    /// variable (already mapped back from any internal substitutions) and the
    /// resulting objective value. LpModel itself has no field to hold a
    /// solution, so this is a separate type rather than trying to reuse it.
    /// </summary>
    public class Solution
    {
        public List<double> VariableValues { get; }
        public double ObjectiveValue { get; }
        public bool IsFeasible { get; }

        public Solution(List<double> variableValues, double objectiveValue, bool isFeasible)
        {
            VariableValues = variableValues;
            ObjectiveValue = objectiveValue;
            IsFeasible = isFeasible;
        }

        public override string ToString()
        {
            if (!IsFeasible)
            {
                return "Infeasible";
            }
            return $"x = [{string.Join(", ", VariableValues)}], z = {ObjectiveValue}";
        }
    }

    /// <summary>
    /// Internal simplex tableau. Column order is: [structural columns for the
    /// expanded/substituted decision variables] [slack/surplus/artificial/cut
    /// columns, appended as needed] [RHS]. Only the structural columns are
    /// mapped back to the caller's original variables.
    /// </summary>
    internal class Tableau
    {
        public List<List<double>> Rows = new List<List<double>>();
        public List<double> ObjRow = new List<double>();
        public List<int> Basis = new List<int>();          // basic variable column index per row
        public List<bool> IsStructural = new List<bool>();  // per column (excludes RHS)
        public List<bool> IsArtificial = new List<bool>();  // per column (excludes RHS)

        // For each structural column: which original variable it came from,
        // and what to multiply its simplex value by before summing into that
        // variable's final value (handles the "-" and "free" substitutions:
        // a free variable x_j = y+ - y- contributes two structural columns
        // with multipliers +1 and -1 that both map to original index j).
        public List<(int originalIndex, double multiplier)> StructuralMap =
            new List<(int, double)>();

        public int NumCols => ObjRow.Count - 1; // excludes RHS

        public void AddColumnEverywhere(bool isStructural, bool isArtificial,
            (int originalIndex, double multiplier)? structuralMap = null)
        {
            foreach (var row in Rows) row.Insert(NumCols, 0.0);
            ObjRow.Insert(NumCols, 0.0);
            IsStructural.Add(isStructural);
            IsArtificial.Add(isArtificial);
            if (isStructural)
            {
                StructuralMap.Add(structuralMap ?? (-1, 1.0));
            }
        }

        public void Pivot(int pivotRow, int pivotCol)
        {
            var row = Rows[pivotRow];
            double pivotVal = row[pivotCol];
            for (int c = 0; c < row.Count; c++) row[c] /= pivotVal;

            for (int r = 0; r < Rows.Count; r++)
            {
                if (r == pivotRow) continue;
                double factor = Rows[r][pivotCol];
                if (Math.Abs(factor) < 1e-12) continue;
                for (int c = 0; c < row.Count; c++)
                {
                    Rows[r][c] -= factor * row[c];
                }
            }

            double objFactor = ObjRow[pivotCol];
            if (Math.Abs(objFactor) > 1e-12)
            {
                for (int c = 0; c < row.Count; c++)
                {
                    ObjRow[c] -= objFactor * row[c];
                }
            }

            Basis[pivotRow] = pivotCol;
        }
    }

    public class CuttingPlaneModel
    {
        private const double Epsilon = 1e-6;
        private const double BigM = 1_000_000.0;
        private const int MaxCuts = 200;
        private const int MaxSimplexIterations = 1000;

        /// <summary>
        /// Solves the LP relaxation of the model to optimality using the
        /// primal simplex method (Big-M variant, so it handles &lt;=, &gt;=
        /// and = constraints in a single pass).
        /// </summary>
        private void PrimalSimplex(Tableau t)
        {
            for (int iter = 0; iter < MaxSimplexIterations; iter++)
            {
                // Optimal once no column has a negative objective-row entry
                // (Bland's rule: always take the smallest such index, to
                // guard against cycling on degenerate tableaus).
                int enter = -1;
                for (int c = 0; c < t.NumCols; c++)
                {
                    if (t.ObjRow[c] < -Epsilon) { enter = c; break; }
                }
                if (enter == -1) return; // optimal

                int leave = -1;
                double bestRatio = double.PositiveInfinity;
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    double a = t.Rows[r][enter];
                    if (a > Epsilon)
                    {
                        double ratio = t.Rows[r][t.NumCols] / a;
                        if (ratio < bestRatio - Epsilon ||
                            (ratio < bestRatio + Epsilon && (leave == -1 || t.Basis[r] < t.Basis[leave])))
                        {
                            bestRatio = ratio;
                            leave = r;
                        }
                    }
                }
                if (leave == -1)
                {
                    throw new InvalidOperationException("LP relaxation is unbounded.");
                }
                t.Pivot(leave, enter);
            }
            throw new InvalidOperationException("Primal simplex did not converge within the iteration limit.");
        }

        /// <summary>
        /// Restores primal feasibility after a cut has been appended (the cut
        /// row starts with a negative RHS) while preserving the dual
        /// feasibility (optimality) of the existing tableau.
        /// </summary>
        private void DualSimplex(Tableau t)
        {
            for (int iter = 0; iter < MaxSimplexIterations; iter++)
            {
                int leave = -1;
                double mostNegative = -Epsilon;
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    double rhs = t.Rows[r][t.NumCols];
                    if (rhs < mostNegative)
                    {
                        mostNegative = rhs;
                        leave = r;
                    }
                }
                if (leave == -1) return; // primal-feasible again

                int enter = -1;
                double bestRatio = double.PositiveInfinity;
                for (int c = 0; c < t.NumCols; c++)
                {
                    double a = t.Rows[leave][c];
                    if (a < -Epsilon)
                    {
                        double ratio = Math.Abs(t.ObjRow[c] / a);
                        if (ratio < bestRatio - Epsilon ||
                            (ratio < bestRatio + Epsilon && (enter == -1 || c < enter)))
                        {
                            bestRatio = ratio;
                            enter = c;
                        }
                    }
                }
                if (enter == -1)
                {
                    throw new InvalidOperationException("Model is infeasible after adding a cut.");
                }
                t.Pivot(leave, enter);
            }
            throw new InvalidOperationException("Dual simplex did not converge within the iteration limit.");
        }

        /// <summary>
        /// Finds a basic, integer-restricted structural column whose current
        /// value is fractional, preferring the most fractional one. Returns
        /// -1 if every integer-restricted variable is already integral.
        /// </summary>
        private int FindFractionalIntegerRow(Tableau t, bool[] mustBeInteger)
        {
            int bestRow = -1;
            double bestDistanceFromHalf = double.PositiveInfinity;
            for (int r = 0; r < t.Rows.Count; r++)
            {
                int basicCol = t.Basis[r];
                if (basicCol >= t.NumCols) continue;
                if (!t.IsStructural[basicCol]) continue;
                if (!mustBeInteger[basicCol]) continue;

                double value = t.Rows[r][t.NumCols];
                double frac = value - Math.Floor(value);
                if (frac > Epsilon && frac < 1 - Epsilon)
                {
                    double distanceFromHalf = Math.Abs(frac - 0.5);
                    if (distanceFromHalf < bestDistanceFromHalf)
                    {
                        bestDistanceFromHalf = distanceFromHalf;
                        bestRow = r;
                    }
                }
            }
            return bestRow;
        }

        /// <summary>
        /// Appends a Gomory fractional cut derived from tableau row
        /// <paramref name="row"/>. The new row is intentionally primal
        /// infeasible (negative RHS); call DualSimplex afterwards.
        /// </summary>
        private void AddGomoryCut(Tableau t, int row)
        {
            double rhs = t.Rows[row][t.NumCols];
            double rhsFrac = rhs - Math.Floor(rhs);

            var cutRow = new List<double>();
            for (int c = 0; c < t.NumCols; c++)
            {
                double a = t.Rows[row][c];
                double aFrac = a - Math.Floor(a);
                cutRow.Add(-aFrac);
            }
            cutRow.Add(-rhsFrac);

            t.Rows.Add(cutRow);
            t.Basis.Add(-1); // placeholder, fixed up once the slack column exists

            t.AddColumnEverywhere(isStructural: false, isArtificial: false);
            int slackCol = t.NumCols - 1;
            t.Rows[t.Rows.Count - 1][slackCol] = 1.0;
            t.Basis[t.Rows.Count - 1] = slackCol;
        }

        /// <summary>
        /// Builds the initial Big-M tableau for the LP relaxation, handling:
        ///  - "+"/"int"      : variable used as-is, x &gt;= 0
        ///  - "bin"          : as "int", plus an added x &lt;= 1 row
        ///  - "-"            : substituted as x = -y, y &gt;= 0
        ///  - "free"/"urs"   : substituted as x = y+ - y-, y+, y- &gt;= 0
        /// and marks which structural columns must be integer in the final
        /// solution (bin/int).
        /// </summary>
        private Tableau BuildInitialTableau(LpModel.LpModel model, out bool[] mustBeInteger)
        {
            int objSens = model.GetObjSens();
            List<int> objFunc = model.GetObjFunc();
            List<List<int>> constraints = model.GetConstraints();
            List<string> signs = new List<string>(model.GetSigns());
            List<string> signRes = model.GetSignRes();
            int n = objFunc.Count;

            // ---- Expand/substitute variables so every simplex column is >= 0 ----
            var expandedObj = new List<double>();
            var mustBeIntegerList = new List<bool>();
            var structuralMap = new List<(int originalIndex, double multiplier)>();
            // colsForVar[j] = list of (columnIndexWithinExpansion, multiplier)
            var colsForVar = new List<int>[n];
            var extraBoundRows = new List<(int col, string sign, int rhs)>();

            for (int j = 0; j < n; j++)
            {
                string sign = (j < signRes.Count ? signRes[j] : "+").Trim().ToLowerInvariant();
                colsForVar[j] = new List<int>();

                switch (sign)
                {
                    case "-":
                        expandedObj.Add(-objFunc[j]);
                        structuralMap.Add((j, -1.0));
                        mustBeIntegerList.Add(false);
                        colsForVar[j].Add(expandedObj.Count - 1);
                        break;

                    case "free":
                    case "urs":
                        expandedObj.Add(objFunc[j]);
                        structuralMap.Add((j, 1.0));
                        mustBeIntegerList.Add(false);
                        colsForVar[j].Add(expandedObj.Count - 1);

                        expandedObj.Add(-objFunc[j]);
                        structuralMap.Add((j, -1.0));
                        mustBeIntegerList.Add(false);
                        colsForVar[j].Add(expandedObj.Count - 1);
                        break;

                    case "bin":
                        expandedObj.Add(objFunc[j]);
                        structuralMap.Add((j, 1.0));
                        mustBeIntegerList.Add(true);
                        colsForVar[j].Add(expandedObj.Count - 1);
                        extraBoundRows.Add((expandedObj.Count - 1, "<=", 1));
                        break;

                    case "int":
                        expandedObj.Add(objFunc[j]);
                        structuralMap.Add((j, 1.0));
                        mustBeIntegerList.Add(true);
                        colsForVar[j].Add(expandedObj.Count - 1);
                        break;

                    default: // "+" or anything unrecognized: treat as non-negative continuous
                        expandedObj.Add(objFunc[j]);
                        structuralMap.Add((j, 1.0));
                        mustBeIntegerList.Add(false);
                        colsForVar[j].Add(expandedObj.Count - 1);
                        break;
                }
            }

            int numStructuralCols = expandedObj.Count;

            // ---- Build expanded constraint rows (original constraints + bound rows) ----
            var rowCoeffs = new List<double[]>();
            var rowSigns = new List<string>();
            var rowRhs = new List<double>();

            foreach (var constraint in constraints)
            {
                var coeffs = new double[numStructuralCols];
                for (int j = 0; j < n; j++)
                {
                    foreach (int col in colsForVar[j])
                    {
                        double multiplier = structuralMap[col].multiplier;
                        coeffs[col] = constraint[j] * multiplier;
                    }
                }
                rowCoeffs.Add(coeffs);
                rowRhs.Add(constraint[constraint.Count - 1]);
            }
            rowSigns.AddRange(signs);

            foreach (var (col, sign, rhs) in extraBoundRows)
            {
                var coeffs = new double[numStructuralCols];
                coeffs[col] = 1.0;
                rowCoeffs.Add(coeffs);
                rowSigns.Add(sign);
                rowRhs.Add(rhs);
            }

            // ---- Normalize so every row has RHS >= 0 (flip sign+row if needed) ----
            for (int i = 0; i < rowRhs.Count; i++)
            {
                if (rowRhs[i] < 0)
                {
                    for (int c = 0; c < numStructuralCols; c++) rowCoeffs[i][c] *= -1;
                    rowRhs[i] *= -1;
                    rowSigns[i] = rowSigns[i] == "<=" ? ">=" : rowSigns[i] == ">=" ? "<=" : "=";
                }
            }

            // ---- Assemble the Big-M tableau ----
            var t = new Tableau();
            int m = rowCoeffs.Count;

            for (int c = 0; c < numStructuralCols; c++)
            {
                t.IsStructural.Add(true);
                t.IsArtificial.Add(false);
                t.StructuralMap.Add(structuralMap[c]);
            }
            // Objective row, internally always maximized: for a "min" model,
            // negate the coefficients first, then negate the final objective
            // value back when reporting the solution.
            double sensAdjustedObjSens = objSens; // 1 = max, -1 = min
            for (int c = 0; c < numStructuralCols; c++)
            {
                double cost = sensAdjustedObjSens * expandedObj[c];
                t.ObjRow.Add(-cost);
            }
            t.ObjRow.Add(0.0); // RHS / running objective value

            for (int i = 0; i < m; i++)
            {
                var row = new List<double>(rowCoeffs[i]);
                t.Rows.Add(row);
            }
            for (int i = 0; i < m; i++) t.Basis.Add(-1);

            for (int i = 0; i < m; i++)
            {
                string sign = rowSigns[i];
                if (sign == "<=")
                {
                    t.AddColumnEverywhere(isStructural: false, isArtificial: false);
                    int slackCol = t.NumCols - 1;
                    t.Rows[i][slackCol] = 1.0;
                    t.Basis[i] = slackCol;
                }
                else if (sign == ">=")
                {
                    t.AddColumnEverywhere(isStructural: false, isArtificial: false);
                    int surplusCol = t.NumCols - 1;
                    t.Rows[i][surplusCol] = -1.0;

                    t.AddColumnEverywhere(isStructural: false, isArtificial: true);
                    int artificialCol = t.NumCols - 1;
                    t.Rows[i][artificialCol] = 1.0;
                    t.ObjRow[artificialCol] = BigM;
                    t.Basis[i] = artificialCol;
                }
                else if (sign == "=")
                {
                    t.AddColumnEverywhere(isStructural: false, isArtificial: true);
                    int artificialCol = t.NumCols - 1;
                    t.Rows[i][artificialCol] = 1.0;
                    t.ObjRow[artificialCol] = BigM;
                    t.Basis[i] = artificialCol;
                }
                else
                {
                    throw new InvalidDataException($"Unrecognized constraint sign \"{sign}\".");
                }
                t.Rows[i].Add(rowRhs[i]); // RHS goes last on the row
            }
            // Put the tableau into canonical form: zero out the objective
            // row's entries for the initial basic (artificial) variables.
            for (int i = 0; i < m; i++)
            {
                int basicCol = t.Basis[i];
                double coeff = t.ObjRow[basicCol];
                if (Math.Abs(coeff) > 1e-12)
                {
                    for (int c = 0; c < t.Rows[i].Count; c++)
                    {
                        t.ObjRow[c] -= coeff * t.Rows[i][c];
                    }
                }
            }

            mustBeInteger = new bool[t.NumCols];
            for (int c = 0; c < numStructuralCols; c++) mustBeInteger[c] = mustBeIntegerList[c];

            return t;
        }

        /// <summary>
        /// After the primal simplex reaches optimality, an artificial
        /// variable left basic with a nonzero value means no assignment of
        /// the real variables can satisfy every constraint: the model itself
        /// is infeasible (this is unrelated to the integer cuts).
        /// </summary>
        private bool HasFeasibleBasis(Tableau t)
        {
            for (int r = 0; r < t.Rows.Count; r++)
            {
                int basicCol = t.Basis[r];
                if (t.IsArtificial[basicCol] && t.Rows[r][t.NumCols] > Epsilon)
                {
                    return false;
                }
            }
            return true;
        }

        private Solution ExtractSolution(Tableau t, LpModel.LpModel model)
        {
            int n = model.GetObjFunc().Count;
            var originalValues = new double[n];

            for (int r = 0; r < t.Rows.Count; r++)
            {
                int basicCol = t.Basis[r];
                if (basicCol < t.NumCols && t.IsStructural[basicCol])
                {
                    var (originalIndex, multiplier) = t.StructuralMap[basicCol];
                    originalValues[originalIndex] += multiplier * t.Rows[r][t.NumCols];
                }
            }

            // Round away floating-point noise for variables that must be integral.
            List<string> signRes = model.GetSignRes();
            for (int j = 0; j < n; j++)
            {
                string sign = (j < signRes.Count ? signRes[j] : "+").Trim().ToLowerInvariant();
                if (sign == "bin" || sign == "int")
                {
                    originalValues[j] = Math.Round(originalValues[j]);
                }
            }

            double objectiveValue = 0;
            List<int> objFunc = model.GetObjFunc();
            for (int j = 0; j < n; j++) objectiveValue += objFunc[j] * originalValues[j];

            return new Solution(originalValues.ToList(), objectiveValue, isFeasible: true);
        }

        /// <summary>
        /// Solves the given integer/binary linear program using the cutting
        /// plane (Gomory) method: solve the LP relaxation, then repeatedly
        /// add a Gomory cut and re-optimize with the dual simplex until every
        /// integer-restricted variable is integral.
        /// </summary>
        public Solution Solve(LpModel.LpModel model)
        {
            Tableau t = BuildInitialTableau(model, out bool[] mustBeInteger);

            try
            {
                PrimalSimplex(t);
            }
            catch (InvalidOperationException)
            {
                return new Solution(new List<double>(), 0, isFeasible: false);
            }

            if (!HasFeasibleBasis(t))
            {
                return new Solution(new List<double>(), 0, isFeasible: false);
            }

            int cutsAdded = 0;
            int fractionalRow;
            while (cutsAdded < MaxCuts && (fractionalRow = FindFractionalIntegerRow(t, mustBeInteger)) != -1)
            {
                AddGomoryCut(t, fractionalRow);
                try
                {
                    DualSimplex(t);
                }
                catch (InvalidOperationException)
                {
                    return new Solution(new List<double>(), 0, isFeasible: false);
                }
                cutsAdded++;
            }

            return ExtractSolution(t, model);
        }
    }
}
