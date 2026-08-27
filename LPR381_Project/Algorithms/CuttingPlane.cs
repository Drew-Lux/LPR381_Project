using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Project.Models;

namespace LPR381_Project.Algorithms
{
    /// <summary>
    /// Cutting Plane (Gomory) solver, implemented on top of the
    /// Revised Primal Simplex machinery so that every step - the
    /// initial LP relaxation and every re-optimisation after a cut -
    /// is expressed using Product Form (eta matrices) and Price Out
    /// (C_B * B^-1, reduced costs) calculations, exactly like
    /// <see cref="RevisedSimplexSolver"/>.
    ///
    /// Algorithm outline:
    ///
    /// 1. Solve the LP relaxation with the Revised Primal Simplex
    ///    method.
    /// 2. If every integer-restricted ("int" / "bin") variable is
    ///    already integer, stop - the relaxation is optimal.
    /// 3. Otherwise, pick the most fractional integer-restricted
    ///    basic variable and derive a Gomory fractional cut from its
    ///    tableau row:
    ///
    ///        sum_j frac(a_ij) * x_j &gt;= frac(b_i)
    ///
    ///    which is appended to the working model in the equivalent
    ///    dual-simplex-ready form
    ///
    ///        -sum_j frac(a_ij) * x_j + s = -frac(b_i),  s &gt;= 0
    ///
    ///    This makes the new slack variable's value negative (since
    ///    every non-basic variable is currently zero), so the
    ///    resulting basis is primal-infeasible but still dual
    ///    feasible (no reduced cost changed sign).
    /// 4. Restore primal feasibility with Dual Simplex pivots
    ///    (Product Form / Price Out, same convention as step 1).
    /// 5. Repeat from step 2 until an integer-feasible solution is
    ///    found, the model proves infeasible, or the cut limit is
    ///    reached.
    ///
    /// The solver performs calculations only. It does not write
    /// directly to the console.
    /// </summary>
    public static class CuttingPlaneSolver
    {
        private const double Epsilon = 1e-6;
        private const int DefaultIterationLimit = 1000;
        private const int DefaultMaxCuts = 50;

        // =============================================================
        // PUBLIC ENTRY POINTS
        // =============================================================

        public static CuttingPlaneResult Solve(ProblemModel rootModel)
        {
            return Solve(rootModel, DefaultMaxCuts, DefaultIterationLimit);
        }

        public static CuttingPlaneResult Solve(
            ProblemModel rootModel,
            int maxCuts,
            int iterationLimit)
        {
            CuttingPlaneResult result = new CuttingPlaneResult();

            if (rootModel == null)
            {
                result.Message = "Programming model is null.";
                return result;
            }

            // ---------------------------------------------------------
            // Step 1: solve the LP relaxation with Revised Primal
            // Simplex. The original ProblemModel's int/bin
            // restrictions are ignored by CanonicalFormConverter for
            // the purpose of building the constraint matrix, so this
            // naturally gives us the relaxation.
            // ---------------------------------------------------------

            CanonicalModel canonical = CanonicalFormConverter.Convert(rootModel);

            RevisedSimplexResult relaxation =
                RevisedSimplexSolver.Solve(canonical, iterationLimit);

            result.Iterations.AddRange(relaxation.Iterations);
            result.PivotCount = relaxation.PivotCount;

            if (relaxation.Status != SimplexResult.SolverStatus.Optimal)
            {
                result.Status = relaxation.Status;

                result.Message = string.IsNullOrEmpty(relaxation.Message)
                    ? "The LP relaxation could not be solved to optimality."
                    : relaxation.Message;

                result.VariableNames = relaxation.VariableNames;
                result.VariableValues = relaxation.VariableValues;
                return result;
            }

            // ---------------------------------------------------------
            // Columns whose value must be an integer at the end of the
            // day - both "int" and "bin" restricted original
            // variables map to a single canonical column each, so no
            // extra bookkeeping is required for split ("urs") columns.
            // ---------------------------------------------------------

            HashSet<int> integerColumns = new HashSet<int>(
                canonical.IntegerVariableColumns
                    .Concat(canonical.BinaryVariableColumns));

            if (integerColumns.Count == 0)
            {
                result.Message =
                    "The model has no 'int' or 'bin' restricted " +
                    "variables - Cutting Plane returns the LP " +
                    "relaxation directly.";
            }

            // ---------------------------------------------------------
            // Working copy of the canonical model. Every cut appends
            // one row and one column, so plain arrays/lists are grown
            // as needed rather than mutating the original canonical
            // model in place.
            // ---------------------------------------------------------

            double[,] matrix = CloneMatrix(canonical.ConstraintMatrix);
            double[] rhs = (double[])canonical.RightHandSide.Clone();
            double[] objective = (double[])canonical.ObjectiveCoefficients.Clone();
            List<string> names = canonical.VariableNames.ToList();
            int[] basis = (int[])relaxation.BasisColumns.Clone();

            int pivots = relaxation.PivotCount;
            int cutCount = 0;

            while (true)
            {
                int m = matrix.GetLength(0);

                double[,] binv;
                if (!TryInverse(BuildBasis(matrix, basis), out binv))
                {
                    result.Status = SimplexResult.SolverStatus.Infeasible;
                    result.Message =
                        "The basis became singular while generating " +
                        "cutting planes.";
                    break;
                }

                double[] xB = Multiply(binv, rhs);

                // -------------------------------------------------
                // Step 2: look for a fractional integer-restricted
                // basic variable. The row with the LARGEST fractional
                // part is chosen, which is the standard Gomory
                // source-row heuristic.
                // -------------------------------------------------

                int sourceRow = -1;
                double bestFraction = -1.0;

                for (int i = 0; i < m; i++)
                {
                    if (!integerColumns.Contains(basis[i])) continue;

                    double value = Clean(xB[i]);
                    double frac = value - Math.Floor(value);

                    if (frac < Epsilon || frac > 1.0 - Epsilon) continue;

                    double distanceFromInteger = Math.Min(frac, 1.0 - frac);

                    if (distanceFromInteger > bestFraction)
                    {
                        bestFraction = distanceFromInteger;
                        sourceRow = i;
                    }
                }

                if (sourceRow == -1)
                {
                    // ---------------------------------------------
                    // Integer-feasible: build the final solution.
                    // ---------------------------------------------

                    int n = matrix.GetLength(1);
                    double[] x = new double[n];

                    for (int i = 0; i < m; i++)
                    {
                        x[basis[i]] = Clean(xB[i]);
                    }

                    double raw = Dot(objective, x);

                    result.Status = SimplexResult.SolverStatus.Optimal;

                    if (cutCount == 0)
                    {
                        if (integerColumns.Count == 0)
                        {
                            // Keep the "no int/bin variables" note set
                            // before the loop started.
                        }
                        else
                        {
                            result.Message =
                                "The LP relaxation was already " +
                                "integer-feasible - no cuts were required.";
                        }
                    }
                    else
                    {
                        result.Message =
                            $"Integer-feasible optimum found after " +
                            $"{cutCount} cut(s).";
                    }

                    result.VariableNames = names.ToArray();
                    result.VariableValues = x;
                    result.BasisColumns = (int[])basis.Clone();
                    result.ObjectiveValue =
                        canonical.WasMinimization ? -raw : raw;

                    break;
                }

                if (cutCount >= maxCuts)
                {
                    result.Status = SimplexResult.SolverStatus.IterationLimit;

                    result.Message =
                        $"Cutting Plane stopped after {maxCuts} cuts " +
                        "without reaching an integer-feasible solution.";

                    break;
                }

                // -------------------------------------------------
                // Step 3: derive the Gomory cut from the source row.
                // -------------------------------------------------

                cutCount++;

                double[] binvRow = GetRow(binv, sourceRow);
                int oldN = matrix.GetLength(1);

                double[] tableauRow = new double[oldN];
                double[] fractional = new double[oldN];

                for (int j = 0; j < oldN; j++)
                {
                    double a = Dot(binvRow, GetColumn(matrix, j));
                    tableauRow[j] = a;
                    fractional[j] = a - Math.Floor(a);
                }

                double bi = Clean(xB[sourceRow]);
                double fi = bi - Math.Floor(bi);

                GomoryCut cut = new GomoryCut
                {
                    CutNumber = cutCount,
                    SourceRow = sourceRow,
                    SourceVariable = names[basis[sourceRow]],
                    SourceValue = bi,
                    TableauRow = tableauRow,
                    FractionalCoefficients = fractional,
                    RightHandSideFraction = fi,
                    SlackVariableName = $"g{cutCount}"
                };

                cut.Description = BuildCutDescription(cut, names);
                result.Cuts.Add(cut);

                // -------------------------------------------------
                // Append the cut, in dual-simplex-ready form:
                //
                //     -sum_j frac(a_ij) x_j + g = -frac(b_i)
                //
                // -------------------------------------------------

                int newN = oldN + 1;
                int newM = m + 1;

                double[,] newMatrix = new double[newM, newN];

                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < oldN; j++)
                    {
                        newMatrix[i, j] = matrix[i, j];
                    }

                    newMatrix[i, oldN] = 0.0;
                }

                for (int j = 0; j < oldN; j++)
                {
                    newMatrix[m, j] = -fractional[j];
                }

                newMatrix[m, oldN] = 1.0;

                double[] newRhs = new double[newM];
                Array.Copy(rhs, newRhs, m);
                newRhs[m] = -fi;

                double[] newObjective = new double[newN];
                Array.Copy(objective, newObjective, oldN);
                newObjective[oldN] = 0.0;

                names.Add(cut.SlackVariableName);

                int[] newBasis = new int[newM];
                Array.Copy(basis, newBasis, m);
                newBasis[m] = oldN;

                matrix = newMatrix;
                rhs = newRhs;
                objective = newObjective;
                basis = newBasis;

                // -------------------------------------------------
                // Step 4: restore primal feasibility with Dual
                // Simplex (Product Form / Price Out).
                //
                // The augmented basis is block-diagonal
                // (old basis columns are all zero in the new row,
                // and the new slack column is zero in every old
                // row), so its inverse is simply the old B^-1
                // extended with a unit row/column.
                // -------------------------------------------------

                if (!TryInverse(BuildBasis(matrix, basis), out binv))
                {
                    result.Status = SimplexResult.SolverStatus.Infeasible;
                    result.Message =
                        $"The basis became singular after adding cut " +
                        $"{cutCount}.";
                    break;
                }

                bool dualFeasible = true;

                for (int guard = 0; guard < iterationLimit; guard++)
                {
                    xB = Multiply(binv, rhs);

                    int leavingRow = -1;
                    double mostNegative = -Epsilon;

                    for (int i = 0; i < matrix.GetLength(0); i++)
                    {
                        double value = Clean(xB[i]);

                        if (value < mostNegative)
                        {
                            mostNegative = value;
                            leavingRow = i;
                        }
                    }

                    if (leavingRow == -1)
                    {
                        // Primal feasible again - done with this cut.
                        break;
                    }

                    double[] leavingRowVector = GetRow(binv, leavingRow);
                    int n = matrix.GetLength(1);
                    double[] rowValues = new double[n];

                    for (int j = 0; j < n; j++)
                    {
                        rowValues[j] = Dot(leavingRowVector, GetColumn(matrix, j));
                    }

                    double[] cB = new double[basis.Length];
                    for (int i = 0; i < basis.Length; i++)
                    {
                        cB[i] = objective[basis[i]];
                    }

                    double[] prices = MultiplyRowByMatrix(cB, binv);

                    double[] reducedCosts = new double[n];
                    for (int j = 0; j < n; j++)
                    {
                        reducedCosts[j] =
                            objective[j] - Dot(prices, GetColumn(matrix, j));
                    }

                    int enteringColumn = -1;
                    double bestRatio = double.PositiveInfinity;

                    for (int j = 0; j < n; j++)
                    {
                        if (Contains(basis, j)) continue;
                        if (rowValues[j] >= -Epsilon) continue;

                        double ratio = reducedCosts[j] / rowValues[j];

                        if (ratio < bestRatio - Epsilon)
                        {
                            bestRatio = ratio;
                            enteringColumn = j;
                        }
                    }

                    if (enteringColumn == -1)
                    {
                        result.Status = SimplexResult.SolverStatus.Infeasible;

                        result.Message =
                            $"The model is infeasible - cut {cutCount} " +
                            "left no valid entering variable during " +
                            "Dual Simplex re-optimisation.";

                        dualFeasible = false;
                        break;
                    }

                    // The pivot row's tableau values double as the
                    // "direction" vector for display purposes here -
                    // in Dual Simplex the leaving row is fixed first,
                    // so this row is what determines both the ratio
                    // test and the pivot.
                    double[] direction = rowValues;

                    double pivotValue = rowValues[enteringColumn];

                    double[] columnDirection = new double[matrix.GetLength(0)];
                    for (int i = 0; i < matrix.GetLength(0); i++)
                    {
                        columnDirection[i] =
                            Dot(GetRow(binv, i), GetColumn(matrix, enteringColumn));
                    }

                    double[,] eta = BuildEtaMatrix(columnDirection, leavingRow);
                    binv = Multiply(eta, binv);

                    string leavingName = names[basis[leavingRow]];
                    string enteringName = names[enteringColumn];

                    basis[leavingRow] = enteringColumn;
                    pivots++;

                    double objectiveValue = Dot(prices, rhs);

                    RevisedSimplexIteration record = new RevisedSimplexIteration
                    {
                        IterationNumber = pivots,
                        BasisColumns = (int[])basis.Clone(),
                        BasisNames = basis.Select(b => names[b]).ToArray(),
                        BasisMatrix = BuildBasis(matrix, basis),
                        BasisInverse = CloneMatrix(binv),
                        BasicCosts = cB,
                        Prices = prices,
                        ReducedCosts = reducedCosts,
                        Direction = direction,
                        Ratios = BuildRatioDisplay(n, enteringColumn, bestRatio),
                        EnteringColumn = enteringColumn,
                        LeavingRow = leavingRow,
                        EnteringVariable = enteringName,
                        LeavingVariable = leavingName,
                        PivotValue = pivotValue,
                        ObjectiveValue =
                            canonical.WasMinimization ? -objectiveValue : objectiveValue,
                        EtaMatrix = eta,
                        IsFinal = false,
                        Stage = $"Cut {cutCount} - Dual Simplex"
                    };

                    result.Iterations.Add(record);
                }

                if (!dualFeasible)
                {
                    break;
                }
            }

            if (result.Status == SimplexResult.SolverStatus.Infeasible &&
                string.IsNullOrEmpty(result.Message))
            {
                result.Message = "The model is infeasible.";
            }

            result.PivotCount = pivots;

            if (result.Iterations.Count > 0)
            {
                result.Iterations[result.Iterations.Count - 1].IsFinal =
                    result.Status == SimplexResult.SolverStatus.Optimal;
            }

            CanonicalModel finalCanonical = canonical.Clone();
            finalCanonical.ConstraintMatrix = matrix;
            finalCanonical.RightHandSide = rhs;
            finalCanonical.ObjectiveCoefficients = objective;
            finalCanonical.VariableNames = names.ToArray();
            result.FinalCanonical = finalCanonical;

            if (result.VariableNames.Length == 0)
            {
                result.VariableNames = names.ToArray();
            }

            return result;
        }

        // =============================================================
        // DISPLAY / DESCRIPTION HELPERS
        // =============================================================

        private static string BuildCutDescription(GomoryCut cut, List<string> names)
        {
            List<string> terms = new List<string>();

            for (int j = 0; j < cut.FractionalCoefficients.Length; j++)
            {
                double coefficient = Clean(cut.FractionalCoefficients[j]);

                if (Math.Abs(coefficient) < Epsilon) continue;

                terms.Add($"{coefficient:F3} {names[j]}");
            }

            string left = terms.Count == 0 ? "0" : string.Join(" + ", terms);

            return $"{left} >= {cut.RightHandSideFraction:F3}  " +
                   $"(from row of {cut.SourceVariable} = {cut.SourceValue:F3})";
        }

        private static double[] BuildRatioDisplay(int n, int enteringColumn, double ratio)
        {
            double[] ratios = new double[n];

            for (int j = 0; j < n; j++)
            {
                ratios[j] = double.PositiveInfinity;
            }

            if (enteringColumn >= 0 && enteringColumn < n)
            {
                ratios[enteringColumn] = ratio;
            }

            return ratios;
        }

        // =============================================================
        // LINEAR ALGEBRA HELPERS
        //
        // Deliberately self-contained (rather than shared with
        // RevisedSimplexSolver) since that class keeps its helpers
        // private. Behaviour matches RevisedSimplexSolver's Product
        // Form conventions exactly.
        // =============================================================

        private static double[,] BuildBasis(double[,] a, int[] basis)
        {
            int m = a.GetLength(0);
            double[,] b = new double[m, m];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    b[i, j] = a[i, basis[j]];
                }
            }

            return b;
        }

        private static double[,] BuildEtaMatrix(double[] direction, int pivotRow)
        {
            int m = direction.Length;
            double[,] eta = new double[m, m];

            for (int i = 0; i < m; i++)
            {
                if (i == pivotRow)
                {
                    eta[i, i] = 1.0 / direction[pivotRow];
                }
                else
                {
                    eta[i, i] = 1.0;
                    eta[i, pivotRow] = -direction[i] / direction[pivotRow];
                }
            }

            return eta;
        }

        private static bool TryInverse(double[,] matrix, out double[,] inverse)
        {
            int n = matrix.GetLength(0);
            inverse = new double[n, n];

            if (matrix.GetLength(1) != n) return false;

            double[,] a = new double[n, 2 * n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    a[i, j] = matrix[i, j];
                    a[i, j + n] = i == j ? 1.0 : 0.0;
                }
            }

            for (int col = 0; col < n; col++)
            {
                int pivotRow = -1;
                double best = Epsilon;

                for (int row = col; row < n; row++)
                {
                    if (Math.Abs(a[row, col]) > best)
                    {
                        best = Math.Abs(a[row, col]);
                        pivotRow = row;
                    }
                }

                if (pivotRow == -1) return false;

                if (pivotRow != col)
                {
                    for (int j = 0; j < 2 * n; j++)
                    {
                        double temp = a[col, j];
                        a[col, j] = a[pivotRow, j];
                        a[pivotRow, j] = temp;
                    }
                }

                double pivotValue = a[col, col];

                for (int j = 0; j < 2 * n; j++)
                {
                    a[col, j] /= pivotValue;
                }

                for (int row = 0; row < n; row++)
                {
                    if (row == col) continue;

                    double factor = a[row, col];
                    if (Math.Abs(factor) < 1e-15) continue;

                    for (int j = 0; j < 2 * n; j++)
                    {
                        a[row, j] -= factor * a[col, j];
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    inverse[i, j] = a[i, j + n];
                }
            }

            return true;
        }

        private static double[] Multiply(double[,] matrix, double[] vector)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[] result = new double[rows];

            for (int i = 0; i < rows; i++)
            {
                double sum = 0.0;

                for (int j = 0; j < cols; j++)
                {
                    sum += matrix[i, j] * vector[j];
                }

                result[i] = sum;
            }

            return result;
        }

        private static double[,] Multiply(double[,] a, double[,] b)
        {
            int rows = a.GetLength(0);
            int inner = a.GetLength(1);
            int cols = b.GetLength(1);

            double[,] result = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0.0;

                    for (int k = 0; k < inner; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }

                    result[i, j] = sum;
                }
            }

            return result;
        }

        private static double[] MultiplyRowByMatrix(double[] row, double[,] matrix)
        {
            int cols = matrix.GetLength(1);
            double[] result = new double[cols];

            for (int j = 0; j < cols; j++)
            {
                double sum = 0.0;

                for (int i = 0; i < row.Length; i++)
                {
                    sum += row[i] * matrix[i, j];
                }

                result[j] = sum;
            }

            return result;
        }

        private static double[] GetRow(double[,] matrix, int row)
        {
            int cols = matrix.GetLength(1);
            double[] result = new double[cols];

            for (int j = 0; j < cols; j++)
            {
                result[j] = matrix[row, j];
            }

            return result;
        }

        private static double[] GetColumn(double[,] matrix, int column)
        {
            int rows = matrix.GetLength(0);
            double[] result = new double[rows];

            for (int i = 0; i < rows; i++)
            {
                result[i] = matrix[i, column];
            }

            return result;
        }

        private static double Dot(double[] a, double[] b)
        {
            double sum = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }

            return sum;
        }

        private static bool Contains(int[] array, int value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value) return true;
            }

            return false;
        }

        private static double[,] CloneMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] clone = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    clone[i, j] = matrix[i, j];
                }
            }

            return clone;
        }

        private static double Clean(double x) => Math.Abs(x) < Epsilon ? 0.0 : x;
    }
}
