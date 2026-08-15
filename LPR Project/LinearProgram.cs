using System;
using System.Collections.Generic;

namespace BranchAndBoundSimplex
{
    public enum Relation
    {
        LE, // <=
        GE, // >=
        EQ  // =
    }

    public class Constraint
    {
        public double[] Coefficients { get; }
        public Relation Relation { get; }
        public double Rhs { get; }

        public Constraint(double[] coefficients, Relation relation, double rhs)
        {
            Coefficients = coefficients;
            Relation = relation;
            Rhs = rhs;
        }
    }

    public class LinearProgram
    {
        public int NumVariables { get; }
        public double[] ObjectiveCoefficients { get; }
        public List<Constraint> Constraints { get; }
        public string[] VariableNames { get; }

        public LinearProgram(double[] objectiveCoefficients, string[]? variableNames = null)
        {
            ObjectiveCoefficients = objectiveCoefficients;
            NumVariables = objectiveCoefficients.Length;
            Constraints = new List<Constraint>();
            VariableNames = variableNames ?? BuildDefaultNames(NumVariables);
        }

        private LinearProgram(double[] objectiveCoefficients, List<Constraint> constraints, string[] variableNames)
        {
            ObjectiveCoefficients = objectiveCoefficients;
            NumVariables = objectiveCoefficients.Length;
            Constraints = constraints;
            VariableNames = variableNames;
        }

        private static string[] BuildDefaultNames(int n)
        {
            var names = new string[n];
            for (int i = 0; i < n; i++) names[i] = $"x{i + 1}";
            return names;
        }

        public void AddConstraint(double[] coefficients, Relation relation, double rhs)
        {
            if (coefficients.Length != NumVariables)
                throw new ArgumentException("Coefficient count must match NumVariables.");
            Constraints.Add(new Constraint(coefficients, relation, rhs));
        }

        public LinearProgram WithExtraConstraint(int variableIndex, Relation relation, double rhs)
        {
            var newConstraints = new List<Constraint>(Constraints);
            var coeffs = new double[NumVariables];
            coeffs[variableIndex] = 1.0;
            newConstraints.Add(new Constraint(coeffs, relation, rhs));
            return new LinearProgram(ObjectiveCoefficients, newConstraints, VariableNames);
        }
    }
}