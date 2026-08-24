using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR_Project
{
    public class BranchAndBoundResult
    {
        public bool Found { get; set; }
        public double[] Solution { get; set; }
        public double Objective { get; set; }
        public int NodesExplored { get; set; }

        public BranchAndBoundResult()
        {
            Solution = new double[0];
        }
    }

    public class BranchAndBoundSolver
    {
        private const double Tolerance = 1e-6;

        private readonly HashSet<int> _integerVariables;
        private readonly bool _verboseSimplex;
        private readonly Action<string> _log;
        private double _bestObjective = double.NegativeInfinity;
        private double[] _bestSolution;
        private int _nodeCounter;

        public BranchAndBoundSolver(IEnumerable<int> integerVariableIndices, bool verboseSimplex, Action<string> logger)
        {
            _integerVariables = new HashSet<int>(integerVariableIndices);
            _verboseSimplex = verboseSimplex;
            _log = logger != null ? logger : (s => Console.WriteLine(s));
            _bestSolution = null;
        }

        public BranchAndBoundResult Solve(LinearProgram rootLp)
        {
            Stack<NodeItem> stack = new Stack<NodeItem>();
            stack.Push(new NodeItem(rootLp, 0, "Root (LP relaxation)"));

            while (stack.Count > 0)
            {
                NodeItem item = stack.Pop();
                _nodeCounter++;
                int nodeId = _nodeCounter;

                _log("");
                _log("=== Node " + nodeId + " (parent " + item.ParentId + ") : " + item.Description + " ===");

                SimplexResult result = SimplexSolver.Solve(item.Lp, _verboseSimplex);
                if (_verboseSimplex)
                {
                    foreach (string line in result.Log) _log(line);
                }

                if (result.Status == SimplexStatus.Infeasible)
                {
                    _log("Node " + nodeId + ": LP relaxation is infeasible -> pruned.");
                    continue;
                }
                if (result.Status == SimplexStatus.Unbounded)
                {
                    _log("Node " + nodeId + ": LP relaxation is unbounded -> pruned (check your model).");
                    continue;
                }

                _log("Node " + nodeId + ": relaxation objective = " + result.ObjectiveValue.ToString("F4") +
                     ", x = [" + string.Join(", ", result.VariableValues.Select(v => v.ToString("F4"))) + "]");

                if (result.ObjectiveValue <= _bestObjective + Tolerance)
                {
                    _log("Node " + nodeId + ": bound " + result.ObjectiveValue.ToString("F4") +
                         " does not improve on incumbent " + _bestObjective.ToString("F4") + " -> pruned by bound.");
                    continue;
                }

                double fractionalValue;
                int branchVar = ChooseBranchingVariable(result.VariableValues, out fractionalValue);

                if (branchVar == -1)
                {
                    _log("Node " + nodeId + ": solution is integer-feasible, objective = " + result.ObjectiveValue.ToString("F4"));
                    if (result.ObjectiveValue > _bestObjective)
                    {
                        _bestObjective = result.ObjectiveValue;
                        _bestSolution = result.VariableValues;
                        _log("Node " + nodeId + ": *** new incumbent solution ***");
                    }
                    continue;
                }

                double floorVal = Math.Floor(fractionalValue);
                double ceilVal = Math.Ceiling(fractionalValue);
                string varName = item.Lp.VariableNames[branchVar];

                _log("Node " + nodeId + ": branching on " + varName + " = " + fractionalValue.ToString("F4") +
                     "  ->  " + varName + " <= " + floorVal + "  and  " + varName + " >= " + ceilVal);

                LinearProgram leftChild = item.Lp.WithExtraConstraint(branchVar, Relation.LE, floorVal);
                LinearProgram rightChild = item.Lp.WithExtraConstraint(branchVar, Relation.GE, ceilVal);

                stack.Push(new NodeItem(rightChild, nodeId, varName + " >= " + ceilVal));
                stack.Push(new NodeItem(leftChild, nodeId, varName + " <= " + floorVal));
            }

            BranchAndBoundResult finalResult = new BranchAndBoundResult();
            finalResult.Found = _bestSolution != null;
            finalResult.Solution = _bestSolution != null ? _bestSolution : new double[0];
            finalResult.Objective = _bestObjective;
            finalResult.NodesExplored = _nodeCounter;
            return finalResult;
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

        private class NodeItem
        {
            public LinearProgram Lp { get; private set; }
            public int ParentId { get; private set; }
            public string Description { get; private set; }

            public NodeItem(LinearProgram lp, int parentId, string description)
            {
                Lp = lp;
                ParentId = parentId;
                Description = description;
            }
        }
    }
}