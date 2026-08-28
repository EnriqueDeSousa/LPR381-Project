using System;
using System.Collections.Generic;
using System.Linq;
using LPR381Solver.Errors;
using LPR381Solver.Models;

namespace LPR381Solver.Solvers
{
    /// <summary>Final answer from a Branch &amp; Bound search.</summary>
    public class BranchAndBoundResult
    {
        public bool Found;
        public double ObjectiveValue;
        public double[] Solution = Array.Empty<double>();
        public int NodesExplored;
        public List<string> Log { get; } = new();
    }

    /// <summary>
    /// Branch &amp; Bound for models with "int"/"bin" sign restrictions. This reuses the exact
    /// same LPModel -&gt; StandardForm -&gt; PrimalSimplex pipeline as the rest of the app: every
    /// node in the search tree is just the original model plus one extra "&lt;=" / "&gt;=" bound
    /// constraint on a single variable, solved as an ordinary LP relaxation.
    ///
    /// (The branching strategy - most-fractional variable, depth-first with bound pruning -
    /// mirrors the approach originally prototyped in the team's WinForms build; this version
    /// targets the shared model types so it can be called from the same menu as the other
    /// algorithms instead of needing its own UI/model classes.)
    /// </summary>
    public static class BranchAndBound
    {
        private const double Tolerance = 1e-6;
        private const int MaxNodes = 5000;

        private class Node
        {
            public LPModel Model = null!;
            public int ParentId;
            public string Description = "";
        }

        public static BranchAndBoundResult Solve(LPModel rootModel)
        {
            var integerVars = new List<int>();
            for (int j = 0; j < rootModel.SignRestrictions.Length; j++)
            {
                var r = rootModel.SignRestrictions[j];
                if (r == SignRestriction.Integer || r == SignRestriction.Binary)
                    integerVars.Add(j);
            }

            if (integerVars.Count == 0)
                throw new AlgorithmNotSupportedException(
                    "Branch & Bound needs at least one 'int' or 'bin' variable in the sign-restriction line " +
                    "-- for a pure LP, use Primal Simplex or Revised Primal Simplex instead.");

            var result = new BranchAndBoundResult();
            double bestObjective = double.NegativeInfinity;
            double[]? bestSolution = null;

            var stack = new Stack<Node>();
            stack.Push(new Node { Model = rootModel, ParentId = 0, Description = "Root (LP relaxation)" });

            int nodeCounter = 0;
            while (stack.Count > 0)
            {
                if (nodeCounter >= MaxNodes)
                {
                    result.Log.Add("Node limit reached -- stopping early with the best incumbent found so far.");
                    break;
                }

                var node = stack.Pop();
                nodeCounter++;
                int nodeId = nodeCounter;
                result.Log.Add($"--- Node {nodeId} (parent {node.ParentId}): {node.Description} ---");

                SimplexResult lp;
                try
                {
                    var sf = StandardForm.Build(node.Model);
                    lp = PrimalSimplex.Solve(sf);
                }
                catch (Exception ex)
                {
                    result.Log.Add($"Node {nodeId}: could not be solved ({ex.Message}) -- pruned.");
                    continue;
                }

                if (lp.Status == SolveStatus.Infeasible)
                {
                    result.Log.Add($"Node {nodeId}: relaxation is infeasible -- pruned.");
                    continue;
                }
                if (lp.Status == SolveStatus.Unbounded)
                {
                    result.Log.Add($"Node {nodeId}: relaxation is unbounded -- pruned.");
                    continue;
                }

                result.Log.Add($"Node {nodeId}: relaxation Z = {lp.ObjectiveValue:F3}, " +
                                $"x = [{string.Join(", ", lp.OriginalSolution.Select(v => v.ToString("F3")))}]");

                if (bestSolution != null && lp.ObjectiveValue <= bestObjective + Tolerance)
                {
                    result.Log.Add($"Node {nodeId}: bound {lp.ObjectiveValue:F3} does not improve the incumbent " +
                                    $"{bestObjective:F3} -- pruned by bound.");
                    continue;
                }

                int branchVar = -1;
                double branchValue = 0.0, mostFractional = Tolerance;
                foreach (int j in integerVars)
                {
                    double v = lp.OriginalSolution[j];
                    double frac = v - Math.Floor(v);
                    double distanceFromInteger = Math.Min(frac, 1.0 - frac);
                    if (distanceFromInteger > mostFractional)
                    {
                        mostFractional = distanceFromInteger;
                        branchVar = j;
                        branchValue = v;
                    }
                }

                if (branchVar == -1)
                {
                    result.Log.Add($"Node {nodeId}: solution is already integer-feasible, Z = {lp.ObjectiveValue:F3}.");
                    if (lp.ObjectiveValue > bestObjective)
                    {
                        bestObjective = lp.ObjectiveValue;
                        bestSolution = lp.OriginalSolution;
                        result.Log.Add($"Node {nodeId}: *** new incumbent solution ***");
                    }
                    continue;
                }

                double floorVal = Math.Floor(branchValue);
                double ceilVal = Math.Ceiling(branchValue);
                string varName = $"x{branchVar + 1}";
                result.Log.Add($"Node {nodeId}: branching on {varName} = {branchValue:F3}  ->  " +
                                $"{varName} <= {floorVal:F0}  and  {varName} >= {ceilVal:F0}");

                var leftChild = WithExtraBound(node.Model, branchVar, Relation.LessOrEqual, floorVal);
                var rightChild = WithExtraBound(node.Model, branchVar, Relation.GreaterOrEqual, ceilVal);

                // Push right first so the left ("<=") branch is explored first (LIFO stack).
                stack.Push(new Node { Model = rightChild, ParentId = nodeId, Description = $"{varName} >= {ceilVal:F0}" });
                stack.Push(new Node { Model = leftChild, ParentId = nodeId, Description = $"{varName} <= {floorVal:F0}" });
            }

            result.Found = bestSolution != null;
            result.Solution = bestSolution ?? Array.Empty<double>();
            result.ObjectiveValue = bestObjective;
            result.NodesExplored = nodeCounter;
            return result;
        }

        /// <summary>Returns a shallow-cloned model with one extra "coefficient of x_varIndex {relation} rhs" row.</summary>
        private static LPModel WithExtraBound(LPModel model, int varIndex, Relation relation, double rhs)
        {
            var coeffs = new double[model.NumVars];
            coeffs[varIndex] = 1.0;

            var newConstraints = new List<LPConstraint>(model.Constraints)
            {
                new LPConstraint(coeffs, relation, rhs)
            };

            return new LPModel
            {
                IsMax = model.IsMax,
                ObjectiveCoefficients = model.ObjectiveCoefficients,
                SignRestrictions = model.SignRestrictions,
                Constraints = newConstraints
            };
        }
    }
}
