using System;
using LPR381_Project.Algorithims;
using LPR381_Project.Models;

namespace LPR381_Project.Algorithms
{
    public class SimplexSolver
    {
        public static void Solve(CanonicalModel canonical)
        {
            int rows = canonical.ConstraintMatrix.GetLength(0) + 1; // Constraints + Z row
            int cols = canonical.ConstraintMatrix.GetLength(1) + 1; // Variables + RHS column

            double[,] tableau = new double[rows, cols];

            // 1. Initialize Tableau
            for (int i = 0; i < canonical.ConstraintMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < canonical.ConstraintMatrix.GetLength(1); j++)
                {
                    tableau[i, j] = canonical.ConstraintMatrix[i, j];
                }
                tableau[i, cols - 1] = canonical.RightHandSide[i];
            }

            int zRowIndex = rows - 1;
            for (int j = 0; j < canonical.ObjectiveCoefficients.Length; j++)
            {
                // Invert signs for the Z-row (standard maximization setup)
                tableau[zRowIndex, j] = -canonical.ObjectiveCoefficients[j];
            }

            Console.WriteLine("--- Starting Primal Simplex Iterations ---");

            // 2. The Core Simplex Loop
            int iteration = 1;
            while (true)
            {
                // Step A: Find the Entering Variable (Pivot Column)
                int pivotCol = GetPivotColumn(tableau, zRowIndex, cols);

                if (pivotCol == -1)
                {
                    Console.WriteLine("\n[OPTIMAL SOLUTION REACHED]");
                    break; // No negative values in Z-row? We are done!
                }

                // Step B: Find the Leaving Variable (Pivot Row) using Minimum Ratio Test
                int pivotRow = GetPivotRow(tableau, pivotCol, rows);

                if (pivotRow == -1)
                {
                    Console.WriteLine("\n[UNBOUNDED PROBLEM]: No valid leaving variable found.");
                    break;
                }

                Console.WriteLine($"Iteration {iteration}: Pivoting on Row {pivotRow}, Column {pivotCol}");

                // Step C: Perform Gaussian Elimination (Row Operations)
                PerformPivot(tableau, pivotRow, pivotCol, rows, cols);

                iteration++;
            }

            // Print the final Optimal Z value (located at the bottom right of the tableau)
            Console.WriteLine($"\nFinal Optimal Objective Value (Z): {Math.Round(tableau[zRowIndex, cols - 1], 3)}");
        }

        // --- HELPER METHODS FOR THE MATH ---

        private static int GetPivotColumn(double[,] tableau, int zRowIndex, int cols)
        {
            int bestCol = -1;
            double minValue = 0.0; // We look for the most negative number

            // Loop through all columns EXCEPT the RHS column
            for (int j = 0; j < cols - 1; j++)
            {
                if (tableau[zRowIndex, j] < minValue)
                {
                    minValue = tableau[zRowIndex, j];
                    bestCol = j;
                }
            }
            return bestCol;
        }

        private static int GetPivotRow(double[,] tableau, int pivotCol, int rows)
        {
            int bestRow = -1;
            double minRatio = double.MaxValue;
            int rhsColIndex = tableau.GetLength(1) - 1;

            // Loop through all constraint rows (ignore the Z-row at the bottom)
            for (int i = 0; i < rows - 1; i++)
            {
                double pivotElement = tableau[i, pivotCol];

                // Ratio test only applies to positive pivot elements
                if (pivotElement > 0)
                {
                    double ratio = tableau[i, rhsColIndex] / pivotElement;
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        bestRow = i;
                    }
                }
            }
            return bestRow;
        }

        private static void PerformPivot(double[,] tableau, int pivotRow, int pivotCol, int rows, int cols)
        {
            double pivotValue = tableau[pivotRow, pivotCol];

            // 1. Divide the entire pivot row by the pivot value so the pivot becomes 1
            for (int j = 0; j < cols; j++)
            {
                tableau[pivotRow, j] /= pivotValue;
            }

            // 2. Make all other values in the pivot column 0 (Gaussian Elimination)
            for (int i = 0; i < rows; i++)
            {
                if (i != pivotRow) // Skip the pivot row itself
                {
                    double factor = tableau[i, pivotCol];
                    for (int j = 0; j < cols; j++)
                    {
                        tableau[i, j] -= factor * tableau[pivotRow, j];
                    }
                }
            }
        }
    }
}