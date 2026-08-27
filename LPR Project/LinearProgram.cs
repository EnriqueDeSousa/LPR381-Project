using System;
using System.Collections.Generic;

namespace LPR_Project
{
    public enum Relation
    {
        LE, // <=
        GE, // >=
        EQ  // =
    }

    public class Constraint
    {
        public double[] Coefficients { get; private set; }
        public Relation Relation { get; private set; }
        public double Rhs { get; private set; }

        public Constraint(double[] coefficients, Relation relation, double rhs)
        {
            Coefficients = coefficients;
            Relation = relation;
            Rhs = rhs;
        }
    }

    public class LinearProgram
    {
        public int NumVariables { get; private set; }
        public double[] ObjectiveCoefficients { get; private set; }
        public List<Constraint> Constraints { get; private set; }
        public string[] VariableNames { get; private set; }

        public LinearProgram(double[] objectiveCoefficients, string[] variableNames)
        {
            ObjectiveCoefficients = objectiveCoefficients;
            NumVariables = objectiveCoefficients.Length;
            Constraints = new List<Constraint>();
            VariableNames = variableNames != null ? variableNames : BuildDefaultNames(NumVariables);
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
            string[] names = new string[n];
            for (int i = 0; i < n; i++) names[i] = "x" + (i + 1);
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
            List<Constraint> newConstraints = new List<Constraint>(Constraints);
            double[] coeffs = new double[NumVariables];
            coeffs[variableIndex] = 1.0;
            newConstraints.Add(new Constraint(coeffs, relation, rhs));
            return new LinearProgram(ObjectiveCoefficients, newConstraints, VariableNames);
        }
    }
}