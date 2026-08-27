using System;
using System.Collections.Generic;
using LPR381_Project.Models;

namespace LPR381_Project.Algorithms
{
    /// <summary>
    /// Branch & Bound solver built on top of the Primal Simplex
    /// solver.
    ///
    /// Supports:
    ///
    /// - Any mix of "int" / "bin" restricted variables alongside
    ///   ordinary continuous ("+", "-", "urs") variables.
    /// - Backtracking via an explicit LIFO stack (depth-first search).
    /// - Fathoming by infeasibility, unboundedness, integer-feasible
    ///   solution, and bound-not-better-than-incumbent.
    /// - A full record of every sub-problem (node) generated,
    ///   including its own canonical form and simplex iterations,
    ///   so the caller can display every tableau.
    /// - Best-candidate (incumbent) tracking.
    ///
    /// The solver performs calculations only. It does not write
    /// directly to the console.
    /// </summary>
    public static class BranchAndBoundSolver
    {
        private const int DefaultNodeLimit = 5000;
        private const double Epsilon = 1e-6;

        // =============================================================
        // PUBLIC ENTRY POINTS
        // =============================================================

        /// <summary>
        /// Solves an LP/IP model using Branch & Bound Simplex.
        /// </summary>
        public static BranchAndBoundResult Solve(ProblemModel rootModel)
        {
            return Solve(rootModel, DefaultNodeLimit);
        }

        /// <summary>
        /// Solves an LP/IP model using Branch & Bound Simplex with a
        /// configurable node limit (safety net against runaway trees).
        /// </summary>
        public static BranchAndBoundResult Solve(
            ProblemModel rootModel,
            int nodeLimit)
        {
            BranchAndBoundResult result =
                new BranchAndBoundResult();

            if (rootModel == null)
            {
                result.Message =
                    "Programming model is null.";

                return result;
            }

            bool isMaximization =
                rootModel.ObjectiveType.Trim().ToLower() == "max";

            bool[] isIntegerVariable =
                DetermineIntegerVariables(rootModel);

            string[] originalVariableNames =
                BuildOriginalVariableNames(
                    rootModel.ObjectiveCoefficients.Length);

            // ---------------------------------------------------------
            // Node bookkeeping.
            //
            // A Stack<> gives us depth-first search with natural
            // backtracking: whenever a branch is fully fathomed we
            // simply pop back to the most recently created sibling
            // or ancestor branch.
            // ---------------------------------------------------------

            Stack<BranchAndBoundNode> pending =
                new Stack<BranchAndBoundNode>();

            int nodeIdCounter = 0;

            BranchAndBoundNode root =
                new BranchAndBoundNode
                {
                    NodeId = nodeIdCounter++,
                    ParentNodeId = -1,
                    Depth = 0,
                    Model = rootModel.Clone()
                };

            result.RootNode = root;

            result.Nodes.Add(root);

            pending.Push(root);

            double bestObjective =
                isMaximization
                    ? double.NegativeInfinity
                    : double.PositiveInfinity;

            BranchAndBoundNode bestNode = null;

            double[] bestValues =
                Array.Empty<double>();

            // ---------------------------------------------------------
            // Main search loop.
            // ---------------------------------------------------------

            while (pending.Count > 0)
            {
                if (result.Nodes.Count > nodeLimit)
                {
                    result.Message =
                        "Branch & Bound node limit reached before " +
                        "the search tree was fully explored.";

                    break;
                }

                BranchAndBoundNode node =
                    pending.Pop();

                // -------------------------------------------------
                // Solve this node's LP relaxation.
                // -------------------------------------------------

                CanonicalModel canonical =
                    CanonicalFormConverter.Convert(node.Model);

                node.CanonicalForm =
                    canonical;

                SimplexResult relaxation =
                    SimplexSolver.Solve(canonical);

                node.RelaxationResult =
                    relaxation;

                // -------------------------------------------------
                // Fathom: infeasible / iteration-limited relaxation.
                // -------------------------------------------------

                if (relaxation.Status ==
                        SimplexResult.SolverStatus.Infeasible ||
                    relaxation.Status ==
                        SimplexResult.SolverStatus.IterationLimit)
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.Infeasible;

                    continue;
                }

                // -------------------------------------------------
                // Fathom: unbounded relaxation.
                //
                // If the LP relaxation of a sub-problem is
                // unbounded, the sub-problem contributes no useful
                // bound. Since further branching only adds more
                // constraints (shrinking the feasible region), an
                // unbounded relaxation this deep would be unusual,
                // but we still fathom defensively instead of
                // branching on an unbounded tableau.
                // -------------------------------------------------

                if (relaxation.Status ==
                    SimplexResult.SolverStatus.Unbounded)
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.Unbounded;

                    continue;
                }

                node.Bound =
                    relaxation.ObjectiveValue;

                // -------------------------------------------------
                // Fathom: bound cannot improve on the incumbent.
                // -------------------------------------------------

                if (bestNode != null &&
                    !IsStrictlyBetter(
                        relaxation.ObjectiveValue,
                        bestObjective,
                        isMaximization))
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.BoundNotBetter;

                    continue;
                }

                // -------------------------------------------------
                // Map the relaxation solution back onto the
                // ORIGINAL decision variables so we can test
                // integer feasibility and branch in the original
                // variable space (independent of how many
                // canonical columns each variable expanded into).
                // -------------------------------------------------

                double[] originalValues =
                    ExtractOriginalVariableValues(
                        canonical,
                        relaxation);

                int branchIndex =
                    FindMostFractionalVariable(
                        originalValues,
                        isIntegerVariable);

                // -------------------------------------------------
                // Fathom: integer-feasible solution found.
                // -------------------------------------------------

                if (branchIndex == -1)
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.IntegerSolution;

                    if (bestNode == null ||
                        IsStrictlyBetter(
                            relaxation.ObjectiveValue,
                            bestObjective,
                            isMaximization))
                    {
                        bestObjective =
                            relaxation.ObjectiveValue;

                        bestNode =
                            node;

                        bestValues =
                            originalValues;
                    }

                    continue;
                }

                // -------------------------------------------------
                // Branch: create the two sub-problems.
                //
                //   floor branch: x <= floor(value)
                //   ceil  branch: x >= ceil(value)
                // -------------------------------------------------

                node.BranchingVariableIndex =
                    branchIndex;

                node.BranchingVariableValue =
                    originalValues[branchIndex];

                double fractionalValue =
                    originalValues[branchIndex];

                double floorBound =
                    Math.Floor(fractionalValue);

                double ceilBound =
                    Math.Ceiling(fractionalValue);

                ProblemModel floorModel =
                    node.Model.Clone();

                AddBoundConstraint(
                    floorModel,
                    branchIndex,
                    "<=",
                    floorBound);

                BranchAndBoundNode floorChild =
                    new BranchAndBoundNode
                    {
                        NodeId = nodeIdCounter++,
                        ParentNodeId = node.NodeId,
                        Depth = node.Depth + 1,
                        Model = floorModel
                    };

                ProblemModel ceilModel =
                    node.Model.Clone();

                AddBoundConstraint(
                    ceilModel,
                    branchIndex,
                    ">=",
                    ceilBound);

                BranchAndBoundNode ceilChild =
                    new BranchAndBoundNode
                    {
                        NodeId = nodeIdCounter++,
                        ParentNodeId = node.NodeId,
                        Depth = node.Depth + 1,
                        Model = ceilModel
                    };

                node.Children.Add(floorChild);
                node.Children.Add(ceilChild);

                result.Nodes.Add(floorChild);
                result.Nodes.Add(ceilChild);

                // Push ceiling first so the floor branch (LIFO) is
                // explored first; order is arbitrary but must be
                // consistent for the search/backtracking to make
                // sense when displayed.
                pending.Push(ceilChild);
                pending.Push(floorChild);
            }

            // ---------------------------------------------------------
            // Build the final result.
            // ---------------------------------------------------------

            result.VariableNames =
                originalVariableNames;

            if (bestNode != null)
            {
                result.HasSolution = true;

                result.BestObjectiveValue =
                    bestObjective;

                result.BestVariableValues =
                    bestValues;

                if (string.IsNullOrEmpty(result.Message))
                {
                    result.Message =
                        "Optimal integer solution found by " +
                        "Branch & Bound.";
                }
            }
            else
            {
                result.HasSolution = false;

                if (string.IsNullOrEmpty(result.Message))
                {
                    result.Message =
                        "No integer-feasible solution exists. " +
                        "The model is infeasible over the integer " +
                        "restrictions supplied.";
                }
            }

            return result;
        }

        // =============================================================
        // BOUND COMPARISON
        // =============================================================

        private static bool IsStrictlyBetter(
            double candidate,
            double incumbent,
            bool isMaximization)
        {
            if (isMaximization)
            {
                return candidate > incumbent + Epsilon;
            }

            return candidate < incumbent - Epsilon;
        }

        // =============================================================
        // INTEGER VARIABLE DETECTION
        // =============================================================

        private static bool[] DetermineIntegerVariables(
            ProblemModel model)
        {
            bool[] flags =
                new bool[model.SignRestrictions.Length];

            for (int i = 0; i < flags.Length; i++)
            {
                string restriction =
                    model.SignRestrictions[i]
                    .Trim()
                    .ToLower();

                flags[i] =
                    restriction == "int" ||
                    restriction == "bin";
            }

            return flags;
        }

        private static string[] BuildOriginalVariableNames(
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
        // MAP CANONICAL SOLUTION BACK TO ORIGINAL VARIABLES
        // =============================================================

        /// <summary>
        /// Reverses the substitutions performed by
        /// CanonicalFormConverter so that Branch & Bound always
        /// branches in terms of the ORIGINAL decision variables
        /// supplied by the user, regardless of sign restriction.
        /// </summary>
        private static double[] ExtractOriginalVariableValues(
            CanonicalModel canonical,
            SimplexResult relaxation)
        {
            double[] originalValues =
                new double[canonical.OriginalVariableCount];

            for (int i = 0;
                 i < canonical.OriginalVariableCount;
                 i++)
            {
                string restriction =
                    canonical.OriginalSignRestrictions[i]
                    .Trim()
                    .ToLower();

                int[] mappedColumns =
                    canonical.OriginalVariableColumnMap[i];

                if (restriction == "-")
                {
                    // x = -y
                    originalValues[i] =
                        -relaxation.VariableValues[
                            mappedColumns[0]];
                }
                else if (restriction == "urs")
                {
                    // x = x+ - x-
                    originalValues[i] =
                        relaxation.VariableValues[mappedColumns[0]] -
                        relaxation.VariableValues[mappedColumns[1]];
                }
                else
                {
                    // "+", "int", "bin"
                    originalValues[i] =
                        relaxation.VariableValues[
                            mappedColumns[0]];
                }
            }

            return originalValues;
        }

        // =============================================================
        // BRANCHING VARIABLE SELECTION
        // =============================================================

        /// <summary>
        /// Selects the integer-restricted variable whose fractional
        /// part is closest to 0.5 (the "most fractional" rule).
        /// Returns -1 if every integer-restricted variable already
        /// holds an integer value, meaning the node is integer
        /// feasible.
        /// </summary>
        private static int FindMostFractionalVariable(
            double[] originalValues,
            bool[] isIntegerVariable)
        {
            int bestIndex = -1;

            double bestDistanceFromHalf =
                double.MaxValue;

            for (int i = 0; i < originalValues.Length; i++)
            {
                if (!isIntegerVariable[i])
                {
                    continue;
                }

                double value =
                    originalValues[i];

                double fractionalPart =
                    value - Math.Floor(value);

                double distanceFromNearestInteger =
                    Math.Min(
                        fractionalPart,
                        1.0 - fractionalPart);

                if (distanceFromNearestInteger <= Epsilon)
                {
                    // Already integer (within tolerance).
                    continue;
                }

                double distanceFromHalf =
                    Math.Abs(fractionalPart - 0.5);

                if (distanceFromHalf <
                    bestDistanceFromHalf)
                {
                    bestDistanceFromHalf =
                        distanceFromHalf;

                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        // =============================================================
        // SUB-PROBLEM CONSTRUCTION
        // =============================================================

        /// <summary>
        /// Adds a new bounding constraint on an original decision
        /// variable (e.g. x2 &lt;= 3, or x2 &gt;= 4).
        ///
        /// The constraint is expressed purely in terms of the
        /// original decision variables, so CanonicalFormConverter
        /// transforms it exactly the same way it transforms every
        /// other constraint - no special-casing is required here for
        /// "-", "urs", "int" or "bin" variables.
        /// </summary>
        private static void AddBoundConstraint(
            ProblemModel model,
            int originalVariableIndex,
            string relationalOperator,
            double bound)
        {
            double[] coefficients =
                new double[model.ObjectiveCoefficients.Length];

            coefficients[originalVariableIndex] = 1.0;

            Constraint constraint =
                new Constraint
                {
                    Coefficients = coefficients,
                    Operator = relationalOperator,
                    RightHandSide = bound
                };

            model.AddConstraint(constraint);
        }
    }
}