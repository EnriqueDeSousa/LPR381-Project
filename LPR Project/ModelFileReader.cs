using System;
using System.IO;

public class ModelFileReader
{
    public LpModel Load(string path)
    {
        int objSens;
        List<int> objFunc;
        List<List<int>> constraints;
        List<string> signs;
        List<string> signRes;

        if(!File.Exists(path))
        {
            throw new FileNotFoundException($"The file at path {path} does not exist.");
        }) else
        {
            using (StreamReader reader = new StreamReader(path))
            {
                // Read the objective function sensitivity (1 for maximization, -1 for minimization)
                // Objective function is always the first line of the file
                string firstLine = reader.ReadLine();
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

                // The rest of the first line contains the coefficients of the objective function
                objFunc = new List<int>();
                string[] firstLineParts = firstLine.Substring(4).Split(' ');
                foreach (string coefficient in firstLineParts)
                {
                    objFunc.Add(int.Parse(coefficient));
                }

                // Read the constraints
                constraints = new List<List<int>>();
                signs = new List<string>();

                while ((nextLine = reader.ReadLine()) != null)
                {
                    string[] parts = nextLine.Split(' ');
                    List<int> constraint = new List<int>();

                    // The last part of the line is the sign of the constraint
                    string sign = parts[parts.Length - 2];
                    signs.Add(sign);

                    // The second to last part of the line is the right-hand side of the constraint
                    int rhs = int.Parse(parts[parts.Length - 1]);
                    constraint.Add(rhs);

                    // The rest of the parts are the coefficients of the constraint
                    for (int i = 0; i < parts.Length - 2; i++)
                    {
                        constraint.Add(int.Parse(parts[i]));
                    }
                    constraints.Add(constraint);
                }

                // Read the signs of the variables
                // Last line of the file contains the signs of the variables
                signRes = new List<string>();
                string[] signs = reader.ReadLine().Split(' ');
                foreach (string sign in signs)
                {
                    signRes.Add(sign);
                }
            }
        }
        
        return new LpModel(objSens, objFunc, constraints, signs, signRes);
    }

    public void Save(string path, List<int> finalValues)
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

    public ModelFileReader(
    int objSens,
    List<int> objFunc,
    List<List<int>> constraints,
    List<string> signRes)
    {
        this.objSens = objSens;
        this.objFunc = objFunc;
        this.constraints = constraints;
        this.signRes = signRes;
    }

}