using System.Collections.Generic;
using System.IO;
using System.Text;
using LPR381_Project.Algorithms;
using LPR381_Project.Models;

namespace LPR381_Project.Parsers
{
    /// <summary>
    /// Writes solver results to a plain-text output file.
    ///
    /// Kept deliberately separate from Program.cs (console
    /// presentation) so the same formatting logic could later be
    /// reused by a GUI or a different front end without depending
    /// on Console.
    ///
    /// Per the project requirements, all decimal values are rounded
    /// to three decimal places in the exported file.
    /// </summary>
    public static class OutputExporter
    {
        // =============================================================
        // PRIMAL SIMPLEX EXPORT
        // =============================================================

        public static void ExportPrimalSimplex(
            string filePath,
            ProblemModel model,
            CanonicalModel canonical,
            SimplexResult result)
        {
            using StreamWriter writer =
                new StreamWriter(filePath, false);

            writer.WriteLine("========================================");
            writer.WriteLine("LPR381 OUTPUT - PRIMAL SIMPLEX");
            writer.WriteLine("========================================");
            writer.WriteLine();

            WriteCanonicalForm(writer, canonical);

            writer.WriteLine();
            writer.WriteLine("----------------------------------------");
            writer.WriteLine("ITERATIONS");
            writer.WriteLine("----------------------------------------");

            WriteIterations(
                writer,
                result.Iterations,
                canonical.VariableNames);

            writer.WriteLine();
            writer.WriteLine("----------------------------------------");
            writer.WriteLine("FINAL RESULT");
            writer.WriteLine("----------------------------------------");

            writer.WriteLine($"Status: {result.Status}");
            writer.WriteLine($"Message: {result.Message}");
            writer.WriteLine($"Pivot count: {result.PivotCount}");
            writer.WriteLine($"Objective value: {result.ObjectiveValue:F3}");

            writer.WriteLine();
            writer.WriteLine("Variable values:");

            WriteVariableValues(
                writer,
                result.VariableNames,
                result.VariableValues);
        }

        // =============================================================
        // BRANCH & BOUND EXPORT
        // =============================================================

        public static void ExportBranchAndBound(
            string filePath,
            ProblemModel model,
            BranchAndBoundResult result)
        {
            using StreamWriter writer =
                new StreamWriter(filePath, false);

            writer.WriteLine("========================================");
            writer.WriteLine("LPR381 OUTPUT - BRANCH & BOUND SIMPLEX");
            writer.WriteLine("========================================");
            writer.WriteLine();

            writer.WriteLine(
                $"Total sub-problems (nodes) generated: " +
                $"{result.Nodes.Count}");

            foreach (BranchAndBoundNode node in result.Nodes)
            {
                writer.WriteLine();
                writer.WriteLine("========================================");

                writer.WriteLine(
                    $"NODE {node.NodeId} " +
                    $"(parent {node.ParentNodeId}, depth {node.Depth})");

                writer.WriteLine("========================================");

                if (node.CanonicalForm == null ||
                    node.RelaxationResult == null)
                {
                    writer.WriteLine(
                        "This node was not processed " +
                        "(node limit reached before it was explored).");

                    continue;
                }

                WriteCanonicalForm(
                    writer,
                    node.CanonicalForm);

                writer.WriteLine();
                writer.WriteLine("Iterations:");

                WriteIterations(
                    writer,
                    node.RelaxationResult.Iterations,
                    node.CanonicalForm.VariableNames);

                writer.WriteLine();

                writer.WriteLine(
                    $"Relaxation status: {node.RelaxationResult.Status}, " +
                    $"objective: {node.RelaxationResult.ObjectiveValue:F3}");

                if (node.IsFathomed)
                {
                    writer.WriteLine(
                        $"FATHOMED - Reason: {node.FathomReason}");
                }
                else if (node.BranchingVariableIndex >= 0 &&
                         node.Children.Count == 2)
                {
                    writer.WriteLine(
                        $"Branching on x{node.BranchingVariableIndex + 1} " +
                        $"= {node.BranchingVariableValue:F3} " +
                        $"-> child nodes {node.Children[0].NodeId} (<=) " +
                        $"and {node.Children[1].NodeId} (>=)");
                }
            }

            writer.WriteLine();
            writer.WriteLine("----------------------------------------");
            writer.WriteLine("BEST CANDIDATE");
            writer.WriteLine("----------------------------------------");

            if (result.HasSolution)
            {
                writer.WriteLine(
                    $"Objective value: {result.BestObjectiveValue:F3}");

                writer.WriteLine();
                writer.WriteLine("Variable values:");

                WriteVariableValues(
                    writer,
                    result.VariableNames,
                    result.BestVariableValues);
            }
            else
            {
                writer.WriteLine(result.Message);
            }
        }

        // =============================================================
        // KNAPSACK BRANCH & BOUND EXPORT
        // =============================================================
        public static void ExportBranchAndBoundKnapsack(
            string filePath,
            ProblemModel model,
            KnapsackBranchAndBoundResult result)
        {
            using StreamWriter writer =
                new StreamWriter(filePath, false);

            writer.WriteLine("========================================");
            writer.WriteLine("LPR381 OUTPUT - BRANCH & BOUND KNAPSACK");
            writer.WriteLine("========================================");
            writer.WriteLine();

            writer.WriteLine(
                $"Total sub-problems (nodes) generated: {result.Nodes.Count}");

            writer.WriteLine();
            writer.WriteLine("Items sorted by value/weight ratio (highest first):");

            for (int i = 0; i < result.SortedItemOriginalIndex.Length; i++)
            {
                int originalIndex = result.SortedItemOriginalIndex[i];

                string name =
                    result.VariableNames != null &&
                    originalIndex < result.VariableNames.Length
                        ? result.VariableNames[originalIndex]
                        : $"x{originalIndex + 1}";

                writer.WriteLine(
                    $"  {name} -> ratio {result.SortedItemRatio[i]:F3}");
            }

            foreach (KnapsackBranchAndBoundNode node in result.Nodes)
            {
                writer.WriteLine();
                writer.WriteLine("========================================");

                writer.WriteLine(
                    $"NODE {node.NodeId} (parent {node.ParentNodeId}, " +
                    $"level {node.Level})");

                writer.WriteLine("========================================");

                writer.WriteLine(
                    DescribeKnapsackDecisions(
                        node.Decisions,
                        result.SortedItemOriginalIndex,
                        result.VariableNames));

                writer.WriteLine($"Total weight: {node.TotalWeight:F3}");
                writer.WriteLine($"Total value: {node.TotalValue:F3}");
                writer.WriteLine($"Bound: {node.Bound:F3}");

                if (node.IsFathomed)
                {
                    writer.WriteLine($"FATHOMED - Reason: {node.FathomReason}");
                }
                else if (node.Children.Count == 2 &&
                         node.Level < result.SortedItemOriginalIndex.Length)
                {
                    int nextItemOriginalIndex =
                        result.SortedItemOriginalIndex[node.Level];

                    string nextItemName =
                        result.VariableNames != null &&
                        nextItemOriginalIndex < result.VariableNames.Length
                            ? result.VariableNames[nextItemOriginalIndex]
                            : $"x{nextItemOriginalIndex + 1}";

                    writer.WriteLine(
                        $"Branching on {nextItemName} -> child nodes " +
                        $"{node.Children[0].NodeId} (include) and " +
                        $"{node.Children[1].NodeId} (exclude)");
                }
            }

            writer.WriteLine();
            writer.WriteLine("----------------------------------------");
            writer.WriteLine("BEST CANDIDATE");
            writer.WriteLine("----------------------------------------");

            if (result.HasSolution)
            {
                writer.WriteLine($"Objective value: {result.BestObjectiveValue:F3}");

                writer.WriteLine();
                writer.WriteLine("Variable values:");

                WriteVariableValues(
                    writer,
                    result.VariableNames,
                    result.BestVariableValues);
            }
            else
            {
                writer.WriteLine(result.Message);
            }
        }

        private static string DescribeKnapsackDecisions(
            int[] decisions,
            int[] sortedItemOriginalIndex,
            string[] variableNames)
        {
            List<string> parts = new List<string>();

            for (int i = 0; i < decisions.Length; i++)
            {
                string name =
                    variableNames != null &&
                    sortedItemOriginalIndex[i] < variableNames.Length
                        ? variableNames[sortedItemOriginalIndex[i]]
                        : $"x{sortedItemOriginalIndex[i] + 1}";

                string state =
                    decisions[i] switch
                    {
                        1 => "in",
                        0 => "out",
                        _ => "?"
                    };

                parts.Add($"{name}={state}");
            }

            return "Decisions: " + string.Join(", ", parts);
        }


        // =============================================================
        // SHARED FORMATTING HELPERS
        // =============================================================

        private static void WriteCanonicalForm(
            StreamWriter writer,
            CanonicalModel canonical)
        {
            writer.WriteLine("----------------------------------------");
            writer.WriteLine("CANONICAL FORM");
            writer.WriteLine("----------------------------------------");

            int constraintCount =
                canonical.ConstraintMatrix.GetLength(0);

            int variableCount =
                canonical.ConstraintMatrix.GetLength(1);

            double[,] display =
                new double[constraintCount + 1, variableCount + 1];

            for (int i = 0; i < constraintCount; i++)
            {
                for (int j = 0; j < variableCount; j++)
                {
                    display[i, j] =
                        canonical.ConstraintMatrix[i, j];
                }

                display[i, variableCount] =
                    canonical.RightHandSide[i];
            }

            for (int j = 0; j < variableCount; j++)
            {
                display[constraintCount, j] =
                    canonical.ObjectiveCoefficients[j];
            }

            WriteTableau(
                writer,
                display,
                canonical.VariableNames,
                "Obj");
        }

        private static void WriteIterations(
            StreamWriter writer,
            List<SimplexIteration> iterations,
            string[] variableNames)
        {
            if (iterations == null || iterations.Count == 0)
            {
                writer.WriteLine("No iterations were recorded.");
                return;
            }

            foreach (SimplexIteration iteration in iterations)
            {
                writer.WriteLine();

                writer.WriteLine(
                    $"Iteration: {iteration.IterationNumber}" +
                    (iteration.IsFinal ? "  (final tableau)" : string.Empty));

                if (!string.IsNullOrWhiteSpace(iteration.EnteringVariable))
                {
                    writer.WriteLine(
                        $"Entering: {iteration.EnteringVariable}");
                }

                if (!string.IsNullOrWhiteSpace(iteration.LeavingVariable))
                {
                    writer.WriteLine(
                        $"Leaving: {iteration.LeavingVariable}");
                }

                WriteTableau(
                    writer,
                    iteration.Tableau,
                    variableNames,
                    "Z");
            }
        }

        private static void WriteVariableValues(
            StreamWriter writer,
            string[] names,
            double[] values)
        {
            if (values == null)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string name =
                    names != null && i < names.Length
                        ? names[i]
                        : $"x{i + 1}";

                writer.WriteLine($"{name} = {values[i]:F3}");
            }
        }

        private static void WriteTableau(
            StreamWriter writer,
            double[,] tableau,
            string[] variableNames,
            string lastRowLabel)
        {
            if (tableau == null)
            {
                writer.WriteLine("No tableau available.");
                return;
            }

            int rows = tableau.GetLength(0);
            int columns = tableau.GetLength(1);

            StringBuilder header =
                new StringBuilder();

            header.Append("Row".PadRight(8));

            for (int j = 0; j < columns - 1; j++)
            {
                string name =
                    variableNames != null && j < variableNames.Length
                        ? variableNames[j]
                        : $"V{j + 1}";

                header.Append(name.PadLeft(10));
            }

            header.Append("RHS".PadLeft(10));

            writer.WriteLine();
            writer.WriteLine(header.ToString());

            for (int i = 0; i < rows; i++)
            {
                string rowName =
                    i == rows - 1
                        ? lastRowLabel
                        : $"C{i + 1}";

                StringBuilder line =
                    new StringBuilder();

                line.Append(rowName.PadRight(8));

                for (int j = 0; j < columns; j++)
                {
                    line.Append(
                        tableau[i, j].ToString("F3").PadLeft(10));
                }

                writer.WriteLine(line.ToString());
            }
        }
    }
}