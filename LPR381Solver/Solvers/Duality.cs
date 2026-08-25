using System;
using System.Collections.Generic;
using LPR381Solver.Models;

namespace LPR381Solver.Solvers
{
    /// <summary>
    /// Constructs the dual of a (max) primal LP using the standard correspondence table:
    ///
    ///   Primal (max)                      Dual (min)
    ///   constraint i is  &lt;= bi            y_i &gt;= 0
    ///   constraint i is  &gt;= bi            y_i &lt;= 0   (modelled here as -y_i &gt;= 0, i.e. Negated)
    ///   constraint i is  =  bi            y_i unrestricted
    ///   var x_j &gt;= 0                      dual constraint j is &lt;=
    ///   var x_j &lt;= 0                      dual constraint j is &gt;=
    ///   var x_j unrestricted              dual constraint j is  =
    ///
    
    public static class Duality
    {
        public static LPModel BuildDual(LPModel primal)
        {
            // Work with an equivalent max-form primal so the standard table above applies directly.
            bool flipped = !primal.IsMax;
            double sign = flipped ? -1.0 : 1.0;

            int m = primal.NumConstraints;
            int n = primal.NumVars;

            var dual = new LPModel { IsMax = false }; // dual of a max primal is always min
            dual.ObjectiveCoefficients = new double[m];
            for (int i = 0; i < m; i++) dual.ObjectiveCoefficients[i] = primal.Constraints[i].Rhs;

            dual.SignRestrictions = new SignRestriction[m];
            for (int i = 0; i < m; i++)
            {
                dual.SignRestrictions[i] = primal.Constraints[i].Relation switch
                {
                    Relation.LessOrEqual => SignRestriction.Positive,
                    Relation.GreaterOrEqual => SignRestriction.Negative,
                    _ => SignRestriction.Unrestricted
                };
            }

            var dualConstraints = new List<LPConstraint>();
            for (int j = 0; j < n; j++)
            {
                var coeffs = new double[m];
                for (int i = 0; i < m; i++) coeffs[i] = primal.Constraints[i].Coefficients[j];

                var restriction = primal.SignRestrictions.Length > j ? primal.SignRestrictions[j] : SignRestriction.Positive;
                Relation rel = restriction switch
                {
                    SignRestriction.Positive => Relation.GreaterOrEqual, // dual of max/<= var>=0 -> >=
                    SignRestriction.Negative => Relation.LessOrEqual,
                    _ => Relation.Equal
                };
                double rhs = sign * primal.ObjectiveCoefficients[j];
                dualConstraints.Add(new LPConstraint(coeffs, rel, rhs));
            }
            dual.Constraints = dualConstraints;

            if (flipped)
            {
                // Undo the max-form trick: negate the dual's objective and switch min<->max back.
                dual.IsMax = true;
                for (int i = 0; i < dual.ObjectiveCoefficients.Length; i++)
                    dual.ObjectiveCoefficients[i] *= -1.0;
            }

            return dual;
        }

        public static string CheckDuality(double primalObjective, double dualObjective)
        {
            double gap = Math.Abs(primalObjective - dualObjective);
            if (gap < 1e-4)
                return $"Strong duality holds: primal Z = {primalObjective:F3} equals dual Z = {dualObjective:F3}.";
            return $"Objective values differ (primal Z = {primalObjective:F3}, dual Z = {dualObjective:F3}); " +
                   "only weak duality is confirmed here (this usually means one of the two problems is not yet optimal, " +
                   "or is infeasible/unbounded).";
        }
    }
}
