using System.Collections.Generic;

namespace LPR381Solver.Models
{
    /// <summary>
    /// Relation type for a constraint.
    /// </summary>
    public enum Relation
    {
        LessOrEqual,
        GreaterOrEqual,
        Equal
    }

    /// <summary>
    /// Sign restriction on a decision variable, as read from the input file
    /// (last line of the input format: +, -, urs, int, bin).
    /// </summary>
    public enum SignRestriction
    {
        Positive,   // "+"  : x >= 0 (default LP behaviour)
        Negative,   // "-"  : x <= 0
        Unrestricted, // "urs"
        Integer,    // "int"
        Binary      // "bin"
    }

    /// <summary>
    /// A single constraint row: coefficients (in decision-variable order), a relation, and an RHS.
    /// </summary>
    public class LPConstraint
    {
        public double[] Coefficients { get; }
        public Relation Relation { get; }
        public double Rhs { get; }

        public LPConstraint(double[] coefficients, Relation relation, double rhs)
        {
            Coefficients = coefficients;
            Relation = relation;
            Rhs = rhs;
        }

        public string RelationSymbol => Relation switch
        {
            Relation.LessOrEqual => "<=",
            Relation.GreaterOrEqual => ">=",
            _ => "="
        };
    }

    /// <summary>
    /// The full parsed Linear/Integer Programming model, exactly as specified in the input file
    /// (NOT a canonical form -- conversion to canonical/standard form happens in the solvers).
    /// </summary>
    public class LPModel
    {
        public bool IsMax { get; set; }
        public double[] ObjectiveCoefficients { get; set; } = System.Array.Empty<double>();
        public List<LPConstraint> Constraints { get; set; } = new();
        public SignRestriction[] SignRestrictions { get; set; } = System.Array.Empty<SignRestriction>();

        public int NumVars => ObjectiveCoefficients.Length;
        public int NumConstraints => Constraints.Count;

        public bool HasIntegerOrBinaryVars
        {
            get
            {
                foreach (var s in SignRestrictions)
                    if (s == SignRestriction.Integer || s == SignRestriction.Binary) return true;
                return false;
            }
        }

        public bool IsAllBinary
        {
            get
            {
                foreach (var s in SignRestrictions)
                    if (s != SignRestriction.Binary) return false;
                return true;
            }
        }
    }
}
