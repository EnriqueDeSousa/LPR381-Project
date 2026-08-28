using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LPR381Solver.Models;

namespace LPR381Solver.IO
{
    public static class InputParser
    {
        /// <summary>Reads and parses a model from a file on disk.</summary>
        public static LPModel Parse(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Input file not found: {path}");

            return ParseText(File.ReadAllText(path));
        }

        /// <summary>Parses a model directly from its text content (e.g. from a GUI text box),
        /// with no file access involved.</summary>
        public static LPModel ParseText(string text)
        {
            var lines = (text ?? "").Replace("\r\n", "\n").Split('\n')
                             .Select(l => l.Trim())
                             .Where(l => l.Length > 0)
                             .ToList();

            if (lines.Count < 3)
                throw new FormatException("Input file must have at least an objective line, one constraint, and a sign-restriction line.");

            var model = new LPModel();

            // ---- Line 1: objective ----
            var objTokens = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (objTokens.Length < 2)
                throw new FormatException("Objective line must contain 'max'/'min' followed by coefficients.");

            string sense = objTokens[0].ToLowerInvariant();
            if (sense != "max" && sense != "min")
                throw new FormatException($"Objective line must start with 'max' or 'min', found '{objTokens[0]}'.");
            model.IsMax = sense == "max";

            var objCoeffs = new List<double>();
            for (int i = 1; i < objTokens.Length; i++)
                objCoeffs.Add(ParseSignedNumber(objTokens[i]));
            model.ObjectiveCoefficients = objCoeffs.ToArray();
            int n = model.ObjectiveCoefficients.Length;

            // ---- Middle lines: constraints ----
            var constraints = new List<LPConstraint>();
            for (int li = 1; li < lines.Count - 1; li++)
            {
                var tokens = lines[li].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != n + 1)
                    throw new FormatException(
                        $"Constraint line {li + 1} must have {n} coefficients plus one relation+RHS token " +
                        $"(e.g. '<=40'), got {tokens.Length} tokens: \"{lines[li]}\"");

                var coeffs = new double[n];
                for (int j = 0; j < n; j++)
                    coeffs[j] = ParseSignedNumber(tokens[j]);

                var (relation, rhs) = ParseRelationAndRhs(tokens[n]);
                constraints.Add(new LPConstraint(coeffs, relation, rhs));
            }

            if (constraints.Count == 0)
                throw new FormatException("Input file must contain at least one constraint.");
            model.Constraints = constraints;

            // ---- Last line: sign restrictions ----
            var srTokens = lines[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (srTokens.Length != n)
                throw new FormatException($"Sign-restriction line must have {n} entries, got {srTokens.Length}.");

            var restrictions = new SignRestriction[n];
            for (int j = 0; j < n; j++)
                restrictions[j] = ParseSignRestriction(srTokens[j]);
            model.SignRestrictions = restrictions;

            return model;
        }

        private static double ParseSignedNumber(string token)
        {
            token = token.Trim();
            if (token.StartsWith("+"))
                token = token.Substring(1);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                throw new FormatException($"Could not parse numeric token '{token}'.");
            return val;
        }

        private static (Relation, double) ParseRelationAndRhs(string token)
        {
            if (token.StartsWith("<="))
                return (Relation.LessOrEqual, ParseSignedNumber(token.Substring(2)));
            if (token.StartsWith(">="))
                return (Relation.GreaterOrEqual, ParseSignedNumber(token.Substring(2)));
            if (token.StartsWith("="))
                return (Relation.Equal, ParseSignedNumber(token.Substring(1)));
            throw new FormatException($"Could not find a relation (<=, >=, =) in token '{token}'.");
        }

        private static SignRestriction ParseSignRestriction(string token) => token.ToLowerInvariant() switch
        {
            "+" => SignRestriction.Positive,
            "-" => SignRestriction.Negative,
            "urs" => SignRestriction.Unrestricted,
            "int" => SignRestriction.Integer,
            "bin" => SignRestriction.Binary,
            _ => throw new FormatException($"Unknown sign restriction token '{token}'.")
        };
    }
}
