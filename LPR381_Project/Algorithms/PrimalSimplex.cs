using LPR381_Project.Algorithms;
using LPR381_Project.Models;
using System;
using System.Collections.Generic;

namespace LPR381_Project.Algorithms
{
    /// <summary>
    /// Primal Simplex solver supporting:
    ///
    /// - Standard maximisation problems
    /// - Minimisation problems converted to maximisation
    /// - <=, >= and = constraints
    /// - Artificial variables
    /// - Phase 1 / Phase 2
    /// - Infeasibility detection
    /// - Unboundedness detection
    /// - Degenerate solutions
    /// - Reusable SimplexResult output
    ///
    /// The solver performs calculations only. It does not write
    /// directly to the console.
    /// </summary>
    public class SimplexSolver
    {
        private const int DefaultIterationLimit = 1000;
        private const double Epsilon = 1e-9;

        // =============================================================
        // PUBLIC ENTRY POINTS
        // =============================================================

        /// <summary>
        /// Solves a canonical model using the Primal Simplex method.
        /// </summary>
        public static SimplexResult Solve(
            CanonicalModel canonical)
        {
            return Solve(
                canonical,
                DefaultIterationLimit);
        }

        /// <summary>
        /// Solves a canonical model using the Primal Simplex method
        /// with a configurable iteration limit.
        /// </summary>
        public static SimplexResult Solve(
            CanonicalModel canonical,
            int iterationLimit)
        {
            SimplexResult result =
                new SimplexResult();

            // ---------------------------------------------------------
            // Validate input
            // ---------------------------------------------------------

            if (canonical == null)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "Canonical model is null.";

                return result;
            }

            if (iterationLimit <= 0)
            {
                result.Status =
                    SimplexResult.SolverStatus.IterationLimit;

                result.Message =
                    "Iteration limit must be greater than zero.";

                return result;
            }

            if (!ValidateCanonicalModel(
                    canonical,
                    result))
            {
                return result;
            }

            // ---------------------------------------------------------
            // Build the initial tableau.
            // ---------------------------------------------------------

            double[,] tableau =
                BuildInitialTableau(
                    canonical);

            int constraintCount =
                tableau.GetLength(0) - 1;

            int variableCount =
                tableau.GetLength(1) - 1;

            int rhsColumn =
                tableau.GetLength(1) - 1;

            int zRow =
                tableau.GetLength(0) - 1;

            // ---------------------------------------------------------
            // Determine the initial basis.
            //
            // The canonical converter has already determined the
            // initial basic variable for each constraint.
            // ---------------------------------------------------------

            int[] basis =
                CreateInitialBasis(
                    canonical,
                    constraintCount);

            // ---------------------------------------------------------
            // Phase 1
            // ---------------------------------------------------------

            if (canonical.RequiresPhaseOne)
            {
                result.Iterations.Add(
                    CreateIterationSnapshot(
                        tableau,
                        0,
                        -1,
                        -1,
                        string.Empty,
                        string.Empty,
                        0.0,
                        false));

                int phaseOnePivots = 0;

                bool phaseOneCompleted =
                    RunPhaseOne(
                        tableau,
                        canonical,
                        basis,
                        iterationLimit,
                        result,
                        ref phaseOnePivots);

                if (!phaseOneCompleted)
                {
                    result.PivotCount =
                        phaseOnePivots;

                    result.FinalTableau =
                        CloneMatrix(tableau);

                    result.VariableNames =
                        GetVariableNames(canonical);

                    result.VariableValues =
                        ExtractVariableValues(
                            tableau,
                            basis,
                            variableCount);

                    result.BasisRows =
                        BuildBasisRows(
                            basis);

                    return result;
                }

                // -----------------------------------------------------
                // Remove artificial variables from the basis where
                // possible.
                // -----------------------------------------------------

                bool removedSuccessfully =
                    RemoveArtificialVariablesFromBasis(
                        tableau,
                        canonical,
                        basis,
                        ref phaseOnePivots,
                        iterationLimit,
                        result);

                if (!removedSuccessfully)
                {
                    result.Status =
                        SimplexResult.SolverStatus.Infeasible;

                    result.Message =
                        "Unable to remove artificial variables " +
                        "after Phase 1.";

                    result.PivotCount =
                        phaseOnePivots;

                    result.FinalTableau =
                        CloneMatrix(tableau);

                    result.VariableNames =
                        GetVariableNames(canonical);

                    result.VariableValues =
                        ExtractVariableValues(
                            tableau,
                            basis,
                            variableCount);

                    result.BasisRows =
                        BuildBasisRows(
                            basis);

                    return result;
                }

                // -----------------------------------------------------
                // Rebuild the Phase 2 objective row.
                // -----------------------------------------------------

                SetObjectiveRow(
                    tableau,
                    canonical,
                    basis);

                // -----------------------------------------------------
                // Phase 2.
                // -----------------------------------------------------

                int phaseTwoPivots = 0;

                SimplexResult phaseTwoResult =
                    RunSimplexPhase(
                        tableau,
                        canonical,
                        basis,
                        iterationLimit - phaseOnePivots,
                        result,
                        ref phaseTwoPivots);

                result.Status =
                    phaseTwoResult.Status;

                result.Message =
                    phaseTwoResult.Message;

                result.PivotCount =
                    phaseOnePivots + phaseTwoPivots;
            }
            else
            {
                // -----------------------------------------------------
                // No artificial variables.
                //
                // We can directly construct the normal objective row
                // and run the simplex algorithm.
                // -----------------------------------------------------

                SetObjectiveRow(
                    tableau,
                    canonical,
                    basis);

                int phaseTwoPivots = 0;

                SimplexResult phaseTwoResult =
                    RunSimplexPhase(
                        tableau,
                        canonical,
                        basis,
                        iterationLimit,
                        result,
                        ref phaseTwoPivots);

                result.Status =
                    phaseTwoResult.Status;

                result.Message =
                    phaseTwoResult.Message;

                result.PivotCount =
                    phaseTwoPivots;
            }

            // ---------------------------------------------------------
            // Convert final internal result into the public result.
            // ---------------------------------------------------------

            result.FinalTableau =
                CloneMatrix(tableau);

            result.VariableNames =
                GetVariableNames(canonical);

            result.VariableValues =
                ExtractVariableValues(
                    tableau,
                    basis,
                    variableCount);

            result.BasisRows =
                BuildBasisRows(
                    basis);

            // Internal tableau is always maximisation.
            double internalObjective =
                tableau[
                    zRow,
                    rhsColumn];

            // Convert the objective back to the user's original
            // minimisation/maximisation convention.
            if (canonical.WasMinimization)
            {
                result.ObjectiveValue =
                    -internalObjective;
            }
            else
            {
                result.ObjectiveValue =
                    internalObjective;
            }

            if (Math.Abs(
                result.ObjectiveValue) < Epsilon)
            {
                result.ObjectiveValue = 0.0;
            }

            return result;
        }

        // =============================================================
        // VALIDATION
        // =============================================================

        private static bool ValidateCanonicalModel(
            CanonicalModel canonical,
            SimplexResult result)
        {
            if (canonical.ConstraintMatrix == null)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "Constraint matrix is null.";

                return false;
            }

            int rows =
                canonical.ConstraintMatrix.GetLength(0);

            int columns =
                canonical.ConstraintMatrix.GetLength(1);

            if (rows == 0)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "The canonical model contains no constraints.";

                return false;
            }

            if (columns == 0)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "The canonical model contains no variables.";

                return false;
            }

            if (canonical.RightHandSide == null ||
                canonical.RightHandSide.Length != rows)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "RHS dimensions do not match the constraint matrix.";

                return false;
            }

            if (canonical.ObjectiveCoefficients == null ||
                canonical.ObjectiveCoefficients.Length != columns)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "Objective dimensions do not match the constraint matrix.";

                return false;
            }

            if (canonical.InitialBasisColumns == null ||
                canonical.InitialBasisColumns.Length != rows)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "Initial basis information is invalid.";

                return false;
            }

            return true;
        }

        // =============================================================
        // INITIAL TABLEAU
        // =============================================================

        private static double[,] BuildInitialTableau(
            CanonicalModel canonical)
        {
            int constraintCount =
                canonical.ConstraintMatrix.GetLength(0);

            int variableCount =
                canonical.ConstraintMatrix.GetLength(1);

            double[,] tableau =
                new double[
                    constraintCount + 1,
                    variableCount + 1];

            int rhsColumn =
                variableCount;

            // ---------------------------------------------------------
            // Constraint rows
            // ---------------------------------------------------------

            for (int i = 0;
                 i < constraintCount;
                 i++)
            {
                for (int j = 0;
                     j < variableCount;
                     j++)
                {
                    tableau[i, j] =
                        canonical.ConstraintMatrix[i, j];
                }

                tableau[i, rhsColumn] =
                    canonical.RightHandSide[i];
            }

            // ---------------------------------------------------------
            // Objective row initially starts at zero.
            //
            // It is constructed separately by SetObjectiveRow()
            // because Phase 1 requires a different objective.
            // ---------------------------------------------------------

            return tableau;
        }

        // =============================================================
        // BASIS
        // =============================================================

        private static int[] CreateInitialBasis(
            CanonicalModel canonical,
            int constraintCount)
        {
            int[] basis =
                new int[constraintCount];

            for (int i = 0;
                 i < constraintCount;
                 i++)
            {
                basis[i] =
                    canonical.InitialBasisColumns[i];
            }

            return basis;
        }

        private static int[] BuildBasisRows(
            int[] basis)
        {
            if (basis == null)
            {
                return new int[0];
            }

            int[] basisRows =
                new int[basis.Length];

            for (int i = 0;
                 i < basis.Length;
                 i++)
            {
                basisRows[i] =
                    basis[i];
            }

            return basisRows;
        }

        // =============================================================
        // PHASE 1
        // =============================================================

        /// <summary>
        /// Performs Phase 1 by maximising:
        ///
        ///     -a1 - a2 - ... - am
        ///
        /// The artificial variables therefore have objective
        /// coefficient -1 in the original mathematical objective.
        ///
        /// Since the tableau convention is:
        ///
        ///     Z-row = -C
        ///
        /// artificial-variable columns initially contain +1 in
        /// the Phase 1 Z-row.
        /// </summary>
        private static bool RunPhaseOne(
            double[,] tableau,
            CanonicalModel canonical,
            int[] basis,
            int iterationLimit,
            SimplexResult result,
            ref int pivotCount)
        {
            int variableCount =
                tableau.GetLength(1) - 1;

            int rhsColumn =
                tableau.GetLength(1) - 1;

            int zRow =
                tableau.GetLength(0) - 1;

            // ---------------------------------------------------------
            // Create Phase 1 objective.
            // ---------------------------------------------------------

            for (int j = 0;
                 j < variableCount;
                 j++)
            {
                tableau[zRow, j] = 0.0;
            }

            tableau[zRow, rhsColumn] = 0.0;

            foreach (int artificialColumn
                in canonical.ArtificialVariableColumns)
            {
                tableau[zRow, artificialColumn] =
                    1.0;
            }

            // ---------------------------------------------------------
            // Because artificial variables are initially basic,
            // eliminate their coefficients from the Z-row.
            // ---------------------------------------------------------

            for (int row = 0;
                 row < basis.Length;
                 row++)
            {
                int basicColumn =
                    basis[row];

                if (Contains(
                    canonical.ArtificialVariableColumns,
                    basicColumn))
                {
                    double factor =
                        tableau[zRow, basicColumn];

                    if (Math.Abs(factor) > Epsilon)
                    {
                        for (int j = 0;
                             j <= rhsColumn;
                             j++)
                        {
                            tableau[zRow, j] -=
                                factor *
                                tableau[row, j];
                        }
                    }
                }
            }

            CleanSmallValues(tableau);

            // ---------------------------------------------------------
            // Record Phase 1 initial tableau.
            // ---------------------------------------------------------

            result.Iterations.Add(
                CreateIterationSnapshot(
                    tableau,
                    pivotCount,
                    -1,
                    -1,
                    "Phase 1",
                    string.Empty,
                    0.0,
                    false));

            // ---------------------------------------------------------
            // Run simplex on the Phase 1 objective.
            // ---------------------------------------------------------

            SimplexResult phaseResult =
                RunSimplexPhase(
                    tableau,
                    canonical,
                    basis,
                    iterationLimit,
                    result,
                    ref pivotCount,
                    true);

            if (phaseResult.Status ==
                SimplexResult.SolverStatus.Unbounded)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "Phase 1 became unbounded. " +
                    "The model cannot produce a valid feasible basis.";

                return false;
            }

            if (phaseResult.Status ==
                SimplexResult.SolverStatus.IterationLimit)
            {
                result.Status =
                    SimplexResult.SolverStatus.IterationLimit;

                result.Message =
                    "Phase 1 reached the simplex iteration limit.";

                return false;
            }

            // ---------------------------------------------------------
            // Phase 1 objective must be zero.
            //
            // Because the objective is:
            //
            //     -sum(artificial variables)
            //
            // any negative value means that artificial variables
            // remain positive and the original LP is infeasible.
            // ---------------------------------------------------------

            double phaseOneObjective =
                tableau[
                    tableau.GetLength(0) - 1,
                    rhsColumn];

            if (phaseOneObjective < -Epsilon)
            {
                result.Status =
                    SimplexResult.SolverStatus.Infeasible;

                result.Message =
                    "The programming model is infeasible. " +
                    "Phase 1 could not drive all artificial variables to zero.";

                return false;
            }

            // ---------------------------------------------------------
            // Explicitly verify artificial variable values.
            // ---------------------------------------------------------

            foreach (int artificialColumn
                in canonical.ArtificialVariableColumns)
            {
                double value =
                    GetVariableValue(
                        tableau,
                        basis,
                        artificialColumn);

                if (value > Epsilon)
                {
                    result.Status =
                        SimplexResult.SolverStatus.Infeasible;

                    result.Message =
                        "The programming model is infeasible. " +
                        "At least one artificial variable remains positive.";

                    return false;
                }
            }

            return true;
        }

        // =============================================================
        // REMOVE ARTIFICIAL VARIABLES FROM BASIS
        // =============================================================

        private static bool RemoveArtificialVariablesFromBasis(
            double[,] tableau,
            CanonicalModel canonical,
            int[] basis,
            ref int pivotCount,
            int iterationLimit,
            SimplexResult result)
        {
            int constraintCount =
                tableau.GetLength(0) - 1;

            int variableCount =
                tableau.GetLength(1) - 1;

            int rhsColumn =
                variableCount;

            // ---------------------------------------------------------
            // Look for artificial variables still in the basis.
            // ---------------------------------------------------------

            for (int row = 0;
                 row < constraintCount;
                 row++)
            {
                int basicColumn =
                    basis[row];

                if (!Contains(
                    canonical.ArtificialVariableColumns,
                    basicColumn))
                {
                    continue;
                }

                // -----------------------------------------------------
                // If the artificial variable is basic with value 0,
                // try to replace it with a non-artificial variable.
                // -----------------------------------------------------

                int replacementColumn = -1;

                for (int column = 0;
                     column < variableCount;
                     column++)
                {
                    if (Contains(
                        canonical.ArtificialVariableColumns,
                        column))
                    {
                        continue;
                    }

                    if (Math.Abs(
                        tableau[row, column]) > Epsilon)
                    {
                        replacementColumn =
                            column;

                        break;
                    }
                }

                if (replacementColumn != -1)
                {
                    string entering =
                        GetVariableName(
                            canonical,
                            replacementColumn);

                    string leaving =
                        GetVariableName(
                            canonical,
                            basicColumn);

                    double pivotValue =
                        tableau[row, replacementColumn];

                    PerformPivot(
                        tableau,
                        row,
                        replacementColumn);

                    basis[row] =
                        replacementColumn;

                    pivotCount++;

                    result.Iterations.Add(
                        CreateIterationSnapshot(
                            tableau,
                            pivotCount,
                            row,
                            replacementColumn,
                            entering,
                            leaving,
                            pivotValue,
                            false));

                    if (pivotCount >=
                        iterationLimit)
                    {
                        result.Status =
                            SimplexResult.SolverStatus.IterationLimit;

                        result.Message =
                            "Iteration limit reached while " +
                            "removing artificial variables.";

                        return false;
                    }
                }
                else
                {
                    // -------------------------------------------------
                    // No non-artificial coefficient exists in this row.
                    //
                    // If RHS is also zero, this is a redundant
                    // constraint.
                    //
                    // We leave the row in place. Its artificial basic
                    // variable has value zero and therefore does not
                    // affect the feasible solution.
                    // -------------------------------------------------

                    if (Math.Abs(
                        tableau[row, rhsColumn]) > Epsilon)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // =============================================================
        // PHASE 2 OBJECTIVE
        // =============================================================

        private static void SetObjectiveRow(
            double[,] tableau,
            CanonicalModel canonical,
            int[] basis)
        {
            int variableCount =
                tableau.GetLength(1) - 1;

            int rhsColumn =
                variableCount;

            int zRow =
                tableau.GetLength(0) - 1;

            // ---------------------------------------------------------
            // Set the objective row to -C.
            //
            // Artificial variables receive coefficient 0 because they
            // are not part of the real objective.
            // ---------------------------------------------------------

            for (int j = 0;
                 j < variableCount;
                 j++)
            {
                tableau[zRow, j] =
                    -canonical.ObjectiveCoefficients[j];
            }

            tableau[zRow, rhsColumn] =
                0.0;

            // ---------------------------------------------------------
            // Make the objective row consistent with the current basis.
            //
            // For every basic variable:
            //
            //     Z-row coefficient = 0
            //
            // ---------------------------------------------------------

            for (int row = 0;
                 row < basis.Length;
                 row++)
            {
                int basicColumn =
                    basis[row];

                if (basicColumn < 0 ||
                    basicColumn >= variableCount)
                {
                    continue;
                }

                double factor =
                    tableau[zRow, basicColumn];

                if (Math.Abs(factor) < Epsilon)
                {
                    continue;
                }

                for (int j = 0;
                     j <= rhsColumn;
                     j++)
                {
                    tableau[zRow, j] -=
                        factor *
                        tableau[row, j];
                }
            }

            CleanSmallValues(tableau);
        }

        // =============================================================
        // SIMPLEX PHASE
        // =============================================================

        private static SimplexResult RunSimplexPhase(
            double[,] tableau,
            CanonicalModel canonical,
            int[] basis,
            int iterationLimit,
            SimplexResult result,
            ref int pivotCount,
            bool phaseOne = false)
        {
            SimplexResult phaseResult =
                new SimplexResult();

            if (iterationLimit <= 0)
            {
                phaseResult.Status =
                    SimplexResult.SolverStatus.IterationLimit;

                phaseResult.Message =
                    "No simplex iterations remain.";

                return phaseResult;
            }

            int constraintCount =
                tableau.GetLength(0) - 1;

            int variableCount =
                tableau.GetLength(1) - 1;

            int rhsColumn =
                variableCount;

            int zRow =
                tableau.GetLength(0) - 1;

            while (pivotCount < iterationLimit)
            {
                // -----------------------------------------------------
                // Select entering variable.
                // -----------------------------------------------------

                int pivotColumn =
                    GetPivotColumn(
                        tableau,
                        zRow,
                        canonical,
                        phaseOne);

                // No negative coefficient means optimal.
                if (pivotColumn == -1)
                {
                    phaseResult.Status =
                        SimplexResult.SolverStatus.Optimal;

                    phaseResult.Message =
                        phaseOne
                            ? "Phase 1 optimal solution reached."
                            : "Optimal solution reached.";

                    return phaseResult;
                }

                // -----------------------------------------------------
                // Select leaving variable.
                // -----------------------------------------------------

                int pivotRow =
                    GetPivotRow(
                        tableau,
                        pivotColumn,
                        constraintCount,
                        rhsColumn,
                        basis);

                // No valid leaving row means unbounded.
                if (pivotRow == -1)
                {
                    phaseResult.Status =
                        SimplexResult.SolverStatus.Unbounded;

                    phaseResult.Message =
                        phaseOne
                            ? "Phase 1 encountered an unbounded direction."
                            : "The programming model is unbounded.";

                    return phaseResult;
                }

                string enteringVariable =
                    GetVariableName(
                        canonical,
                        pivotColumn);

                string leavingVariable =
                    GetVariableName(
                        canonical,
                        basis[pivotRow]);

                double pivotValue =
                    tableau[
                        pivotRow,
                        pivotColumn];

                // -----------------------------------------------------
                // Perform pivot.
                // -----------------------------------------------------

                PerformPivot(
                    tableau,
                    pivotRow,
                    pivotColumn);

                // Update basis.
                basis[pivotRow] =
                    pivotColumn;

                pivotCount++;

                // -----------------------------------------------------
                // Determine whether the resulting tableau is optimal.
                // -----------------------------------------------------

                bool isFinal =
                    GetPivotColumn(
                        tableau,
                        zRow,
                        canonical,
                        phaseOne) == -1;

                result.Iterations.Add(
                    CreateIterationSnapshot(
                        tableau,
                        pivotCount,
                        pivotRow,
                        pivotColumn,
                        enteringVariable,
                        leavingVariable,
                        pivotValue,
                        isFinal));

                if (isFinal)
                {
                    phaseResult.Status =
                        SimplexResult.SolverStatus.Optimal;

                    phaseResult.Message =
                        phaseOne
                            ? "Phase 1 optimal solution reached."
                            : "Optimal solution reached.";

                    return phaseResult;
                }
            }

            phaseResult.Status =
                SimplexResult.SolverStatus.IterationLimit;

            phaseResult.Message =
                phaseOne
                    ? "Phase 1 reached the simplex iteration limit."
                    : "The simplex iteration limit was reached.";

            return phaseResult;
        }

        // =============================================================
        // ENTERING VARIABLE
        // =============================================================

        private static int GetPivotColumn(
            double[,] tableau,
            int zRow,
            CanonicalModel canonical,
            bool phaseOne)
        {
            int variableCount =
                tableau.GetLength(1) - 1;

            int bestColumn = -1;

            double mostNegative =
                -Epsilon;

            for (int j = 0;
                 j < variableCount;
                 j++)
            {
                // -----------------------------------------------------
                // Artificial variables may participate during Phase 1
                // but must NEVER enter the basis during Phase 2.
                // -----------------------------------------------------

                if (!phaseOne &&
                    Contains(
                        canonical.ArtificialVariableColumns,
                        j))
                {
                    continue;
                }

                if (tableau[zRow, j] <
                    mostNegative)
                {
                    mostNegative =
                        tableau[zRow, j];

                    bestColumn =
                        j;
                }
            }

            return bestColumn;
        }

        // =============================================================
        // LEAVING VARIABLE
        // =============================================================

        private static int GetPivotRow(
            double[,] tableau,
            int pivotColumn,
            int constraintCount,
            int rhsColumn,
            int[] basis)
        {
            int bestRow = -1;

            double minimumRatio =
                double.MaxValue;

            for (int i = 0;
                 i < constraintCount;
                 i++)
            {
                double coefficient =
                    tableau[i, pivotColumn];

                if (coefficient <= Epsilon)
                {
                    continue;
                }

                double rhs =
                    tableau[i, rhsColumn];

                // A valid basic feasible tableau should not contain
                // significantly negative RHS values.
                if (rhs < -Epsilon)
                {
                    continue;
                }

                double ratio =
                    rhs / coefficient;

                // -----------------------------------------------------
                // Standard minimum ratio test.
                //
                // <= is used so that a zero ratio can correctly
                // participate in a degenerate pivot.
                // -----------------------------------------------------

                if (ratio <
                    minimumRatio - Epsilon)
                {
                    minimumRatio =
                        ratio;

                    bestRow =
                        i;
                }
                else if (Math.Abs(
                    ratio - minimumRatio) <= Epsilon)
                {
                    // -------------------------------------------------
                    // Bland-style tie breaking:
                    //
                    // choose the row whose basic variable has the
                    // smaller column index.
                    // -------------------------------------------------

                    if (bestRow == -1 ||
                        basis[i] < basis[bestRow])
                    {
                        bestRow =
                            i;
                    }
                }
            }

            return bestRow;
        }

        // =============================================================
        // PIVOT
        // =============================================================

        private static void PerformPivot(
            double[,] tableau,
            int pivotRow,
            int pivotColumn)
        {
            int rowCount =
                tableau.GetLength(0);

            int columnCount =
                tableau.GetLength(1);

            double pivot =
                tableau[
                    pivotRow,
                    pivotColumn];

            if (Math.Abs(pivot) <
                Epsilon)
            {
                throw new InvalidOperationException(
                    "Cannot perform a pivot on a zero element.");
            }

            // ---------------------------------------------------------
            // Divide pivot row by pivot.
            // ---------------------------------------------------------

            for (int j = 0;
                 j < columnCount;
                 j++)
            {
                tableau[pivotRow, j] /=
                    pivot;
            }

            // ---------------------------------------------------------
            // Eliminate pivot column from every other row.
            // ---------------------------------------------------------

            for (int i = 0;
                 i < rowCount;
                 i++)
            {
                if (i == pivotRow)
                {
                    continue;
                }

                double factor =
                    tableau[i, pivotColumn];

                if (Math.Abs(factor) <
                    Epsilon)
                {
                    continue;
                }

                for (int j = 0;
                     j < columnCount;
                     j++)
                {
                    tableau[i, j] -=
                        factor *
                        tableau[pivotRow, j];
                }
            }

            CleanSmallValues(tableau);
        }

        // =============================================================
        // VARIABLE VALUE EXTRACTION
        // =============================================================

        private static double[] ExtractVariableValues(
            double[,] tableau,
            int[] basis,
            int variableCount)
        {
            double[] values =
                new double[variableCount];

            int rhsColumn =
                variableCount;

            for (int row = 0;
                 row < basis.Length;
                 row++)
            {
                int basicColumn =
                    basis[row];

                if (basicColumn < 0 ||
                    basicColumn >= variableCount)
                {
                    continue;
                }

                values[basicColumn] =
                    tableau[row, rhsColumn];
            }

            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                if (Math.Abs(
                    values[i]) < Epsilon)
                {
                    values[i] = 0.0;
                }
            }

            return values;
        }

        private static double GetVariableValue(
            double[,] tableau,
            int[] basis,
            int variableColumn)
        {
            int rhsColumn =
                tableau.GetLength(1) - 1;

            for (int row = 0;
                 row < basis.Length;
                 row++)
            {
                if (basis[row] ==
                    variableColumn)
                {
                    return tableau[
                        row,
                        rhsColumn];
                }
            }

            return 0.0;
        }

        // =============================================================
        // VARIABLE NAMES
        // =============================================================

        private static string[] GetVariableNames(
            CanonicalModel canonical)
        {
            int variableCount =
                canonical.ConstraintMatrix.GetLength(1);

            string[] names =
                new string[variableCount];

            for (int i = 0;
                 i < variableCount;
                 i++)
            {
                names[i] =
                    GetVariableName(
                        canonical,
                        i);
            }

            return names;
        }

        private static string GetVariableName(
            CanonicalModel canonical,
            int column)
        {
            if (canonical.VariableNames != null &&
                column >= 0 &&
                column < canonical.VariableNames.Length)
            {
                return canonical.VariableNames[column];
            }

            return $"x{column + 1}";
        }

        // =============================================================
        // ITERATION SNAPSHOTS
        // =============================================================

        private static SimplexIteration
            CreateIterationSnapshot(
                double[,] tableau,
                int iterationNumber,
                int pivotRow,
                int pivotColumn,
                string enteringVariable,
                string leavingVariable,
                double pivotValue,
                bool isFinal)
        {
            return new SimplexIteration
            {
                IterationNumber =
                    iterationNumber,

                Tableau =
                    CloneMatrix(tableau),

                PivotRow =
                    pivotRow,

                PivotColumn =
                    pivotColumn,

                EnteringVariable =
                    enteringVariable ?? string.Empty,

                LeavingVariable =
                    leavingVariable ?? string.Empty,

                PivotValue =
                    pivotValue,

                IsFinal =
                    isFinal
            };
        }

        // =============================================================
        // MATRIX UTILITIES
        // =============================================================

        private static double[,] CloneMatrix(
            double[,] source)
        {
            int rows =
                source.GetLength(0);

            int columns =
                source.GetLength(1);

            double[,] clone =
                new double[rows, columns];

            for (int i = 0;
                 i < rows;
                 i++)
            {
                for (int j = 0;
                     j < columns;
                     j++)
                {
                    clone[i, j] =
                        source[i, j];
                }
            }

            return clone;
        }

        private static void CleanSmallValues(
            double[,] matrix)
        {
            for (int i = 0;
                 i < matrix.GetLength(0);
                 i++)
            {
                for (int j = 0;
                     j < matrix.GetLength(1);
                     j++)
                {
                    if (Math.Abs(
                        matrix[i, j]) <
                        Epsilon)
                    {
                        matrix[i, j] = 0.0;
                    }
                }
            }
        }

        // =============================================================
        // COLLECTION UTILITIES
        // =============================================================

        private static bool Contains(
            List<int> list,
            int value)
        {
            if (list == null)
            {
                return false;
            }

            return list.Contains(value);
        }
    }
}