using System;
using System.Linq;
using LPR381Solver.Models;
using LPR381Solver.Utils;

namespace LPR381Solver.Solvers
{
    /// <summary>
    /// A single iteration's worth of information for the Revised Simplex, kept for the output file:
    /// the current basis inverse (product form), the price vector (y = cB * B^-1), and the
    /// reduced ("priced out") costs of every non-basic column.
    /// </summary>
    public class RevisedIteration
    {
        public int IterationNumber;
        public double[,] BInverse = new double[0, 0];
        public double[] Price = System.Array.Empty<double>();       // y = cB * B^-1
        public double[] ReducedCosts = System.Array.Empty<double>();
        public double[] XB = System.Array.Empty<double>();          // B^-1 * b
        public int[] Basis = System.Array.Empty<int>();
        public int Entering = -1;
        public int Leaving = -1;
        public double[] EtaColumn = System.Array.Empty<double>();   // the "eta" (product-form) column used to update B^-1
        public double ObjectiveValue;
    }

    public class RevisedSimplexResult
    {
        public SolveStatus Status;
        public double ObjectiveValue;
        public double[] StandardSolution = System.Array.Empty<double>();
        public double[] OriginalSolution = System.Array.Empty<double>();
        public int[] FinalBasis = System.Array.Empty<int>();
        public double[,] FinalBInverse = new double[0, 0];
        public System.Collections.Generic.List<RevisedIteration> Iterations { get; } = new();
        public StandardForm StandardForm = null!;
        public string Message = "";
    }

    /// <summary>
    /// Revised Primal Simplex Algorithm using the product form of the inverse.
    /// Instead of carrying a full tableau, only B^-1 (m x m) is maintained; at each iteration it is
    /// updated by pre-multiplying by an "eta" (elementary) matrix built from the entering column,
    /// which is the "Product Form and Price-Out" the assignment brief asks for.
    /// </summary>
    public static class RevisedPrimalSimplex
    {
        private const double Eps = 1e-9;
        private const int MaxIterations = 500;

        public static RevisedSimplexResult Solve(StandardForm sf)
        {
            int m = sf.NumRows;
            int ncols = sf.NumCols;
            var basis = (int[])sf.InitialBasis.Clone();

            // Because the initial basis is always slacks/artificials, B0 is the identity matrix
            // (each slack/artificial column is a unit vector by construction in StandardForm.Build).
            var Binv = MatrixUtils.Identity(m);

            var result = new RevisedSimplexResult { StandardForm = sf };
            int iteration = 0;

            while (true)
            {
                var cB = basis.Select(bi => sf.c[bi]).ToArray();
                var y = MatrixUtils.MultiplyRowVector(cB, Binv);          // price vector: y = cB * B^-1
                var xB = MatrixUtils.Multiply(Binv, sf.b);                // current basic solution
                double zVal = 0;
                for (int i = 0; i < m; i++) zVal += cB[i] * xB[i];

                var reduced = new double[ncols];
                for (int j = 0; j < ncols; j++)
                {
                    double yAj = 0;
                    for (int i = 0; i < m; i++) yAj += y[i] * sf.A[i, j];
                    reduced[j] = sf.c[j] - yAj;                            // priced-out reduced cost
                }

                var iter = new RevisedIteration
                {
                    IterationNumber = iteration,
                    BInverse = (double[,])Binv.Clone(),
                    Price = y,
                    ReducedCosts = reduced,
                    XB = xB,
                    Basis = (int[])basis.Clone(),
                    ObjectiveValue = zVal
                };
                result.Iterations.Add(iter);

                int entering = -1;
                double best = Eps;
                for (int j = 0; j < ncols; j++)
                {
                    if (reduced[j] > best + 1e-12) { best = reduced[j]; entering = j; }
                }

                if (entering == -1)
                {
                    result.Status = SolveStatus.Optimal;
                    FillFinal(result, sf, Binv, basis, xB, zVal, m, ncols);
                    return result;
                }

                // Direction: d = B^-1 * A_entering
                var Aentering = MatrixUtils.GetColumnVector(sf.A, entering);
                var d = MatrixUtils.Multiply(Binv, Aentering);

                int leaving = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (d[i] > Eps)
                    {
                        double ratio = xB[i] / d[i];
                        if (ratio < bestRatio - 1e-12 ||
                            (Math.Abs(ratio - bestRatio) < 1e-9 && (leaving == -1 || basis[i] < basis[leaving])))
                        {
                            bestRatio = ratio;
                            leaving = i;
                        }
                    }
                }

                if (leaving == -1)
                {
                    result.Status = SolveStatus.Unbounded;
                    result.Message = "Unbounded: the entering column has no positive entry in the direction vector d.";
                    return result;
                }

                iter.Entering = entering;
                iter.Leaving = leaving;
                iter.EtaColumn = d;

                // Product-form update: B^-1_new = E * B^-1_old, where E is identity except column 'leaving':
                //   E[leaving,leaving] = 1/d[leaving]
                //   E[i,leaving]       = -d[i]/d[leaving]   for i != leaving
                var E = MatrixUtils.Identity(m);
                double pivot = d[leaving];
                for (int i = 0; i < m; i++)
                    E[i, leaving] = (i == leaving) ? 1.0 / pivot : -d[i] / pivot;

                Binv = MatrixUtils.Multiply(E, Binv);
                basis[leaving] = entering;
                iteration++;

                if (iteration > MaxIterations)
                {
                    result.Status = SolveStatus.Unbounded;
                    result.Message = "Exceeded the maximum number of iterations (possible cycling). Aborting.";
                    return result;
                }
            }
        }

        private static void FillFinal(RevisedSimplexResult result, StandardForm sf, double[,] Binv, int[] basis,
                                       double[] xB, double zVal, int m, int ncols)
        {
            for (int i = 0; i < m; i++)
            {
                if (sf.IsArtificial[basis[i]] && xB[i] > 1e-6)
                {
                    result.Status = SolveStatus.Infeasible;
                    result.Message = $"Artificial variable '{sf.ColNames[basis[i]]}' remains in the basis " +
                                      $"at value {xB[i]:F3} -- the model is infeasible.";
                    return;
                }
            }

            var xStandard = new double[ncols];
            for (int i = 0; i < m; i++) xStandard[basis[i]] = xB[i];

            result.StandardSolution = xStandard;
            result.OriginalSolution = sf.RecoverOriginalValues(xStandard);
            result.FinalBasis = (int[])basis.Clone();
            result.FinalBInverse = Binv;
            result.ObjectiveValue = sf.OriginalWasMin ? -zVal : zVal;
        }
    }
}
