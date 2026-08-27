using System;
using System.Collections.Generic;
using LPR381_Project.Models;

namespace LPR381_Project.Algorithms
{
    /// <summary>
    /// Represents the canonical representation of the original
    /// programming model.
    ///
    /// The original ProblemModel must remain unchanged. This class
    /// represents the transformed model used internally by the
    /// solution algorithms.
    /// </summary>
    public class CanonicalModel
    {
        // =============================================================
        // BASIC MODEL INFORMATION
        // =============================================================

        /// <summary>
        /// Original objective type supplied by the user.
        /// </summary>
        public string ObjectiveType { get; set; } = "max";

        /// <summary>
        /// Indicates whether the objective was transformed into a
        /// maximisation objective.
        ///
        /// The simplex implementation currently works using the
        /// maximisation tableau convention.
        /// </summary>
        public bool WasMinimization { get; set; }

        /// <summary>
        /// Original number of decision variables.
        /// </summary>
        public int OriginalVariableCount { get; set; }

        /// <summary>
        /// Original number of constraints before generated constraints
        /// such as binary upper bounds are added.
        /// </summary>
        public int OriginalConstraintCount { get; set; }

        // =============================================================
        // CANONICAL TABLEAU DATA
        // =============================================================

        /// <summary>
        /// Objective coefficients for the canonical variables.
        ///
        /// For a minimisation problem, the coefficients are negated so
        /// that the internal problem becomes a maximisation problem.
        /// </summary>
        public double[] ObjectiveCoefficients { get; set; }
            = new double[0];

        /// <summary>
        /// Canonical constraint coefficient matrix.
        /// </summary>
        public double[,] ConstraintMatrix { get; set; }
            = new double[0, 0];

        /// <summary>
        /// Right-hand-side values for the canonical constraints.
        /// </summary>
        public double[] RightHandSide { get; set; }
            = new double[0];

        /// <summary>
        /// Names of every canonical variable.
        ///
        /// Example:
        ///
        /// x1, x2, s1, s2, a1
        /// </summary>
        public string[] VariableNames { get; set; }
            = new string[0];

        // =============================================================
        // ORIGINAL VARIABLE INFORMATION
        // =============================================================

        /// <summary>
        /// Names of the original decision variables.
        ///
        /// The current input format does not explicitly provide names,
        /// so these are generated as x1, x2, x3, etc.
        /// </summary>
        public string[] OriginalVariableNames { get; set; }
            = new string[0];

        /// <summary>
        /// Maps each original variable to one or more canonical
        /// variable columns.
        ///
        /// Examples:
        ///
        /// x1 >= 0:
        ///     x1 -> [0]
        ///
        /// x2 <= 0:
        ///     x2 -> [-y2]
        ///
        /// unrestricted x3:
        ///     x3 -> [y3+, y3-]
        /// </summary>
        public List<int[]> OriginalVariableColumnMap { get; set; }
            = new List<int[]>();

        /// <summary>
        /// Original sign restrictions.
        /// </summary>
        public string[] OriginalSignRestrictions { get; set; }
            = new string[0];

        // =============================================================
        // INTEGER / BINARY INFORMATION
        // =============================================================

        /// <summary>
        /// Canonical columns that correspond to integer-restricted
        /// variables.
        /// </summary>
        public List<int> IntegerVariableColumns { get; set; }
            = new List<int>();

        /// <summary>
        /// Canonical columns that correspond to binary variables.
        /// </summary>
        public List<int> BinaryVariableColumns { get; set; }
            = new List<int>();

        /// <summary>
        /// Original variables that were specified as integer.
        /// </summary>
        public List<int> OriginalIntegerVariables { get; set; }
            = new List<int>();

        /// <summary>
        /// Original variables that were specified as binary.
        /// </summary>
        public List<int> OriginalBinaryVariables { get; set; }
            = new List<int>();

        // =============================================================
        // SPECIAL VARIABLE INFORMATION
        // =============================================================

        /// <summary>
        /// Canonical columns containing slack variables.
        /// </summary>
        public List<int> SlackVariableColumns { get; set; }
            = new List<int>();

        /// <summary>
        /// Canonical columns containing surplus variables.
        /// </summary>
        public List<int> SurplusVariableColumns { get; set; }
            = new List<int>();

        /// <summary>
        /// Canonical columns containing artificial variables.
        /// </summary>
        public List<int> ArtificialVariableColumns { get; set; }
            = new List<int>();

        // =============================================================
        // CONSTRAINT INFORMATION
        // =============================================================

        /// <summary>
        /// Names of the canonical constraints.
        ///
        /// Example:
        ///
        /// C1
        /// C2
        /// B1
        /// </summary>
        public string[] ConstraintNames { get; set; }
            = new string[0];

        /// <summary>
        /// Original constraint operators.
        /// </summary>
        public string[] OriginalConstraintOperators { get; set; }
            = new string[0];

        /// <summary>
        /// Indicates whether a constraint was generated internally.
        ///
        /// For example, a binary variable x1 generates:
        ///
        /// x1 <= 1
        /// </summary>
        public bool[] GeneratedConstraints { get; set; }
            = new bool[0];

        // =============================================================
        // BASIS INFORMATION
        // =============================================================

        /// <summary>
        /// Initial basis column for each canonical constraint.
        ///
        /// -1 means that no natural starting basic variable exists and
        /// an artificial variable / Phase 1 is required.
        /// </summary>
        public int[] InitialBasisColumns { get; set; }
            = new int[0];

        /// <summary>
        /// Indicates that the canonical model requires Phase 1 because
        /// one or more artificial variables are present.
        /// </summary>
        public bool RequiresPhaseOne { get; set; }

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public CanonicalModel()
        {
        }

        /// <summary>
        /// Creates a deep copy of the canonical model.
        ///
        /// This prevents Branch & Bound and other algorithms from
        /// accidentally modifying the canonical representation belonging
        /// to another node or the original solution process.
        /// </summary>
        public CanonicalModel Clone()
        {
            CanonicalModel clone = new CanonicalModel();

            clone.ObjectiveType = ObjectiveType;
            clone.WasMinimization = WasMinimization;

            clone.OriginalVariableCount =
                OriginalVariableCount;

            clone.OriginalConstraintCount =
                OriginalConstraintCount;

            clone.ObjectiveCoefficients =
                (double[])ObjectiveCoefficients.Clone();

            clone.RightHandSide =
                (double[])RightHandSide.Clone();

            clone.VariableNames =
                (string[])VariableNames.Clone();

            clone.OriginalVariableNames =
                (string[])OriginalVariableNames.Clone();

            clone.OriginalSignRestrictions =
                (string[])OriginalSignRestrictions.Clone();

            clone.ConstraintNames =
                (string[])ConstraintNames.Clone();

            clone.OriginalConstraintOperators =
                (string[])OriginalConstraintOperators.Clone();

            clone.GeneratedConstraints =
                (bool[])GeneratedConstraints.Clone();

            clone.InitialBasisColumns =
                (int[])InitialBasisColumns.Clone();

            clone.OriginalVariableColumnMap =
                new List<int[]>();

            foreach (int[] mapping in OriginalVariableColumnMap)
            {
                clone.OriginalVariableColumnMap.Add(
                    (int[])mapping.Clone());
            }

            clone.IntegerVariableColumns =
                new List<int>(
                    IntegerVariableColumns);

            clone.BinaryVariableColumns =
                new List<int>(
                    BinaryVariableColumns);

            clone.OriginalIntegerVariables =
                new List<int>(
                    OriginalIntegerVariables);

            clone.OriginalBinaryVariables =
                new List<int>(
                    OriginalBinaryVariables);

            clone.SlackVariableColumns =
                new List<int>(
                    SlackVariableColumns);

            clone.SurplusVariableColumns =
                new List<int>(
                    SurplusVariableColumns);

            clone.ArtificialVariableColumns =
                new List<int>(
                    ArtificialVariableColumns);

            // Deep-copy matrix.
            int rows =
                ConstraintMatrix.GetLength(0);

            int columns =
                ConstraintMatrix.GetLength(1);

            clone.ConstraintMatrix =
                new double[rows, columns];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    clone.ConstraintMatrix[i, j] =
                        ConstraintMatrix[i, j];
                }
            }

            clone.RequiresPhaseOne =
                RequiresPhaseOne;

            return clone;
        }
    }


    /// <summary>
    /// Converts the original user-supplied ProblemModel into a
    /// canonical internal representation.
    ///
    /// The original ProblemModel is never modified.
    /// </summary>
    public static class CanonicalFormConverter
    {
        private const double Epsilon = 1e-9;

        /// <summary>
        /// Converts an original LP/IP model into canonical form.
        /// </summary>
        public static CanonicalModel Convert(
            ProblemModel rawModel)
        {
            if (rawModel == null)
            {
                throw new ArgumentNullException(
                    nameof(rawModel));
            }

            ValidateRawModel(rawModel);

            CanonicalModel canonical =
                new CanonicalModel();

            // ---------------------------------------------------------
            // Basic information
            // ---------------------------------------------------------

            canonical.ObjectiveType =
                rawModel.ObjectiveType.Trim().ToLower();

            canonical.WasMinimization =
                canonical.ObjectiveType == "min";

            canonical.OriginalVariableCount =
                rawModel.ObjectiveCoefficients.Length;

            canonical.OriginalConstraintCount =
                rawModel.Constraints.Count;

            canonical.OriginalVariableNames =
                CreateOriginalVariableNames(
                    canonical.OriginalVariableCount);

            canonical.OriginalSignRestrictions =
                (string[])rawModel.SignRestrictions.Clone();

            canonical.OriginalConstraintOperators =
                GetOriginalConstraintOperators(
                    rawModel);

            // ---------------------------------------------------------
            // We construct the model using lists first because the
            // number of canonical variables and constraints is not
            // necessarily equal to the number in the original model.
            // ---------------------------------------------------------

            List<string> variableNames =
                new List<string>();

            List<double> objective =
                new List<double>();

            List<List<double>> rows =
                new List<List<double>>();

            List<double> rhs =
                new List<double>();

            List<string> constraintNames =
                new List<string>();

            List<bool> generatedConstraints =
                new List<bool>();

            List<int> slackColumns =
                new List<int>();

            List<int> surplusColumns =
                new List<int>();

            List<int> artificialColumns =
                new List<int>();

            List<int> integerColumns =
                new List<int>();

            List<int> binaryColumns =
                new List<int>();

            List<int> initialBasis =
                new List<int>();

            List<int[]> variableColumnMap =
                new List<int[]>();

            // ---------------------------------------------------------
            // STEP 1:
            // Transform original decision variables.
            // ---------------------------------------------------------

            for (int originalIndex = 0;
                 originalIndex < canonical.OriginalVariableCount;
                 originalIndex++)
            {
                string restriction =
                    rawModel.SignRestrictions[originalIndex]
                    .Trim()
                    .ToLower();

                double originalObjectiveCoefficient =
                    rawModel.ObjectiveCoefficients[
                        originalIndex];

                // Convert minimisation to maximisation internally.
                if (canonical.WasMinimization)
                {
                    originalObjectiveCoefficient =
                        -originalObjectiveCoefficient;
                }

                List<int> mappedColumns =
                    new List<int>();

                switch (restriction)
                {
                    // -------------------------------------------------
                    // x >= 0
                    // -------------------------------------------------
                    case "+":
                    case "int":
                    case "bin":
                        {
                            int column =
                                AddVariable(
                                    variableNames,
                                    objective,
                                    $"x{originalIndex + 1}",
                                    originalObjectiveCoefficient);

                            mappedColumns.Add(column);

                            if (restriction == "int" ||
                                restriction == "bin")
                            {
                                integerColumns.Add(column);
                            }

                            if (restriction == "bin")
                            {
                                binaryColumns.Add(column);
                            }

                            break;
                        }

                    // -------------------------------------------------
                    // x <= 0
                    //
                    // Substitute:
                    //
                    // x = -y
                    //
                    // where y >= 0.
                    // -------------------------------------------------
                    case "-":
                        {
                            int column =
                                AddVariable(
                                    variableNames,
                                    objective,
                                    $"x{originalIndex + 1}_neg",
                                    -originalObjectiveCoefficient);

                            mappedColumns.Add(column);

                            break;
                        }

                    // -------------------------------------------------
                    // unrestricted x
                    //
                    // Substitute:
                    //
                    // x = x+ - x-
                    //
                    // where:
                    //
                    // x+ >= 0
                    // x- >= 0
                    // -------------------------------------------------
                    case "urs":
                        {
                            int positiveColumn =
                                AddVariable(
                                    variableNames,
                                    objective,
                                    $"x{originalIndex + 1}_plus",
                                    originalObjectiveCoefficient);

                            int negativeColumn =
                                AddVariable(
                                    variableNames,
                                    objective,
                                    $"x{originalIndex + 1}_minus",
                                    -originalObjectiveCoefficient);

                            mappedColumns.Add(
                                positiveColumn);

                            mappedColumns.Add(
                                negativeColumn);

                            break;
                        }

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported sign restriction " +
                            $"'{restriction}' for x{originalIndex + 1}.");
                }

                variableColumnMap.Add(
                    mappedColumns.ToArray());
            }

            // ---------------------------------------------------------
            // STEP 2:
            // Create transformed constraint rows.
            // ---------------------------------------------------------

            for (int i = 0;
                 i < rawModel.Constraints.Count;
                 i++)
            {
                Constraint originalConstraint =
                    rawModel.Constraints[i];

                string relation =
                    originalConstraint.Operator
                    .Trim();

                double[] transformedCoefficients =
                    TransformConstraintCoefficients(
                        originalConstraint.Coefficients,
                        rawModel.SignRestrictions);

                double transformedRhs =
                    originalConstraint.RightHandSide;

                // -----------------------------------------------------
                // Make RHS non-negative.
                //
                // If:
                //
                // -2x <= -4
                //
                // becomes:
                //
                // 2x >= 4
                // -----------------------------------------------------

                if (transformedRhs < -Epsilon)
                {
                    for (int j = 0;
                         j < transformedCoefficients.Length;
                         j++)
                    {
                        transformedCoefficients[j] *= -1.0;
                    }

                    transformedRhs *= -1.0;

                    relation =
                        ReverseRelation(relation);
                }

                List<double> row =
                    CreateZeroRow(
                        variableNames.Count);

                CopyOriginalCoefficientsIntoRow(
                    row,
                    transformedCoefficients,
                    variableColumnMap);

                // -----------------------------------------------------
                // Add slack/surplus/artificial variables.
                // -----------------------------------------------------

                int basisColumn = -1;

                if (relation == "<=")
                {
                    basisColumn =
                        AddVariable(
                            variableNames,
                            objective,
                            $"s{i + 1}",
                            0.0);

                    EnsureRowSize(
                        row,
                        variableNames.Count);

                    row[basisColumn] = 1.0;

                    slackColumns.Add(
                        basisColumn);
                }
                else if (relation == ">=")
                {
                    // Surplus variable.
                    int surplusColumn =
                        AddVariable(
                            variableNames,
                            objective,
                            $"e{i + 1}",
                            0.0);

                    EnsureRowSize(
                        row,
                        variableNames.Count);

                    row[surplusColumn] = -1.0;

                    surplusColumns.Add(
                        surplusColumn);

                    // Artificial variable is required to establish
                    // an initial basic solution.
                    int artificialColumn =
                        AddVariable(
                            variableNames,
                            objective,
                            $"a{i + 1}",
                            0.0);

                    EnsureRowSize(
                        row,
                        variableNames.Count);

                    row[artificialColumn] = 1.0;

                    artificialColumns.Add(
                        artificialColumn);

                    basisColumn =
                        artificialColumn;
                }
                else if (relation == "=")
                {
                    int artificialColumn =
                        AddVariable(
                            variableNames,
                            objective,
                            $"a{i + 1}",
                            0.0);

                    EnsureRowSize(
                        row,
                        variableNames.Count);

                    row[artificialColumn] = 1.0;

                    artificialColumns.Add(
                        artificialColumn);

                    basisColumn =
                        artificialColumn;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unsupported constraint operator " +
                        $"'{relation}'.");
                }

                EnsureRowSize(
                    row,
                    variableNames.Count);

                rows.Add(row);
                rhs.Add(transformedRhs);

                constraintNames.Add(
                    $"C{i + 1}");

                generatedConstraints.Add(false);
                initialBasis.Add(basisColumn);
            }

            // ---------------------------------------------------------
            // STEP 3:
            // Add x <= 1 constraints for binary variables.
            //
            // Binary variables must satisfy:
            //
            // 0 <= x <= 1
            //
            // Non-negativity is already handled by the '+' form.
            // Therefore we only need to add:
            //
            // x <= 1
            // ---------------------------------------------------------

            for (int i = 0;
                 i < binaryColumns.Count;
                 i++)
            {
                int binaryColumn =
                    binaryColumns[i];

                List<double> binaryRow =
                    CreateZeroRow(
                        variableNames.Count);

                binaryRow[binaryColumn] = 1.0;

                // Add slack variable.
                int slackColumn =
                    AddVariable(
                        variableNames,
                        objective,
                        $"sb{i + 1}",
                        0.0);

                EnsureRowSize(
                    binaryRow,
                    variableNames.Count);

                binaryRow[slackColumn] = 1.0;

                rows.Add(binaryRow);
                rhs.Add(1.0);

                slackColumns.Add(
                    slackColumn);

                constraintNames.Add(
                    $"B{i + 1}");

                generatedConstraints.Add(true);

                initialBasis.Add(
                    slackColumn);
            }

            // ---------------------------------------------------------
            // STEP 4:
            // Expand all rows to the final number of columns.
            // ---------------------------------------------------------

            int rowCount =
                rows.Count;

            int columnCount =
                variableNames.Count;

            double[,] matrix =
                new double[rowCount, columnCount];

            for (int i = 0;
                 i < rowCount;
                 i++)
            {
                EnsureRowSize(
                    rows[i],
                    columnCount);

                for (int j = 0;
                     j < columnCount;
                     j++)
                {
                    matrix[i, j] =
                        rows[i][j];
                }
            }

            // ---------------------------------------------------------
            // STEP 5:
            // Populate canonical model.
            // ---------------------------------------------------------

            canonical.ObjectiveCoefficients =
                objective.ToArray();

            canonical.ConstraintMatrix =
                matrix;

            canonical.RightHandSide =
                rhs.ToArray();

            canonical.VariableNames =
                variableNames.ToArray();

            canonical.ConstraintNames =
                constraintNames.ToArray();

            canonical.GeneratedConstraints =
                generatedConstraints.ToArray();

            canonical.OriginalVariableColumnMap =
                variableColumnMap;

            canonical.SlackVariableColumns =
                slackColumns;

            canonical.SurplusVariableColumns =
                surplusColumns;

            canonical.ArtificialVariableColumns =
                artificialColumns;

            canonical.IntegerVariableColumns =
                integerColumns;

            canonical.BinaryVariableColumns =
                binaryColumns;

            canonical.InitialBasisColumns =
                initialBasis.ToArray();

            canonical.RequiresPhaseOne =
                artificialColumns.Count > 0;

            return canonical;
        }

        // =============================================================
        // VALIDATION
        // =============================================================

        private static void ValidateRawModel(
            ProblemModel model)
        {
            if (string.IsNullOrWhiteSpace(
                model.ObjectiveType))
            {
                throw new InvalidOperationException(
                    "Objective type is missing.");
            }

            string objectiveType =
                model.ObjectiveType.Trim().ToLower();

            if (objectiveType != "max" &&
                objectiveType != "min")
            {
                throw new InvalidOperationException(
                    "Objective type must be 'max' or 'min'.");
            }

            if (model.ObjectiveCoefficients == null ||
                model.ObjectiveCoefficients.Length == 0)
            {
                throw new InvalidOperationException(
                    "The objective function must contain at least " +
                    "one decision variable.");
            }

            if (model.Constraints == null ||
                model.Constraints.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one constraint is required.");
            }

            int variableCount =
                model.ObjectiveCoefficients.Length;

            if (model.SignRestrictions == null ||
                model.SignRestrictions.Length != variableCount)
            {
                throw new InvalidOperationException(
                    "The number of sign restrictions must match " +
                    "the number of decision variables.");
            }

            for (int i = 0;
                 i < model.SignRestrictions.Length;
                 i++)
            {
                string restriction =
                    model.SignRestrictions[i]
                    .Trim()
                    .ToLower();

                if (restriction != "+" &&
                    restriction != "-" &&
                    restriction != "urs" &&
                    restriction != "int" &&
                    restriction != "bin")
                {
                    throw new InvalidOperationException(
                        $"Invalid sign restriction " +
                        $"'{model.SignRestrictions[i]}' " +
                        $"for x{i + 1}.");
                }
            }

            for (int i = 0;
                 i < model.Constraints.Count;
                 i++)
            {
                Constraint constraint =
                    model.Constraints[i];

                if (constraint.Coefficients == null ||
                    constraint.Coefficients.Length !=
                    variableCount)
                {
                    throw new InvalidOperationException(
                        $"Constraint {i + 1} must contain exactly " +
                        $"{variableCount} coefficients.");
                }

                if (constraint.Operator != "<=" &&
                    constraint.Operator != ">=" &&
                    constraint.Operator != "=")
                {
                    throw new InvalidOperationException(
                        $"Constraint {i + 1} has invalid operator " +
                        $"'{constraint.Operator}'.");
                }
            }
        }

        // =============================================================
        // VARIABLE CREATION
        // =============================================================

        private static int AddVariable(
            List<string> variableNames,
            List<double> objective,
            string name,
            double objectiveCoefficient)
        {
            int column =
                variableNames.Count;

            variableNames.Add(name);
            objective.Add(objectiveCoefficient);

            return column;
        }

        // =============================================================
        // ORIGINAL VARIABLE NAMES
        // =============================================================

        private static string[] CreateOriginalVariableNames(
            int count)
        {
            string[] names =
                new string[count];

            for (int i = 0; i < count; i++)
            {
                names[i] =
                    $"x{i + 1}";
            }

            return names;
        }

        // =============================================================
        // CONSTRAINT OPERATORS
        // =============================================================

        private static string[] GetOriginalConstraintOperators(
            ProblemModel model)
        {
            string[] result =
                new string[model.Constraints.Count];

            for (int i = 0;
                 i < model.Constraints.Count;
                 i++)
            {
                result[i] =
                    model.Constraints[i].Operator;
            }

            return result;
        }

        private static string ReverseRelation(
            string relation)
        {
            switch (relation)
            {
                case "<=":
                    return ">=";

                case ">=":
                    return "<=";

                case "=":
                    return "=";

                default:
                    throw new InvalidOperationException(
                        $"Cannot reverse relation '{relation}'.");
            }
        }

        // =============================================================
        // SIGN-RESTRICTION TRANSFORMATION
        // =============================================================

        /// <summary>
        /// Transforms coefficients according to the sign restriction
        /// of each original variable.
        ///
        /// For:
        ///
        /// x >= 0:
        ///     coefficient remains c
        ///
        /// x <= 0:
        ///     x = -y
        ///     coefficient becomes -c
        ///
        /// unrestricted:
        ///     x = x+ - x-
        ///     coefficient becomes:
        ///     c for x+
        ///     -c for x-
        /// </summary>
        private static double[] TransformConstraintCoefficients(
            double[] coefficients,
            string[] restrictions)
        {
            int canonicalVariableCount = 0;

            for (int i = 0;
                 i < restrictions.Length;
                 i++)
            {
                string restriction =
                    restrictions[i]
                    .Trim()
                    .ToLower();

                if (restriction == "urs")
                {
                    canonicalVariableCount += 2;
                }
                else
                {
                    canonicalVariableCount++;
                }
            }

            double[] result =
                new double[canonicalVariableCount];

            int position = 0;

            for (int i = 0;
                 i < coefficients.Length;
                 i++)
            {
                string restriction =
                    restrictions[i]
                    .Trim()
                    .ToLower();

                double coefficient =
                    coefficients[i];

                if (restriction == "+")
                {
                    result[position] =
                        coefficient;

                    position++;
                }
                else if (restriction == "int" ||
                         restriction == "bin")
                {
                    result[position] =
                        coefficient;

                    position++;
                }
                else if (restriction == "-")
                {
                    result[position] =
                        -coefficient;

                    position++;
                }
                else if (restriction == "urs")
                {
                    // x = x+ - x-
                    result[position] =
                        coefficient;

                    result[position + 1] =
                        -coefficient;

                    position += 2;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unsupported sign restriction " +
                        $"'{restriction}'.");
                }
            }

            return result;
        }

        // =============================================================
        // ROW OPERATIONS
        // =============================================================

        private static List<double> CreateZeroRow(
            int size)
        {
            List<double> row =
                new List<double>();

            for (int i = 0; i < size; i++)
            {
                row.Add(0.0);
            }

            return row;
        }

        private static void EnsureRowSize(
            List<double> row,
            int size)
        {
            while (row.Count < size)
            {
                row.Add(0.0);
            }
        }

        /// <summary>
        /// Copies transformed original-variable coefficients into
        /// their corresponding canonical columns.
        /// </summary>
        private static void CopyOriginalCoefficientsIntoRow(
            List<double> row,
            double[] transformedCoefficients,
            List<int[]> variableColumnMap)
        {
            int transformedPosition = 0;

            for (int originalVariable = 0;
                 originalVariable < variableColumnMap.Count;
                 originalVariable++)
            {
                int[] mappedColumns =
                    variableColumnMap[originalVariable];

                for (int k = 0;
                     k < mappedColumns.Length;
                     k++)
                {
                    int actualColumn =
                        mappedColumns[k];

                    EnsureRowSize(
                        row,
                        actualColumn + 1);

                    row[actualColumn] =
                        transformedCoefficients[
                            transformedPosition];

                    transformedPosition++;
                }
            }
        }
    }
}