using System;
using System.Collections.Generic;
using System.Linq;

namespace BranchAndBoundSimplex
{
    public class BranchAndBoundResult
    {
        public bool Found { get; set; }
        public double[] Solution { get; set; } = Array.Empty<double>();
        public double Objective { get; set; }
        public int NodesExplored { get; set; }
    }

    public class BranchAndBoundSolver
    {
        private const double Tolerance = 1e-6;

        private readonly HashSet<int> _integerVariables;
        private readonly bool _verboseSimplex;
        private double _bestObjective = double.NegativeInfinity;
        private double[]? _bestSolution;
        private int _nodeCounter;

        public BranchAndBoundSolver(IEnumerable<int> integerVariableIndices, bool verboseSimplex = false)
        {
            _integerVariables = new HashSet<int>(integerVariableIndices);
            _verboseSimplex = verboseSimplex;
        }

        public BranchAndBoundResult Solve(LinearProgram rootLp)
        {
            var stack = new Stack<(LinearProgram lp, int parentId, string description)>();
            stack.Push((rootLp, 0, "Root (LP relaxation)"));

            while (stack.Count > 0)
            {
                var (lp, parentId, description) = stack.Pop();
                _nodeCounter++;
                int nodeId = _nodeCounter;

                Console.WriteLine();
                Console.WriteLine($"=== Node {nodeId} (parent {parentId}) : {description} ===");

                var result = SimplexSolver.Solve(lp, _verboseSimplex);
                if (_verboseSimplex)
                    foreach (var line in result.Log) Console.WriteLine(line);

                if (result.Status == SimplexStatus.Infeasible)
                {
                    Console.WriteLine($"Node {nodeId}: LP relaxation is infeasible -> pruned.");
                    continue;
                }
                if (result.Status == SimplexStatus.Unbounded)
                {
                    Console.WriteLine($"Node {nodeId}: LP relaxation is unbounded -> pruned (check your model).");
                    continue;
                }

                Console.WriteLine($"Node {nodeId}: relaxation objective = {result.ObjectiveValue:F4}, " +
                                   $"x = [{string.Join(", ", result.VariableValues.Select(v => v.ToString("F4")))}]");

                // Bound: if even the relaxed optimum can't beat the best
                // integer solution found so far, this branch is useless.
                if (result.ObjectiveValue <= _bestObjective + Tolerance)
                {
                    Console.WriteLine($"Node {nodeId}: bound {result.ObjectiveValue:F4} does not improve on " +
                                       $"incumbent {_bestObjective:F4} -> pruned by bound.");
                    continue;
                }

                int branchVar = ChooseBranchingVariable(result.VariableValues, out double fractionalValue);

                if (branchVar == -1)
                {
                    // All required variables are (numerically) integral.
                    Console.WriteLine($"Node {nodeId}: solution is integer-feasible, objective = {result.ObjectiveValue:F4}");
                    if (result.ObjectiveValue > _bestObjective)
                    {
                        _bestObjective = result.ObjectiveValue;
                        _bestSolution = result.VariableValues;
                        Console.WriteLine($"Node {nodeId}: *** new incumbent solution ***");
                    }
                    continue;
                }

                double floorVal = Math.Floor(fractionalValue);
                double ceilVal = Math.Ceiling(fractionalValue);
                string varName = lp.VariableNames[branchVar];

                Console.WriteLine($"Node {nodeId}: branching on {varName} = {fractionalValue:F4}  ->  " +
                                   $"{varName} <= {floorVal}  and  {varName} >= {ceilVal}");

                var leftChild = lp.WithExtraConstraint(branchVar, Relation.LE, floorVal);
                var rightChild = lp.WithExtraConstraint(branchVar, Relation.GE, ceilVal);

                // Push right first so the left (<=) branch is explored first (DFS order is cosmetic here).
                stack.Push((rightChild, nodeId, $"{varName} >= {ceilVal}"));
                stack.Push((leftChild, nodeId, $"{varName} <= {floorVal}"));
            }

            return new BranchAndBoundResult
            {
                Found = _bestSolution != null,
                Solution = _bestSolution ?? Array.Empty<double>(),
                Objective = _bestObjective,
                NodesExplored = _nodeCounter
            };
        }

        private int ChooseBranchingVariable(double[] values, out double fractionalValue)
        {
            int chosen = -1;
            double mostFractional = Tolerance;
            fractionalValue = 0.0;

            foreach (int idx in _integerVariables)
            {
                double v = values[idx];
                double frac = v - Math.Floor(v);
                double distanceFromInteger = Math.Min(frac, 1.0 - frac);
                if (distanceFromInteger > mostFractional)
                {
                    mostFractional = distanceFromInteger;
                    chosen = idx;
                    fractionalValue = v;
                }
            }
            return chosen;
        }
    }
}
