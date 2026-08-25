using System;

namespace LPR381Solver.Utils
{
    
    public static class MatrixUtils
    {
        public static double[,] Identity(int n)
        {
            var m = new double[n, n];
            for (int i = 0; i < n; i++) m[i, i] = 1.0;
            return m;
        }

        public static double[,] Multiply(double[,] a, double[,] b)
        {
            int ar = a.GetLength(0), ac = a.GetLength(1);
            int br = b.GetLength(0), bc = b.GetLength(1);
            if (ac != br) throw new InvalidOperationException("Matrix dimension mismatch in Multiply.");
            var result = new double[ar, bc];
            for (int i = 0; i < ar; i++)
                for (int j = 0; j < bc; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < ac; k++) sum += a[i, k] * b[k, j];
                    result[i, j] = sum;
                }
            return result;
        }

        public static double[] Multiply(double[,] a, double[] v)
        {
            int ar = a.GetLength(0), ac = a.GetLength(1);
            if (ac != v.Length) throw new InvalidOperationException("Matrix/vector dimension mismatch.");
            var result = new double[ar];
            for (int i = 0; i < ar; i++)
            {
                double sum = 0;
                for (int j = 0; j < ac; j++) sum += a[i, j] * v[j];
                result[i] = sum;
            }
            return result;
        }

        public static double[] MultiplyRowVector(double[] rowVec, double[,] a)
        {
            // rowVec (1 x n) * a (n x m) -> (1 x m)
            int n = a.GetLength(0), m = a.GetLength(1);
            if (rowVec.Length != n) throw new InvalidOperationException("Row-vector/matrix dimension mismatch.");
            var result = new double[m];
            for (int j = 0; j < m; j++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++) sum += rowVec[i] * a[i, j];
                result[j] = sum;
            }
            return result;
        }

        public static double[,] GetColumn(double[,] a, int col)
        {
            int rows = a.GetLength(0);
            var result = new double[rows, 1];
            for (int i = 0; i < rows; i++) result[i, 0] = a[i, col];
            return result;
        }

        public static double[] GetColumnVector(double[,] a, int col)
        {
            int rows = a.GetLength(0);
            var result = new double[rows];
            for (int i = 0; i < rows; i++) result[i] = a[i, col];
            return result;
        }

        /// <summary>
        /// Inverts a square matrix using Gauss-Jordan elimination with partial pivoting.
        /// Used to (re)build B^-1 from scratch as a sanity check / fallback for the revised simplex.
        /// </summary>
        public static double[,] Invert(double[,] a)
        {
            int n = a.GetLength(0);
            if (a.GetLength(1) != n) throw new InvalidOperationException("Matrix must be square to invert.");

            var work = (double[,])a.Clone();
            var inv = Identity(n);

            for (int col = 0; col < n; col++)
            {
                // partial pivot
                int pivotRow = col;
                double best = Math.Abs(work[col, col]);
                for (int r = col + 1; r < n; r++)
                {
                    if (Math.Abs(work[r, col]) > best)
                    {
                        best = Math.Abs(work[r, col]);
                        pivotRow = r;
                    }
                }
                if (best < 1e-10) throw new InvalidOperationException("Matrix is singular; cannot invert (basis is not valid).");

                if (pivotRow != col)
                {
                    SwapRows(work, col, pivotRow);
                    SwapRows(inv, col, pivotRow);
                }

                double pivot = work[col, col];
                for (int j = 0; j < n; j++) { work[col, j] /= pivot; inv[col, j] /= pivot; }

                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double factor = work[r, col];
                    if (Math.Abs(factor) < 1e-14) continue;
                    for (int j = 0; j < n; j++)
                    {
                        work[r, j] -= factor * work[col, j];
                        inv[r, j] -= factor * inv[col, j];
                    }
                }
            }
            return inv;
        }

        private static void SwapRows(double[,] m, int r1, int r2)
        {
            int cols = m.GetLength(1);
            for (int j = 0; j < cols; j++) (m[r1, j], m[r2, j]) = (m[r2, j], m[r1, j]);
        }

        public static double[,] BuildMatrix(int rows, int cols, Func<int, int, double> f)
        {
            var m = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    m[i, j] = f(i, j);
            return m;
        }
    }
}
