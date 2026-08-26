using System;
using System.Linq;
using System.Text;
using LPR381Solver.Models;
using LPR381Solver.Solvers;

namespace LPR381Solver.IO
{
    public static class OutputWriter
    {
        public static string FormatCanonicalForm(StandardForm sf)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Canonical Form ===");
            sb.Append(sf.OriginalWasMin ? "min " : "max ");
            sb.Append("(solved internally as max) Z = ");
            sb.AppendLine(string.Join(" + ", Enumerable.Range(0, sf.NumCols)
                .Where(j => !sf.IsArtificial[j] || true)
                .Select(j => $"{Fmt(sf.c[j])}{sf.ColNames[j]}")));
            for (int i = 0; i < sf.NumRows; i++)
            {
                var terms = Enumerable.Range(0, sf.NumCols).Select(j => $"{Fmt(sf.A[i, j])}{sf.ColNames[j]}");
                sb.AppendLine($"  {string.Join(" + ", terms)} = {sf.b[i]:F3}");
            }
            sb.AppendLine("  all variables >= 0");
            sb.AppendLine();
            return sb.ToString();
        }

        public static string FormatSnapshot(TableauSnapshot snap, StandardForm sf)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- {snap.Note} ---");
            int m = snap.Basis.Length;
            int ncols = sf.NumCols;

            sb.Append("Basis\\Col".PadRight(10));
            for (int j = 0; j < ncols; j++) sb.Append(sf.ColNames[j].PadLeft(10));
            sb.AppendLine("RHS".PadLeft(10));

            for (int i = 0; i < m; i++)
            {
                sb.Append(sf.ColNames[snap.Basis[i]].PadRight(10));
                for (int j = 0; j < ncols; j++) sb.Append(Round3(snap.Tableau[i, j]).PadLeft(10));
                sb.AppendLine(Round3(snap.Tableau[i, ncols]).PadLeft(10));
            }
            sb.Append("z (cj-zj)".PadRight(10));
            for (int j = 0; j < ncols; j++) sb.Append(Round3(snap.Tableau[m, j]).PadLeft(10));
            sb.AppendLine(Round3(snap.Tableau[m, ncols]).PadLeft(10));

            if (snap.EnteringCol >= 0)
                sb.AppendLine($"  Entering: {sf.ColNames[snap.EnteringCol]}   Leaving: {sf.ColNames[snap.Basis[snap.LeavingRow]]}");
            sb.AppendLine();
            return sb.ToString();
        }

        public static string FormatPrimalResult(SimplexResult r, StandardForm sf, LPModel model)
        {
            var sb = new StringBuilder();
            sb.Append(FormatCanonicalForm(sf));
            foreach (var snap in r.Iterations) sb.Append(FormatSnapshot(snap, sf));

            sb.AppendLine("=== Result ===");
            sb.AppendLine($"Status: {r.Status}");
            if (r.Status == SolveStatus.Optimal)
            {
                sb.AppendLine($"Optimal Z = {r.ObjectiveValue:F3}");
                for (int j = 0; j < model.NumVars; j++)
                    sb.AppendLine($"  x{j + 1} = {r.OriginalSolution[j]:F3}");
            }
            else
            {
                sb.AppendLine(r.Message);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        public static string FormatRevisedIteration(RevisedIteration it, StandardForm sf)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- Iteration {it.IterationNumber} ---");
            sb.AppendLine("B^-1:");
            int m = it.Basis.Length;
            for (int i = 0; i < m; i++)
                sb.AppendLine("  " + string.Join("  ", Enumerable.Range(0, m).Select(j => Round3(it.BInverse[i, j]).PadLeft(9))));

            sb.AppendLine($"Basis: {string.Join(", ", it.Basis.Select(b => sf.ColNames[b]))}");
            sb.AppendLine($"Price vector y = cB*B^-1: [{string.Join(", ", it.Price.Select(v => v.ToString("F3")))}]");
            sb.AppendLine($"x_B (B^-1 * b): [{string.Join(", ", it.XB.Select(v => v.ToString("F3")))}]");
            sb.AppendLine("Priced-out reduced costs:");
            for (int j = 0; j < sf.NumCols; j++)
                sb.Append($"  {sf.ColNames[j]}={it.ReducedCosts[j]:F3}");
            sb.AppendLine();
            sb.AppendLine($"Objective value this iteration: {it.ObjectiveValue:F3}");
            if (it.Entering >= 0)
                sb.AppendLine($"Entering: {sf.ColNames[it.Entering]}   Leaving: {sf.ColNames[it.Basis[it.Leaving]]}   " +
                               $"(eta/direction column d = [{string.Join(", ", it.EtaColumn.Select(v => v.ToString("F3")))}])");
            sb.AppendLine();
            return sb.ToString();
        }

        public static string FormatRevisedResult(RevisedSimplexResult r, StandardForm sf, LPModel model)
        {
            var sb = new StringBuilder();
            sb.Append(FormatCanonicalForm(sf));
            foreach (var it in r.Iterations) sb.Append(FormatRevisedIteration(it, sf));

            sb.AppendLine("=== Result ===");
            sb.AppendLine($"Status: {r.Status}");
            if (r.Status == SolveStatus.Optimal)
            {
                sb.AppendLine($"Optimal Z = {r.ObjectiveValue:F3}");
                for (int j = 0; j < model.NumVars; j++)
                    sb.AppendLine($"  x{j + 1} = {r.OriginalSolution[j]:F3}");
            }
            else
            {
                sb.AppendLine(r.Message);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private static string Fmt(double v) => v >= 0 ? $"+{v:0.###}" : $"{v:0.###}";
        private static string Round3(double v) => Math.Round(v, 3).ToString("0.000");
    }
}
