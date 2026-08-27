using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381Solver.Models
{
    public enum VarMapType { Direct, Negated, Split }

    /// <summary>
    /// Records how one original decision variable maps onto column(s) of the standard-form matrix,
    /// so that once the standard-form problem is solved we can translate the answer back.
    /// </summary>
    public class VariableMapping
    {
        public int OriginalIndex;
        public VarMapType Type;
        public int Col;      // used for Direct / Negated
        public int PosCol;   // used for Split
        public int NegCol;   // used for Split
        public string Name = "";
    }

    /// <summary>
    /// The standard (canonical, Big-M) form of the model:
    ///   max  c^T x
    ///   s.t. A x = b,  x >= 0
    /// All original >=/=/negative/unrestricted/binary handling is folded in here, so the two
    /// simplex solvers only ever need to deal with a straightforward equality-constrained max problem.
    /// </summary>
    public class StandardForm
    {
        public const double BigM = 1_000_000d;

        public double[,] A = new double[0, 0];
        public double[] b = System.Array.Empty<double>();
        public double[] c = System.Array.Empty<double>();
        public string[] ColNames = System.Array.Empty<string>();
        public bool[] IsArtificial = System.Array.Empty<bool>();
        public int[] InitialBasis = System.Array.Empty<int>();

        public bool OriginalWasMin;
        public List<VariableMapping> Mappings = new();

        public int NumRows => A.GetLength(0);
        public int NumCols => A.GetLength(1);

        /// <summary>Recovers original decision-variable values from a standard-form solution vector.</summary>
        public double[] RecoverOriginalValues(double[] xStandard)
        {
            var result = new double[Mappings.Count];
            foreach (var map in Mappings)
            {
                result[map.OriginalIndex] = map.Type switch
                {
                    VarMapType.Direct => xStandard[map.Col],
                    VarMapType.Negated => -xStandard[map.Col],
                    VarMapType.Split => xStandard[map.PosCol] - xStandard[map.NegCol],
                    _ => throw new InvalidOperationException()
                };
            }
            return result;
        }

        public static StandardForm Build(LPModel model)
        {
            var sf = new StandardForm { OriginalWasMin = !model.IsMax };
            int n = model.NumVars;

            // ---- Decide column layout for each original variable ----
            var mappings = new List<VariableMapping>();
            var colBuilders = new List<(VariableMapping map, double objSign, int which)>(); // which: 0=direct/neg, 1=pos,2=neg
            int colCount = 0;
            var colOrigCoeffSign = new List<double>();  // multiplier applied to original column data (a_ij, c_j) for this std column
            var colOwner = new List<VariableMapping>();
            var colKind = new List<int>(); // 0 = direct-owner col, 1 = pos col of split, 2 = neg col of split

            for (int j = 0; j < n; j++)
            {
                var restriction = model.SignRestrictions.Length > j ? model.SignRestrictions[j] : SignRestriction.Positive;
                var map = new VariableMapping { OriginalIndex = j, Name = $"x{j + 1}" };

                if (restriction == SignRestriction.Negative)
                {
                    map.Type = VarMapType.Negated;
                    map.Col = colCount++;
                    colOrigCoeffSign.Add(-1.0);
                    colOwner.Add(map); colKind.Add(0);
                }
                else if (restriction == SignRestriction.Unrestricted)
                {
                    map.Type = VarMapType.Split;
                    map.PosCol = colCount++;
                    colOrigCoeffSign.Add(1.0); colOwner.Add(map); colKind.Add(1);
                    map.NegCol = colCount++;
                    colOrigCoeffSign.Add(-1.0); colOwner.Add(map); colKind.Add(2);
                }
                else
                {
                    // Positive, Integer, Binary all live as a single nonnegative column.
                    // NOTE: Integer/Binary are solved here as their LP relaxation (Binary gets an
                    // explicit x <= 1 row added below). Branch & Bound is outside the scope of
                    // this deliverable (primal simplex / revised simplex / sensitivity analysis).
                    map.Type = VarMapType.Direct;
                    map.Col = colCount++;
                    colOrigCoeffSign.Add(1.0);
                    colOwner.Add(map); colKind.Add(0);
                }
                mappings.Add(map);
            }

            int numDecisionCols = colCount;
            int numOriginalConstraints = model.NumConstraints;

            // Extra rows for binary upper bounds (x_j <= 1)
            var binaryRows = new List<int>(); // original variable indices needing x<=1 row
            for (int j = 0; j < n; j++)
                if (model.SignRestrictions.Length > j && model.SignRestrictions[j] == SignRestriction.Binary)
                    binaryRows.Add(j);

            int totalConstraintRows = numOriginalConstraints + binaryRows.Count;

            // ---- First pass: figure out how many slack/surplus/artificial columns we need ----
            var relations = new Relation[totalConstraintRows];
            var rhs = new double[totalConstraintRows];
            var rowCoeffs = new double[totalConstraintRows][]; // over the numDecisionCols standard columns

            for (int i = 0; i < numOriginalConstraints; i++)
            {
                var cons = model.Constraints[i];
                var rowVec = new double[numDecisionCols];
                for (int j = 0; j < n; j++)
                {
                    var map = mappings[j];
                    if (map.Type == VarMapType.Direct) rowVec[map.Col] = cons.Coefficients[j];
                    else if (map.Type == VarMapType.Negated) rowVec[map.Col] = -cons.Coefficients[j];
                    else { rowVec[map.PosCol] = cons.Coefficients[j]; rowVec[map.NegCol] = -cons.Coefficients[j]; }
                }
                double r = cons.Rhs;
                var relation = cons.Relation;
                if (r < 0)
                {
                    // flip the whole row so RHS >= 0
                    for (int k = 0; k < numDecisionCols; k++) rowVec[k] = -rowVec[k];
                    r = -r;
                    relation = relation switch
                    {
                        Relation.LessOrEqual => Relation.GreaterOrEqual,
                        Relation.GreaterOrEqual => Relation.LessOrEqual,
                        _ => Relation.Equal
                    };
                }
                rowCoeffs[i] = rowVec;
                rhs[i] = r;
                relations[i] = relation;
            }
            for (int k = 0; k < binaryRows.Count; k++)
            {
                int rowIdx = numOriginalConstraints + k;
                var rowVec = new double[numDecisionCols];
                var map = mappings[binaryRows[k]];
                rowVec[map.Col] = 1.0; // Direct column only (binary vars are never negated/split)
                rowCoeffs[rowIdx] = rowVec;
                rhs[rowIdx] = 1.0;
                relations[rowIdx] = Relation.LessOrEqual;
            }

            int numSlackSurplus = 0, numArtificial = 0;
            foreach (var rel in relations)
            {
                if (rel == Relation.LessOrEqual) numSlackSurplus++;
                else if (rel == Relation.GreaterOrEqual) { numSlackSurplus++; numArtificial++; }
                else numArtificial++; // Equal
            }

            int totalCols = numDecisionCols + numSlackSurplus + numArtificial;
            var A = new double[totalConstraintRows, totalCols];
            var c = new double[totalCols];
            var colNames = new string[totalCols];
            var isArtificial = new bool[totalCols];
            var basis = new int[totalConstraintRows];

            // decision columns
            for (int j = 0; j < numDecisionCols; j++) colNames[j] = colOwner[j].Name + (colKind[j] == 1 ? "+" : colKind[j] == 2 ? "-" : "");
            // fix names for split vars distinctly
            for (int j = 0; j < numDecisionCols; j++)
            {
                if (colKind[j] == 1) colNames[j] = colOwner[j].Name + "p";
                else if (colKind[j] == 2) colNames[j] = colOwner[j].Name + "n";
                else colNames[j] = colOwner[j].Name;
            }

            // objective coefficients on decision columns
            for (int j = 0; j < n; j++)
            {
                var map = mappings[j];
                double objCoeff = model.ObjectiveCoefficients[j] * (model.IsMax ? 1.0 : -1.0); // convert min -> max
                if (map.Type == VarMapType.Direct) c[map.Col] = objCoeff;
                else if (map.Type == VarMapType.Negated) c[map.Col] = -objCoeff;
                else { c[map.PosCol] = objCoeff; c[map.NegCol] = -objCoeff; }
            }

            int slackCol = numDecisionCols;
            int artCol = numDecisionCols + numSlackSurplus;

            for (int i = 0; i < totalConstraintRows; i++)
            {
                for (int j = 0; j < numDecisionCols; j++) A[i, j] = rowCoeffs[i][j];

                switch (relations[i])
                {
                    case Relation.LessOrEqual:
                        A[i, slackCol] = 1.0;
                        colNames[slackCol] = $"s{i + 1}";
                        c[slackCol] = 0.0;
                        basis[i] = slackCol;
                        slackCol++;
                        break;
                    case Relation.GreaterOrEqual:
                        A[i, slackCol] = -1.0;
                        colNames[slackCol] = $"e{i + 1}"; // surplus/excess
                        c[slackCol] = 0.0;
                        slackCol++;
                        A[i, artCol] = 1.0;
                        colNames[artCol] = $"a{i + 1}";
                        c[artCol] = -BigM;
                        isArtificial[artCol] = true;
                        basis[i] = artCol;
                        artCol++;
                        break;
                    case Relation.Equal:
                        A[i, artCol] = 1.0;
                        colNames[artCol] = $"a{i + 1}";
                        c[artCol] = -BigM;
                        isArtificial[artCol] = true;
                        basis[i] = artCol;
                        artCol++;
                        break;
                }
            }

            sf.A = A;
            sf.b = rhs;
            sf.c = c;
            sf.ColNames = colNames;
            sf.IsArtificial = isArtificial;
            sf.InitialBasis = basis;
            sf.Mappings = mappings;
            return sf;
        }
    }
}
