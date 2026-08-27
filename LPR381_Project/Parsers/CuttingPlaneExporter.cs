using LPR381_Project.Algorithms;
using LPR381_Project.Models;
using System.Collections.Generic;
using System.IO;

namespace LPR381_Project.Parsers
{
    /// <summary>
    /// Exports the Cutting Plane calculations required by the
    /// project: the initial canonical form, every Gomory cut that was
    /// generated, and every Product Form / Price Out iteration
    /// (initial Revised Simplex relaxation plus every Dual Simplex
    /// re-optimisation), rounded to three decimal places.
    /// </summary>
    public static class CuttingPlaneExporter
    {
        public static void Export(
            string filePath,
            CanonicalModel canonical,
            CuttingPlaneResult result)
        {
            using StreamWriter writer = new StreamWriter(filePath, false);

            writer.WriteLine("========================================");
            writer.WriteLine("LPR381 OUTPUT - CUTTING PLANE ALGORITHM");
            writer.WriteLine("========================================");
            writer.WriteLine();

            writer.WriteLine("INITIAL CANONICAL FORM (LP RELAXATION)");
            writer.WriteLine("----------------------------------------");
            WriteCanonical(writer, canonical);

            writer.WriteLine();
            writer.WriteLine("PRODUCT FORM / PRICE OUT ITERATIONS");
            writer.WriteLine("----------------------------------------");

            foreach (RevisedSimplexIteration it in result.Iterations)
            {
                writer.WriteLine();

                writer.WriteLine(
                    $"Iteration {it.IterationNumber} - {it.Stage}" +
                    (it.IsFinal ? " (FINAL)" : string.Empty));

                writer.WriteLine($"Basis: {string.Join(", ", it.BasisNames)}");

                WriteVector(writer, "C_B", it.BasicCosts);
                WriteMatrix(writer, "B", it.BasisMatrix);
                WriteMatrix(writer, "B^-1", it.BasisInverse);
                WriteVector(writer, "Prices (C_B B^-1)", it.Prices);
                WriteVector(writer, "Reduced Costs (C_j - y A_j)", it.ReducedCosts);

                if (it.EnteringColumn >= 0)
                {
                    writer.WriteLine($"Entering variable: {it.EnteringVariable}");
                    WriteVector(writer, "Direction / Pivot row (B^-1 A_j)", it.Direction);
                    WriteVector(writer, "Ratio Test", it.Ratios);

                    if (it.LeavingRow >= 0)
                    {
                        writer.WriteLine(
                            $"Leaving variable: {it.LeavingVariable} " +
                            $"(row {it.LeavingRow + 1})");
                    }

                    writer.WriteLine($"Pivot value: {it.PivotValue:F3}");
                    writer.WriteLine("Product Form - Eta Matrix:");
                    WriteMatrix(writer, "Eta", it.EtaMatrix);
                }

                writer.WriteLine($"Objective value: {it.ObjectiveValue:F3}");
            }

            writer.WriteLine();
            writer.WriteLine("GOMORY CUTS GENERATED");
            writer.WriteLine("----------------------------------------");

            if (result.Cuts.Count == 0)
            {
                writer.WriteLine("No cuts were required.");
            }
            else
            {
                foreach (GomoryCut cut in result.Cuts)
                {
                    writer.WriteLine();
                    writer.WriteLine($"Cut {cut.CutNumber}:");
                    writer.WriteLine($"  Source row     : {cut.SourceRow + 1}");
                    writer.WriteLine($"  Source variable: {cut.SourceVariable} = {cut.SourceValue:F3}");
                    writer.WriteLine($"  Slack variable : {cut.SlackVariableName}");
                    writer.WriteLine($"  Inequality     : {cut.Description}");
                }
            }

            writer.WriteLine();
            writer.WriteLine("FINAL RESULT");
            writer.WriteLine("----------------------------------------");
            writer.WriteLine($"Status: {result.Status}");
            writer.WriteLine($"Message: {result.Message}");
            writer.WriteLine($"Cuts added: {result.Cuts.Count}");
            writer.WriteLine($"Pivot count: {result.PivotCount}");
            writer.WriteLine($"Objective value: {result.ObjectiveValue:F3}");
            writer.WriteLine();
            writer.WriteLine("VARIABLE VALUES");

            for (int i = 0; i < result.VariableValues.Length; i++)
            {
                string name =
                    result.VariableNames != null && i < result.VariableNames.Length
                        ? result.VariableNames[i]
                        : $"x{i + 1}";

                writer.WriteLine($"{name} = {result.VariableValues[i]:F3}");
            }
        }

        // =============================================================
        // SHARED FORMATTING HELPERS
        // =============================================================

        private static void WriteCanonical(StreamWriter writer, CanonicalModel canonical)
        {
            int constraintCount = canonical.ConstraintMatrix.GetLength(0);
            int variableCount = canonical.ConstraintMatrix.GetLength(1);

            double[,] display = new double[constraintCount + 1, variableCount + 1];

            for (int i = 0; i < constraintCount; i++)
            {
                for (int j = 0; j < variableCount; j++)
                {
                    display[i, j] = canonical.ConstraintMatrix[i, j];
                }

                display[i, variableCount] = canonical.RightHandSide[i];
            }

            for (int j = 0; j < variableCount; j++)
            {
                display[constraintCount, j] = canonical.ObjectiveCoefficients[j];
            }

            WriteTableau(writer, display, canonical.VariableNames, "Obj");
        }

        private static void WriteTableau(
            StreamWriter writer,
            double[,] tableau,
            string[] variableNames,
            string lastRowLabel)
        {
            int rows = tableau.GetLength(0);
            int columns = tableau.GetLength(1);

            writer.Write($"{"Row",-8}");

            for (int j = 0; j < columns - 1; j++)
            {
                string name =
                    variableNames != null && j < variableNames.Length
                        ? variableNames[j]
                        : $"V{j + 1}";

                writer.Write($"{name,10}");
            }

            writer.Write($"{"RHS",10}");
            writer.WriteLine();

            for (int i = 0; i < rows; i++)
            {
                string rowName = i == rows - 1 ? lastRowLabel : $"C{i + 1}";

                writer.Write($"{rowName,-8}");

                for (int j = 0; j < columns; j++)
                {
                    writer.Write($"{tableau[i, j],10:F3}");
                }

                writer.WriteLine();
            }
        }

        private static void WriteVector(StreamWriter writer, string label, double[] values)
        {
            if (values == null || values.Length == 0) return;

            writer.Write($"{label}: ");

            foreach (double value in values)
            {
                writer.Write($"{value:F3} ");
            }

            writer.WriteLine();
        }

        private static void WriteMatrix(StreamWriter writer, string label, double[,] matrix)
        {
            writer.WriteLine($"{label}:");

            if (matrix == null || matrix.Length == 0)
            {
                writer.WriteLine("(none)");
                return;
            }

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    writer.Write($"{matrix[i, j],10:F3}");
                }

                writer.WriteLine();
            }
        }
    }
}
