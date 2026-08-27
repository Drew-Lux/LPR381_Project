using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Project.Models;

namespace LPR381_Project.Algorithms
{
    /// <summary>
    /// Dedicated Branch & Bound solver for 0/1 knapsack models:
    ///
    ///     max  c1 x1 + c2 x2 + ... + cn xn
    ///     s.t. a1 x1 + a2 x2 + ... + an xn &lt;= b
    ///          xi in {0, 1}
    ///
    /// Unlike the general Branch & Bound Simplex solver, this
    /// algorithm never calls the simplex solver. Bounds are computed
    /// directly using the classic fractional-relaxation (Dantzig)
    /// upper bound: sort items by value/weight ratio, greedily fill
    /// remaining capacity, and add one fractional item at the break
    /// point. This is far cheaper per node than solving an LP and is
    /// the standard technique for 0/1 knapsack Branch & Bound.
    ///
    /// The solver performs calculations only. It does not write
    /// directly to the console.
    /// </summary>
    public static class BranchAndBoundKnapsackSolver
    {
        private const double Epsilon = 1e-9;
        private const int DefaultNodeLimit = 20000;

        private class KnapsackItem
        {
            public int OriginalIndex;
            public double Value;
            public double Weight;

            public double Ratio =>
                Weight > Epsilon
                    ? Value / Weight
                    : double.PositiveInfinity;
        }

        // =============================================================
        // PUBLIC ENTRY POINTS
        // =============================================================

        public static KnapsackBranchAndBoundResult Solve(
            ProblemModel model)
        {
            return Solve(model, DefaultNodeLimit);
        }

        public static KnapsackBranchAndBoundResult Solve(
            ProblemModel model,
            int nodeLimit)
        {
            KnapsackBranchAndBoundResult result =
                new KnapsackBranchAndBoundResult();

            if (model == null)
            {
                result.Message =
                    "Programming model is null.";

                return result;
            }

            // ---------------------------------------------------------
            // This algorithm only applies to models that are actually
            // shaped like a 0/1 knapsack problem. Reject anything
            // else with a clear explanation rather than guessing.
            // ---------------------------------------------------------

            string? validationError =
                ValidateKnapsackShape(model);

            if (validationError != null)
            {
                result.Message =
                    validationError;

                return result;
            }

            int itemCount =
                model.ObjectiveCoefficients.Length;

            double capacity =
                model.Constraints[0].RightHandSide;

            List<KnapsackItem> items =
                new List<KnapsackItem>();

            for (int i = 0; i < itemCount; i++)
            {
                items.Add(
                    new KnapsackItem
                    {
                        OriginalIndex = i,
                        Value = model.ObjectiveCoefficients[i],
                        Weight = model.Constraints[0].Coefficients[i]
                    });
            }

            // ---------------------------------------------------------
            // Sort items by descending efficiency (value / weight).
            // This ordering drives both branching and bounding.
            // ---------------------------------------------------------

            List<KnapsackItem> sortedItems =
                items
                    .OrderByDescending(item => item.Ratio)
                    .ToList();

            result.VariableNames =
                BuildVariableNames(itemCount);

            result.SortedItemOriginalIndex =
                sortedItems
                    .Select(item => item.OriginalIndex)
                    .ToArray();

            result.SortedItemRatio =
                sortedItems
                    .Select(item => item.Ratio)
                    .ToArray();

            // ---------------------------------------------------------
            // Node bookkeeping - identical DFS/backtracking pattern to
            // the general Branch & Bound Simplex solver, for
            // consistency between the two algorithms.
            // ---------------------------------------------------------

            Stack<KnapsackBranchAndBoundNode> pending =
                new Stack<KnapsackBranchAndBoundNode>();

            int nodeIdCounter = 0;

            KnapsackBranchAndBoundNode root =
                new KnapsackBranchAndBoundNode
                {
                    NodeId = nodeIdCounter++,
                    ParentNodeId = -1,
                    Level = 0,
                    Decisions = CreateUndecidedArray(itemCount),
                    TotalWeight = 0.0,
                    TotalValue = 0.0
                };

            result.RootNode =
                root;

            result.Nodes.Add(root);

            pending.Push(root);

            double bestValue =
                double.NegativeInfinity;

            KnapsackBranchAndBoundNode? bestNode =
                null;

            // ---------------------------------------------------------
            // Main search loop.
            // ---------------------------------------------------------

            while (pending.Count > 0)
            {
                if (result.Nodes.Count > nodeLimit)
                {
                    result.Message =
                        "Branch & Bound Knapsack node limit reached " +
                        "before the search tree was fully explored.";

                    break;
                }

                KnapsackBranchAndBoundNode node =
                    pending.Pop();

                // -------------------------------------------------
                // Fathom: over capacity.
                //
                // Only an "include" branch can push weight past
                // capacity, since "exclude" never adds weight.
                // -------------------------------------------------

                if (node.TotalWeight >
                    capacity + Epsilon)
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.Infeasible;

                    continue;
                }

                node.Bound =
                    ComputeBound(
                        sortedItems,
                        node.Level,
                        node.TotalWeight,
                        node.TotalValue,
                        capacity);

                // -------------------------------------------------
                // Fathom: leaf node - every item has a decision.
                // -------------------------------------------------

                if (node.Level == itemCount)
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.IntegerSolution;

                    if (node.TotalValue >
                        bestValue + Epsilon)
                    {
                        bestValue =
                            node.TotalValue;

                        bestNode =
                            node;
                    }

                    continue;
                }

                // -------------------------------------------------
                // Fathom: bound cannot improve on the incumbent.
                // -------------------------------------------------

                if (bestNode != null &&
                    node.Bound <= bestValue + Epsilon)
                {
                    node.IsFathomed = true;

                    node.FathomReason =
                        FathomReason.BoundNotBetter;

                    continue;
                }

                // -------------------------------------------------
                // Branch on the next item in efficiency order.
                // -------------------------------------------------

                KnapsackItem nextItem =
                    sortedItems[node.Level];

                int[] includeDecisions =
                    (int[])node.Decisions.Clone();

                includeDecisions[node.Level] = 1;

                KnapsackBranchAndBoundNode includeChild =
                    new KnapsackBranchAndBoundNode
                    {
                        NodeId = nodeIdCounter++,
                        ParentNodeId = node.NodeId,
                        Level = node.Level + 1,
                        Decisions = includeDecisions,
                        TotalWeight =
                            node.TotalWeight + nextItem.Weight,
                        TotalValue =
                            node.TotalValue + nextItem.Value
                    };

                int[] excludeDecisions =
                    (int[])node.Decisions.Clone();

                excludeDecisions[node.Level] = 0;

                KnapsackBranchAndBoundNode excludeChild =
                    new KnapsackBranchAndBoundNode
                    {
                        NodeId = nodeIdCounter++,
                        ParentNodeId = node.NodeId,
                        Level = node.Level + 1,
                        Decisions = excludeDecisions,
                        TotalWeight = node.TotalWeight,
                        TotalValue = node.TotalValue
                    };

                node.Children.Add(includeChild);
                node.Children.Add(excludeChild);

                result.Nodes.Add(includeChild);
                result.Nodes.Add(excludeChild);

                // Push exclude first so include (LIFO) is explored
                // first - trying the greedy/high-ratio branch first
                // tends to find a strong incumbent quickly, which
                // improves pruning of everything explored after it.
                pending.Push(excludeChild);
                pending.Push(includeChild);
            }

            // ---------------------------------------------------------
            // Build the final result.
            // ---------------------------------------------------------

            if (bestNode != null)
            {
                result.HasSolution = true;

                result.BestObjectiveValue =
                    bestValue;

                double[] bestValues =
                    new double[itemCount];

                for (int sortedPosition = 0;
                     sortedPosition < itemCount;
                     sortedPosition++)
                {
                    int originalIndex =
                        sortedItems[sortedPosition].OriginalIndex;

                    bestValues[originalIndex] =
                        bestNode.Decisions[sortedPosition] == 1
                            ? 1.0
                            : 0.0;
                }

                result.BestVariableValues =
                    bestValues;

                if (string.IsNullOrEmpty(result.Message))
                {
                    result.Message =
                        "Optimal knapsack solution found by " +
                        "Branch & Bound.";
                }
            }
            else
            {
                result.HasSolution = false;

                if (string.IsNullOrEmpty(result.Message))
                {
                    result.Message =
                        "No feasible knapsack solution was found.";
                }
            }

            return result;
        }

        // =============================================================
        // FRACTIONAL-RELAXATION (DANTZIG) UPPER BOUND
        // =============================================================

        /// <summary>
        /// Computes the best value achievable from a node onward by
        /// greedily filling the remaining capacity in efficiency
        /// order, allowing the single item that doesn't fully fit to
        /// be taken fractionally. This is a valid upper bound because
        /// the true 0/1 optimum can never exceed the fractional
        /// relaxation's optimum.
        /// </summary>
        private static double ComputeBound(
            List<KnapsackItem> sortedItems,
            int level,
            double currentWeight,
            double currentValue,
            double capacity)
        {
            double bound =
                currentValue;

            double remainingCapacity =
                capacity - currentWeight;

            int i = level;

            while (i < sortedItems.Count &&
                   sortedItems[i].Weight <=
                   remainingCapacity + Epsilon)
            {
                remainingCapacity -=
                    sortedItems[i].Weight;

                bound +=
                    sortedItems[i].Value;

                i++;
            }

            if (i < sortedItems.Count &&
                remainingCapacity > Epsilon)
            {
                bound +=
                    sortedItems[i].Value *
                    (remainingCapacity / sortedItems[i].Weight);
            }

            return bound;
        }

        // =============================================================
        // HELPERS
        // =============================================================

        private static int[] CreateUndecidedArray(
            int count)
        {
            int[] decisions =
                new int[count];

            for (int i = 0; i < count; i++)
            {
                decisions[i] = -1;
            }

            return decisions;
        }

        private static string[] BuildVariableNames(
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
        // VALIDATION
        // =============================================================

        /// <summary>
        /// Confirms the model is a well-formed 0/1 knapsack problem.
        /// Returns null when valid, or a human-readable explanation
        /// of what disqualified it.
        /// </summary>
        private static string? ValidateKnapsackShape(
            ProblemModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ObjectiveType) ||
                model.ObjectiveType.Trim().ToLower() != "max")
            {
                return "Branch & Bound Knapsack requires a " +
                       "maximisation objective.";
            }

            if (model.Constraints == null ||
                model.Constraints.Count != 1)
            {
                return "Branch & Bound Knapsack requires exactly " +
                       "one constraint (the capacity constraint).";
            }

            Constraint constraint =
                model.Constraints[0];

            if (constraint.Operator != "<=")
            {
                return "Branch & Bound Knapsack requires a single " +
                       "'<=' capacity constraint.";
            }

            if (constraint.RightHandSide <= 0)
            {
                return "The knapsack capacity (constraint RHS) " +
                       "must be positive.";
            }

            int variableCount =
                model.ObjectiveCoefficients.Length;

            if (model.SignRestrictions == null ||
                model.SignRestrictions.Length != variableCount)
            {
                return "Sign restrictions do not match the number " +
                       "of decision variables.";
            }

            if (constraint.Coefficients == null ||
                constraint.Coefficients.Length != variableCount)
            {
                return "The constraint does not contain a " +
                       "coefficient for every decision variable.";
            }

            for (int i = 0; i < variableCount; i++)
            {
                string restriction =
                    model.SignRestrictions[i]
                    .Trim()
                    .ToLower();

                if (restriction != "bin")
                {
                    return "Branch & Bound Knapsack currently " +
                           "supports binary ('bin') variables only " +
                           $"- x{i + 1} is '{restriction}'. Use " +
                           "Branch & Bound Simplex for general " +
                           "integer/mixed models.";
                }

                if (model.ObjectiveCoefficients[i] <= 0)
                {
                    return "Branch & Bound Knapsack requires " +
                           "positive objective coefficients - " +
                           $"x{i + 1} has coefficient " +
                           $"{model.ObjectiveCoefficients[i]}.";
                }

                if (constraint.Coefficients[i] <= 0)
                {
                    return "Branch & Bound Knapsack requires " +
                           "positive constraint coefficients - " +
                           $"x{i + 1} has coefficient " +
                           $"{constraint.Coefficients[i]}.";
                }
            }

            return null;
        }
    }
}