using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace LpModel
{
    public class ModelFileReader
    {
        public LpModel load(string path)
        {
            int objSens;
            List<int> objFunc;
            List<List<int>> constraints;
            List<string> signs;
            List<string> signRes;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The file at path {path} does not exist.");
            }

            string[] allLines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            if (allLines.Length < 2)
            {
                throw new InvalidDataException("The file must contain at least an objective function line and a variable-signs line.");
            }

            // Read the objective function sensitivity (1 for maximization, -1 for minimization)
            // Objective function is always the first line of the file
            string firstLine = allLines[0];
            if (firstLine.Substring(0, 3) == "max")
            {
                objSens = 1;
            }
            else if (firstLine.Substring(0, 3) == "min")
            {
                objSens = -1;
            }
            else
            {
                throw new InvalidDataException("The first line of the file must start with 'max' or 'min'.");
            }

            List<int> objFunc = new List<int>();
            string[] firstLineParts = firstLine.Substring(4).Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string coefficient in firstLineParts)
            {
                objFunc.Add(int.Parse(coefficient));
            }

            // Getting the constraints and their signs
            List<List<int>> constraints = new List<List<int>>();
            List<string> signs = new List<string>();

            for (int lineIndex = 1; lineIndex < allLines.Length - 1; lineIndex++)
            {
                string[] parts = allLines[lineIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // The rhs must first be splited from the last coefficient, as it may contain a sign (<=, >=, =)
                string lastToken = parts[parts.Length - 1];
                string sign;
                string rhsText;

                if (lastToken.StartsWith("<=") || lastToken.StartsWith(">="))
                {
                    sign = lastToken.Substring(0, 2);
                    rhsText = lastToken.Substring(2);
                }
                else if (lastToken.StartsWith("="))
                {
                    sign = lastToken.Substring(0, 1);
                    rhsText = lastToken.Substring(1);
                }
                else
                {
                    throw new InvalidDataException(
                        $"Constraint line \"{allLines[lineIndex]}\" is missing a valid sign (<=, >=, =).");
                }

                signs.Add(sign);
                int rhs = int.Parse(rhsText);

                // Everything except the last token is a coefficient, in order.
                // rhs is appended last, so constraints[i][0] is the coefficient of
                // the first variable, matching the LpModel documentation below, and constraints[i][^1] is the right-hand side.
                List<int> constraint = new List<int>();
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    constraint.Add(int.Parse(parts[i]));
                }
                constraint.Add(rhs);
                constraints.Add(constraint);
            }

            // Read the signs of the variable
            // Last line of the file contains the signs of the variables
            List<string> signRes = allLines[allLines.Length - 1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            return new LpModel(objSens, objFunc, constraints, signs, signRes);
        }


        public void save(string path, List<int> finalValues)
        {
            /*
             * path is the path to the file where the final values will be saved.
             * finalValues is a list of integers representing the final values of the variables in the linear programming model.
            */

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("Final values of the variables:");
                writer.WriteLine(string.Join(" ", finalValues));
            }
        }
    }

    public class LpModel
    {
        /* 
            * objSens can be 1 for maximization or -1 for minimization.
            * 
            * objFunc is a list of integers representing the coefficients of the objective function in the linear programming model.
            * 
            * constraints is a list of lists of integers, where each inner list represents a constraint in the linear programming model.
            * constraints[0][0] represents the coefficient of the first variable in the first constraint.
            * The last number in each inner list represents the right-hand side of the constraint.
            * signs contains the signs of the constraints, where each sign corresponds to a constraint in the same order as they appear in the constraints list.
            * 
            * signRes is a list of strings representing the signs of the variables in the linear programming model.
        */

        int objSens;
        List<int> objFunc;
        List<List<int>> constraints;
        List<string> signs;
        List<string> signRes;

        public LpModel(
        int objSens,
        List<int> objFunc,
        List<List<int>> constraints,
        List<string> signs,
        List<string> signRes)
        {
            this.objSens = objSens;
            this.objFunc = objFunc;
            this.constraints = constraints;
            this.signs = signs;
            this.signRes = signRes;
        }

        // Getters for the private fields
        public int GetObjSens() => objSens;
        public List<int> GetObjFunc() => objFunc;
        public List<List<int>> GetConstraints() => constraints;
        public List<string> GetSigns() => signs;
        public List<string> GetSignRes() => signRes;
    }
}