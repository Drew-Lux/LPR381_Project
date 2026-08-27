using LPR381_Project.Algorithms;
using LPR381_Project.Models;
using System.IO;

namespace LPR381_Project.Parsers
{
    /// <summary>
    /// Exports the Revised Primal Simplex calculations required by the
    /// project: canonical form, basis, B, B^-1, prices, reduced costs,
    /// direction/ratio calculations and Product Form eta matrices.
    /// </summary>
    public static class RevisedSimplexExporter
    {
        public static void Export(string filePath, CanonicalModel canonical, RevisedSimplexResult result)
        {
            using StreamWriter writer = new StreamWriter(filePath, false);
            writer.WriteLine("========================================");
            writer.WriteLine("LPR381 OUTPUT - REVISED PRIMAL SIMPLEX");
            writer.WriteLine("========================================");
            writer.WriteLine();

            writer.WriteLine("CANONICAL FORM");
            writer.WriteLine("----------------------------------------");
            WriteCanonical(writer, canonical);

            writer.WriteLine();
            writer.WriteLine("REVISED SIMPLEX ITERATIONS");
            writer.WriteLine("----------------------------------------");

            foreach (RevisedSimplexIteration it in result.Iterations)
            {
                writer.WriteLine();
                writer.WriteLine($"Iteration {it.IterationNumber} - {it.Stage}" +
                    (it.IsFinal ? " (FINAL)" : ""));
                writer.WriteLine($"Basis: {string.Join(", ", it.BasisNames)}");
                WriteVector(writer, "C_B", it.BasicCosts);
                WriteMatrix(writer, "B", it.BasisMatrix);
                WriteMatrix(writer, "B^-1", it.BasisInverse);
                WriteVector(writer, "Prices (C_B B^-1)", it.Prices);
                WriteVector(writer, "Reduced Costs (C_j - y A_j)", it.ReducedCosts);

                if (it.EnteringColumn >= 0)
                {
                    writer.WriteLine($"Entering variable: {it.EnteringVariable}");
                    WriteVector(writer, "Direction (B^-1 A_j)", it.Direction);
                    WriteVector(writer, "Ratio Test", it.Ratios);
                    if (it.LeavingRow >= 0)
                        writer.WriteLine($"Leaving variable: {it.LeavingVariable} (row {it.LeavingRow + 1})");
                    writer.WriteLine($"Pivot value: {it.PivotValue:F3}");
                    writer.WriteLine("Product Form - Eta Matrix:");
                    WriteMatrix(writer, "Eta", it.EtaMatrix);
                }
                writer.WriteLine($"Objective value: {it.ObjectiveValue:F3}");
            }

            writer.WriteLine();
            writer.WriteLine("FINAL RESULT");
            writer.WriteLine("----------------------------------------");
            writer.WriteLine($"Status: {result.Status}");
            writer.WriteLine($"Message: {result.Message}");
            writer.WriteLine($"Pivot count: {result.PivotCount}");
            writer.WriteLine($"Objective value: {result.ObjectiveValue:F3}");
            for (int i = 0; i < result.VariableValues.Length; i++)
                writer.WriteLine($"{result.VariableNames[i]} = {result.VariableValues[i]:F3}");
        }

        private static void WriteCanonical(StreamWriter w, CanonicalModel c)
        {
            int rows = c.ConstraintMatrix.GetLength(0), cols = c.ConstraintMatrix.GetLength(1);
            w.WriteLine("Variables: " + string.Join(", ", c.VariableNames));
            for (int i = 0; i < rows; i++)
            {
                w.Write("C" + (i + 1) + ": ");
                for (int j = 0; j < cols; j++) w.Write($"{c.ConstraintMatrix[i, j]:F3} ");
                w.WriteLine($"<=/EQ RHS {c.RightHandSide[i]:F3}");
            }
            w.Write("Objective: ");
            for (int j = 0; j < cols; j++) w.Write($"{c.ObjectiveCoefficients[j]:F3} ");
            w.WriteLine();
        }

        private static void WriteVector(StreamWriter w, string label, double[] v)
        {
            w.Write(label + ": ");
            for (int i = 0; i < v.Length; i++) w.Write($"{v[i]:F3} ");
            w.WriteLine();
        }

        private static void WriteMatrix(StreamWriter w, string label, double[,] a)
        {
            w.WriteLine(label + ":");
            if (a == null) { w.WriteLine("(none)"); return; }
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++) w.Write($"{a[i, j]:F3} ");
                w.WriteLine();
            }
        }
    }
}
