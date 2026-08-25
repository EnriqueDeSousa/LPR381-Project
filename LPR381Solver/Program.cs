using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LPR381Solver.IO;
using LPR381Solver.Models;
using LPR381Solver.Solvers;

namespace LPR381Solver
{
    /// <summary>
    /// Menu-driven entry point. Builds solve.exe as required by the assignment brief:
    ///   - Reads an input text file with the mathematical model.
    ///   - Lets the user pick Primal Simplex or Revised Primal Simplex.
    ///   - Displays the canonical form and every iteration.
    ///   - Writes everything to an output text file.
    ///   - Offers a sensitivity-analysis sub-menu once an optimal solution exists.
    /// </summary>
    public static class Program
    {
        private static LPModel? _model;
        private static StandardForm? _sf;
        private static SimplexResult? _primalResult;      // used as the basis for all sensitivity analysis
        private static RevisedSimplexResult? _revisedResult;
        private static string _outputPath = "output.txt";

        public static void Main(string[] args)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("  LPR381 - LP/IP Solver (solve.exe)");
            Console.WriteLine("=========================================");

            if (args.Length > 0 && File.Exists(args[0]))
                LoadModel(args[0]);

            bool running = true;
            while (running)
            {
                Console.WriteLine();
                Console.WriteLine("MAIN MENU");
                Console.WriteLine(" 1) Load input file");
                Console.WriteLine(" 2) Solve with Primal Simplex");
                Console.WriteLine(" 3) Solve with Revised Primal Simplex");
                Console.WriteLine(" 4) Sensitivity Analysis");
                Console.WriteLine(" 5) Show canonical form");
                Console.WriteLine(" 0) Exit");
                Console.Write("Choose an option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": PromptLoadModel(); break;
                    case "2": RunPrimal(); break;
                    case "3": RunRevised(); break;
                    case "4": SensitivityMenu(); break;
                    case "5": ShowCanonicalForm(); break;
                    case "0": running = false; break;
                    default: Console.WriteLine("Unrecognised option."); break;
                }
            }
        }

        private static void PromptLoadModel()
        {
            Console.Write("Path to input file: ");
            var path = Console.ReadLine()?.Trim() ?? "";
            LoadModel(path);
        }

        private static void LoadModel(string path)
        {
            try
            {
                _model = InputParser.Parse(path);
                _sf = StandardForm.Build(_model);
                _primalResult = null;
                _revisedResult = null;
                Console.WriteLine($"Loaded model: {_model.NumVars} variables, {_model.NumConstraints} constraints, " +
                                   $"{(_model.IsMax ? "maximise" : "minimise")}.");
                if (_model.HasIntegerOrBinaryVars)
                    Console.WriteLine("Note: int/bin variables are solved here as their LP relaxation " +
                                       "(this build covers Primal Simplex, Revised Simplex, and Sensitivity Analysis only).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load model: {ex.Message}");
            }
        }

        private static bool RequireModel()
        {
            if (_model == null || _sf == null)
            {
                Console.WriteLine("No model loaded yet -- use option 1 first.");
                return false;
            }
            return true;
        }

        private static void ShowCanonicalForm()
        {
            if (!RequireModel()) return;
            Console.WriteLine(OutputWriter.FormatCanonicalForm(_sf!));
        }

        private static void RunPrimal()
        {
            if (!RequireModel()) return;
            _primalResult = PrimalSimplex.Solve(_sf!);
            string text = OutputWriter.FormatPrimalResult(_primalResult, _sf!, _model!);
            Console.WriteLine(text);
            File.WriteAllText(_outputPath, text);
            Console.WriteLine($"(Also written to {_outputPath})");
        }

        private static void RunRevised()
        {
            if (!RequireModel()) return;
            _revisedResult = RevisedPrimalSimplex.Solve(_sf!);
            string text = OutputWriter.FormatRevisedResult(_revisedResult, _sf!, _model!);
            Console.WriteLine(text);
            File.WriteAllText(_outputPath, text);
            Console.WriteLine($"(Also written to {_outputPath})");

            // Sensitivity analysis is implemented against the tableau-form PrimalSimplex result,
            // so quietly keep one in sync whenever Revised Simplex reaches an optimum too.
            if (_revisedResult.Status == SolveStatus.Optimal)
                _primalResult = PrimalSimplex.Solve(_sf!);
        }

        private static void SensitivityMenu()
        {
            if (!RequireModel()) return;
            if (_primalResult == null) _primalResult = PrimalSimplex.Solve(_sf!);
            if (_primalResult.Status != SolveStatus.Optimal)
            {
                Console.WriteLine($"Cannot run sensitivity analysis: last solve status was {_primalResult.Status}.");
                return;
            }

            bool back = false;
            while (!back)
            {
                Console.WriteLine();
                Console.WriteLine("SENSITIVITY ANALYSIS");
                Console.WriteLine(" 1) Display shadow prices");
                Console.WriteLine(" 2) Range of a variable's objective coefficient (basic or non-basic)");
                Console.WriteLine(" 3) Apply a change to a variable's objective coefficient");
                Console.WriteLine(" 4) Range of a constraint's RHS");
                Console.WriteLine(" 5) Apply a change to a constraint's RHS");
                Console.WriteLine(" 6) Add a new activity (decision variable)");
                Console.WriteLine(" 7) Add a new constraint");
                Console.WriteLine(" 8) Duality (build dual, solve, verify strong/weak duality)");
                Console.WriteLine(" 0) Back");
                Console.Write("Choose an option: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": ShowShadowPrices(); break;
                    case "2": ShowVariableRange(); break;
                    case "3": ApplyVariableChange(); break;
                    case "4": ShowRhsRange(); break;
                    case "5": ApplyRhsChange(); break;
                    case "6": AddNewActivity(); break;
                    case "7": AddNewConstraint(); break;
                    case "8": RunDuality(); break;
                    case "0": back = true; break;
                    default: Console.WriteLine("Unrecognised option."); break;
                }
            }
        }

        private static void PrintColumnList()
        {
            Console.WriteLine("Columns: " + string.Join(", ", Enumerable.Range(0, _sf!.NumCols)
                .Select(j => $"{j}={_sf.ColNames[j]}")));
        }

        private static int ReadColumnIndex()
        {
            PrintColumnList();
            Console.Write("Column index: ");
            return int.TryParse(Console.ReadLine(), out int v) ? v : -1;
        }

        private static void ShowShadowPrices()
        {
            var y = SensitivityAnalysis.ShadowPrices(_primalResult!, _sf!);
            for (int i = 0; i < y.Length; i++)
                Console.WriteLine($"  Constraint {i + 1}: shadow price = {y[i]:F3}");
        }

        private static void ShowVariableRange()
        {
            int col = ReadColumnIndex();
            if (col < 0 || col >= _sf!.NumCols) { Console.WriteLine("Invalid column."); return; }
            var range = SensitivityAnalysis.RangeOfVariable(_primalResult!, _sf!, col);
            bool basic = _primalResult!.FinalBasis.Contains(col);
            Console.WriteLine($"  {_sf.ColNames[col]} is {(basic ? "BASIC" : "NON-BASIC")}. " +
                               $"Allowable range for its objective coefficient: {range}");
        }

        private static void ApplyVariableChange()
        {
            int col = ReadColumnIndex();
            if (col < 0 || col >= _sf!.NumCols) { Console.WriteLine("Invalid column."); return; }
            Console.Write("New objective coefficient value: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double newVal))
            { Console.WriteLine("Invalid number."); return; }

            var range = SensitivityAnalysis.RangeOfVariable(_primalResult!, _sf!, col);
            bool withinRange = (range.LowerIsInfinite || newVal >= range.Lower) &&
                                (range.UpperIsInfinite || newVal <= range.Upper);
            Console.WriteLine(withinRange
                ? $"  {newVal:F3} is within the allowable range {range} -- the current basis stays optimal."
                : $"  {newVal:F3} is OUTSIDE the allowable range {range} -- re-solving is required to find the new optimum.");
        }

        private static void ShowRhsRange()
        {
            Console.Write($"Constraint row (1..{_sf!.NumRows}): ");
            if (!int.TryParse(Console.ReadLine(), out int row) || row < 1 || row > _sf.NumRows)
            { Console.WriteLine("Invalid row."); return; }
            var range = SensitivityAnalysis.RangeRhs(_primalResult!, _sf, row - 1);
            Console.WriteLine($"  Allowable RHS range for constraint {row}: {range}");
        }

        private static void ApplyRhsChange()
        {
            Console.Write($"Constraint row (1..{_sf!.NumRows}): ");
            if (!int.TryParse(Console.ReadLine(), out int row) || row < 1 || row > _sf.NumRows)
            { Console.WriteLine("Invalid row."); return; }
            Console.Write("New RHS value: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double newRhs))
            { Console.WriteLine("Invalid number."); return; }

            var (newXB, newObj, feasible) = SensitivityAnalysis.ApplyRhsChange(_primalResult!, _sf, row - 1, newRhs);
            Console.WriteLine(feasible
                ? $"  Still feasible. New objective value = {newObj:F3}."
                : "  This change makes the current basis INFEASIBLE (a basic variable would go negative); " +
                  "re-solving (e.g. dual simplex) is required.");
        }

        private static void AddNewActivity()
        {
            Console.Write("Objective coefficient of the new variable: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double obj))
            { Console.WriteLine("Invalid number."); return; }

            var col = new double[_sf!.NumRows];
            for (int i = 0; i < _sf.NumRows; i++)
            {
                Console.Write($"Coefficient in constraint {i + 1}: ");
                double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out col[i]);
            }

            var (reduced, wouldImprove) = SensitivityAnalysis.EvaluateNewActivity(_primalResult!, _sf, obj, col);
            Console.WriteLine($"  Priced-out reduced cost of the new activity: {reduced:F3}");
            Console.WriteLine(wouldImprove
                ? "  This activity WOULD improve the solution -- add it to the model and re-solve."
                : "  This activity would NOT improve the solution -- the current optimum stays optimal.");
        }

        private static void AddNewConstraint()
        {
            var coeffs = new double[_model!.NumVars];
            for (int j = 0; j < _model.NumVars; j++)
            {
                Console.Write($"Coefficient for x{j + 1}: ");
                double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out coeffs[j]);
            }
            Console.Write("Relation (<=, >=, =): ");
            var relToken = Console.ReadLine()?.Trim() ?? "<=";
            var relation = relToken switch { ">=" => Relation.GreaterOrEqual, "=" => Relation.Equal, _ => Relation.LessOrEqual };
            Console.Write("RHS: ");
            double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double rhs);

            var (lhs, ok) = SensitivityAnalysis.EvaluateNewConstraint(_primalResult!, coeffs, relation, rhs);
            Console.WriteLine($"  Current solution gives LHS = {lhs:F3}.");
            Console.WriteLine(ok
                ? "  The current optimal solution already satisfies this constraint -- it stays optimal."
                : "  The current optimal solution VIOLATES this constraint -- add it to the input file and re-solve.");
        }

        private static void RunDuality()
        {
            var dualModel = Duality.BuildDual(_model!);
            var dualSf = StandardForm.Build(dualModel);
            var dualResult = PrimalSimplex.Solve(dualSf);

            Console.WriteLine(OutputWriter.FormatCanonicalForm(dualSf));
            Console.WriteLine($"Dual status: {dualResult.Status}");
            if (dualResult.Status == SolveStatus.Optimal)
            {
                Console.WriteLine($"Dual optimal Z = {dualResult.ObjectiveValue:F3}");
                for (int i = 0; i < dualModel.NumVars; i++)
                    Console.WriteLine($"  y{i + 1} = {dualResult.OriginalSolution[i]:F3}");
                Console.WriteLine(Duality.CheckDuality(_primalResult!.ObjectiveValue, dualResult.ObjectiveValue));
            }
            else
            {
                Console.WriteLine(dualResult.Message);
            }
        }
    }
}
