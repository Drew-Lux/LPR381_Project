using LPR381_Project.Models;
using System;
using System.Collections.Generic;

namespace LPR381_Project.Algorithms
{
    /// <summary>
    /// Revised Primal Simplex solver.
    ///
    /// Each iteration calculates B, B^-1, C_B, prices, reduced costs,
    /// the direction vector and the minimum-ratio test. The elementary
    /// eta matrix is retained as the Product Form representation of the
    /// basis update. The original ProblemModel is never modified.
    /// </summary>
    public static class RevisedSimplexSolver
    {
        private const double Epsilon = 1e-9;
        private const int DefaultIterationLimit = 1000;

        public static RevisedSimplexResult Solve(CanonicalModel canonical)
            => Solve(canonical, DefaultIterationLimit);

        public static RevisedSimplexResult Solve(CanonicalModel canonical, int iterationLimit)
        {
            RevisedSimplexResult result = new RevisedSimplexResult();
            if (!Validate(canonical, iterationLimit, result)) return result;

            int m = canonical.ConstraintMatrix.GetLength(0);
            int n = canonical.ConstraintMatrix.GetLength(1);
            int[] basis = (int[])canonical.InitialBasisColumns.Clone();

            if (!IsValidBasis(canonical.ConstraintMatrix, basis))
            {
                result.Status = SimplexResult.SolverStatus.Infeasible;
                result.Message = "The canonical model does not contain a valid initial basis.";
                return result;
            }

            int pivots = 0;

            if (canonical.RequiresPhaseOne)
            {
                double[] phase1C = new double[n];
                foreach (int col in canonical.ArtificialVariableColumns)
                    if (col >= 0 && col < n) phase1C[col] = -1.0;

                SimplexResult.SolverStatus status = RunPhase(
                    canonical, basis, phase1C, "Phase 1", iterationLimit,
                    result.Iterations, ref pivots, out double phase1Objective,
                    out string phase1Message);

                if (status != SimplexResult.SolverStatus.Optimal)
                {
                    result.Status = status;
                    result.Message = phase1Message;
                    Finish(result, canonical, basis, phase1C, phase1Objective);
                    result.PivotCount = pivots;
                    return result;
                }

                if (Math.Abs(phase1Objective) > 1e-7)
                {
                    result.Status = SimplexResult.SolverStatus.Infeasible;
                    result.Message = "Phase 1 ended with a non-zero artificial-variable objective.";
                    Finish(result, canonical, basis, phase1C, phase1Objective);
                    result.PivotCount = pivots;
                    return result;
                }

                RemoveArtificialBasics(canonical, basis, ref pivots);
            }

            double[] objective = (double[])canonical.ObjectiveCoefficients.Clone();
            SimplexResult.SolverStatus phase2Status = RunPhase(
                canonical, basis, objective, "Phase 2", Math.Max(1, iterationLimit - pivots),
                result.Iterations, ref pivots, out double objectiveValue, out string message);

            result.Status = phase2Status;
            result.Message = message;
            result.PivotCount = pivots;
            Finish(result, canonical, basis, objective, objectiveValue);
            return result;
        }

        private static SimplexResult.SolverStatus RunPhase(
            CanonicalModel canonical, int[] basis, double[] c, string stage,
            int iterationLimit, List<RevisedSimplexIteration> iterations,
            ref int pivotCount, out double objectiveValue, out string message)
        {
            objectiveValue = 0.0;
            message = string.Empty;
            int m = basis.Length;
            int n = c.Length;

            for (int loop = 0; loop <= iterationLimit; loop++)
            {
                double[,] B = BuildBasis(canonical.ConstraintMatrix, basis);
                if (!TryInverse(B, out double[,] binv))
                {
                    message = "The current basis matrix is singular.";
                    return SimplexResult.SolverStatus.Infeasible;
                }

                double[] xB = Multiply(binv, canonical.RightHandSide);
                double[] cb = GetBasicCosts(c, basis);
                double[] prices = Multiply(cb, binv);
                double[] reduced = new double[n];
                for (int j = 0; j < n; j++)
                    reduced[j] = c[j] - Dot(prices, GetColumn(canonical.ConstraintMatrix, j));

                objectiveValue = Dot(cb, xB);
                int entering = SelectEntering(canonical, basis, reduced);

                RevisedSimplexIteration iteration = CreateIteration(
                    iterations.Count, stage, canonical, basis, B, binv, cb,
                    prices, reduced, null, null, entering, -1, "", "", 0,
                    objectiveValue, null, false);

                if (entering < 0)
                {
                    iteration.IsFinal = true;
                    iterations.Add(iteration);
                    message = "Optimal solution found.";
                    return SimplexResult.SolverStatus.Optimal;
                }

                double[] direction = Multiply(binv, GetColumn(canonical.ConstraintMatrix, entering));
                double[] ratios = new double[m];
                int leaving = SelectLeaving(xB, direction, ratios);
                iteration.Direction = (double[])direction.Clone();
                iteration.Ratios = (double[])ratios.Clone();
                iteration.LeavingRow = leaving;
                iteration.EnteringVariable = canonical.VariableNames[entering];

                if (leaving < 0)
                {
                    iterations.Add(iteration);
                    message = "The objective is unbounded in the selected entering-variable direction.";
                    return SimplexResult.SolverStatus.Unbounded;
                }

                iteration.LeavingVariable = canonical.VariableNames[basis[leaving]];
                iteration.PivotValue = direction[leaving];
                iteration.EtaMatrix = BuildEtaMatrix(direction, leaving);
                iterations.Add(iteration);

                basis[leaving] = entering;
                pivotCount++;
            }

            message = "The revised simplex iteration limit was reached.";
            return SimplexResult.SolverStatus.IterationLimit;
        }

        private static RevisedSimplexIteration CreateIteration(
            int number, string stage, CanonicalModel canonical, int[] basis,
            double[,] B, double[,] binv, double[] cb, double[] prices,
            double[] reduced, double[] direction, double[] ratios, int entering,
            int leaving, string enteringName, string leavingName, double pivot,
            double objective, double[,] eta, bool final)
        {
            RevisedSimplexIteration r = new RevisedSimplexIteration
            {
                IterationNumber = number,
                Stage = stage,
                BasisColumns = (int[])basis.Clone(),
                BasisNames = GetBasisNames(canonical, basis),
                BasisMatrix = CloneMatrix(B),
                BasisInverse = CloneMatrix(binv),
                BasicCosts = (double[])cb.Clone(),
                Prices = (double[])prices.Clone(),
                ReducedCosts = (double[])reduced.Clone(),
                EnteringColumn = entering,
                LeavingRow = leaving,
                EnteringVariable = enteringName,
                LeavingVariable = leavingName,
                PivotValue = pivot,
                ObjectiveValue = objective,
                IsFinal = final
            };
            if (direction != null) r.Direction = (double[])direction.Clone();
            if (ratios != null) r.Ratios = (double[])ratios.Clone();
            if (eta != null) r.EtaMatrix = CloneMatrix(eta);
            return r;
        }

        private static int SelectEntering(CanonicalModel c, int[] basis, double[] reduced)
        {
            for (int j = 0; j < reduced.Length; j++)
            {
                if (Contains(basis, j)) continue;
                if (c.ArtificialVariableColumns.Contains(j)) continue;
                if (reduced[j] > Epsilon) return j;
            }
            return -1;
        }

        private static int SelectLeaving(double[] xB, double[] direction, double[] ratios)
        {
            int leaving = -1;
            double best = double.PositiveInfinity;
            for (int i = 0; i < direction.Length; i++)
            {
                if (direction[i] > Epsilon)
                {
                    ratios[i] = xB[i] / direction[i];
                    if (ratios[i] >= -Epsilon && ratios[i] < best - Epsilon)
                    {
                        best = ratios[i];
                        leaving = i;
                    }
                }
                else ratios[i] = double.PositiveInfinity;
            }
            return leaving;
        }

        private static void RemoveArtificialBasics(CanonicalModel c, int[] basis, ref int pivots)
        {
            int m = basis.Length;
            int n = c.ConstraintMatrix.GetLength(1);
            for (int row = 0; row < m; row++)
            {
                if (!c.ArtificialVariableColumns.Contains(basis[row])) continue;
                double[,] B = BuildBasis(c.ConstraintMatrix, basis);
                if (!TryInverse(B, out double[,] binv)) continue;
                double[] inverseRow = GetRow(binv, row);
                for (int j = 0; j < n; j++)
                {
                    if (Contains(basis, j) || c.ArtificialVariableColumns.Contains(j)) continue;
                    if (Math.Abs(Dot(inverseRow, GetColumn(c.ConstraintMatrix, j))) > Epsilon)
                    {
                        basis[row] = j;
                        pivots++;
                        break;
                    }
                }
            }
        }

        private static void Finish(RevisedSimplexResult result, CanonicalModel c,
            int[] basis, double[] objective, double objectiveValue)
        {
            double[,] B = BuildBasis(c.ConstraintMatrix, basis);
            TryInverse(B, out double[,] binv);
            double[] x = new double[c.ConstraintMatrix.GetLength(1)];
            if (binv.GetLength(0) == basis.Length)
            {
                double[] xb = Multiply(binv, c.RightHandSide);
                for (int i = 0; i < basis.Length; i++) x[basis[i]] = Clean(xb[i]);
            }
            result.VariableNames = (string[])c.VariableNames.Clone();
            result.VariableValues = x;
            result.BasisColumns = (int[])basis.Clone();
            result.BasisRows = (int[])basis.Clone();
            result.ObjectiveValue = c.WasMinimization ? -objectiveValue : objectiveValue;
            result.FinalBasisInverse = binv;
        }

        private static bool Validate(CanonicalModel c, int limit, RevisedSimplexResult r)
        {
            if (c == null) { Fail(r, SimplexResult.SolverStatus.Infeasible, "Canonical model is null."); return false; }
            if (limit <= 0) { Fail(r, SimplexResult.SolverStatus.IterationLimit, "Iteration limit must be greater than zero."); return false; }
            if (c.ConstraintMatrix == null || c.RightHandSide == null || c.ObjectiveCoefficients == null || c.InitialBasisColumns == null)
            { Fail(r, SimplexResult.SolverStatus.Infeasible, "Canonical model data is incomplete."); return false; }
            int rows = c.ConstraintMatrix.GetLength(0), cols = c.ConstraintMatrix.GetLength(1);
            if (rows == 0 || cols == 0 || c.RightHandSide.Length != rows || c.ObjectiveCoefficients.Length != cols || c.InitialBasisColumns.Length != rows)
            { Fail(r, SimplexResult.SolverStatus.Infeasible, "Canonical model dimensions are inconsistent."); return false; }
            return true;
        }

        private static void Fail(RevisedSimplexResult r, SimplexResult.SolverStatus s, string message)
        { r.Status = s; r.Message = message; }

        private static bool IsValidBasis(double[,] A, int[] basis)
        {
            HashSet<int> seen = new HashSet<int>();
            foreach (int b in basis) if (b < 0 || b >= A.GetLength(1) || !seen.Add(b)) return false;
            return TryInverse(BuildBasis(A, basis), out _);
        }

        private static double[,] BuildBasis(double[,] A, int[] basis)
        {
            int m = A.GetLength(0); double[,] B = new double[m, m];
            for (int i = 0; i < m; i++) for (int j = 0; j < m; j++) B[i, j] = A[i, basis[j]];
            return B;
        }

        private static double[,] BuildEtaMatrix(double[] direction, int pivotRow)
        {
            int m = direction.Length; double[,] eta = new double[m, m];
            for (int i = 0; i < m; i++)
            {
                if (i == pivotRow) eta[i, i] = 1.0 / direction[pivotRow];
                else { eta[i, i] = 1.0; eta[i, pivotRow] = -direction[i] / direction[pivotRow]; }
            }
            return eta;
        }

        private static bool TryInverse(double[,] matrix, out double[,] inverse)
        {
            int n = matrix.GetLength(0); inverse = new double[n, n];
            if (matrix.GetLength(1) != n) return false;
            double[,] a = new double[n, 2 * n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { a[i, j] = matrix[i, j]; a[i, j + n] = i == j ? 1 : 0; }
            for (int col = 0; col < n; col++)
            {
                int p = col;
                for (int row = col + 1; row < n; row++) if (Math.Abs(a[row, col]) > Math.Abs(a[p, col])) p = row;
                if (Math.Abs(a[p, col]) < Epsilon) return false;
                if (p != col) SwapRows(a, p, col);
                double pivot = a[col, col];
                for (int j = 0; j < 2 * n; j++) a[col, j] /= pivot;
                for (int row = 0; row < n; row++)
                {
                    if (row == col) continue;
                    double f = a[row, col]; if (Math.Abs(f) < Epsilon) continue;
                    for (int j = 0; j < 2 * n; j++) a[row, j] -= f * a[col, j];
                }
            }
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) inverse[i, j] = Clean(a[i, j + n]);
            return true;
        }

        private static double[] GetBasicCosts(double[] c, int[] basis) { double[] r = new double[basis.Length]; for (int i = 0; i < basis.Length; i++) r[i] = c[basis[i]]; return r; }
        private static string[] GetBasisNames(CanonicalModel c, int[] basis) { string[] r = new string[basis.Length]; for (int i = 0; i < basis.Length; i++) r[i] = c.VariableNames[basis[i]]; return r; }
        private static double[] GetColumn(double[,] a, int col) { double[] r = new double[a.GetLength(0)]; for (int i = 0; i < r.Length; i++) r[i] = a[i, col]; return r; }
        private static double[] GetRow(double[,] a, int row) { double[] r = new double[a.GetLength(1)]; for (int j = 0; j < r.Length; j++) r[j] = a[row, j]; return r; }
        private static double[] Multiply(double[,] a, double[] x) { double[] r = new double[a.GetLength(0)]; for (int i = 0; i < a.GetLength(0); i++) for (int j = 0; j < a.GetLength(1); j++) r[i] += a[i, j] * x[j]; return r; }
        private static double[] Multiply(double[] x, double[,] a) { double[] r = new double[a.GetLength(1)]; for (int j = 0; j < a.GetLength(1); j++) for (int i = 0; i < a.GetLength(0); i++) r[j] += x[i] * a[i, j]; return r; }
        private static double Dot(double[] a, double[] b) { double s = 0; for (int i = 0; i < a.Length; i++) s += a[i] * b[i]; return s; }
        private static bool Contains(int[] a, int value) { foreach (int x in a) if (x == value) return true; return false; }
        private static double[,] CloneMatrix(double[,] a) { double[,] b = new double[a.GetLength(0), a.GetLength(1)]; Array.Copy(a, b, a.Length); return b; }
        private static void SwapRows(double[,] a, int r1, int r2) { for (int j = 0; j < a.GetLength(1); j++) { double t = a[r1, j]; a[r1, j] = a[r2, j]; a[r2, j] = t; } }
        private static double Clean(double x) => Math.Abs(x) < Epsilon ? 0 : x;
    }

    public class RevisedSimplexResult
    {
        public SimplexResult.SolverStatus Status { get; set; } = SimplexResult.SolverStatus.Infeasible;
        public string Message { get; set; } = string.Empty;
        public double ObjectiveValue { get; set; }
        public string[] VariableNames { get; set; } = Array.Empty<string>();
        public double[] VariableValues { get; set; } = Array.Empty<double>();
        public int[] BasisColumns { get; set; } = Array.Empty<int>();
        public int[] BasisRows { get; set; } = Array.Empty<int>();
        public int PivotCount { get; set; }
        public double[,] FinalBasisInverse { get; set; } = new double[0, 0];
        public List<RevisedSimplexIteration> Iterations { get; set; } = new List<RevisedSimplexIteration>();
    }
}
