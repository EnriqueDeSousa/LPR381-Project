using System.Collections.Generic;

namespace LPR381Solver.Models
{
    public enum SolveStatus { Optimal, Infeasible, Unbounded }

    /// <summary>A snapshot of one tableau iteration, kept for display in the output file / console.</summary>
    public class TableauSnapshot
    {
        public int IterationNumber;
        public double[,] Tableau = new double[0, 0];   // rows = constraints + 1 (obj row last), cols = NumCols+1 (rhs last)
        public int[] Basis = System.Array.Empty<int>();
        public int EnteringCol = -1;   // -1 if none (e.g. final/optimal snapshot)
        public int LeavingRow = -1;
        public string Note = "";
    }

    public class SimplexResult
    {
        public SolveStatus Status;
        public double ObjectiveValue;
        public double[] StandardSolution = System.Array.Empty<double>();
        public double[] OriginalSolution = System.Array.Empty<double>();
        public int[] FinalBasis = System.Array.Empty<int>();
        public double[,] FinalTableau = new double[0, 0];
        public List<TableauSnapshot> Iterations { get; } = new();
        public StandardForm StandardForm = null!;
        public string Message = "";
    }
}
